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

