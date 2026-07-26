# Battle Draw

Map-style **drawing in combat** for Slay the Spire 2, plus **map pen color/size** controls.

## Performance (v0.5 rewrite)

Older builds caused combat lag (per-frame ProcessFrame hooks, full-rect layout thrash, unbounded `Line2D` stacks, unthrottled erase net).

**v0.5+** rewrites the ink path:

| Old | New |
|-----|-----|
| Many antialiased `Line2D` nodes forever | Near-full-res **baked ImageTexture** |
| Soft erase (dim leftover) | **Hard erase** (full wipe under the disk) |
| Soft fuzzy stamps | Hard disks + dense steps (sharper at any size) |
| Generated icon plates with weak contrast | Dark collapsible panel + readable text buttons |
| Two tabs (map stuck over combat) | **One** global collapsible menu; combat tools hide on map |

## Combat

| Input | Action |
|-------|--------|
| **RMB drag** | Draw |
| **MMB drag** | Erase |
| **Bottom-right tab** | Tools palette |
| **Color / size** | Shared with map pen |
| **Hide others** | Local hide of peer combat ink |

Hand strip / card drag / palette block ink. Strokes clear when combat ends.

Shortcuts: **B** brush, **E** eraser, **`[` / `]`** size.

## Map

Vanilla map draw still works. A **map pen palette** (same color + size as combat) sits bottom-right so you can change brush without opening mod settings. Local lines use your chosen color/width.

## Multiplayer

Combat doodles sync (begin / points / erase / clear). All peers need Battle Draw.

## Settings

Mod menu → **Battle Draw**: default size + color preset (BaseLib). Color picker overrides preset until you pick a preset again.

Requires **BaseLib** ≥ 3.3.0.

## Build

```bash
cd mods/battle-draw
dotnet build -c Release
```
