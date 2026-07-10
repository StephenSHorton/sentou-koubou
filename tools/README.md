# tools

## Brennen card frame compositor

The Feeding mock’s chrome was extracted to:

- `docs/assets/brennen/frame/shell-clean.png` — full card shell (1032×1523) with art hole + cleared text
- `docs/assets/brennen/frame/layout.json` — art box, sizes, game export specs
- `docs/assets/brennen/cards/*.jpg` — catalog previews (framed, as if in-game)
- `docs/assets/brennen/game-portraits/*.png` — game-ready portraits

### Game vs catalog

| Asset | Size | Used by |
|-------|------|---------|
| Portrait big | **1000×760** | STS2 / BaseLib `card_portraits/big/` |
| Portrait small | **250×190** | STS2 / BaseLib `card_portraits/` |
| Art panel (shell hole) | **~796×795** | Catalog frame composite only |
| Framed preview | **1032×1523** | `docs/index.html` only — **not** loaded by the game |

The game draws its own card UI around the portrait. We only ship PNG portraits in the mod.

### Regen

Use the project venv and re-run the compose pipeline from a prior session, or ask the agent to recompose after new art lands.
