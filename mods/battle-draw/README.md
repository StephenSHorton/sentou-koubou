# Battle Draw

Combat whiteboard + map pen controls for Slay the Spire 2.

## Drawing dock (v1.0)

UI is modeled after **Excalidraw / FigJam / Canva Draw** rather than a flat list of text buttons:

| Piece | Behavior |
|-------|----------|
| **Collapsed pill** | Shows armed tool glyph, ink color, size — one click opens the dock |
| **Icon tools** | Line · Rect · Oval · Stamp · Bucket (selected = gold rim) |
| **Fill shapes (F)** | Toggle: Rect/Oval become solid instead of separate “filled” tools |
| **Quick swatches** | One-tap palette + full color picker |
| **Width** | Thin ↔ thick slider (`[` `]`) |
| **Clear / hide peers** | Session actions on the tool row |

### Inputs

| Input | Action |
|-------|--------|
| **RMB drag** | Freehand pen (always) |
| **MMB drag** | Erase (yours **and** teammates’ ink) |
| **LMB** | Armed tool (line, shape, stamp, bucket) |
| **G** | Bucket fill (closed ink regions only) |
| **F** | Toggle fill-shapes mode |
| **B / L / R / O** | Brush / line / rect / oval |
| **`[` `]`** | Size |

Bucket fill never paints open canvas — only regions fully enclosed by ink.

## Combat ink

Half-res SubViewport + map `Line2D` pen/eraser (subtractive erase), composited **under** the hand/cards/menus.

## Multiplayer

Strokes sync (begin / points / end / clear). Eraser is `isEraser` on the stroke. Everyone needs the same Battle Draw version.

## Settings

BaseLib: default size + color preset.

## Build

```bash
cd mods/battle-draw
dotnet build -c Release
```
