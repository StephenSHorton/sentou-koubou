/**
 * STS2 Character Animation Pipeline — local preview & production tracker.
 * Data lives in IndexedDB (frames) + localStorage (project metadata).
 */

const DB_NAME = "sts2-char-anim-pipeline";
const DB_VER = 1;
const LS_KEY = "sts2-anim-pipeline-v1";

/** BaseLib SetupAnimationState-aligned combat states */
const DEFAULT_STATES = [
  { key: "idle", label: "Idle", clipName: "idle", loop: true, required: true, fps: 12 },
  { key: "attack", label: "Attack", clipName: "attack", loop: false, required: true, fps: 14 },
  { key: "cast", label: "Cast", clipName: "cast", loop: false, required: true, fps: 12 },
  { key: "hit", label: "Hit", clipName: "hit", loop: false, required: true, fps: 16 },
  { key: "dead", label: "Dead", clipName: "dead", loop: false, required: true, fps: 10 },
  { key: "relaxed", label: "Relaxed", clipName: "relaxed", loop: true, required: false, fps: 10 },
  { key: "revive", label: "Revive", clipName: "revive", loop: false, required: false, fps: 12 },
];

const DEFAULT_PARTS = [
  "root_anchor",
  "torso",
  "pelvis",
  "head",
  "hair_front",
  "hair_back",
  "hat",
  "upper_arm_l",
  "forearm_l",
  "hand_l",
  "upper_arm_r",
  "forearm_r",
  "hand_r",
  "weapon",
  "thigh_l",
  "shin_l",
  "foot_l",
  "thigh_r",
  "shin_r",
  "foot_r",
  "cape_or_skirt",
  "fx_slot",
];

const DEFAULT_BONES = [
  { name: "root", parent: null },
  { name: "hip", parent: "root" },
  { name: "torso", parent: "hip" },
  { name: "chest", parent: "torso" },
  { name: "neck", parent: "chest" },
  { name: "head", parent: "neck" },
  { name: "hat", parent: "head" },
  { name: "shoulder_l", parent: "chest" },
  { name: "upper_arm_l", parent: "shoulder_l" },
  { name: "forearm_l", parent: "upper_arm_l" },
  { name: "hand_l", parent: "forearm_l" },
  { name: "shoulder_r", parent: "chest" },
  { name: "upper_arm_r", parent: "shoulder_r" },
  { name: "forearm_r", parent: "upper_arm_r" },
  { name: "hand_r", parent: "forearm_r" },
  { name: "weapon", parent: "hand_r" },
  { name: "thigh_l", parent: "hip" },
  { name: "shin_l", parent: "thigh_l" },
  { name: "foot_l", parent: "shin_l" },
  { name: "thigh_r", parent: "hip" },
  { name: "shin_r", parent: "thigh_r" },
  { name: "foot_r", parent: "shin_r" },
];

// ─── IndexedDB helpers ───────────────────────────────────────────────────────

function openDb() {
  return new Promise((resolve, reject) => {
    const req = indexedDB.open(DB_NAME, DB_VER);
    req.onupgradeneeded = () => {
      const db = req.result;
      if (!db.objectStoreNames.contains("blobs")) {
        db.createObjectStore("blobs");
      }
    };
    req.onsuccess = () => resolve(req.result);
    req.onerror = () => reject(req.error);
  });
}

async function idbPut(key, value) {
  const db = await openDb();
  return new Promise((resolve, reject) => {
    const tx = db.transaction("blobs", "readwrite");
    tx.objectStore("blobs").put(value, key);
    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error);
  });
}

async function idbGet(key) {
  const db = await openDb();
  return new Promise((resolve, reject) => {
    const tx = db.transaction("blobs", "readonly");
    const req = tx.objectStore("blobs").get(key);
    req.onsuccess = () => resolve(req.result);
    req.onerror = () => reject(req.error);
  });
}

async function idbDel(key) {
  const db = await openDb();
  return new Promise((resolve, reject) => {
    const tx = db.transaction("blobs", "readwrite");
    tx.objectStore("blobs").delete(key);
    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error);
  });
}

function blobKey(charId, kind, ...rest) {
  return [charId, kind, ...rest].join("::");
}

// ─── Project model ───────────────────────────────────────────────────────────

function makeStateDefaults() {
  const out = {};
  for (const s of DEFAULT_STATES) {
    out[s.key] = {
      clipName: s.clipName,
      loop: s.loop,
      fps: s.fps,
      duration: null,
      notes: "",
      frameIds: [], // ids into IndexedDB
    };
  }
  return out;
}

