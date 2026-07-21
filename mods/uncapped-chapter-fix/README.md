# Uncapped Chapter Fix

Compat patch for **UncappedSpire** multiplayer chapter transitions and boss-reward choice apply.

## Problems

### 1. Closing the Chapter → Neow (unfinished shared event)

When all players pick **Through the Mysterious Door** on `EVENT.UNCAPPEDSPIRE-CLOSING_THE_CHAPTER`, UncappedSpire’s `StartANewChapter` returns early for non-local player event instances **without** marking the event finished. Host logs then show:

```text
Beginning new event EVENT.NEOW, but event EVENT.UNCAPPEDSPIRE-CLOSING_THE_CHAPTER is not yet finished!
```

…followed by multiplayer **state divergence** and forced disconnects (RitsuLib diagnostic dump).

Upstream source: [Tobiline/UncappedSpire](https://github.com/Tobiline/UncappedSpire)  
`ClosingTheChapter.StartANewChapter` only mutates chapter state when `LocalContext.IsMe(Owner)` and never calls `SetEventFinished` (workshop ≤0.3.12).

### 2. Boss rewards → choice-ID drift (Hefty Tablet / Claws)

Even with the chapter event finished correctly, act-2 boss rewards with interactive relics can desync `PlayerChoiceSynchronizer` next-IDs:

| Relic | Host throw | Cause |
|-------|------------|--------|
| **Hefty Tablet** | `ArgumentOutOfRangeException` in `FromChooseACardScreen` | Remote skip sometimes arrives as `indexes == cards.Count` (e.g. 3); vanilla only treats `num < 0` as skip |
| **Claws** | `InvalidOperationException` in `AsDeckCards` | Remote cancel/misroute sends **Index** where **DeckCard** was expected |

Host reserves the choice ID, then throws mid-apply → host counters end **+1** vs clients → checksum fails at **Exiting event room EVENT.NEOW**. Inventory/RNG often still match.

## Fixes (v0.2.0)

1. **ClosingTheChapter** postfix — force-finish every shared-event instance before act/Neow.
2. **`FromChooseACardScreen` prefix** — bounds-check remote index; OOB / reward-style sentinel → null skip (also allows &gt;3 cards like Downfall’s NOP of the vanilla throw).
3. **`AsDeckCards` finalizer** — wrong type (esp. Index) → empty list instead of throw under relic/deck select.
4. **`AsIndexOrNull` finalizer** — wrong type under reward/relic/card-select stacks → null (broader than CardReward-only).

## Install

Quit STS2, build, or unzip into `Slay the Spire 2/mods/UncappedChapterFix/`.

**All multiplayer clients must run the same version** of this mod (and UncappedSpire).

```bash
dotnet build mods/uncapped-chapter-fix -c Release
```

On load you should see four patch groups applied.

## Relation to upstream

- Prefer a proper `SetEventFinished` in UncappedSpire `StartANewChapter`.
- Vanilla would also benefit from `num < 0 \|\| num >= cards.Count` in `FromChooseACardScreen` remote apply.
- This mod is a stopgap until those land; safe to leave enabled after.
