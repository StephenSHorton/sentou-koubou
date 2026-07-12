# Brennen combat animation pipeline (Blender → STS2)

End-to-end guide for authoring a character combat body in **Blender**, exporting it, and wiring it into a BaseLib character mod so combat no longer falls back to Ironclad.

This documents what we learned building **Brennen**’s first deform-rig + flipbook combat visual.

---

## Goals and layers of animation

| Layer | What it is | STS2 fit |
|-------|------------|----------|
| **UI portraits** | Char select, map marker, energy icons | Separate PNGs under `images/charui/` — not this pipeline |
| **Combat body** | Creature that plays idle / attack / hit / die in fights | This pipeline |
| **Rest / merchant** | Optional sit / shop poses | Same pattern later (`CustomRestSiteAnimPath`, `CustomMerchantAnimPath`) |

BaseLib combat hooks (on `CustomCharacterModel` / `PlaceholderCharacterModel`):

- **`CreateCustomVisuals()`** — build `NCreatureVisuals` in code (what Brennen uses now)
- **`CustomVisualPath`** — path to a Godot `.tscn` under `res://scenes/creature_visuals/…`
- **`SetupCustomAnimationStates(MegaSprite)`** — Spine / MegaSpine clip remaps via `SetupAnimationState(...)`
- Rest/merchant paths — separate scenes or sprites

**Three animation tech levels:**

1. **Static sprite** — `NodeFactory<NCreatureVisuals>.CreateFromResource(texture)` (one pose forever)
2. **Godot flipbook / AnimationPlayer** — `AnimatedSprite2D` or `AnimationPlayer` under visuals; BaseLib `CustomAnimation.PlayCustomAnimation` finds them
3. **Spine / MegaSprite** — native STS2 path; `.skel`/`.json` + `.atlas` + PNG; use `SetupAnimationState` for missing clips

This repo’s current Brennen combat path is **(2) flipbook** exported from a Blender armature deform setup. Spine remains the long-term native target.

---

## Prerequisites

- **Blender 5.x** (project used 5.1) with the **Lab MCP add-on** (optional but how the agent drives the scene)
- Combat source art: transparent fullbody PNG (Brennen: `docs/assets/brennen/variants/brennen_combat_right.png`)
- **STS2 + BaseLib** character mod (Brennen extends `PlaceholderCharacterModel`)
- For export renders: EEVEE, transparent film, orthographic camera

---

## Source of truth files

| Path | Role |
|------|------|
| `tools/char-anim-pipeline/blender/brennen_combat_rig.blend` | Authoring scene (mesh, armature, weights, actions) |
| `tools/char-anim-pipeline/export/brennen/` | glTF + PNG frames + `manifest.json` |
| `mods/brennen/Brennen/images/combat/{idle,attack,hit,dead}/` | Frames packed into the mod `.pck` |
| `mods/brennen/BrennenCode/Character/BrennenCombatVisuals.cs` | Builds `AnimatedSprite2D` visuals at runtime |
| `mods/brennen/BrennenCode/Character/Brennen.cs` | `CreateCustomVisuals()` override |

---

## Pipeline stages

```
Art PNG (alpha)
    → Blender plane + humanoid armature
    → Weight paint (deform-only mesh)
    → Actions: idle / attack / hit / dead
    → Export: .blend save + glTF + PNG flipbooks
    → Copy frames into mod assets
    → CreateCustomVisuals() → AnimatedSprite2D
    → Build .dll + pack .pck → play combat
```

### 1. Scene setup (once per character)

1. Create a vertical **image plane** (Brennen: X ≈ −0.75…0.75, Z ≈ 0…2, Y = 0) with the combat PNG and **alpha** material.
2. Build a **humanoid armature** (minimum useful set):
   - `root` → `hip` → `torso` → `chest` → `neck` → `head`
   - arms: `shoulder_*` → `upper_arm_*` → `forearm_*` → `hand_*`
   - legs: `thigh_*` → `shin_*` → `foot_*`
   - **`weapon`** parented to the hand that holds the weapon
3. Subdivide the plane until silhouette edges (sword fire, limbs) have enough verts for clean weights (Brennen ended ~50k verts).
4. **Armature modifier** + vertex groups named like bones.
5. **Facing convention (this rig):** up = +Z; default front look from −Y; art faces +X (sword side).

### 2. Weight painting rules (hard-won)

**Body first:** re-run **Automatic Weights** when body deformations look broken (rips through chest/thigh after experimental sword paint). Smooth multi-bone body weights matter more than a slightly incomplete sword.

**Sword second (deform-only paper doll):**

| Do | Don’t |
|----|--------|
| Assign **outer blade** (side **away from torso**) to **100% `weapon`** | Use geometric “corridor along bone” alone — it eats torso/face |
| Grow from **fire / steel pixels** in UV | Trust auto “skin” color on outer fire (false positives → verts stuck on arm bones) |
| Hard-ban face UV, head sphere, thighs unless clearly hilt | Leave outer mid-blade on `upper_arm_r` (causes outer-edge **rip**) |
| Keep grip mostly `weapon` with tiny hand mix only at the pommel | Scale the weapon bone in **Pose Mode** to “fit” art (destroys mesh until cleared) |

