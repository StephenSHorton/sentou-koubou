# STS2 Character Anim Editor

Local browser **full pipeline editor** for combat characters:

| Mode | What it does |
|------|----------------|
| **Rig** | Bone hierarchy (FK), place/edit bones, attach part images, reference fullbody |
| **Animate** | Keyframe poses per combat state, timeline scrub, playback |
| **Frames** | Flipbook fallback (PNG sequences) when you skip bones |
| **Spine preview** | Official **Spine Player** for real `.json`/`.skel` + `.atlas` exports |
| **Export** | Manifest / full project / skeleton JSON + BaseLib C# snippet |

## Libraries we use

| Library | Role | License note |
|---------|------|----------------|
| **[Konva](https://konvajs.org/)** (`unpkg.com/konva`) | Interactive canvas, bone handles, drag | MIT |
| **[@esotericsoftware/spine-player](https://en.esotericsoftware.com/spine-player)** 4.2.x | Play real Spine exports in-browser | Runtime free; **Spine Editor** is paid for authoring |
| **Our FK core** (`core.js`) | Local-angle skeleton + keyframe interpolation | Project code |
| *(optional later)* **spine-webgl / pixi-spine** | Deeper Spine tooling if we need mesh edit | Runtime free |

### Why not “only Spine Editor”?

- Spine Editor is still the **pro authoring** path that matches STS2 (MegaSpine).
- This tool lets you **block out rigs, states, and timing without the game**, and **preview Spine exports** when you have them.
- Flipbook + bone keyframes cover the path before a paid Spine license / rigger is available.

### Other ecosystem options (not embedded)

| Tool | Notes |
|------|--------|
| **DragonBones / LoongBones** | Free-ish alternative; JS runtimes stale; not STS2-native |
| **Synfig** | FOSS 2D animation; different export path |
| **Godot Skeleton2D** | Free bones inside Godot; BaseLib still prefers Spine for combat |
| **CustomSkeletonLoader** (Nexus) | Runtime swap of Spine assets in STS2 |

## Run

CDN scripts need network once. Prefer a tiny local server (avoids some browser `file://` limits):

```powershell
cd tools/char-anim-pipeline
python -m http.server 8765
# open http://localhost:8765/
```

Or double-click `index.html` (works in Chrome/Edge if CDN is allowed).

## Workflow

1. **Seed Brennen / Whitney** (or create character).
2. **Rig** — default humanoid bones; drag tips to pose; attach cut-out part PNGs; set ref fullbody.
3. **Animate** — pick state (Idle/Attack/…); scrub timeline; pose; **Key pose**; play.
4. **Frames** (optional) — drop flipbook PNGs per state.
5. **Spine preview** — load export set from Spine Editor when ready.
6. **Export** — skeleton JSON + C# `SetupAnimationState` wiring for BaseLib.

## Combat states (BaseLib)

`idle`, `attack`, `cast`, `hit`, `dead`, `relaxed`, `revive`  
Mapped via `SetupAnimationState` (clip names editable per state).

## Data storage

- **localStorage** — project metadata  
- **IndexedDB** — images  
- **Export full project** for backup/share  

## Files

```
index.html    UI shell + CDN tags
styles.css
core.js       Model, IDB, FK, export skeleton
editor.js     Konva stages
app.js        UI glue, flipbook, Spine player, IO
README.md
```

## Honest scope

This is a **production bone/keyframe editor + Spine player**, not a 1:1 clone of Spine Pro (no mesh weights, IK solvers, or constraints yet). Next upgrades if you need them: simple 2-bone IK, mesh deformation, and Spine JSON export that opens in Spine Editor.
