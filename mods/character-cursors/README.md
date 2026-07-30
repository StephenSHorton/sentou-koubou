# Character Cursors

Tints the **mouse cursor** to match each character’s **primary color** (`NameColor`), **or a custom color**.

## In-run color picker (v0.3)

During a run, a small **cursor chip** appears **bottom-left**. Click it to open:

| Control | Action |
|---------|--------|
| **Color picker** | Sets a custom tint immediately (saved for next runs) |
| **Character color** | Back to your character `NameColor` |

Teammates see your custom color on the remote cursor (net sync).

BaseLib mod settings still work for Enable Tint / defaults when not in a run.

## What you get

| Cursor | Behavior |
|--------|----------|
| **Local** | Character `NameColor`, or custom color |
| **Remote** | Shader tint: peer’s custom color if set, else their character color |
| Map draw tools | Left untinted (quill/eraser stay vanilla) |

## Install

```bash
cd mods/character-cursors
dotnet build -c Release
```

Requires **BaseLib**.

## License

MIT — sentou-koubou.