function makeParts() {
  return DEFAULT_PARTS.map((name) => ({ name, done: false, artId: null }));
}

function makeCharacter(name, id) {
  return {
    id: id.toLowerCase().replace(/[^a-z0-9_]/g, "_"),
    name,
    notes: "",
    states: makeStateDefaults(),
    parts: makeParts(),
    bones: DEFAULT_BONES.map((b) => ({ ...b })),
    boneNotes: "IK: arm_r chain for attacks.\nMesh: cape/skirt, hair, cloth.\nWeapon: separate bone under hand_r.",
    refImageId: null,
    createdAt: Date.now(),
    updatedAt: Date.now(),
  };
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

// ─── App state ───────────────────────────────────────────────────────────────

const meta = loadMeta();
const imageCache = new Map(); // id -> HTMLImageElement | null loading

let playing = false;
let playRaf = 0;
let playAcc = 0;
let lastTs = 0;
let frameIndex = 0;

// ─── DOM ─────────────────────────────────────────────────────────────────────

const $ = (sel) => document.querySelector(sel);
const canvas = $("#stage");
const ctx = canvas.getContext("2d");

function activeChar() {
  return meta.characters.find((c) => c.id === meta.activeCharId) || null;
}

function activeStateData(char = activeChar()) {
  if (!char) return null;
  return char.states[meta.activeState] || null;
}

function stateDef(key) {
  return DEFAULT_STATES.find((s) => s.key === key);
}

// ─── Image loading ───────────────────────────────────────────────────────────

async function loadImageFromId(id) {
  if (!id) return null;
  if (imageCache.has(id)) return imageCache.get(id);
  const blob = await idbGet(id);
  if (!blob) return null;
  const url = URL.createObjectURL(blob);
  const img = await new Promise((resolve, reject) => {
    const i = new Image();
    i.onload = () => resolve(i);
    i.onerror = reject;
    i.src = url;
  });
  imageCache.set(id, img);
  return img;
}

async function storeBlob(id, blob) {
  await idbPut(id, blob);
  imageCache.delete(id);
}

function uid() {
  return `${Date.now().toString(36)}_${Math.random().toString(36).slice(2, 9)}`;
}

// ─── Render UI lists ─────────────────────────────────────────────────────────

function renderCharList() {
  const ul = $("#char-list");
  ul.innerHTML = "";
  if (!meta.characters.length) {
    ul.innerHTML = `<li class="empty-state" style="cursor:default">No characters yet</li>`;
    return;
  }
  for (const c of meta.characters) {
    const li = document.createElement("li");
    li.className = c.id === meta.activeCharId ? "active" : "";
    li.innerHTML = `<span>${escapeHtml(c.name)}</span><span class="char-id">${escapeHtml(c.id)}</span>`;
    li.onclick = () => {
      meta.activeCharId = c.id;
      frameIndex = 0;
      saveMeta(meta);
      refreshAll();
    };
    ul.appendChild(li);
  }
}

function renderStateList() {
  const ul = $("#state-list");
  ul.innerHTML = "";
  const char = activeChar();
  for (const s of DEFAULT_STATES) {
    const data = char?.states[s.key];
    const count = data?.frameIds?.length || 0;
    const li = document.createElement("li");
    li.className = [
      s.key === meta.activeState ? "active" : "",
      count ? "has-frames" : "",
      s.required ? "required" : "",
    ].filter(Boolean).join(" ");
    li.innerHTML = `<span class="dot"></span><span class="name">${s.label}</span><span class="badge">${count}</span>`;
    li.onclick = () => {
      meta.activeState = s.key;
      frameIndex = 0;
      playing = false;
      updatePlayBtn();
      saveMeta(meta);
      refreshStateUi();
      draw();
    };
    ul.appendChild(li);
  }
}

function renderFrameStrip() {
  const strip = $("#frame-strip");
  strip.innerHTML = "";
  const st = activeStateData();
  if (!st || !st.frameIds.length) {
    strip.innerHTML = `<div class="empty-state">No frames — add PNGs for this state</div>`;
    return;
  }
  st.frameIds.forEach((id, i) => {
    const img = document.createElement("img");
    img.className = "frame-thumb" + (i === frameIndex ? " active" : "");
    img.alt = `Frame ${i + 1}`;
    loadImageFromId(id).then((im) => {
      if (im) img.src = im.src;
    });
    img.onclick = () => {
      frameIndex = i;
      playing = false;
      updatePlayBtn();
      refreshFrameChrome();
      draw();
    };
    strip.appendChild(img);
  });
}

function renderParts() {
  const char = activeChar();
  const ul = $("#part-list");
  const slots = $("#part-slots");
  ul.innerHTML = "";
  slots.innerHTML = "";
  if (!char) return;

  for (const part of char.parts) {
    const li = document.createElement("li");
    if (part.done) li.classList.add("done");
    const cb = document.createElement("input");
    cb.type = "checkbox";
    cb.checked = !!part.done;
    cb.onchange = () => {
      part.done = cb.checked;
      char.updatedAt = Date.now();
      saveMeta(meta);
      renderParts();
      renderExportGuide();
    };
    const span = document.createElement("span");
    span.textContent = part.name;
    const del = document.createElement("button");
    del.type = "button";
    del.className = "btn btn-sm btn-ghost";
    del.textContent = "×";
    del.style.marginLeft = "auto";
    del.onclick = () => {
      char.parts = char.parts.filter((p) => p !== part);
      saveMeta(meta);
      renderParts();
    };
    li.append(cb, span, del);
    ul.appendChild(li);

    const slot = document.createElement("div");
    slot.className = "part-slot" + (part.artId ? " has-art" : "");
    const label = document.createElement("label");
    const preview = document.createElement("div");
    preview.style.minHeight = "72px";
    if (part.artId) {
      const img = document.createElement("img");
      loadImageFromId(part.artId).then((im) => {
        if (im) img.src = im.src;
      });
      preview.appendChild(img);
    } else {
      preview.textContent = "—";
      preview.style.display = "grid";
      preview.style.placeItems = "center";
      preview.style.color = "var(--muted)";
      preview.style.fontSize = "0.75rem";
    }
    const file = document.createElement("input");
    file.type = "file";
    file.accept = "image/*";
    file.hidden = true;
    file.onchange = async () => {
      const f = file.files?.[0];
      if (!f) return;
      const id = blobKey(char.id, "part", part.name, uid());
      await storeBlob(id, f);
      part.artId = id;
      part.done = true;
      char.updatedAt = Date.now();
      saveMeta(meta);
      renderParts();
    };
    label.append(preview, document.createTextNode(part.name), file);
    label.onclick = (e) => {
      if (e.target !== file) file.click();
    };
    slot.appendChild(label);
    slots.appendChild(slot);
  }
}

function boneDepth(bones, name, seen = new Set()) {
  if (!name || seen.has(name)) return 0;
  seen.add(name);
  const b = bones.find((x) => x.name === name);
  if (!b || !b.parent) return 0;
  return 1 + boneDepth(bones, b.parent, seen);
}

function renderBones() {
  const char = activeChar();
  const ul = $("#bone-tree");
  ul.innerHTML = "";
  if (!char) return;
  $("#bone-notes").value = char.boneNotes || "";

  const sorted = [...char.bones].sort((a, b) => {
    const da = boneDepth(char.bones, a.name);
    const db = boneDepth(char.bones, b.name);
    if (da !== db) return da - db;
    return a.name.localeCompare(b.name);
  });

  for (const b of sorted) {
    const depth = boneDepth(char.bones, b.name);
    const li = document.createElement("li");
    const indent = "· ".repeat(depth);
    li.innerHTML = `<span class="indent">${indent}</span><span>${escapeHtml(b.name)}</span>` +
      (b.parent ? `<span class="parent-tag">← ${escapeHtml(b.parent)}</span>` : "");
    ul.appendChild(li);
  }
}

function renderExportGuide() {
  const char = activeChar();
  const steps = $("#pipeline-steps");
  const codegen = $("#codegen");
  const folder = $("#folder-layout");
  if (!char) {
    steps.innerHTML = "<li>Create a character first</li>";
    codegen.textContent = "// no character";
    folder.textContent = "";
    return;
  }

  const checks = [
    ["Character id set", !!char.id],
    ["Idle has frames", (char.states.idle?.frameIds?.length || 0) > 0],
    ["Attack has frames", (char.states.attack?.frameIds?.length || 0) > 0],
    ["Hit has frames", (char.states.hit?.frameIds?.length || 0) > 0],
    ["Dead has frames", (char.states.dead?.frameIds?.length || 0) > 0],
    ["Cast has frames (or will alias idle)", (char.states.cast?.frameIds?.length || 0) > 0],
    ["Core parts ticked (≥8)", char.parts.filter((p) => p.done).length >= 8],
    ["Bone tree reviewed", (char.bones?.length || 0) >= 10],
  ];

  steps.innerHTML = checks
    .map(([label, ok]) => `<li class="${ok ? "ok" : "todo"}">${ok ? "✓" : "○"} ${escapeHtml(label)}</li>`)
    .join("");

  const className = char.id.charAt(0).toUpperCase() + char.id.slice(1);
  const mapLine = (key, prop, loopProp) => {
    const st = char.states[key];
    if (!st) return "";
    const clip = st.clipName || key;
    if (loopProp) return `            ${prop}: "${clip}", ${loopProp}: ${st.loop},`;
    return `            ${prop}: "${clip}",`;
  };

  codegen.textContent = `// ${className}.cs — combat visuals (BaseLib)
// Scene default: res://scenes/creature_visuals/${char.id}.tscn
// Or override CreateCustomVisuals / CustomVisualPath.

public override string CustomVisualPath =>
    "res://scenes/creature_visuals/${char.id}.tscn";

protected override CreatureAnimator SetupCustomAnimationStates(MegaSprite controller)
{
    return SetupAnimationState(
        controller,
${mapLine("idle", "idleName")}
${mapLine("dead", "deadName", "deadLoop")}
${mapLine("hit", "hitName", "hitLoop")}
${mapLine("attack", "attackName", "attackLoop")}
${mapLine("cast", "castName", "castLoop")}
${mapLine("relaxed", "relaxedName", "relaxedLoop")}
    );
}

// Clip inventory from this pipeline:
${DEFAULT_STATES.map((s) => {
    const st = char.states[s.key];
    const n = st?.frameIds?.length || 0;
    return `//  ${s.key.padEnd(8)} clip="${st?.clipName || s.clipName}" loop=${st?.loop} frames=${n}`;
  }).join("\n")}
