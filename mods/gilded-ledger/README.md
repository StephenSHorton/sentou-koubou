# Gilded Ledger

Random **question-mark** event for Slay the Spire 2 (BaseLib).

## The Gilded Ledger

| Choice | Effect |
|--------|--------|
| **Gild a card** | Lose **all gold**. Pick **any** enchantment that can apply to your deck, then pick a card. |
| **Remove cards** | Remove **any number** of cards from your deck (pick 1…N removable). |

- Appears in **all acts** (shared event pool).
- Enchantment option list is **scrollable** when it would otherwise go off-screen (only on this event’s long list — does not touch Neow / other events).
- Locked options when you lack gold / enchantable cards, or lack removable cards.
- Card pick for Gild is cancelable (returns to the enchantment list; gold not taken until the enchant lands).
- Multiplayer: non-shared event (each player resolves their own ledger). Gold spent is synced.

## Requirements

- **BaseLib** ≥ 3.3.0 (Workshop or manual).
- STS2 ≥ 0.107.0

## Build

```bash
dotnet build -c Release
```

Copies `GildedLedger.dll` + `.json` into the game `mods/GildedLedger/` folder.

## Notes

- Portrait currently reuses Self-Help Book art until custom event art ships.
- Enchant amount is **1** for every enchantment (including Sharp / Nimble / Swift).
