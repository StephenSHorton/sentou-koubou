# Battle Draw

**Map-style drawing in combat** for Slay the Spire 2, plus **map pen color/size** controls.

## v0.6 — same ink path as the map

Combat now uses the vanilla map drawing stack instead of a custom image stamp:

| Piece | Implementation |
|-------|----------------|
| Surface | Half-res transparent **SubViewport** |
| Pen | `map_line_draw` **Line2D** (trail texture + mix shader) |
| Eraser | `map_line_erase` **Line2D** (**subtractive** `blend_sub` shader) |
| Composite | TextureRect with **premultiplied alpha** |

That fixes weak erase / “negative residual” multi-swipe erase and makes pen strokes match map ink.

## Combat

| Input | Action |
|-------|--------|
| **RMB drag** | Draw |
| **MMB drag** | Erase (full-strength subtractive stroke) |
| **Bottom-right tab** | Collapsible tools palette (color/size; combat also has Brush/Clear) |
| **Color / size** | Shared with map pen |
| **Hide others** | Local hide of peer combat ink |

Hand strip / card drag / palette block ink. Combat ink composites **under** the hand, cards, and menus (not a high CanvasLayer). Strokes clear when combat ends. No click-arm eraser — **MMB only**.

Shortcuts: **B** arm LMB brush, **`[` / `]`** size.

## Map

Vanilla map draw still works. A **map pen palette** (same color + size as combat) sits bottom-right. Local lines use your chosen color/width.

## Multiplayer

Combat doodles sync (begin / points / end / clear). Eraser is a stroke flag (`isEraser`), not stamp circles. All peers need Battle Draw **≥ 0.6.0**.

## Settings

Mod menu → **Battle Draw**: default size + color preset (BaseLib). Color picker overrides preset until you pick a preset again.

Requires **BaseLib** ≥ 3.3.0.

## Build

```bash
cd mods/battle-draw
dotnet build -c Release
```
