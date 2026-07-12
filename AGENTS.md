# sentou-koubou agent notes

Monorepo of Slay the Spire 2 mods under `mods/`.

## Conventions

- One folder per mod: `mods/<mod-id>/` with matching `ModId`, `.csproj`, `.json` manifest, and asset root folder.
- Prefer **BaseLib** (`CustomCharacterModel` / `CustomCardModel` / etc.) over raw Harmony for content.
- Character mods: start from Alchyr’s Character template patterns; do not fork full character mods as bases.
- Local paths live in gitignored `Directory.Build.props` (copy from `*.example`).
- Publish regenerates `.pck` (assets/loc); Build alone is fine for C#-only changes.
- Game is Early Access — expect BaseLib/game bumps to break mods; pin `min_game_version` and BaseLib `min_version` in manifests.

## Mods

- `mods/henro` — The Pilgrim vertical slice (Strike, Defend, Pilgrim Beads).
- `mods/brennen` — Full character kit (vanilla 20/35/25 reward pool). Regen generated cards with `python tools/generate_brennen_kit.py`.

- `mods/whitney` — Whitney (atelier witch): Energy + Ink, four elements, dual-purpose seals.

## Card / character art pipeline

**Full write-up (what worked, what failed, opinions):** see `AGENTS.md` on `brennen/kit-pass` (or merge that section into monorepo `main` when landing). Do not invent a new art process.

### Whitney locks (current)

| Role | Path |
|------|------|
| Identity | `docs/assets/whitney/variants/whitney_locked_d3.jpg` (or kitpass catalog copy) |
| Ready combat | `whitney_combat_right.png` — ready stance, 3/4 face, faces right |
| Header | `portrait_sts2.jpg` from D3 lock |

### Hard rules (summary)

1. Lock character base with user-facing variants page **before** mass card gen.
2. STS2 bold painted graphic — not anime, not photoreal; use `image_edit` from locked base for likeness.
3. Unique composition per card; audit near-duplicates (same pose / zoom-only).
4. Relics = transparent cutouts; green screen only for cutouts, never on card portraits.
5. Install via `tools/install_sts2_art.py`; ship art with **PckPacker** `.pck` + `dotnet build` into STS2 `Mods/`.

### Opinions

- Composition is content — same pose + different prop still feels lazy.
- Still-lifes with shared props are good kit glue.
- Header portraits need a real half-body hero shot, not a combat-sprite crop.
- Budget a re-roll pass after first full gen; user taste overrides model defaults.

