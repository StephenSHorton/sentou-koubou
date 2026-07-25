# Trading Post

Co-op trading for **Slay the Spire 2** multiplayer.

## What it does

| Where | Trade | Cost |
|-------|-------|------|
| **Shop** | Give gold to a friend (any amount, as often as you like) | Just the gold |
| **Shop** | **Sell a potion** to the merchant (extra option on the potion popup) | Half base buy price |
| **Shop** | **Sell a relic** to the merchant (Sell button on relic inspect) | Half `MerchantCost` |
| **Campfire** | Give a card from your deck — **upgrades carry over** | **Your campfire action** |

### Sell details

- Sell options only appear **in the merchant room**.
- Potion prices match the shop rarity ladder (50 / 75 / 100 base), paid at **half** (25 / 37 / 50).
- Relics require `IsTradable` and a positive `MerchantCost` (starter/untradable relics stay unsellable).
- Sales are multiplayer-synced (`SellPotionMessage` / `SellRelicMessage`).

### UI

- Shop **Trade** button and menus use painted rest-site style plates (`btn_rest_bar` / `btn_plate`), not sharp chrome.
- Potion sell reuses the vanilla Use/Discard popup button chrome.
- Relic sell is a custom painted button on the inspect screen.

## Build

```bash
dotnet build   # copies TradingPost.dll + .json + UI PNGs into the game's mods folder
```

No `.pck` needed — UI is stock Godot controls + mod PNG cutouts loaded from the mod folder.

## Local 2-player testing

1. `steam_appid.txt` with `2868840` in the game folder for a second process.
2. Both windows: console → `multiplayer test` → Host / Join.
3. Useful: `gold 500`, `room shop`, `room restsite`.