`;

  folder.textContent = `mods/${char.id}/
  ${className}/
    images/
      creature/
        ${char.id}_atlas.png
        parts/                 # cut pieces for Spine
        frames/                # optional AnimatedSprite fallback
          idle/
          attack/
          cast/
          hit/
          dead/
    scenes/
      creature_visuals/
        ${char.id}.tscn
  spine/                       # source Spine project (not packed)
    ${char.id}.spine
    export/
      ${char.id}.skel
      ${char.id}.atlas
      ${char.id}.png
`;
}

function refreshStateUi() {
  const char = activeChar();
  const def = stateDef(meta.activeState);
  const st = activeStateData();
  $("#stage-state-label").textContent = def?.label || meta.activeState;
  $("#insp-state-name").textContent = def?.label || meta.activeState;

  if (!char || !st) {
    $("#insp-clip-name").value = "";
    $("#insp-loop").checked = false;
    $("#insp-fps").value = "";
    $("#insp-duration").value = "";
    $("#insp-notes").value = "";
    renderFrameStrip();
    refreshFrameChrome();
    return;
  }

  $("#char-name").value = char.name;
  $("#char-id").value = char.id;
  $("#char-notes").value = char.notes || "";

  $("#insp-clip-name").value = st.clipName || def?.clipName || "";
  $("#insp-loop").checked = !!st.loop;
  $("#insp-fps").value = st.fps ?? "";
  $("#insp-duration").value = st.duration ?? "";
  $("#insp-notes").value = st.notes || "";
  $("#input-loop").checked = !!st.loop;
  if (st.fps) $("#input-fps").value = st.fps;

  renderStateList();
  renderFrameStrip();
  refreshFrameChrome();
}

function refreshFrameChrome() {
  const st = activeStateData();
  const n = st?.frameIds?.length || 0;
  if (frameIndex >= n) frameIndex = Math.max(0, n - 1);
  $("#stage-frame-label").textContent = n ? `${frameIndex + 1} / ${n}` : "0 / 0";
  const scrub = $("#input-scrub");
  scrub.max = String(Math.max(0, n - 1));
  scrub.value = String(frameIndex);
  // update thumb active
  document.querySelectorAll(".frame-thumb").forEach((el, i) => {
    el.classList.toggle("active", i === frameIndex);
  });
}

function refreshAll() {
  renderCharList();
  refreshStateUi();
  renderParts();
  renderBones();
  renderExportGuide();
  draw();
}

function escapeHtml(s) {
  return String(s)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

// ─── Canvas draw ─────────────────────────────────────────────────────────────

async function draw() {
  const w = canvas.width;
  const h = canvas.height;
  ctx.clearRect(0, 0, w, h);

  // vignette floor line
  ctx.strokeStyle = "rgba(232,93,76,0.25)";
  ctx.beginPath();
  ctx.moveTo(w * 0.15, h * 0.88);
  ctx.lineTo(w * 0.85, h * 0.88);
  ctx.stroke();

  const char = activeChar();
  if (!char) {
    ctx.fillStyle = "#8b93a7";
    ctx.font = "16px Segoe UI";
    ctx.textAlign = "center";
    ctx.fillText("Create or select a character", w / 2, h / 2);
    return;
  }

  // reference ghost
  if (char.refImageId) {
    const ref = await loadImageFromId(char.refImageId);
    if (ref) {
      ctx.globalAlpha = 0.22;
      drawContained(ref, w, h, 0.9);
      ctx.globalAlpha = 1;
    }
  }

  const st = activeStateData();
  const ids = st?.frameIds || [];
  if (!ids.length) {
    ctx.fillStyle = "#8b93a7";
    ctx.font = "15px Segoe UI";
    ctx.textAlign = "center";
    ctx.fillText(`No ${meta.activeState} frames yet`, w / 2, h / 2);
    ctx.font = "13px Segoe UI";
    ctx.fillText("Add PNGs in the inspector →", w / 2, h / 2 + 24);
    return;
  }

  if ($("#input-onion").checked && frameIndex > 0) {
    const prev = await loadImageFromId(ids[frameIndex - 1]);
    if (prev) {
      ctx.globalAlpha = 0.28;
      drawContained(prev, w, h, 0.92);
      ctx.globalAlpha = 1;
    }
  }

  const img = await loadImageFromId(ids[frameIndex]);
  if (img) drawContained(img, w, h, 0.92);
}

function drawContained(img, cw, ch, scale = 1) {
  const maxW = cw * scale;
  const maxH = ch * scale;
  const r = Math.min(maxW / img.width, maxH / img.height);
  const dw = img.width * r;
  const dh = img.height * r;
  const x = (cw - dw) / 2;
  const y = (ch - dh) / 2 + ch * 0.02;
  ctx.drawImage(img, x, y, dw, dh);
}

// ─── Playback ────────────────────────────────────────────────────────────────

function updatePlayBtn() {
  $("#btn-play").textContent = playing ? "❚❚" : "▶";
}

function tick(ts) {
  if (!playing) return;
  if (!lastTs) lastTs = ts;
  const dt = (ts - lastTs) / 1000;
  lastTs = ts;

  const st = activeStateData();
  const n = st?.frameIds?.length || 0;
  if (!n) {
    playing = false;
    updatePlayBtn();
    return;
  }

  const fps = Number($("#input-fps").value) || st?.fps || 12;
  playAcc += dt;
  const spf = 1 / fps;
  while (playAcc >= spf) {
    playAcc -= spf;
    frameIndex += 1;
    if (frameIndex >= n) {
      if ($("#input-loop").checked || st?.loop) {
        frameIndex = 0;
      } else {
        frameIndex = n - 1;
        playing = false;
        updatePlayBtn();
      }
    }
    refreshFrameChrome();
    draw();
  }
  playRaf = requestAnimationFrame(tick);
}

function startPlay() {
  if (playing) return;
  playing = true;
  lastTs = 0;
  playAcc = 0;
  updatePlayBtn();
  playRaf = requestAnimationFrame(tick);
}

function stopPlay() {
  playing = false;
  cancelAnimationFrame(playRaf);
  updatePlayBtn();
}

// ─── Events ──────────────────────────────────────────────────────────────────

function bindEvents() {
  $("#btn-add-char").onclick = () => {
    $("#dlg-new-char").showModal();
    $("#form-new-char").elements.name.focus();
  };
  $("#btn-cancel-char").onclick = () => $("#dlg-new-char").close();
  $("#form-new-char").onsubmit = (e) => {
    e.preventDefault();
    const fd = new FormData(e.target);
    const name = String(fd.get("name") || "").trim();
    const id = String(fd.get("id") || "").trim().toLowerCase();
    if (!name || !id) return;
    if (meta.characters.some((c) => c.id === id)) {
      alert("Character id already exists");
      return;
    }
    const ch = makeCharacter(name, id);
    meta.characters.push(ch);
    meta.activeCharId = ch.id;
    meta.activeState = "idle";
    saveMeta(meta);
    $("#dlg-new-char").close();
    e.target.reset();
    refreshAll();
  };

  $("#char-name").onchange = $("#char-name").oninput = () => {
    const c = activeChar();
    if (!c) return;
    c.name = $("#char-name").value;
    c.updatedAt = Date.now();
    saveMeta(meta);
    renderCharList();
  };
  $("#char-id").onchange = () => {
    const c = activeChar();
    if (!c) return;
    const next = $("#char-id").value.toLowerCase().replace(/[^a-z0-9_]/g, "_");
    if (!next || meta.characters.some((x) => x.id === next && x !== c)) {
      $("#char-id").value = c.id;
      return;
    }
    c.id = next;
    meta.activeCharId = next;
    c.updatedAt = Date.now();
    saveMeta(meta);
    refreshAll();
  };
  $("#char-notes").oninput = () => {
    const c = activeChar();
    if (!c) return;
    c.notes = $("#char-notes").value;
    saveMeta(meta);
  };

  document.querySelectorAll(".tab").forEach((tab) => {
    tab.onclick = () => {
      document.querySelectorAll(".tab").forEach((t) => t.classList.remove("active"));
      document.querySelectorAll(".tab-panel").forEach((p) => p.classList.remove("active"));
      tab.classList.add("active");
      $(`#tab-${tab.dataset.tab}`).classList.add("active");
      if (tab.dataset.tab === "export") renderExportGuide();
    };
  });

  const bindInsp = (el, fn) => {
    el.oninput = el.onchange = () => {
      const c = activeChar();
      const st = activeStateData();
      if (!c || !st) return;
      fn(st, c);
      c.updatedAt = Date.now();
      saveMeta(meta);
      renderStateList();
      renderExportGuide();
    };
  };
  bindInsp($("#insp-clip-name"), (st) => {
    st.clipName = $("#insp-clip-name").value.trim();
  });
  bindInsp($("#insp-loop"), (st) => {
    st.loop = $("#insp-loop").checked;
    $("#input-loop").checked = st.loop;
  });
  bindInsp($("#insp-fps"), (st) => {
    const v = $("#insp-fps").value;
    st.fps = v === "" ? null : Number(v);
    if (st.fps) $("#input-fps").value = st.fps;
  });
  bindInsp($("#insp-duration"), (st) => {
    const v = $("#insp-duration").value;
    st.duration = v === "" ? null : Number(v);
  });
  bindInsp($("#insp-notes"), (st) => {
    st.notes = $("#insp-notes").value;
  });

  $("#input-frames").onchange = async () => {
    const c = activeChar();
    const st = activeStateData();
    if (!c || !st) return;
    const files = [...($("#input-frames").files || [])];
    for (const f of files) {
      const id = blobKey(c.id, "frame", meta.activeState, uid());
      await storeBlob(id, f);
      st.frameIds.push(id);
    }
    $("#input-frames").value = "";
    c.updatedAt = Date.now();
    saveMeta(meta);
    refreshStateUi();
    draw();
    renderExportGuide();
  };

  $("#btn-clear-frames").onclick = async () => {
    const c = activeChar();
    const st = activeStateData();
    if (!c || !st || !st.frameIds.length) return;
    if (!confirm(`Clear all ${meta.activeState} frames?`)) return;
    for (const id of st.frameIds) await idbDel(id);
    st.frameIds = [];
    frameIndex = 0;
    saveMeta(meta);
    refreshStateUi();
    draw();
  };

  $("#btn-del-frame").onclick = async () => {
    const c = activeChar();
    const st = activeStateData();
    if (!c || !st?.frameIds?.length) return;
    const [id] = st.frameIds.splice(frameIndex, 1);
    await idbDel(id);
    frameIndex = Math.min(frameIndex, Math.max(0, st.frameIds.length - 1));
    saveMeta(meta);
    refreshStateUi();
    draw();
  };

  $("#btn-dup-frame").onclick = async () => {
    const c = activeChar();
    const st = activeStateData();
    if (!c || !st?.frameIds?.length) return;
    const srcId = st.frameIds[frameIndex];
    const blob = await idbGet(srcId);
    if (!blob) return;
    const id = blobKey(c.id, "frame", meta.activeState, uid());
    await storeBlob(id, blob);
    st.frameIds.splice(frameIndex + 1, 0, id);
    frameIndex += 1;
    saveMeta(meta);
    refreshStateUi();
    draw();
  };

  $("#input-ref").onchange = async () => {
    const c = activeChar();
    const f = $("#input-ref").files?.[0];
    if (!c || !f) return;
    const id = blobKey(c.id, "ref", uid());
    await storeBlob(id, f);
    if (c.refImageId) await idbDel(c.refImageId);
    c.refImageId = id;
    saveMeta(meta);
    draw();
  };
  $("#btn-clear-ref").onclick = async () => {
    const c = activeChar();
    if (!c?.refImageId) return;
    await idbDel(c.refImageId);
    c.refImageId = null;
    saveMeta(meta);
    draw();
  };

  $("#btn-play").onclick = () => (playing ? stopPlay() : startPlay());
  $("#btn-prev").onclick = () => {
    stopPlay();
    const n = activeStateData()?.frameIds?.length || 0;
    if (!n) return;
    frameIndex = (frameIndex - 1 + n) % n;
    refreshFrameChrome();
    draw();
  };
  $("#btn-next").onclick = () => {
    stopPlay();
    const n = activeStateData()?.frameIds?.length || 0;
    if (!n) return;
    frameIndex = (frameIndex + 1) % n;
    refreshFrameChrome();
    draw();
  };
  $("#input-scrub").oninput = () => {
    stopPlay();
    frameIndex = Number($("#input-scrub").value) || 0;
    refreshFrameChrome();
    draw();
  };
  $("#input-onion").onchange = () => draw();
  $("#input-loop").onchange = () => {
    const st = activeStateData();
    if (st) {
      st.loop = $("#input-loop").checked;
      $("#insp-loop").checked = st.loop;
      saveMeta(meta);
    }
  };

  $("#btn-add-part").onclick = () => {
    const c = activeChar();
    const name = $("#part-new").value.trim();
    if (!c || !name) return;
    c.parts.push({ name, done: false, artId: null });
    $("#part-new").value = "";
    saveMeta(meta);
    renderParts();
  };
  $("#btn-reset-parts").onclick = () => {
    const c = activeChar();
    if (!c || !confirm("Reset parts to default template?")) return;
    c.parts = makeParts();
    saveMeta(meta);
    renderParts();
  };

  $("#btn-add-bone").onclick = () => {
    const c = activeChar();
    const name = $("#bone-new").value.trim();
    const parent = $("#bone-parent").value.trim() || null;
    if (!c || !name) return;
    c.bones.push({ name, parent });
    $("#bone-new").value = "";
    $("#bone-parent").value = "";
    saveMeta(meta);
    renderBones();
  };
  $("#btn-reset-bones").onclick = () => {
    const c = activeChar();
    if (!c || !confirm("Reset bone tree to default?")) return;
    c.bones = DEFAULT_BONES.map((b) => ({ ...b }));
    saveMeta(meta);
    renderBones();
  };
  $("#bone-notes").oninput = () => {
    const c = activeChar();
    if (!c) return;
    c.boneNotes = $("#bone-notes").value;
    saveMeta(meta);
  };

  $("#btn-export-manifest").onclick = () => exportManifest(false);
  $("#btn-export-project").onclick = () => exportManifest(true);
  $("#input-import").onchange = async () => {
    const f = $("#input-import").files?.[0];
    if (!f) return;
    try {
      const data = JSON.parse(await f.text());
      await importProject(data);
    } catch (err) {
      alert("Import failed: " + err.message);
    }
    $("#input-import").value = "";
  };

  $("#btn-copy-codegen").onclick = async () => {
    const text = $("#codegen").textContent;
    await navigator.clipboard.writeText(text);
    $("#btn-copy-codegen").textContent = "Copied!";
    setTimeout(() => {
      $("#btn-copy-codegen").textContent = "Copy C#";
    }, 1200);
  };

  $("#btn-seed-brennen").onclick = () => {
    seedShells();
  };

  // drag-drop frames onto stage
  const stage = canvas;
  stage.addEventListener("dragover", (e) => {
    e.preventDefault();
    stage.style.outline = "2px solid var(--accent)";
  });
  stage.addEventListener("dragleave", () => {
    stage.style.outline = "";
  });
  stage.addEventListener("drop", async (e) => {
    e.preventDefault();
    stage.style.outline = "";
    const c = activeChar();
    const st = activeStateData();
    if (!c || !st) return;
    const files = [...e.dataTransfer.files].filter((f) => f.type.startsWith("image/"));
    for (const f of files) {
      const id = blobKey(c.id, "frame", meta.activeState, uid());
      await storeBlob(id, f);
      st.frameIds.push(id);
    }
    saveMeta(meta);
    refreshStateUi();
    draw();
  });
}

