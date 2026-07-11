/**
 * Konva rig + animate stages.
 * Global: window.AnimEditor
 */
(function (global) {
  const C = () => global.AnimCore;
  const imageCache = new Map();

  async function loadImg(id) {
    if (!id) return null;
    if (imageCache.has(id)) return imageCache.get(id);
    const blob = await C().idbGet(id);
    if (!blob) return null;
    const url = URL.createObjectURL(blob);
    const img = await new Promise((res, rej) => {
      const i = new Image();
      i.onload = () => res(i);
      i.onerror = rej;
      i.src = url;
    });
    imageCache.set(id, img);
    return img;
  }

  function clearImageCache() {
    imageCache.clear();
  }

  function createStage(hostId, opts = {}) {
    const host = document.getElementById(hostId);
    if (!host) return null;
    host.innerHTML = "";
    const w = host.clientWidth || 720;
    const h = Math.min(host.clientHeight || 640, 720) || 640;
    const stage = new Konva.Stage({ container: hostId, width: w, height: h });
    const layerBg = new Konva.Layer();
    const layerImg = new Konva.Layer();
    const layerBones = new Konva.Layer();
    stage.add(layerBg);
    stage.add(layerImg);
    stage.add(layerBones);

    // checker bg
    const bg = new Konva.Rect({
      x: 0,
      y: 0,
      width: w,
      height: h,
      fill: "#0c0e14",
    });
    layerBg.add(bg);
    // ground line
    layerBg.add(
      new Konva.Line({
        points: [w * 0.12, h * 0.9, w * 0.88, h * 0.9],
        stroke: "rgba(232,93,76,0.35)",
        strokeWidth: 2,
      })
    );

    return {
      stage,
      layerBg,
      layerImg,
      layerBones,
      width: w,
      height: h,
      mode: opts.mode || "rig", // rig | animate
      tool: "select",
      selectedId: null,
      showBones: true,
      showAttach: true,
      showRef: false, // ghost fullbody guide — off by default (starter used to stack it on the attachment)
      pose: {}, // temporary pose override for animate scrub
      onSelect: opts.onSelect || (() => {}),
      onChange: opts.onChange || (() => {}),
      onBeforeEdit: opts.onBeforeEdit || (() => {}),
      getChar: opts.getChar || (() => null),
    };
  }

  function resizeStage(ed) {
    if (!ed) return;
    const host = ed.stage.container();
    const w = host.clientWidth || 720;
    const h = Math.min(640, Math.max(420, window.innerHeight - 220));
    ed.stage.width(w);
    ed.stage.height(h);
    ed.width = w;
    ed.height = h;
    ed.layerBg.find("Rect")[0]?.size({ width: w, height: h });
  }

  function worldBBox(world) {
    let minX = Infinity;
    let maxX = -Infinity;
    let minY = Infinity;
    let maxY = -Infinity;
    for (const w of Object.values(world)) {
      minX = Math.min(minX, w.x, w.tipX);
      maxX = Math.max(maxX, w.x, w.tipX);
      minY = Math.min(minY, w.y, w.tipY);
      maxY = Math.max(maxY, w.y, w.tipY);
    }
    if (!Number.isFinite(minX)) {
      return { minX: 0, maxX: 0, minY: 0, maxY: 0, cx: 0, cy: 0, bottom: 0 };
    }
    return {
      minX,
      maxX,
      minY,
      maxY,
      cx: (minX + maxX) / 2,
      cy: (minY + maxY) / 2,
      bottom: maxY,
    };
  }

  async function redraw(ed) {
    if (!ed) return;
    const char = ed.getChar();
    ed.layerImg.destroyChildren();
    ed.layerBones.destroyChildren();
    ed.layerImg.position({ x: 0, y: 0 });
    ed.layerBones.position({ x: 0, y: 0 });
    ed.viewShift = { x: 0, y: 0 };
    if (!char) {
      ed.stage.draw();
      return;
    }

    const pose = ed.mode === "animate" ? ed.pose : {};
    const world = C().computeWorld(char.bones, pose);

    // Keep rig + sprites centered on the stage (bone setup is in its own space;
    // the panel is often wider than the 720-ish coords used when the starter was built).
    const bb = worldBBox(world);
    const shiftX = ed.width / 2 - bb.cx;
    const shiftY = ed.height * 0.9 - bb.bottom;
    ed.viewShift = { x: shiftX, y: shiftY };
    ed.layerImg.position({ x: shiftX, y: shiftY });
    ed.layerBones.position({ x: shiftX, y: shiftY });

    // Ghost reference — alignment guide only (not part of the rig). Not draggable.
    if (char.refImageId && ed.showRef) {
      const ref = await loadImg(char.refImageId);
      if (ref) {
        const maxH = Math.min(ed.height * 0.85, Math.max(80, bb.maxY - bb.minY) * 1.15 || ed.height * 0.85);
        const scale = maxH / ref.height;
        const dw = ref.width * scale;
        const dh = ref.height * scale;
        ed.layerImg.add(
          new Konva.Image({
            image: ref,
            x: bb.cx - dw / 2,
            y: bb.bottom - dh,
            width: dw,
            height: dh,
            opacity: 0.22,
            listening: false,
            name: "ghost-ref",
          })
        );
      }
    }

    // Bone attachments — paper-doll pieces parented to bones (draggable in rig mode).
    if (ed.showAttach) {
      for (const b of char.bones) {
        const att = char.attachments[b.id];
        if (!att?.imageId) continue;
        const w = world[b.id];
        if (!w) continue;
        const img = await loadImg(att.imageId);
        if (!img) continue;
        const scale = att.scale ?? 0.35;
        const ox = att.offsetX ?? 0;
        const oy = att.offsetY ?? 0;
        // Art is authored upright (head up). Bones use local +X along the bone;
        // rest spine points world -PI/2 (up). Map so upright bone => 0° image,
        // and the sprite still tilts when the bone rotates.
        const rotRad = (att.rotation ?? 0) + w.worldAngle + Math.PI / 2;
        const rot = (rotRad * 180) / Math.PI;
        const node = new Konva.Image({
          image: img,
          x: w.x + ox,
          y: w.y + oy,
          width: img.width * scale,
          height: img.height * scale,
          offsetX: (img.width * scale) / 2,
          offsetY: (img.height * scale) / 2,
          rotation: rot,
          opacity: 0.95,
          listening: ed.mode === "rig",
          name: "att-" + b.id,
        });
        if (ed.mode === "rig") {
          node.draggable(true);
          node.on("dragstart", () => ed.onBeforeEdit("move attachment"));
          node.on("dragend", () => {
            const a = char.attachments[b.id];
            if (!a) return;
            a.offsetX = node.x() - w.x;
            a.offsetY = node.y() - w.y;
            ed.onChange();
          });
        }
        ed.layerImg.add(node);
      }
    }

    // bones
    if (ed.showBones) {
      for (const b of char.bones) {
        const w = world[b.id];
        if (!w) continue;
        const selected = b.id === ed.selectedId;
        const line = new Konva.Line({
          points: [w.x, w.y, w.tipX, w.tipY],
          stroke: selected ? "#e85d4c" : "#7a9e8a",
          strokeWidth: selected ? 4 : 3,
          lineCap: "round",
          listening: false,
        });
        ed.layerBones.add(line);

        // joint
        const joint = new Konva.Circle({
          x: w.x,
          y: w.y,
          radius: selected ? 8 : 6,
          fill: selected ? "#e85d4c" : "#c8d0e0",
          stroke: "#0f1116",
          strokeWidth: 1,
          draggable: ed.mode === "rig" && ed.tool === "select" && !b.parent,
          name: "joint-" + b.id,
        });
        joint.on("mousedown touchstart", (e) => {
          e.cancelBubble = true;
          ed.selectedId = b.id;
          ed.onSelect(b.id);
          redraw(ed);
        });
        if (!b.parent && ed.mode === "rig") {
          joint.on("dragstart", () => ed.onBeforeEdit("move root"));
          joint.on("dragmove", () => {
            b.setupX = joint.x();
            b.setupY = joint.y();
            C().bakeSetupFromAngles(char.bones);
            redraw(ed);
          });
          joint.on("dragend", () => ed.onChange());
        }
        ed.layerBones.add(joint);

        // tip handle
        const tip = new Konva.Circle({
          x: w.tipX,
          y: w.tipY,
          radius: selected ? 7 : 5,
          fill: selected ? "#ffb4a8" : "#5a8f7b",
          stroke: "#0f1116",
          strokeWidth: 1,
          draggable: ed.tool === "select" || ed.mode === "animate",
          name: "tip-" + b.id,
        });
        tip.on("mousedown touchstart", (e) => {
          e.cancelBubble = true;
          ed.selectedId = b.id;
          ed.onSelect(b.id);
        });
        tip.on("dragstart", () =>
          ed.onBeforeEdit(ed.mode === "animate" ? "pose bone" : "edit bone")
        );
        tip.on("dragmove", () => {
          const parentWorld = b.parent ? world[b.parent] : null;
          const ox = parentWorld ? parentWorld.tipX : b.setupX;
          const oy = parentWorld ? parentWorld.tipY : b.setupY;
          // For root, joint is setupX/Y; tip drag sets world angle & length
          const jx = b.parent ? parentWorld.tipX : (world[b.id]?.x ?? b.setupX);
          const jy = b.parent ? parentWorld.tipY : (world[b.id]?.y ?? b.setupY);
          // re-read joint from live world of parent only
          let jx2, jy2, parentAng;
          if (!b.parent) {
            jx2 = b.setupX;
            jy2 = b.setupY;
            parentAng = 0;
          } else {
            // use current bone joint = parent tip from pose world
            const pw = C().computeWorld(char.bones, ed.mode === "animate" ? ed.pose : {})[b.parent];
            jx2 = pw.tipX;
            jy2 = pw.tipY;
            parentAng = pw.worldAngle;
          }
          const dx = tip.x() - jx2;
          const dy = tip.y() - jy2;
          const worldAng = Math.atan2(dy, dx);
          const len = Math.max(8, Math.hypot(dx, dy));
          const local = worldAng - parentAng;

          if (ed.mode === "animate") {
            if (!ed.pose[b.id]) ed.pose[b.id] = {};
            ed.pose[b.id].angle = local;
            ed.pose[b.id].length = len;
          } else {
            b.angle = local;
            b.length = len;
            C().bakeSetupFromAngles(char.bones);
          }
          redraw(ed);
        });
        tip.on("dragend", () => ed.onChange());
        ed.layerBones.add(tip);

        // label
        ed.layerBones.add(
          new Konva.Text({
            x: w.x + 8,
            y: w.y - 14,
            text: b.name,
            fontSize: 11,
            fontFamily: "Segoe UI",
            fill: selected ? "#e85d4c" : "rgba(200,208,224,0.7)",
            listening: false,
          })
        );
      }
    }

    ed.stage.draw();
  }

  function bindStageEvents(ed) {
    ed.stage.on("click tap", (e) => {
      if (ed.tool !== "bone" || ed.mode !== "rig") {
        if (e.target === ed.stage || e.target.getLayer() === ed.layerBg) {
          // deselect if clicking empty
        }
        return;
      }
      const char = ed.getChar();
      if (!char) return;
      // Layer is view-shifted for centering; work in bone/world space.
      const pos =
        ed.layerBones.getRelativePointerPosition() ||
        (() => {
          const p = ed.stage.getPointerPosition();
          if (!p) return null;
          return { x: p.x - (ed.viewShift?.x || 0), y: p.y - (ed.viewShift?.y || 0) };
        })();
      if (!pos) return;

      ed.onBeforeEdit("add bone");
      const parentId = ed.selectedId || (char.bones.find((b) => !b.parent)?.id ?? null);
      const world = C().computeWorld(char.bones, {});
      let jx = pos.x;
      let jy = pos.y;
      let parentAng = 0;
      if (parentId && world[parentId]) {
        jx = world[parentId].tipX;
        jy = world[parentId].tipY;
        parentAng = world[parentId].worldAngle;
      }
      const dx = pos.x - jx;
      const dy = pos.y - jy;
      const worldAng = Math.atan2(dy, dx);
      const len = Math.max(12, Math.hypot(dx, dy));
      const local = worldAng - parentAng;
      const id = "bone_" + C().uid();
      char.bones.push({
        id,
        name: id,
        parent: parentId && world[parentId] ? parentId : null,
        length: len,
        angle: parentId && world[parentId] ? local : worldAng,
        setupX: parentId ? 0 : pos.x,
        setupY: parentId ? 0 : pos.y,
      });
      // if no parent, place root-like at click
      if (!parentId || !world[parentId]) {
        const b = char.bones[char.bones.length - 1];
        b.parent = null;
        b.setupX = pos.x;
        b.setupY = pos.y;
        b.angle = -Math.PI / 2;
        b.length = 40;
      }
      C().bakeSetupFromAngles(char.bones);
      ed.selectedId = id;
      ed.onSelect(id);
      ed.onChange();
      redraw(ed);
    });
  }

  function setPose(ed, pose) {
    ed.pose = pose || {};
    redraw(ed);
  }

  function getWorkingPose(ed) {
    const char = ed.getChar();
    if (!char) return {};
    // merge bone rest with pose overrides
    const base = C().capturePose(char.bones);
    return { ...base, ...ed.pose };
  }

  global.AnimEditor = {
    createStage,
    resizeStage,
    redraw,
    bindStageEvents,
    setPose,
    getWorkingPose,
    loadImg,
    clearImageCache,
  };
})(window);
