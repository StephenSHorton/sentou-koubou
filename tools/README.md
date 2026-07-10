# tools

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
