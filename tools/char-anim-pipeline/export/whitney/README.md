# Whitney combat export

Flipbook combat body from `tools/char-anim-pipeline/blender/whitney_combat_rig.blend`.

## Clips

| State | Frames | Loop | Notes |
|-------|--------|------|-------|
| idle | 1–48 | yes | Soft breath + knee; seamless loop |
| attack | 1–20 | no | Restrained cast flourish |
| hit | 1–10 | no | Head/neck flinch only; feet pinned |
| dead | 1–24 | no | Gentle collapse |

Cast → attack; relaxed/revive → idle until dedicated clips exist.

## Ship path

```
export/whitney/frames/{idle,attack,hit,dead}/*.png
  → mods/whitney/Whitney/images/combat/{idle,attack,hit,dead}/
```

Runtime: `Whitney.CreateCustomVisuals()` → `WhitneyCombatVisuals` (`AnimatedSprite2D`).
