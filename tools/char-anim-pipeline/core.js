/**
 * Project model, IndexedDB, FK math.
 * Global: window.AnimCore
 */
(function (global) {
  const DB_NAME = "sts2-char-anim-editor";
  const DB_VER = 1;
  const LS_KEY = "sts2-anim-editor-v2";

  const DEFAULT_STATES = [
    { key: "idle", label: "Idle", clipName: "idle", loop: true, required: true },
    { key: "attack", label: "Attack", clipName: "attack", loop: false, required: true },
    { key: "cast", label: "Cast", clipName: "cast", loop: false, required: true },
    { key: "hit", label: "Hit", clipName: "hit", loop: false, required: true },
    { key: "dead", label: "Dead", clipName: "dead", loop: false, required: true },
    { key: "relaxed", label: "Relaxed", clipName: "relaxed", loop: true, required: false },
    { key: "revive", label: "Revive", clipName: "revive", loop: false, required: false },
  ];

  const DEFAULT_PARTS = [
    "torso", "pelvis", "head", "hair", "hat",
    "upper_arm_l", "forearm_l", "hand_l",
    "upper_arm_r", "forearm_r", "hand_r", "weapon",
    "thigh_l", "shin_l", "foot_l",
    "thigh_r", "shin_r", "foot_r",
    "cape_or_skirt", "fx_slot",
  ];

  /**
   * Local-angle FK: angle is relative to parent direction (0 = continue forward).
   * Root angle is world space. -PI/2 is up on canvas.
   */
  function defaultBones() {
    const bones = [
      { id: "root", name: "root", parent: null, length: 16, angle: -Math.PI / 2, setupX: 360, setupY: 620 },
      { id: "hip", name: "hip", parent: "root", length: 36, angle: 0, setupX: 0, setupY: 0 },
      { id: "torso", name: "torso", parent: "hip", length: 88, angle: 0, setupX: 0, setupY: 0 },
      { id: "chest", name: "chest", parent: "torso", length: 44, angle: 0, setupX: 0, setupY: 0 },
      { id: "neck", name: "neck", parent: "chest", length: 22, angle: 0, setupX: 0, setupY: 0 },
      { id: "head", name: "head", parent: "neck", length: 48, angle: 0, setupX: 0, setupY: 0 },
      { id: "shoulder_l", name: "shoulder_l", parent: "chest", length: 30, angle: Math.PI / 2 + 0.12, setupX: 0, setupY: 0 },
      { id: "upper_arm_l", name: "upper_arm_l", parent: "shoulder_l", length: 58, angle: 0.3, setupX: 0, setupY: 0 },
      { id: "forearm_l", name: "forearm_l", parent: "upper_arm_l", length: 52, angle: 0.2, setupX: 0, setupY: 0 },
      { id: "hand_l", name: "hand_l", parent: "forearm_l", length: 22, angle: 0.1, setupX: 0, setupY: 0 },
      { id: "shoulder_r", name: "shoulder_r", parent: "chest", length: 30, angle: -Math.PI / 2 - 0.12, setupX: 0, setupY: 0 },
      { id: "upper_arm_r", name: "upper_arm_r", parent: "shoulder_r", length: 58, angle: -0.3, setupX: 0, setupY: 0 },
      { id: "forearm_r", name: "forearm_r", parent: "upper_arm_r", length: 52, angle: -0.2, setupX: 0, setupY: 0 },
      { id: "hand_r", name: "hand_r", parent: "forearm_r", length: 22, angle: -0.1, setupX: 0, setupY: 0 },
      { id: "weapon", name: "weapon", parent: "hand_r", length: 110, angle: -0.85, setupX: 0, setupY: 0 },
      { id: "thigh_l", name: "thigh_l", parent: "hip", length: 72, angle: Math.PI - 0.32, setupX: 0, setupY: 0 },
      { id: "shin_l", name: "shin_l", parent: "thigh_l", length: 70, angle: 0.12, setupX: 0, setupY: 0 },
      { id: "foot_l", name: "foot_l", parent: "shin_l", length: 30, angle: Math.PI / 2 - 0.15, setupX: 0, setupY: 0 },
      { id: "thigh_r", name: "thigh_r", parent: "hip", length: 72, angle: Math.PI + 0.32, setupX: 0, setupY: 0 },
      { id: "shin_r", name: "shin_r", parent: "thigh_r", length: 70, angle: -0.12, setupX: 0, setupY: 0 },
      { id: "foot_r", name: "foot_r", parent: "shin_r", length: 30, angle: -Math.PI / 2 + 0.15, setupX: 0, setupY: 0 },
    ];
    bakeSetupFromAngles(bones);
    return bones;
  }

  function makeStateBlock(def) {
    return {
      clipName: def.clipName,
      loop: def.loop,
      notes: "",
      frameIds: [],
      keys: [],
      lengthMs: def.key === "idle" ? 1200 : 600,
    };
  }

  function makeCharacter(name, id) {
    const states = {};
    for (const s of DEFAULT_STATES) states[s.key] = makeStateBlock(s);
    return {
      id: id.toLowerCase().replace(/[^a-z0-9_]/g, "_"),
      name,
      notes: "",
      bones: defaultBones(),
      attachments: {},
      parts: DEFAULT_PARTS.map((n) => ({ name: n, done: false })),
      states,
      refImageId: null,
      createdAt: Date.now(),
      updatedAt: Date.now(),
    };
  }

  function openDb() {
    return new Promise((resolve, reject) => {
      const req = indexedDB.open(DB_NAME, DB_VER);
      req.onupgradeneeded = () => {
        const db = req.result;
        if (!db.objectStoreNames.contains("blobs")) db.createObjectStore("blobs");
      };
      req.onsuccess = () => resolve(req.result);
      req.onerror = () => reject(req.error);
    });
  }

  async function idbPut(key, value) {
    const db = await openDb();
    return new Promise((res, rej) => {
      const tx = db.transaction("blobs", "readwrite");
      tx.objectStore("blobs").put(value, key);
      tx.oncomplete = () => res();
      tx.onerror = () => rej(tx.error);
    });
  }

  async function idbGet(key) {
    const db = await openDb();
    return new Promise((res, rej) => {
      const r = db.transaction("blobs", "readonly").objectStore("blobs").get(key);
      r.onsuccess = () => res(r.result);
      r.onerror = () => rej(r.error);
    });
  }

  async function idbDel(key) {
    const db = await openDb();
    return new Promise((res, rej) => {
      const tx = db.transaction("blobs", "readwrite");
      tx.objectStore("blobs").delete(key);
      tx.oncomplete = () => res();
      tx.onerror = () => rej(tx.error);
    });
  }

  function uid() {
    return `${Date.now().toString(36)}_${Math.random().toString(36).slice(2, 8)}`;
  }

  function blobKey(...parts) {
    return parts.join("::");
  }

  function loadMeta() {
    try {
      const raw = localStorage.getItem(LS_KEY);
      if (!raw) return { characters: [], activeCharId: null, activeState: "idle" };
      return JSON.parse(raw);
    } catch {
      return { characters: [], activeCharId: null, activeState: "idle" };
    }
  }

  function saveMeta(meta) {
    localStorage.setItem(LS_KEY, JSON.stringify(meta));
  }

  function computeWorld(bones, pose = {}) {
    const byId = Object.fromEntries(bones.map((b) => [b.id, b]));
    const world = {};

    function worldOf(id, stack = new Set()) {
      if (world[id]) return world[id];
      if (stack.has(id)) throw new Error("Bone cycle: " + id);
      stack.add(id);
      const b = byId[id];
      if (!b) return null;
      const ov = pose[id] || {};
      const localAngle = ov.angle != null ? ov.angle : b.angle;
      const length = ov.length != null ? ov.length : b.length;

      let x, y, parentWorldAngle;
      if (!b.parent) {
        x = b.setupX;
        y = b.setupY;
        parentWorldAngle = 0;
      } else {
        const p = worldOf(b.parent, stack);
        x = p.tipX;
        y = p.tipY;
        parentWorldAngle = p.worldAngle;
      }
      const worldAngle = parentWorldAngle + localAngle;
      world[id] = {
        x,
        y,
        tipX: x + Math.cos(worldAngle) * length,
        tipY: y + Math.sin(worldAngle) * length,
        worldAngle,
        length,
        localAngle,
      };
      return world[id];
    }

    for (const b of bones) worldOf(b.id);
    return world;
  }

  function bakeSetupFromAngles(bones) {
    const w = computeWorld(bones, {});
    for (const b of bones) {
      const t = w[b.id];
      if (!t) continue;
      b.setupX = t.x;
      b.setupY = t.y;
    }
  }

  function poseAt(keys, tMs) {
    if (!keys || !keys.length) return {};
    const sorted = [...keys].sort((a, b) => a.t - b.t);
    if (tMs <= sorted[0].t) return clonePose(sorted[0].bones);
    if (tMs >= sorted[sorted.length - 1].t) return clonePose(sorted[sorted.length - 1].bones);
    let i = 0;
    while (i < sorted.length - 1 && sorted[i + 1].t < tMs) i++;
    const a = sorted[i];
    const b = sorted[i + 1];
    const u = (tMs - a.t) / Math.max(1, b.t - a.t);
    const ids = new Set([...Object.keys(a.bones || {}), ...Object.keys(b.bones || {})]);
    const out = {};
    for (const id of ids) {
      const pa = a.bones[id] || {};
      const pb = b.bones[id] || {};
      out[id] = {
        angle: lerpAngle(pa.angle ?? 0, pb.angle ?? 0, u),
        length: lerp(pa.length ?? 40, pb.length ?? 40, u),
      };
    }
    return out;
  }

  function clonePose(p) {
    const o = {};
    for (const [k, v] of Object.entries(p || {})) o[k] = { ...v };
    return o;
  }

  function lerp(a, b, t) {
    return a + (b - a) * t;
  }

  function lerpAngle(a, b, t) {
    let d = b - a;
    while (d > Math.PI) d -= Math.PI * 2;
    while (d < -Math.PI) d += Math.PI * 2;
    return a + d * t;
  }

  function capturePose(bones) {
    const pose = {};
    for (const b of bones) pose[b.id] = { angle: b.angle, length: b.length };
    return pose;
  }

  function applyPoseToBones(bones, pose) {
    for (const b of bones) {
      const p = pose[b.id];
      if (!p) continue;
      if (p.angle != null) b.angle = p.angle;
      if (p.length != null) b.length = p.length;
    }
    bakeSetupFromAngles(bones);
  }

  function exportSkeletonJson(char) {
    return {
      format: "sts2-pipeline-skeleton",
      version: 1,
      character: char.id,
      name: char.name,
      bones: char.bones.map((b) => ({
        id: b.id,
        name: b.name,
        parent: b.parent,
        length: b.length,
        angle: b.angle,
        setupX: b.setupX,
        setupY: b.setupY,
      })),
      attachments: Object.fromEntries(
        Object.entries(char.attachments || {}).map(([k, v]) => [
          k,
          {
            offsetX: v.offsetX,
            offsetY: v.offsetY,
            scale: v.scale,
            rotation: v.rotation,
            hasImage: !!v.imageId,
          },
        ])
      ),
      animations: Object.fromEntries(
        Object.entries(char.states).map(([key, st]) => [
          key,
          {
            clipName: st.clipName,
            loop: st.loop,
            lengthMs: st.lengthMs,
            keys: st.keys || [],
            frameCount: (st.frameIds || []).length,
          },
        ])
      ),
    };
  }

  global.AnimCore = {
    DEFAULT_STATES,
    DEFAULT_PARTS,
    defaultBones,
    makeCharacter,
    idbPut,
    idbGet,
    idbDel,
    uid,
    blobKey,
    loadMeta,
    saveMeta,
    computeWorld,
    bakeSetupFromAngles,
    poseAt,
    capturePose,
    applyPoseToBones,
    exportSkeletonJson,
    LS_KEY,
  };
})(window);