**Bone placement:** edit **`weapon` head/tail in Edit Mode** (optionally disable Armature viewport deform while aligning). Pose Mode is animation only.

### 3. Animation actions

| Action | Frames (Brennen) | Notes |
|--------|------------------|--------|
| `idle` | 1–48 (~2s @ 24fps) | Soft knee bend + torso breath; loop |
| `attack` | 1–20 | One-shot |
| `hit` | 1–12 | One-shot |
| `dead` | 1–20 | One-shot; `DeathAnimTime` ≈ 1.0s |

BaseLib states to cover eventually: **idle, attack, cast, hit, dead, relaxed, revive**.  
Brennen maps cast→attack and relaxed/revive→idle until dedicated clips exist.

### 4. Export from Blender

From the authoring scene (scripted or manual):

1. **Save** `.blend` (and optional dated snapshot).
2. **glTF** (`export/brennen/gltf/brennen_combat.glb`) — mesh + armature + actions for tooling / Godot.
3. **PNG flipbooks** (`export/brennen/frames/<state>/`):
   - Ortho camera from −Y, transparent background, EEVEE
   - Emission-ish material so lighting isn’t required
   - 512×682 RGBA @ 24 fps (adjust per character)

`export/brennen/manifest.json` records ranges and state aliases.

Re-render recipe (high level): set action on armature → `frame_set` → `render.render(write_still=True)` per frame.

### 5. Ship into the mod

1. Copy  
   `export/brennen/frames/{idle,attack,hit,dead}/*.png`  
   →  
   `mods/brennen/Brennen/images/combat/{idle,attack,hit,dead}/`
2. Ensure PckPacker / Godot export includes `Brennen/**` (already via project layout).
3. Implement **`CreateCustomVisuals()`** (see `BrennenCombatVisuals.cs`):
   - Build `SpriteFrames` from those paths via `PreloadManager` / `ResourceLoader`
   - Parent `AnimatedSprite2D` as unique `%Visuals` under `NCreatureVisuals`
   - Provide `Bounds`, `CenterPos`, `IntentPos`, etc. (BaseLib factory expectations)
4. Register animation name aliases (`idle`/`Idle`, `attack`/`Attack`, …) so BaseLib’s `CustomAnimation` lookup hits.
5. Build mod (`.dll` + `.pck`) and run a combat encounter.

### 6. Verify in-game

| Check | Expected |
|-------|----------|
| Enter combat as Brennen | Custom body, not Ironclad |
| Idle | Soft knee + breath loop |
| Play an attack card | Attack flipbook, then back to idle |
| Take damage | Hit clip |
| Die | Dead clip (~1s) |

If still Ironclad: confirm `CreateCustomVisuals` override is present, frames exist in the packed `.pck`, and paths match `res://Brennen/images/combat/...`.

---

## Common failure modes

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| Ironclad in combat | No custom visuals | `CreateCustomVisuals` / `CustomVisualPath` |
| Outer sword tears, core moves | Outer verts still on arm/body groups | Pure `weapon` on outer side of bone; fix false skin detection |
| Chest/thigh diagonal rip | Body verts on `weapon` | Strip weapon from body; re-auto-weight body if needed |
| Sword “destroyed” at rest | Weapon bone scaled/rotated in pose or idle keys | Clear pose; zero weapon scale keys |
| Black PNG exports | Bad camera / no emission / no light | Ortho front camera + transparent film + textured emission |
| Frames exist but no motion | Wrong action assigned when rendering | Set `animation_data.action` per export |
| Death cut short | `DeathAnimTime` too low | Raise override (~ frame_count / fps) |

---

## Checklist: new character (Whitney, etc.)

1. [ ] Combat fullbody PNG (alpha), facing convention agreed  
2. [ ] New `.blend` from Brennen template or scratch  
3. [ ] Armature + subdiv + body auto-weights  
4. [ ] Weapon / prop bone + careful outer-blade pure weights  
5. [ ] Actions: idle (loop), attack, hit, dead (cast/relaxed optional)  
6. [ ] Export glTF + PNG frames + update `manifest.json`  
7. [ ] Copy frames into `mods/<char>/…/images/combat/`  
8. [ ] `CreateCustomVisuals` (or scene) + scale/bounds tune  
9. [ ] Build pack, smoke-test combat  
10. [ ] Later: Spine export if you need MegaSpine parity  

---

## Related tools

- **Browser bone/keyframe editor:** `tools/char-anim-pipeline/index.html` (blockouts / flipbook UI; not a substitute for Blender deform weights)
- **Char select splash inject:** `tools/inject_char_select_bg.py` (PckPacker cannot ship some `.tscn` paths)
- **MCP:** Blender Lab MCP for agent-driven weight/animation edits (`127.0.0.1:9876`)

---

## Honest limits

- Flipbook is **interim**. STS2’s polished path is **Spine**.
- Paper-doll deform cannot match multi-mesh Spine polish (overlaps, self-occlusion).
- Rest site / merchant still Ironclad until those paths are customized.
- Attack/hit/dead clips may need more authoring polish than idle.
