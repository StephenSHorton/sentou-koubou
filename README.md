# 尖塔工房 · sentou-koubou

**尖塔工房** (*sentō-kōbō*) — *Spire Workshop*.

Monorepo for [Slay the Spire 2](https://store.steampowered.com/app/2868840/Slay_the_Spire_2/) mods: characters, content packs, and tools. Named for 殺戮の尖塔 (the Japanese title’s “pointed tower / spire”) and a 工房 (workshop / atelier) where those mods are forged.

```
sentou-koubou/
└── mods/
    ├── brennen/        # Tank character — full kit
    ├── whitney/        # Atelier witch — Energy + Ink
    └── trading-post/   # Co-op trading QoL
```

## Why this name?

| Reading | Kanji | Sense |
|--------|-------|--------|
| sentō | 尖塔 | Spire (same characters as in 殺戮の尖塔) |
| kōbō | 工房 | Workshop / craft studio |

## Mods

| Folder | Status | What it is |
|--------|--------|------------|
| [`mods/brennen`](mods/brennen) | Full character kit | **Brennen** — frontline tank; Barricade/Block payoffs, peel, HP→energy |
| [`mods/whitney`](mods/whitney) | Full character kit | **Whitney** — atelier witch; Energy + Ink dual mana, Attunement |
| [`mods/trading-post`](mods/trading-post) | Co-op QoL | Shop gold gifts + campfire card trades |

### Card catalog

**Live site (GitHub Pages):** https://stephenshorton.github.io/sentou-koubou/

```bash
# Local
python -m http.server -d docs 8765   # http://localhost:8765
```

Pages deploys from the `docs/` folder on `main` (`index.html` + assets).

## Stack

- **C# / .NET 9** + **Godot 4.5.x** (prefer [MegaDot](https://megadot.megacrit.com/))
- Community library: **[BaseLib](https://github.com/Alchyr/BaseLib-StS2)** ([Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3737335127))
- Project templates: **[ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2)** (Character template)
- Docs: [template wiki](https://github.com/Alchyr/ModTemplate-StS2/wiki) · [BaseLib wiki](https://alchyr.github.io/BaseLib-Wiki/)

## Prerequisites

1. Slay the Spire 2 on Steam (mod loader is built-in; Workshop since ~v0.107.1)
2. [.NET SDK 9+](https://dotnet.microsoft.com/download)
3. [MegaDot 4.5.1](https://megadot.megacrit.com/) or matching Godot .NET
4. [BaseLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3737335127) subscribed (or manual install into `mods/BaseLib/`)
5. IDE: Rider recommended (or VS)

## Build a mod

```bash
cd mods/brennen   # or whitney / trading-post
cp Directory.Build.props.example Directory.Build.props
# edit GodotPath / Sts2Path if discovery fails

dotnet restore
dotnet build          # copies .dll + .json into the game mods folder
# Publish (IDE) or Godot export-pack for .pck when assets/loc change
```

**Quit STS2 before build** — the game locks `Mods/*.dll`.

## Workshop upload

Official tool: [megacrit/sts2-mod-uploader](https://github.com/megacrit/sts2-mod-uploader).

## Help

- `#sts2-modding` on the [Slay the Spire Discord](https://discord.com/invite/SlayTheSpire)
- This repo’s issues for sentou-koubou-specific work

## License

Each mod may declare its own license. Unless noted otherwise, new code here is MIT.
