# Character Cursors

Tints the **mouse cursor** to match each character’s **primary color** (`NameColor` — Ironclad red, Silent green, Defect blue, etc.), **or a custom color** from BaseLib mod settings.

Yes, this works in Godot: STS2 already uses custom cursor **Images** (`NCursorManager` → `Input.SetCustomMouseCursor`). We recolor those pixels for the local cursor, and shader-tint multiplayer remote cursors.

## What you get

| Cursor | Behavior |
|--------|----------|
| **Local** (your pointer) | Character `NameColor`, or **Custom Color** when enabled in settings |
| **Remote** (teammates) | Desaturate + tint shader using each teammate’s character color |
| Map draw tools | Left untinted (quill/eraser stay vanilla) |

### Settings (BaseLib)

| Setting | Default | Meaning |
|--------|---------|---------|
| **Enable Tint** | on | Master switch |
| **Use Custom Color** | off | Local cursor ignores character color |
| **Custom Color** | red-ish | Free color picker |

Outline stays dark so the pointer stays readable on light and dark UI.

## Install

```bash
cd mods/character-cursors
dotnet build -c Release
```

Enable **Character Cursors** in the mod list. Requires **BaseLib**.

### Note vs LemonSpire

LemonSpire can set a **custom** player color that also tints remote cursors. If both are on, LemonSpire’s custom color may win for remotes. Local character-color tint still applies from this mod.

## License

MIT — sentou-koubou. Remote shader approach inspired by LemonSpire’s color tint (MIT).
