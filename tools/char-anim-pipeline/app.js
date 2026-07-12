/**
 * UI glue: lists, frames, spine player, export.
 */
(function () {
  const C = window.AnimCore;
  const E = window.AnimEditor;
  const $ = (s) => document.querySelector(s);

  const meta = C.loadMeta();
  // migrate empty
  let rigEd = null;
  let animEd = null;
  let spinePlayer = null;
  let spineUrls = [];

  let frameIndex = 0;
  let framePlaying = false;
  let animPlaying = false;
  let animRaf = 0;
  let animT = 0;
  let lastTs = 0;

  // ─── Undo / redo ─────────────────────────────────────────────────────────
  const history = {
    undo: [],
    redo: [],
    max: 80,
    applying: false,
    coalescing: false, // skip duplicate pushes in same gesture if needed
  };

  function cloneMetaCharacters() {
    return structuredClone(meta.characters);
  }

  function takeSnapshot(label) {
    return {
      label: label || "edit",
      activeCharId: meta.activeCharId,
      activeState: meta.activeState,
      characters: cloneMetaCharacters(),
      animT,
      frameIndex,
      animPose: animEd ? structuredClone(animEd.pose || {}) : {},
    };
  }

  function updateHistoryButtons() {
    const u = $("#btn-undo");
    const r = $("#btn-redo");
    if (u) {
      u.disabled = history.undo.length === 0;
      u.title =
        history.undo.length === 0
          ? "Undo (Ctrl+Z)"
          : `Undo: ${history.undo[history.undo.length - 1].label} (Ctrl+Z)`;
    }
    if (r) {
      r.disabled = history.redo.length === 0;
      r.title =
        history.redo.length === 0
          ? "Redo (Ctrl+Y)"
          : `Redo: ${history.redo[history.redo.length - 1].label} (Ctrl+Y)`;
    }
  }

  /** Call *before* mutating project data. */
  function pushHistory(label) {
    if (history.applying) return;
    history.undo.push(takeSnapshot(label));
    if (history.undo.length > history.max) history.undo.shift();
    history.redo.length = 0;
    updateHistoryButtons();
  }

  function applySnapshot(snap) {
    history.applying = true;
    meta.characters = structuredClone(snap.characters);
    meta.activeCharId = snap.activeCharId;
    meta.activeState = snap.activeState || "idle";
    animT = snap.animT ?? 0;
    frameIndex = snap.frameIndex ?? 0;
    if (animEd) animEd.pose = structuredClone(snap.animPose || {});
    if (rigEd) rigEd.selectedId = null;
    C.saveMeta(meta);
    refreshAll();
    history.applying = false;
    updateHistoryButtons();
  }

  function undo() {
    if (!history.undo.length || history.applying) return;
    history.redo.push(takeSnapshot("before-undo"));
    if (history.redo.length > history.max) history.redo.shift();
    const snap = history.undo.pop();
    applySnapshot(snap);
  }

  function redo() {
    if (!history.redo.length || history.applying) return;
    history.undo.push(takeSnapshot("before-redo"));
    if (history.undo.length > history.max) history.undo.shift();
    const snap = history.redo.pop();
    applySnapshot(snap);
  }

  function clearHistory() {
    history.undo.length = 0;
    history.redo.length = 0;
    updateHistoryButtons();
  }

  function activeChar() {
    return meta.characters.find((c) => c.id === meta.activeCharId) || null;
  }

  function stateData(char = activeChar()) {
    if (!char) return null;
    return char.states[meta.activeState];
  }

  function save() {
    if (history.applying) return;
    const ch = activeChar();
    if (ch) ch.updatedAt = Date.now();
    C.saveMeta(meta);
  }

  function escapeHtml(s) {
    return String(s)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;");
  }

  // ─── Lists ───────────────────────────────────────────────────────────────

  function renderCharList() {
    const ul = $("#char-list");
    ul.innerHTML = "";
    if (!meta.characters.length) {
      ul.innerHTML = `<li style="cursor:default;color:var(--muted);font-size:0.85rem">No characters</li>`;
      return;
    }
    for (const c of meta.characters) {
      const li = document.createElement("li");
      li.className = c.id === meta.activeCharId ? "active" : "";
      li.innerHTML = `<span>${escapeHtml(c.name)}</span><span class="char-id">${escapeHtml(c.id)}</span>`;
      li.onclick = () => {
        meta.activeCharId = c.id;
        save();
        refreshAll();
      };
      ul.appendChild(li);
    }
  }

  function renderStateList() {
    const ul = $("#state-list");
    ul.innerHTML = "";
    const char = activeChar();
    for (const s of C.DEFAULT_STATES) {
      const st = char?.states[s.key];
      const keys = st?.keys?.length || 0;
      const frames = st?.frameIds?.length || 0;
      const li = document.createElement("li");
      li.className = [
        s.key === meta.activeState ? "active" : "",
        keys || frames ? "has-frames" : "",
        s.required ? "required" : "",
      ]
        .filter(Boolean)
        .join(" ");
      li.innerHTML = `<span class="dot"></span><span class="name">${s.label}</span><span class="badge">${keys}k/${frames}f</span>`;
      li.onclick = () => {
        meta.activeState = s.key;
        frameIndex = 0;
        animT = 0;
        save();
        refreshStateUi();
      };
      ul.appendChild(li);
    }
  }

  function renderBoneList() {
    const ul = $("#bone-list");
    ul.innerHTML = "";
    const char = activeChar();
    if (!char) return;
    const depth = (id, seen = new Set()) => {
      const b = char.bones.find((x) => x.id === id);
      if (!b || !b.parent || seen.has(id)) return 0;
      seen.add(id);
      return 1 + depth(b.parent, seen);
    };
    const sorted = [...char.bones].sort((a, b) => depth(a.id) - depth(b.id) || a.name.localeCompare(b.name));
    for (const b of sorted) {
      const li = document.createElement("li");
      const d = depth(b.id);
      li.className = b.id === (rigEd?.selectedId || animEd?.selectedId) ? "active" : "";
      li.innerHTML = `<span style="color:var(--muted)">${"· ".repeat(d)}</span>${escapeHtml(b.name)}`;
      li.onclick = () => {
        if (rigEd) {
          rigEd.selectedId = b.id;
          E.redraw(rigEd);
        }
        if (animEd) {
          animEd.selectedId = b.id;
          E.redraw(animEd);
        }
        renderBoneInsp(b.id);
        renderBoneList();
      };
      ul.appendChild(li);
    }
  }

  function renderBoneInsp(id) {
    const char = activeChar();
    const box = $("#bone-insp");
    const b = char?.bones.find((x) => x.id === id);
    if (!b) {
      box.className = "bone-insp empty";
      box.textContent = "Select a bone";
      return;
    }
    box.className = "bone-insp";
    const att = char.attachments[b.id];
    box.innerHTML = `
      <label>Name <input id="bi-name" type="text" value="${escapeHtml(b.name)}" /></label>
      <label>Length <input id="bi-len" type="number" step="1" value="${b.length.toFixed(1)}" /></label>
      <label>Local angle (rad) <input id="bi-ang" type="number" step="0.01" value="${b.angle.toFixed(3)}" /></label>
      <label>Parent <input type="text" value="${escapeHtml(b.parent || "—")}" disabled /></label>
      <p class="hint">${att?.imageId ? "Has attachment image" : "No attachment — use Attach image"}</p>
      ${att?.imageId ? `<button type="button" class="btn btn-sm btn-ghost" id="bi-clear-att">Detach image</button>` : ""}
    `;
    $("#bi-name").onchange = () => {
      pushHistory("rename bone");
      b.name = $("#bi-name").value.trim() || b.id;
      save();
      renderBoneList();
      E.redraw(rigEd);
    };
    $("#bi-len").onchange = () => {
      pushHistory("bone length");
      b.length = Math.max(8, Number($("#bi-len").value) || 8);
      C.bakeSetupFromAngles(char.bones);
      save();
      E.redraw(rigEd);
    };
    $("#bi-ang").onchange = () => {
      pushHistory("bone angle");
      b.angle = Number($("#bi-ang").value) || 0;
      C.bakeSetupFromAngles(char.bones);
      save();
      E.redraw(rigEd);
    };
    const clr = $("#bi-clear-att");
    if (clr) {
      clr.onclick = async () => {
        pushHistory("detach image");
        if (att?.imageId) await C.idbDel(att.imageId);
        delete char.attachments[b.id];
        save();
        renderBoneInsp(b.id);
        E.redraw(rigEd);
      };
    }
  }

  function renderParts() {
    const ul = $("#part-list");
    ul.innerHTML = "";
    const char = activeChar();
    if (!char) return;
    for (const p of char.parts) {
      const li = document.createElement("li");
      if (p.done) li.classList.add("done");
      li.innerHTML = `<input type="checkbox" ${p.done ? "checked" : ""} /><span>${escapeHtml(p.name)}</span>`;
      li.querySelector("input").onchange = (e) => {
        pushHistory("part checklist");
        p.done = e.target.checked;
        save();
        renderParts();
        renderExport();
      };
      ul.appendChild(li);
    }
  }

  function renderKeys() {
    const list = $("#key-list");
    list.innerHTML = "";
    const st = stateData();
    if (!st) return;
    const keys = [...(st.keys || [])].sort((a, b) => a.t - b.t);
    if (!keys.length) {
      list.innerHTML = `<div class="empty-state">No keys — pose bones &amp; Key pose</div>`;
      return;
    }
    for (const k of keys) {
      const div = document.createElement("div");
      div.className = "key-item" + (Math.abs(k.t - animT) < 1 ? " active" : "");
      div.innerHTML = `<strong>${k.t}ms</strong> · ${Object.keys(k.bones || {}).length} bones`;
      div.onclick = () => {
        animT = k.t;
        $("#timeline").value = String(animT);
        $("#timeline-time").textContent = String(animT);
        if (animEd) E.setPose(animEd, C.poseAt(st.keys, animT));
        renderKeys();
      };
      list.appendChild(div);
    }
    // marks
    const marks = $("#key-marks");
    marks.innerHTML = "";
    const len = st.lengthMs || 1000;
    for (const k of keys) {
      const m = document.createElement("span");
      m.className = "key-mark";
      m.style.left = `${(k.t / len) * 100}%`;
      marks.appendChild(m);
    }
  }

  function refreshStateUi() {
    const char = activeChar();
    const st = stateData();
    const def = C.DEFAULT_STATES.find((s) => s.key === meta.activeState);

    if (char) {
      $("#char-name").value = char.name;
      $("#char-id").value = char.id;
      $("#char-notes").value = char.notes || "";
    }
    $("#anim-state-label").textContent = def?.label || meta.activeState;
    $("#frame-state-label").textContent = def?.label || meta.activeState;

    if (st) {
      $("#clip-length").value = st.lengthMs || 1000;
      $("#clip-name").value = st.clipName || def?.clipName || "";
      $("#anim-loop").checked = !!st.loop;
      $("#state-notes").value = st.notes || "";
      $("#frame-state-loop").checked = !!st.loop;
      $("#frame-notes").value = st.notes || "";
      $("#timeline").max = String(st.lengthMs || 1000);
      $("#timeline").value = String(Math.min(animT, st.lengthMs || 1000));
    }

    renderStateList();
    renderBoneList();
    renderParts();
    renderKeys();
    renderFrameStrip();
    renderExport();

    if (rigEd) E.redraw(rigEd);
    if (animEd) {
      const pose = st ? C.poseAt(st.keys || [], animT) : {};
      E.setPose(animEd, pose);
    }
    drawFrames();
  }

  function refreshAll() {
    renderCharList();
    refreshStateUi();
    if (rigEd) {
      E.resizeStage(rigEd);
      E.redraw(rigEd);
    }
    if (animEd) {
      E.resizeStage(animEd);
      E.redraw(animEd);
    }
  }

  // ─── Frames flipbook ─────────────────────────────────────────────────────

  async function drawFrames() {
    const canvas = $("#frame-stage");
    const ctx = canvas.getContext("2d");
    const w = canvas.width;
    const h = canvas.height;
    ctx.clearRect(0, 0, w, h);
    ctx.fillStyle = "#0c0e14";
    ctx.fillRect(0, 0, w, h);
    ctx.strokeStyle = "rgba(232,93,76,0.3)";
    ctx.beginPath();
    ctx.moveTo(w * 0.15, h * 0.88);
    ctx.lineTo(w * 0.85, h * 0.88);
    ctx.stroke();

    const st = stateData();
    const ids = st?.frameIds || [];
    if (!ids.length) {
      ctx.fillStyle = "#8b93a7";
      ctx.font = "15px Segoe UI";
      ctx.textAlign = "center";
      ctx.fillText("Add flipbook frames for this state", w / 2, h / 2);
      return;
    }
    if ($("#frame-onion").checked && frameIndex > 0) {
      const prev = await E.loadImg(ids[frameIndex - 1]);
      if (prev) {
        ctx.globalAlpha = 0.25;
        drawContained(ctx, prev, w, h);
        ctx.globalAlpha = 1;
      }
    }
    const img = await E.loadImg(ids[frameIndex]);
    if (img) drawContained(ctx, img, w, h);
  }

  function drawContained(ctx, img, cw, ch) {
    const scale = Math.min((cw * 0.9) / img.width, (ch * 0.9) / img.height);
    const dw = img.width * scale;
    const dh = img.height * scale;
    ctx.drawImage(img, (cw - dw) / 2, (ch - dh) / 2 + 10, dw, dh);
  }

  function renderFrameStrip() {
    const strip = $("#frame-strip");
    strip.innerHTML = "";
    const st = stateData();
    const ids = st?.frameIds || [];
    $("#frame-scrub").max = String(Math.max(0, ids.length - 1));
    $("#frame-scrub").value = String(frameIndex);
    if (!ids.length) {
      strip.innerHTML = `<div class="empty-state">No frames</div>`;
      return;
    }
    ids.forEach((id, i) => {
      const img = document.createElement("img");
      img.className = "frame-thumb" + (i === frameIndex ? " active" : "");
      E.loadImg(id).then((im) => {
        if (im) img.src = im.src;
      });
      img.onclick = () => {
        frameIndex = i;
        renderFrameStrip();
        drawFrames();
      };
      strip.appendChild(img);
    });
  }

  // ─── Export panel ────────────────────────────────────────────────────────

  function renderExport() {
    const char = activeChar();
    const steps = $("#pipeline-steps");
    const codegen = $("#codegen");
    const folder = $("#folder-layout");
    const libs = $("#lib-grid");

    libs.innerHTML = `
      <div class="lib-card"><h4>Konva</h4><p>Interactive 2D canvas for bones, handles, image attach.</p><code>unpkg.com/konva</code></div>
      <div class="lib-card"><h4>Spine Player</h4><p>Official Esoteric runtime — preview real .json/.skel exports.</p><code>@esotericsoftware/spine-player@4.2</code></div>
      <div class="lib-card"><h4>Pipeline FK</h4><p>Our lightweight local-angle FK + keyframe lerp (no license).</p><code>core.js</code></div>
      <div class="lib-card"><h4>STS2 path</h4><p>Game uses Spine/MegaSpine. Author in Spine Editor or iterate here then export.</p><code>BaseLib SetupAnimationState</code></div>
    `;

    if (!char) {
      steps.innerHTML = "<li>Create a character</li>";
      codegen.textContent = "// …";
      folder.textContent = "";
      return;
    }

    const checks = [
      ["Bones ≥ 10", char.bones.length >= 10],
      ["Idle keys or frames", (char.states.idle.keys?.length || 0) + (char.states.idle.frameIds?.length || 0) > 0],
      ["Attack keys or frames", (char.states.attack.keys?.length || 0) + (char.states.attack.frameIds?.length || 0) > 0],
      ["Hit ready", (char.states.hit.keys?.length || 0) + (char.states.hit.frameIds?.length || 0) > 0],
      ["Dead ready", (char.states.dead.keys?.length || 0) + (char.states.dead.frameIds?.length || 0) > 0],
      ["Parts ticked ≥ 6", char.parts.filter((p) => p.done).length >= 6],
      ["Any attachment images", Object.keys(char.attachments || {}).length > 0],
    ];
    steps.innerHTML = checks
      .map(([l, ok]) => `<li class="${ok ? "ok" : "todo"}">${ok ? "✓" : "○"} ${escapeHtml(l)}</li>`)
      .join("");

    const cls = char.id.charAt(0).toUpperCase() + char.id.slice(1);
    const st = (k) => char.states[k];
    codegen.textContent = `// ${cls} combat visuals
public override string CustomVisualPath =>
    "res://scenes/creature_visuals/${char.id}.tscn";

protected override CreatureAnimator SetupCustomAnimationStates(MegaSprite controller)
{
    return SetupAnimationState(
        controller,
        idleName: "${st("idle")?.clipName || "idle"}",
        deadName: "${st("dead")?.clipName || "dead"}", deadLoop: ${!!st("dead")?.loop},
        hitName: "${st("hit")?.clipName || "hit"}", hitLoop: ${!!st("hit")?.loop},
        attackName: "${st("attack")?.clipName || "attack"}", attackLoop: ${!!st("attack")?.loop},
        castName: "${st("cast")?.clipName || "cast"}", castLoop: ${!!st("cast")?.loop},
        relaxedName: "${st("relaxed")?.clipName || "relaxed"}", relaxedLoop: ${!!st("relaxed")?.loop}
    );
}
`;
    folder.textContent = `mods/${char.id}/
  spine/${char.id}.spine          # Spine Editor source
  spine/export/${char.id}.skel
  spine/export/${char.id}.atlas
  spine/export/${char.id}.png
  scenes/creature_visuals/${char.id}.tscn
  images/creature/parts/…
`;
  }

  // ─── Spine ───────────────────────────────────────────────────────────────

  function clearSpine() {
    for (const u of spineUrls) URL.revokeObjectURL(u);
    spineUrls = [];
    const host = $("#spine-host");
    host.innerHTML = "";
    spinePlayer = null;
    $("#spine-status").textContent = "No skeleton loaded";
  }

  async function loadSpineFiles(fileList) {
    clearSpine();
    const files = [...fileList];
    const byExt = (ext) => files.find((f) => f.name.toLowerCase().endsWith(ext));
    const atlasFile = byExt(".atlas") || byExt(".atlas.txt");
    const jsonFile = byExt(".json");
    const skelFile = byExt(".skel");
    const pngs = files.filter((f) => f.name.toLowerCase().endsWith(".png"));

    if (!atlasFile || (!jsonFile && !skelFile)) {
      $("#spine-status").textContent =
        "Need .atlas + (.json or .skel) + texture PNG(s).\nGot: " + files.map((f) => f.name).join(", ");
      return;
    }
    if (typeof spine === "undefined" || !spine.SpinePlayer) {
      $("#spine-status").textContent = "Spine Player failed to load from CDN (check network).";
      return;
    }

    // Build blob URLs; atlas text must reference blob textures — rewrite atlas paths
    let atlasText = await atlasFile.text();
    const pngMap = {};
    for (const p of pngs) {
      const url = URL.createObjectURL(p);
      spineUrls.push(url);
      pngMap[p.name] = url;
      // rewrite first path line that matches filename
      atlasText = atlasText.replace(new RegExp(`^${p.name.replace(".", "\\.")}$`, "m"), url);
      // also bare names without matching full line at start of atlas
      if (!atlasText.includes(url)) {
        atlasText = atlasText.replace(p.name, url);
      }
    }
    const atlasUrl = URL.createObjectURL(new Blob([atlasText], { type: "text/plain" }));
    spineUrls.push(atlasUrl);

    const skelUrl = URL.createObjectURL(jsonFile || skelFile);
    spineUrls.push(skelUrl);

    const host = $("#spine-host");
    host.innerHTML = "";
    try {
      const cfg = {
        atlasUrl,
        rawDataURIs: {},
        alpha: true,
        backgroundColor: "#0c0e14ff",
        showControls: true,
        premultipliedAlpha: true,
      };
      if (jsonFile) cfg.jsonUrl = skelUrl;
      else cfg.skelUrl = skelUrl;

      spinePlayer = new spine.SpinePlayer(host, cfg);
      $("#spine-status").textContent =
        `Loaded ${jsonFile ? jsonFile.name : skelFile.name}\n+ ${atlasFile.name}\n+ ${pngs.map((p) => p.name).join(", ")}\nRuntime: spine-player 4.2.x`;
    } catch (err) {
      $("#spine-status").textContent = "Spine load error: " + err.message;
      console.error(err);
    }
  }

  // ─── Export files ────────────────────────────────────────────────────────

  function downloadJson(obj, name) {
    const blob = new Blob([JSON.stringify(obj, null, 2)], { type: "application/json" });
    const a = document.createElement("a");
    a.href = URL.createObjectURL(blob);
    a.download = name;
    a.click();
    setTimeout(() => URL.revokeObjectURL(a.href), 2000);
  }

  async function exportProject(full) {
    const char = activeChar();
    if (!char) return alert("Select a character");
    const payload = {
      format: "sts2-char-anim-pipeline",
      version: 2,
      exportedAt: new Date().toISOString(),
      character: structuredClone(char),
      blobs: {},
    };
    if (full) {
      const collect = async (id) => {
        if (!id || payload.blobs[id]) return;
        const b = await C.idbGet(id);
        if (!b) return;
        const buf = new Uint8Array(await b.arrayBuffer());
        let s = "";
        for (let i = 0; i < buf.length; i++) s += String.fromCharCode(buf[i]);
        payload.blobs[id] = { type: b.type || "image/png", data: btoa(s) };
      };
      if (char.refImageId) await collect(char.refImageId);
      for (const st of Object.values(char.states)) {
        for (const id of st.frameIds || []) await collect(id);
      }
      for (const att of Object.values(char.attachments || {})) {
        if (att.imageId) await collect(att.imageId);
      }
    } else {
      for (const st of Object.values(payload.character.states)) {
        st.frameCount = st.frameIds?.length || 0;
        st.keyCount = st.keys?.length || 0;
        delete st.frameIds;
      }
    }
    downloadJson(payload, full ? `${char.id}-anim-project.json` : `${char.id}-anim-manifest.json`);
  }

  async function importProject(data) {
    if (!data || !String(data.format || "").includes("sts2-char-anim")) {
      throw new Error("Unrecognized format");
    }
    if (data.blobs) {
      for (const [id, rec] of Object.entries(data.blobs)) {
        const bin = atob(rec.data);
        const bytes = new Uint8Array(bin.length);
        for (let i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);
        await C.idbPut(id, new Blob([bytes], { type: rec.type || "image/png" }));
      }
    }
    const base = C.makeCharacter(data.character.name, data.character.id);
    const merged = {
      ...base,
      ...data.character,
      bones: data.character.bones?.length ? data.character.bones : base.bones,
      states: { ...base.states, ...data.character.states },
      attachments: data.character.attachments || {},
      parts: data.character.parts?.length ? data.character.parts : base.parts,
    };
    for (const s of C.DEFAULT_STATES) {
      if (!merged.states[s.key]) merged.states[s.key] = base.states[s.key];
      if (!merged.states[s.key].frameIds) merged.states[s.key].frameIds = [];
      if (!merged.states[s.key].keys) merged.states[s.key].keys = [];
    }
    const idx = meta.characters.findIndex((c) => c.id === merged.id);
    if (idx >= 0) meta.characters[idx] = merged;
    else meta.characters.push(merged);
    meta.activeCharId = merged.id;
    if (!history.applying) save();
    else C.saveMeta(meta);
    E.clearImageCache();
    refreshAll();
  }

  // ─── Events ──────────────────────────────────────────────────────────────

  function bind() {
    $("#btn-undo").onclick = () => undo();
    $("#btn-redo").onclick = () => redo();
    window.addEventListener("keydown", (e) => {
      const tag = (e.target && e.target.tagName) || "";
      const typing =
        tag === "INPUT" || tag === "TEXTAREA" || e.target?.isContentEditable;
      if (typing && !e.ctrlKey && !e.metaKey) return;
      const mod = e.ctrlKey || e.metaKey;
      if (mod && e.key.toLowerCase() === "z" && !e.shiftKey) {
        e.preventDefault();
        undo();
      } else if (mod && (e.key.toLowerCase() === "y" || (e.key.toLowerCase() === "z" && e.shiftKey))) {
        e.preventDefault();
        redo();
      }
    });

    $("#btn-add-char").onclick = () => $("#dlg-new-char").showModal();
    $("#btn-cancel-char").onclick = () => $("#dlg-new-char").close();
    $("#form-new-char").onsubmit = (e) => {
      e.preventDefault();
      const fd = new FormData(e.target);
      const name = String(fd.get("name")).trim();
      const id = String(fd.get("id")).trim().toLowerCase();
      if (meta.characters.some((c) => c.id === id)) return alert("Id exists");
      pushHistory("create character");
      meta.characters.push(C.makeCharacter(name, id));
      meta.activeCharId = id;
      save();
      $("#dlg-new-char").close();
      e.target.reset();
      refreshAll();
    };

    let nameHistArmed = false;
    $("#char-name").onfocus = () => {
      nameHistArmed = true;
    };
    $("#char-name").oninput = () => {
      const c = activeChar();
      if (!c) return;
      if (nameHistArmed) {
        pushHistory("rename character");
        nameHistArmed = false;
      }
      c.name = $("#char-name").value;
      save();
      renderCharList();
    };
    let notesHistArmed = false;
    $("#char-notes").onfocus = () => {
      notesHistArmed = true;
    };
    $("#char-notes").oninput = () => {
      const c = activeChar();
      if (!c) return;
      if (notesHistArmed) {
        pushHistory("edit notes");
        notesHistArmed = false;
      }
      c.notes = $("#char-notes").value;
      save();
    };

    document.querySelectorAll(".tab").forEach((tab) => {
      tab.onclick = () => {
        document.querySelectorAll(".tab").forEach((t) => t.classList.remove("active"));
        document.querySelectorAll(".tab-panel").forEach((p) => p.classList.remove("active"));
        tab.classList.add("active");
        $(`#tab-${tab.dataset.tab}`).classList.add("active");
        if (tab.dataset.tab === "rig" && rigEd) {
          E.resizeStage(rigEd);
          E.redraw(rigEd);
        }
        if (tab.dataset.tab === "animate" && animEd) {
          E.resizeStage(animEd);
          refreshStateUi();
        }
        if (tab.dataset.tab === "export") renderExport();
      };
    });

    document.querySelectorAll(".tool").forEach((btn) => {
      btn.onclick = () => {
        document.querySelectorAll(".tool").forEach((b) => b.classList.remove("active"));
        btn.classList.add("active");
        if (rigEd) rigEd.tool = btn.dataset.tool;
      };
    });

    $("#chk-show-bones").onchange = () => {
      if (rigEd) {
        rigEd.showBones = $("#chk-show-bones").checked;
        E.redraw(rigEd);
      }
      if (animEd) {
        animEd.showBones = $("#chk-show-bones").checked;
        E.redraw(animEd);
      }
    };
    $("#chk-show-attach").onchange = () => {
      const on = $("#chk-show-attach").checked;
      if (rigEd) {
        rigEd.showAttach = on;
        E.redraw(rigEd);
      }
      if (animEd) {
        animEd.showAttach = on;
        E.redraw(animEd);
      }
    };
    $("#chk-show-ref").onchange = () => {
      const on = $("#chk-show-ref").checked;
      if (rigEd) {
        rigEd.showRef = on;
        E.redraw(rigEd);
      }
      if (animEd) {
        animEd.showRef = on;
        E.redraw(animEd);
      }
    };

    $("#btn-add-bone").onclick = () => {
      if (rigEd) rigEd.tool = "bone";
      document.querySelectorAll(".tool").forEach((b) => b.classList.toggle("active", b.dataset.tool === "bone"));
    };
    $("#btn-del-bone").onclick = () => {
      const char = activeChar();
      const id = rigEd?.selectedId;
      if (!char || !id) return;
      if (char.bones.some((b) => b.parent === id)) return alert("Delete children first");
      pushHistory("delete bone");
      char.bones = char.bones.filter((b) => b.id !== id);
      delete char.attachments[id];
      rigEd.selectedId = null;
      save();
      refreshAll();
    };
    $("#btn-reset-rig").onclick = () => {
      const char = activeChar();
      if (!char || !confirm("Reset bones to default humanoid template?")) return;
      pushHistory("reset rig");
      char.bones = C.defaultBones();
      char.attachments = {};
      save();
      refreshAll();
    };

    $("#input-attach").onchange = async () => {
      const char = activeChar();
      const id = rigEd?.selectedId;
      const f = $("#input-attach").files?.[0];
      if (!char || !id || !f) return alert("Select a bone first");
      pushHistory("attach image");
      const bid = C.blobKey(char.id, "att", id, C.uid());
      await C.idbPut(bid, f);
      char.attachments[id] = {
        imageId: bid,
        offsetX: 0,
        offsetY: 0,
        scale: 0.35,
        rotation: 0,
      };
      save();
      E.clearImageCache();
      E.redraw(rigEd);
      renderBoneInsp(id);
    };

    $("#input-ref").onchange = async () => {
      const char = activeChar();
      const f = $("#input-ref").files?.[0];
      if (!char || !f) return;
      pushHistory("set reference");
      const id = C.blobKey(char.id, "ref", C.uid());
      await C.idbPut(id, f);
      if (char.refImageId) await C.idbDel(char.refImageId);
      char.refImageId = id;
      save();
      E.clearImageCache();
      E.redraw(rigEd);
      E.redraw(animEd);
    };
    $("#btn-clear-ref").onclick = async () => {
      const char = activeChar();
      if (!char?.refImageId) return;
      pushHistory("clear reference");
      await C.idbDel(char.refImageId);
      char.refImageId = null;
      save();
      E.redraw(rigEd);
      E.redraw(animEd);
    };

    /** One body sprite: drop ghost ref; keep a single torso attachment if any exist. */
    $("#btn-fix-sprites").onclick = () => {
      const char = activeChar();
      if (!char) return;
      pushHistory("fix stacked sprites");
      char.refImageId = null;
      const atts = char.attachments || {};
      const keys = Object.keys(atts);
      if (keys.length > 1) {
        const keep =
          atts.torso ||
          atts[keys.find((k) => k.includes("torso"))] ||
          atts[keys[0]];
        char.attachments = keep ? { torso: { ...keep, offsetX: 0, offsetY: keep.offsetY ?? 40 } } : {};
        if (char.attachments.torso && !char.bones.some((b) => b.id === "torso")) {
          // map onto first chest/torso-like bone id
          const host = char.bones.find((b) => /torso|chest|hip/.test(b.id)) || char.bones[0];
          if (host) {
            char.attachments = {
              [host.id]: { ...char.attachments.torso, offsetX: 0, offsetY: 40 },
            };
          }
        }
      } else if (keys.length === 1) {
        const id = keys[0];
        char.attachments[id].offsetX = 0;
      }
      if ($("#chk-show-ref")) $("#chk-show-ref").checked = false;
      if (rigEd) rigEd.showRef = false;
      if (animEd) animEd.showRef = false;
      save();
      E.redraw(rigEd);
      E.redraw(animEd);
      alert(
        "Cleaned sprites.\n\n• Ghost ref removed\n• One body attachment kept\n• Ghost ref toggle forced off\n\nYou should see a single Brennen."
      );
    };

    // animate
    $("#btn-key").onclick = () => {
      const char = activeChar();
      const st = stateData();
      if (!char || !st || !animEd) return;
      pushHistory("key pose");
      const bones = {};
      for (const b of char.bones) {
        const p = animEd.pose[b.id];
        bones[b.id] = {
          angle: p?.angle != null ? p.angle : b.angle,
          length: p?.length != null ? p.length : b.length,
        };
      }
      st.keys = st.keys || [];
      const existing = st.keys.findIndex((k) => Math.abs(k.t - animT) < 1);
      const key = { t: Math.round(animT), bones };
      if (existing >= 0) st.keys[existing] = key;
      else st.keys.push(key);
      st.keys.sort((a, b) => a.t - b.t);
      save();
      renderKeys();
      renderStateList();
    };
    $("#btn-del-key").onclick = () => {
      const st = stateData();
      if (!st) return;
      pushHistory("delete key");
      st.keys = (st.keys || []).filter((k) => Math.abs(k.t - animT) >= 1);
      save();
      renderKeys();
    };
    $("#btn-reset-pose").onclick = () => {
      if (animEd) E.setPose(animEd, {});
    };
    $("#timeline").oninput = () => {
      animT = Number($("#timeline").value) || 0;
      $("#timeline-time").textContent = String(animT);
      const st = stateData();
      if (animEd && st) E.setPose(animEd, C.poseAt(st.keys || [], animT));
      renderKeys();
    };
    $("#clip-length").onchange = () => {
      const st = stateData();
      if (!st) return;
      pushHistory("clip length");
      st.lengthMs = Math.max(100, Number($("#clip-length").value) || 1000);
      $("#timeline").max = String(st.lengthMs);
      save();
      renderKeys();
    };
    $("#clip-name").onchange = () => {
      const st = stateData();
      if (!st) return;
      pushHistory("clip name");
      st.clipName = $("#clip-name").value.trim();
      save();
      renderExport();
    };
    let stateNotesArmed = false;
    $("#state-notes").onfocus = () => {
      stateNotesArmed = true;
    };
    $("#state-notes").oninput = () => {
      const st = stateData();
      if (!st) return;
      if (stateNotesArmed) {
        pushHistory("state notes");
        stateNotesArmed = false;
      }
      st.notes = $("#state-notes").value;
      save();
    };
    $("#anim-loop").onchange = () => {
      const st = stateData();
      if (!st) return;
      pushHistory("loop flag");
      st.loop = $("#anim-loop").checked;
      save();
    };

    $("#btn-anim-play").onclick = () => {
      animPlaying = !animPlaying;
      $("#btn-anim-play").textContent = animPlaying ? "❚❚" : "▶";
      if (animPlaying) {
        lastTs = 0;
        const loop = () => {
          if (!animPlaying) return;
          const st = stateData();
          if (!st) return;
          const now = performance.now();
          if (!lastTs) lastTs = now;
          const dt = now - lastTs;
          lastTs = now;
          animT += dt;
          const len = st.lengthMs || 1000;
          if (animT > len) {
            if ($("#anim-loop").checked || st.loop) animT = 0;
            else {
              animT = len;
              animPlaying = false;
              $("#btn-anim-play").textContent = "▶";
            }
          }
          $("#timeline").value = String(animT);
          $("#timeline-time").textContent = String(Math.round(animT));
          if (animEd) E.setPose(animEd, C.poseAt(st.keys || [], animT));
          animRaf = requestAnimationFrame(loop);
        };
        animRaf = requestAnimationFrame(loop);
      } else cancelAnimationFrame(animRaf);
    };

    // frames
    $("#input-frames").onchange = async () => {
      const char = activeChar();
      const st = stateData();
      if (!char || !st) return;
      pushHistory("add frames");
      for (const f of $("#input-frames").files || []) {
        const id = C.blobKey(char.id, "frame", meta.activeState, C.uid());
        await C.idbPut(id, f);
        st.frameIds.push(id);
      }
      $("#input-frames").value = "";
      save();
      renderFrameStrip();
      drawFrames();
      renderStateList();
    };
    $("#btn-clear-frames").onclick = async () => {
      const st = stateData();
      if (!st?.frameIds?.length) return;
      if (!confirm("Clear flipbook frames?")) return;
      pushHistory("clear frames");
      for (const id of st.frameIds) await C.idbDel(id);
      st.frameIds = [];
      frameIndex = 0;
      save();
      renderFrameStrip();
      drawFrames();
    };
    $("#frame-scrub").oninput = () => {
      frameIndex = Number($("#frame-scrub").value) || 0;
      renderFrameStrip();
      drawFrames();
    };
    $("#frame-onion").onchange = () => drawFrames();
    $("#btn-frame-play").onclick = () => {
      framePlaying = !framePlaying;
      $("#btn-frame-play").textContent = framePlaying ? "❚❚" : "▶";
      const tick = () => {
        if (!framePlaying) return;
        const st = stateData();
        const n = st?.frameIds?.length || 0;
        if (!n) {
          framePlaying = false;
          return;
        }
        frameIndex = (frameIndex + 1) % n;
        if (frameIndex === 0 && !$("#frame-loop").checked) {
          framePlaying = false;
          $("#btn-frame-play").textContent = "▶";
          frameIndex = n - 1;
        }
        renderFrameStrip();
        drawFrames();
        setTimeout(tick, 1000 / (Number($("#frame-fps").value) || 12));
      };
      tick();
    };

    $("#btn-add-part").onclick = () => {
      const char = activeChar();
      const name = $("#part-new").value.trim();
      if (!char || !name) return;
      pushHistory("add part");
      char.parts.push({ name, done: false });
      $("#part-new").value = "";
      save();
      renderParts();
    };

    $("#input-spine").onchange = () => loadSpineFiles($("#input-spine").files);
    $("#btn-spine-clear").onclick = clearSpine;

    $("#btn-export-manifest").onclick = () => exportProject(false);
    $("#btn-export-project").onclick = () => exportProject(true);
    $("#btn-export-skeleton").onclick = () => {
      const char = activeChar();
      if (!char) return;
      downloadJson(C.exportSkeletonJson(char), `${char.id}-skeleton.json`);
    };
    $("#input-import").onchange = async () => {
      const f = $("#input-import").files?.[0];
      if (!f) return;
      try {
        pushHistory("import project");
        await importProject(JSON.parse(await f.text()));
      } catch (err) {
        alert("Import failed: " + err.message);
      }
    };
    $("#btn-seed").onclick = () => {
      pushHistory("seed shells");
      for (const s of [
        { name: "Brennen", id: "brennen", notes: "Tank · greatsword" },
        { name: "Whitney", id: "whitney", notes: "Witch · quill" },
      ]) {
        if (meta.characters.some((c) => c.id === s.id)) continue;
        const ch = C.makeCharacter(s.name, s.id);
        ch.notes = s.notes;
        meta.characters.push(ch);
      }
      if (!meta.activeCharId) meta.activeCharId = meta.characters[0]?.id;
      save();
      refreshAll();
    };

    async function loadStarter(path, label) {
      try {
        const res = await fetch(path, { cache: "no-store" });
        if (!res.ok) throw new Error(`HTTP ${res.status} — is the server running from char-anim-pipeline/?`);
        const data = await res.json();
        // Force single-sprite setup (old local projects stacked ghost+attach).
        if (data.character) {
          data.character.refImageId = null;
          const atts = data.character.attachments || {};
          if (atts.torso) data.character.attachments = { torso: atts.torso };
        }
        await importProject(data);
        if ($("#chk-show-ref")) $("#chk-show-ref").checked = false;
        if (rigEd) rigEd.showRef = false;
        if (animEd) animEd.showRef = false;
        clearHistory();
        E.redraw(rigEd);
        E.redraw(animEd);
        alert(
          `${label} loaded (single body sprite).\n\nIf you still see doubles, click **Fix stacked sprites** on the Rig toolbar.\n\nUndo: Ctrl+Z · Redo: Ctrl+Y`
        );
      } catch (err) {
        console.error(err);
        alert(
          `Could not auto-load ${label}.\n\nUse Import project and pick:\n${path}\n\n(${err.message})`
        );
      }
    }
    $("#btn-load-brennen-starter").onclick = () =>
      loadStarter("starters/brennen-starter-project.json", "Brennen starter");
    $("#btn-load-whitney-starter").onclick = () =>
      loadStarter("starters/whitney-starter-project.json", "Whitney starter");
    $("#btn-copy-codegen").onclick = async () => {
      await navigator.clipboard.writeText($("#codegen").textContent);
      $("#btn-copy-codegen").textContent = "Copied!";
      setTimeout(() => ($("#btn-copy-codegen").textContent = "Copy C#"), 1000);
    };

    window.addEventListener("resize", () => {
      if (rigEd) {
        E.resizeStage(rigEd);
        E.redraw(rigEd);
      }
      if (animEd) {
        E.resizeStage(animEd);
        E.redraw(animEd);
      }
    });
  }

  function initStages() {
    rigEd = E.createStage("konva-host", {
      mode: "rig",
      getChar: activeChar,
      onSelect: (id) => {
        renderBoneInsp(id);
        renderBoneList();
      },
      onBeforeEdit: (label) => pushHistory(label || "rig edit"),
      onChange: () => {
        save();
        renderBoneList();
        if (rigEd?.selectedId) renderBoneInsp(rigEd.selectedId);
      },
    });
    if (rigEd) E.bindStageEvents(rigEd);

    animEd = E.createStage("konva-anim-host", {
      mode: "animate",
      getChar: activeChar,
      onSelect: (id) => {
        renderBoneList();
        renderBoneInsp(id);
      },
      onBeforeEdit: (label) => pushHistory(label || "pose edit"),
      onChange: () => {
        /* live pose; keyframe commits separately */
      },
    });
  }

  // boot
  bind();
  initStages();
  refreshAll();
  updateHistoryButtons();
})();
