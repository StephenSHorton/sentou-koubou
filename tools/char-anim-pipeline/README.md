# STS2 Character Anim Pipeline

Tools for authoring **combat character animation** for sentou-koubou mods (Brennen / Whitney / …).

| Piece | Path | Role |
|-------|------|------|
| **Blender rig** | `blender/brennen_combat_rig.blend` | Source of truth for mesh, weights, actions |
| **Export package** | `export/brennen/` | glTF + PNG flipbooks + `manifest.json` |
| **Full pipeline guide** | **[BLENDER_PIPELINE.md](./BLENDER_PIPELINE.md)** | How to set up a character end-to-end |
| **Browser editor** | `index.html` | Optional bone/keyframe blockout + Spine preview |

## Quick start (Brennen — already wired)

1. Author / tweak in Blender → save `blender/brennen_combat_rig.blend`
2. Export frames (see pipeline doc) into `export/brennen/frames/`
3. Copy frames → `mods/brennen/Brennen/images/combat/{idle,attack,hit,dead}/`
4. Runtime: `Brennen.CreateCustomVisuals()` → `BrennenCombatVisuals` (AnimatedSprite2D)
5. Build the Brennen mod (`.dll` + `.pck`)

## Browser editor (optional)

CDN scripts need network once:

```powershell
cd tools/char-anim-pipeline
python -m http.server 8765
# open http://localhost:8765/
```

Useful for flipbook review and non-Blender blockouts. **Weighted paper-doll combat** is authored in Blender (see pipeline doc).

## Combat states (BaseLib)

`idle`, `attack`, `cast`, `hit`, `dead`, `relaxed`, `revive`

Brennen flipbook currently authors **idle / attack / hit / dead**; cast→attack, relaxed/revive→idle until dedicated clips exist.

## Honest scope

- **Now:** Blender deform + PNG flipbook in combat (not Ironclad).
- **Later:** Spine / MegaSpine for native STS2 fidelity.
- Browser tool is **not** a Spine Pro replacement.