// ─── Export / import ─────────────────────────────────────────────────────────

async function exportManifest(includeBlobs) {
  const char = activeChar();
  if (!char) {
    alert("Select a character first");
    return;
  }

  const payload = {
    format: "sts2-char-anim-pipeline",
    version: 1,
    exportedAt: new Date().toISOString(),
    baselibStates: DEFAULT_STATES,
    character: structuredClone(char),
    blobs: {},
  };

  if (includeBlobs) {
    const collect = async (id) => {
      if (!id || payload.blobs[id]) return;
      const b = await idbGet(id);
      if (!b) return;
      const buf = await b.arrayBuffer();
      const bytes = new Uint8Array(buf);
      let bin = "";
      for (let i = 0; i < bytes.length; i++) bin += String.fromCharCode(bytes[i]);
      payload.blobs[id] = {
        type: b.type || "image/png",
        data: btoa(bin),
      };
    };
    if (char.refImageId) await collect(char.refImageId);
    for (const st of Object.values(char.states)) {
      for (const id of st.frameIds || []) await collect(id);
    }
    for (const p of char.parts) {
      if (p.artId) await collect(p.artId);
    }
  } else {
    // strip frame ids from lightweight manifest? keep counts + meta only
    for (const [k, st] of Object.entries(payload.character.states)) {
      st.frameCount = st.frameIds?.length || 0;
      delete st.frameIds;
    }
    for (const p of payload.character.parts) {
      p.hasArt = !!p.artId;
      delete p.artId;
    }
    delete payload.character.refImageId;
  }

  const name = includeBlobs
    ? `${char.id}-anim-project.json`
    : `${char.id}-anim-manifest.json`;
  downloadJson(payload, name);
}

