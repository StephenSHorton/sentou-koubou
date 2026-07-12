# Whitney — Atelier Witch (char 2)

Playable character for **sentou-koubou**. Kit architecture adapted from **MarisaMod**
(Amplify / Inkbound / Saturate), rethemed to atelier ink + violet chrome.

> Mechanics source: local `STS2_MarisaMod` (Flynn, Hell, Hohner_257, Kishin, Samsara).
> Art is temporary placeholders until Whitney-generated portraits ship.

## Kit (v0.2 rearch)

| Piece | Detail |
|-------|--------|
| HP | 75 |
| Starter relic | **Mini Hakkero** (Marisa starter — rename/reart later) |
| Starting deck | 4× Spark Strike, 4× Defend, Master Spark, Up Sweep |
| Pillars | **Amplify** (energy kicker), **Inkbound** (was Starlit), **Saturate** (was Charge-Up) |
| Compendium | Pool `SeenByDefault` |
| Combat body | Whitney Blender flipbook (`WhitneyCombatVisuals`) |
| Color | Indigo / violet `#4B3F8C` (D3 clothing lock) |

### Theme mapping

| Marisa | Whitney |
|--------|---------|
| Starlit | **Inkbound** |
| Charge-Up | **Saturate** (loc; class still `ChargeUpPower`) |
| Amplify | Amplify (kept) |
| Spark cards | Spark (ink sparks) |
| Blue frames | Violet energy orbs + WTN frames (recolor pass pending) |

## Build

```bash
# From mods/whitney (worktree: sentou-koubou-whitney-marisa)
dotnet build -c Release
```

Requires BaseLib + STS2. Quit STS2 before build (DLL lock).

PckPacker packs image assets under `Whitney/`. Godot `.tscn` scenes from Marisa live in
`_marisa_scenes_unused/` until a MegaDot export is wired; combat uses flipbook, not spine.

## Art TODO

- Full card portrait pass (STS2 graphic, D3 Whitney lock) — replace `Whitney/images/cards/whitney-*.png`
- Violet recolor of `images/ui/bg_*_WTN.png` frames
- Power/relic cutouts in ink palette
- Starter relic rename + art (Inkpot / quill fantasy)
- Catalog: regenerate `docs/whitney-cards.json` from new kit

## Design note

See `docs/whitney-marisa-rearch.md`.
