# Trading Post

Co-op trading for **Slay the Spire 2** multiplayer.

## What it does

| Where | Trade | Cost |
|-------|-------|------|
| **Shop** | Give gold to a friend (any amount, as often as you like) | Just the gold |
| **Campfire** | Give a card from your deck — **upgrades carry over** (works with uncapped upgrades) | **Your campfire action** — no resting, smithing, or digging after |

- The shop gets a **Trade** button (bottom-left, co-op only) with a slider + typed amount.
- Campfires get a **Trade** tile alongside Rest/Smith/etc. with custom art. Completing a
  trade consumes the action through the game's own rest-site flow; backing out refunds it.
- Notifications and prompts use the game's native popup scene; the trade menu adopts the
  game's fonts at runtime.
- Only the card's upgrade level transfers; enchantments and other one-off card mods don't.
- **Every player in the lobby must have the mod installed** — trades are custom network
  messages, and clients without the mod cannot decode them.

## Build

```bash
dotnet build   # copies TradingPost.dll + .json + option_trade.png into the game's mods folder
```

No `.pck` needed — UI is stock Godot controls styled from the game's own theme, localization
is injected at runtime, and the campfire icon is a plain PNG loaded from the mod folder.

## Implementation notes

- `TradeSynchronizer` mirrors the game's `OneOffSynchronizer` pattern: the initiating client
  applies the change locally and broadcasts an `INetMessage` (auto-registered by the game's
  mod scan); peers mirror it.
- The campfire option is a `RestSiteOption` subclass appended via a Harmony postfix on
  `RestSiteOption.Generate`. Selection syncs by option index through the game's
  `RestSiteSynchronizer`; remote mirrors await a `CampfireTradeResultMessage` so the action
  is consumed identically everywhere.
- Fresh card copies must be registered via `RunState.AddCard` before `CardPileCmd.Add`.
- Harmony patches: `RunManager.InitializeShared`/`CleanUp` (synchronizer lifecycle),
  `NMerchantRoom._Ready` (shop button), `RestSiteOption.Generate` (campfire tile),
  `RestSiteOption.Icon` (custom art from disk).

## Local 2-player testing (no second PC needed)

1. `steam_appid.txt` containing `2868840` in the game folder lets a second copy launch
   directly from `SlayTheSpire2.exe` while Steam runs.
2. In both windows: dev console (`` ` ``) → `multiplayer test` → **Host** in one,
   **Join** (defaults) in the other → Ready in both.
3. Useful console commands: `gold 500`, `room shop`, `room restsite` (networked — warps
   everyone together).