async function importProject(data) {
  if (data.format !== "sts2-char-anim-pipeline") {
    throw new Error("Unrecognized project format");
  }
  const ch = data.character;
  if (!ch?.id) throw new Error("Missing character");

  // restore blobs
  if (data.blobs) {
    for (const [id, rec] of Object.entries(data.blobs)) {
      const bin = atob(rec.data);
      const bytes = new Uint8Array(bin.length);
      for (let i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);
      const blob = new Blob([bytes], { type: rec.type || "image/png" });
      await storeBlob(id, blob);
    }
  }

  // ensure state shape
  const base = makeCharacter(ch.name || ch.id, ch.id);
  const merged = {
    ...base,
    ...ch,
    states: { ...base.states, ...(ch.states || {}) },
    parts: ch.parts?.length ? ch.parts : base.parts,
    bones: ch.bones?.length ? ch.bones : base.bones,
  };
  // restore frameIds if missing (manifest-only)
  for (const s of DEFAULT_STATES) {
    if (!merged.states[s.key]) merged.states[s.key] = base.states[s.key];
    if (!merged.states[s.key].frameIds) merged.states[s.key].frameIds = [];
  }

  const idx = meta.characters.findIndex((c) => c.id === merged.id);
  if (idx >= 0) meta.characters[idx] = merged;
  else meta.characters.push(merged);
  meta.activeCharId = merged.id;
  saveMeta(meta);
  imageCache.clear();
  refreshAll();
}

