# tools

## STS2 vanilla reference dump

Pulls every base character's cards/relics/potions (plus shared content) from the
[Spire Codex](https://spire-codex.com) public API into a **gitignored** tree for
kit balancing:

```bash
python tools/fetch_sts2_reference.py
# → reference/sts2/  (gitignored)
```

Layout:

| Path | Contents |
|------|----------|
| `reference/sts2/README.md` | Index + per-character reward pool counts |
| `reference/sts2/by_character/<id>/` | `character.json`, `cards.json`, `relics.json`, `summary.json`, readable `README.md` |
| `reference/sts2/shared/` | Colorless / curse / status / token / event cards + shared relics/potions |
| `reference/sts2/raw/` | Full API dumps (monsters, events, powers, …) |

Observed vanilla reward shape (C/U/R only): about **20 / 36 / 26** per character —
see `reference/sts2/by_character/*/summary.json` after a fetch. Basics/Ancients are extra.

Re-run after game patches. Data is community-sourced (Spire Codex), not an official MegaCrit export.

## Brennen full kit generator

```bash
python tools/generate_brennen_kit.py
```

Writes reward-card C# under `mods/brennen/BrennenCode/Cards/{Common,Uncommon,Rare}/`,
merges `cards.json` localization, copies portrait placeholders, and refreshes `docs/cards.json`.

Vanilla target: **20 Common / 35 Uncommon / 25 Rare** plus Basic Strike/Defend/Feeding.

Hand-authored keepers (not overwritten): Strike, Defend, Feed, Gank, Flash, Tilt, Ward,
FirstBlood, MainCharacter, MuteAll, Pentakill, AFK, Remake.

## Card catalog pipeline

### Catalog (layered HTML — preferred)

`docs/index.html` renders cards as **layers**:

1. CSS frame chrome (no baked-in text)
2. Portrait art only (`docs/assets/brennen/*.jpg`)
3. Live text: cost, title, type, description

**Fonts (Google Fonts / OFL):**

| Role | Font |
|------|------|
| Title, type pill, energy digit | **Cinzel** |
| Body description | **EB Garamond** |
| Keywords | same body, gold color |
| Numbers | same body, blue color |

**Data:** `docs/cards.json` — single source for catalog text.

```bash
# Serve so fetch('cards.json') works
cd docs && python3 -m http.server 8765
# open http://localhost:8765
```

Opening `index.html` via `file://` may block `fetch`; use the server above.

### Game assets (mod package)

STS2 only loads portraits, not framed cards:

| Asset | Size | Path |
|-------|------|------|
| Portrait big | 1000×760 | `mods/brennen/Brennen/images/card_portraits/big/` |
| Portrait small | 250×190 | `mods/brennen/Brennen/images/card_portraits/` |

Card names/descriptions for the game live in `mods/brennen/Brennen/localization/eng/*.json`.

### Legacy frame extract

Earlier experiments extracted chrome from the Feeding mock into `docs/assets/brennen/frame/`. Useful as style reference only; catalog no longer re-letters those PNGs.
