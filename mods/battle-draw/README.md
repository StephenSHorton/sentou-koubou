# Battle Draw

Scribble on the **combat field** and restyle your **map pen** in Slay the Spire 2.

## Controls (combat)

| Input | Action |
|-------|--------|
| **Middle-mouse drag** | Draw |
| **Alt + left-drag** | Draw |
| Plain left-click | Unchanged — plays / selects cards |
| **`[` / `]`** | Smaller / larger brush |
| **`;` / `'`** | Previous / next color preset |

Ink never starts over the hand, play strip, or card previews. Canvas sits under combat UI so cards render on top. Strokes **clear when combat ends**.

## Map pen

Vanilla map drawing still uses the game’s draw tool. When **you** draw, lines use this mod’s **brush size + color** (same as combat).

Friends see your character’s default map color on their screens (packets don’t carry brush settings). Install this mod on every client if you all want custom map pens.

## Settings

Mod menu → **Battle Draw**:

- **Brush size** (1–24 px)
- **Color preset** (Yellow, Red, Orange, Green, Cyan, Blue, Purple, Pink, White, Black)

Requires [BaseLib](https://github.com/Alchyr/BaseLib-StS2/releases) ≥ 3.3.0.

## Build

```bash
cd mods/battle-draw
dotnet build -c Release
```

Copies into `Slay the Spire 2/mods/BattleDraw/`.
