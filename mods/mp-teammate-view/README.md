# MP Teammate View

Combined multiplayer QoL for **Slay the Spire 2**: show each teammate’s **potions** and **hand cards** next to the multiplayer player list.

Based on (MIT, © OLC / BAKAOLC):

- [STS2-MultiPlayerPotionView](https://github.com/BAKAOLC/STS2-MultiPlayerPotionView)
- [STS2-ShowPlayerHandCards](https://github.com/BAKAOLC/STS2-ShowPlayerHandCards)

Rewritten as one Harmony mod (no RitsuLib runtime dependency) with a more reliable hand attach path.

## Why a rewrite

Upstream **ShowPlayerHandCards** only hooks `CombatManager.SetUpCombat`. If player-state rows are not ready yet, or `PlayerCombatState` is still null, hands never subscribe — so the overlay sometimes does nothing until a later combat (or never).

This mod:

1. Attaches on **`NMultiplayerPlayerState._Ready`** (same idea as the potion mod)
2. Retries hand subscription every ~0.35s until combat hand exists
3. Refreshes on **SetUpCombat**, **AfterCombatRoomLoaded**, and **TurnStarted**
4. Cleans up on combat end and player-state exit

## Install

1. **Disable** Workshop **MultiPlayerPotionView** and **ShowPlayerHandCards** (duplicate UI if both run).
2. Unzip / build into `Slay the Spire 2/mods/MpTeammateView/`.
3. Enable **MP Teammate View**.

```bash
cd mods/mp-teammate-view
dotnet build -c Release
```

## Scope (v0.1.0)

| Feature | Status |
|---------|--------|
| Teammate potion icons + hover tips | yes |
| Teammate mini hand cards + hover tips | yes |
| Real-time hand updates | yes (ContentsChanged + turn refresh) |
| RitsuLib settings / hotkeys / highlight rules | not ported yet (fixed layout) |
| LemonSpire / Typing chat links | not ported |

## License

MIT — original work by OLC; combination and reliability rewrite for sentou-koubou.
