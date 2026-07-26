# MP Teammate View

Combined multiplayer QoL for **Slay the Spire 2**: show each teammate’s **potions** and **hand cards** next to the multiplayer player list.

Based on (MIT, © OLC / BAKAOLC):

- [STS2-MultiPlayerPotionView](https://github.com/BAKAOLC/STS2-MultiPlayerPotionView)
- [STS2-ShowPlayerHandCards](https://github.com/BAKAOLC/STS2-ShowPlayerHandCards)

Rewritten as one mod with a more reliable hand attach path, plus the full settings / hotkeys / highlights / chat interop from both upstreams.

## Features

| Feature | Status |
|---------|--------|
| Teammate potion icons + hover tips | yes |
| Teammate mini hand cards + hover tips | yes |
| Real-time hand updates | yes (ContentsChanged + turn refresh) |
| RitsuLib settings UI (scale, offset, rules) | yes |
| Hand toggle hotkey (default Shift) | yes |
| Manual drag positioning (hands) | yes |
| Rule-based border highlights (text/regex/template) | yes (cards + potions) |
| LemonSpire Alt+click share / flash | yes (if LemonSpire loaded) |
| Typing Alt+click item links | yes (if Typing loaded) |
| EnergyVar color-prefix rollback (MP-safe) | yes |

### Reliable hand attach

Upstream **ShowPlayerHandCards** only hooks `CombatManager.SetUpCombat`. If player-state rows are not ready yet, or `PlayerCombatState` is still null, hands never subscribe.

This mod:

1. Attaches on **`NMultiplayerPlayerState._Ready`**
2. Retries hand subscription every ~0.35s until combat hand exists
3. Refreshes on **SetUpCombat**, **AfterCombatRoomLoaded**, and **TurnStarted**
4. Cleans up on combat end and player-state exit

### Chat interop

With **LemonSpire** and/or **Typing** installed:

- **Alt + Left/Right click** a mini card → share / flash (LemonSpire) + Typing card link (left)
- **Alt + Left/Right click** a potion icon → share (LemonSpire) + Typing potion link (left)

## Requirements

- **STS2-RitsuLib** (Workshop / deps) — settings storage, settings UI, runtime hotkeys

## Install

1. **Disable** Workshop **MultiPlayerPotionView** and **ShowPlayerHandCards** (duplicate UI if both run).
2. Ensure **STS2-RitsuLib** is enabled.
3. Unzip / build into `Slay the Spire 2/mods/MpTeammateView/`.
4. Enable **MP Teammate View**.

```bash
cd mods/mp-teammate-view
dotnet build -c Release
```

Open the mod settings page in-game for scale, offsets, highlight rules, and the toggle key.

## License

MIT — original work by OLC; combination, reliability rewrite, and unified settings for sentou-koubou.
