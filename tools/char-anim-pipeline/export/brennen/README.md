# Brennen combat export

## Contents

| Path | What |
|------|------|
| `gltf/brennen_combat.glb` | Mesh + armature + actions (tooling / Godot) |
| `frames/<state>/*.png` | Transparent PNG flipbook per state (512×682 @ 24fps) |
| `manifest.json` | Frame ranges, fps, BaseLib state map |
| Source `.blend` | `../blender/brennen_combat_rig.blend` |

## States

| State | Frames | Loop |
|-------|--------|------|
| idle | 1–48 | yes (~2s) |
| attack | 1–20 | no |
| hit | 1–12 | no |
| dead | 1–20 | no |

`cast` → attack; `relaxed` / `revive` → idle (until dedicated clips exist).

## In-game wiring (done)

1. Frames live in the mod at:  
   `mods/brennen/Brennen/images/combat/{idle,attack,hit,dead}/`
2. `Brennen.CreateCustomVisuals()` → `BrennenCombatVisuals.Create()`  
   builds `NCreatureVisuals` + `AnimatedSprite2D` from those PNGs.
3. Build the Brennen project to refresh `.dll` + `.pck`.

Full authoring guide: **[../../BLENDER_PIPELINE.md](../../BLENDER_PIPELINE.md)**
