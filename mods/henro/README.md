# Henro — The Pilgrim (遍路)

Vertical-slice playable character for **sentou-koubou**. Goal: selectable on the character screen, start a run, play Strike/Defend, and get a heal from the starter relic after winning combat.

Scaffolded from [Alchyr/ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2) **Character** template + BaseLib.

## What’s in the slice

| Piece | Implementation |
|-------|----------------|
| Character | `Henro` extends `PlaceholderCharacterModel` (Ironclad fallback visuals) |
| Starting deck | 5× `StrikeHenro`, 5× `DefendHenro` |
| Starter relic | `PilgrimBeads` — heal 4 HP after combat victory |
| Pools | Card / Relic / Potion pools wired (empty reward pools for now) |
| Loc | English strings under `Henro/localization/eng/` |
| Tint | Soft indigo card back (`H/S/V` on `HenroCardPool`) |

Combat Spine, rest-site, merchant, and energy counter are **not** custom yet — placeholders until art lands.

## Layout

```
mods/henro/
├── Henro.csproj / Henro.sln / Henro.json
├── project.godot / export_presets.cfg
├── Directory.Build.props.example   # copy → Directory.Build.props (gitignored)
├── Sts2PathDiscovery.props
├── HenroCode/                      # C# gameplay
│   ├── MainFile.cs                 # [ModInitializer]
│   ├── Character/                  # Henro + pools
│   ├── Cards/Basic/                # Strike, Defend
│   ├── Relics/                     # PilgrimBeads + base class
│   ├── Powers/ Potions/ Extensions/
└── Henro/                          # Godot assets + localization
    ├── localization/eng/
    ├── images/                     # portraits, char UI, relic placeholders
    └── mod_image.png
```

## Build & install

1. Install **BaseLib** (Workshop or [releases](https://github.com/Alchyr/BaseLib-StS2/releases)).
2. Copy `Directory.Build.props.example` → `Directory.Build.props` and set `GodotPath` if needed.
3. Ensure STS2 is installed (path discovery covers default Steam locations, including macOS arm64 `data_sts2_*`).
4. From this folder:

```bash
dotnet restore
dotnet build
```

Build copies `Henro.dll` + `Henro.json` into the game’s `mods/Henro/` folder.

5. **Publish** the project (or Godot `--export-pack`) so `Henro.pck` is generated after any localization/asset change. Code-only edits can use Build alone.

6. Launch STS2 → enable mods → pick **The Pilgrim**.

### Logs

| OS | Path |
|----|------|
| macOS | `~/Library/Application Support/SlayTheSpire2/logs/` |
| Windows | `%APPDATA%\SlayTheSpire2\logs\` |
| Linux | `~/.local/share/SlayTheSpire2/logs/` |

## Next steps (past the slice)

- Unique common/uncommon/rare cards for the reward pool  
- Custom combat creature scene / Spine  
- Rest site + merchant presentation  
- Second character kit fantasy (this one is intentionally thin)  
- Workshop upload via [sts2-mod-uploader](https://github.com/megacrit/sts2-mod-uploader)

## Credits

- Template & BaseLib: [Alchyr](https://github.com/Alchyr)
- API patterns referenced from community character mods (Watcher, The Cursed, Buu)
