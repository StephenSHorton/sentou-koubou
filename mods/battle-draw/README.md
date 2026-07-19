# Battle Draw

Scribble on the **combat field** in Slay the Spire 2 co-op or solo — for callouts, arrows, and “hit this” notes.

## Controls

| Input | Action |
|-------|--------|
| **Middle-mouse drag** | Draw |
| **Alt + left-drag** | Draw (same) |
| Plain left-click | Unchanged — still plays / selects cards |

Ink **never** starts over the hand, play strip, or card previews. Mid-stroke into those areas stops the line. The canvas sits **under** combat UI so cards render on top.

## Lifetime

Strokes **clear when combat is won or otherwise ends** (and again if the combat room is torn down).

## Build

```bash
cd mods/battle-draw
dotnet build -c Release
```

Copies `BattleDraw.dll` + `BattleDraw.json` into `Slay the Spire 2/mods/BattleDraw/`.

- `affects_gameplay: false` — safe cosmetic QoL.
- No BaseLib / multiplayer sync required (each client doodles locally).