function downloadJson(obj, filename) {
  const blob = new Blob([JSON.stringify(obj, null, 2)], { type: "application/json" });
  const a = document.createElement("a");
  a.href = URL.createObjectURL(blob);
  a.download = filename;
  a.click();
  URL.revokeObjectURL(a.href);
}

function seedShells() {
  const seeds = [
    { name: "Brennen", id: "brennen", notes: "Tank · greatsword · ember armor" },
    { name: "Whitney", id: "whitney", notes: "Atelier witch · indigo · quill" },
  ];
  for (const s of seeds) {
    if (meta.characters.some((c) => c.id === s.id)) continue;
    const ch = makeCharacter(s.name, s.id);
    ch.notes = s.notes;
    if (s.id === "whitney") {
      // emphasize hat
      const hat = ch.parts.find((p) => p.name === "hat");
      if (hat) hat.done = false;
    }
    if (s.id === "brennen") {
      const w = ch.parts.find((p) => p.name === "weapon");
      if (w) w.done = false;
    }
    meta.characters.push(ch);
  }
  if (!meta.activeCharId && meta.characters.length) {
    meta.activeCharId = meta.characters[0].id;
  }
  saveMeta(meta);
  refreshAll();
}

// ─── Boot ────────────────────────────────────────────────────────────────────

bindEvents();
if (!meta.characters.length) {
  // empty — user can seed
}
refreshAll();
