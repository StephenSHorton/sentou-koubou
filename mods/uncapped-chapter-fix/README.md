# Uncapped Chapter Fix

Compat patch for **UncappedSpire** multiplayer: chapter transitions, boss-reward choice apply, and STS2 hash/RNG API breaks (embark + Mysterious Door).

## Problems

### 0a. "Through the Mysterious Door" hangs (both voted, nothing happens)

After both peers vote **START_A_NEW_CHAPTER**, UncappedSpire sends `ChapterChangeMessage` then runs `DoSeedChange`, which throws:

```text
MissingMethodException: Method not found:
  'UInt64 MegaCrit.Sts2.Core.Helpers.StringHelper.GetDeterministicHashCode(System.String)'
   at ChapterChangeSynchronizer.DoSeedChange
```

STS2 now returns `int` from that API. Seed/act reseed never completes → UI stuck on Closing the Chapter.

This mod **replaces** `DoSeedChange` with a uint/int-safe implementation.

### 0b. Multiplayer embark crash (`PlayerRngSet.get_Seed` MissingMethodException)

Starting a multiplayer run with UncappedSpire workshop **≤0.3.15** fails during combat-state sync:

```text
[ERROR] Exception starting multiplayer run : System.MissingMethodException:
  Method not found: 'UInt64 MegaCrit.Sts2.Core.Random.PlayerRngSet.get_Seed()'.
   at UncappedSpire...PlayerRngSetPatches.Patch_LoadFromSerializable.Prefix(...)
   at PlayerRngSet.LoadFromSerializable_Patch1(...)
   at CombatStateSynchronizer.WaitForSync()
```

UncappedSpire’s Harmony prefix still calls the old `UInt64` Seed getter; STS2 0.107.x exposes `uint Seed`. This mod **unpatches** that prefix and re-applies the same logic against `uint`.

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

## Fixes (v0.2.3)

1. **ClosingTheChapter** postfix — force-finish every shared-event instance before act/Neow.
2. **`FromChooseACardScreen` prefix** — bounds-check remote index; OOB / reward-style sentinel → null skip (also allows &gt;3 cards like Downfall’s NOP of the vanilla throw).
3. **`AsDeckCards` finalizer** — wrong type (esp. Index) → empty list instead of throw under relic/deck select.
4. **`AsIndexOrNull` finalizer** — wrong type under reward/relic/card-select stacks → null (broader than CardReward-only).
5. **PlayerRngSet.LoadFromSerializable** — replace UncappedSpire’s broken `UInt64` Seed prefix with a `uint` version so multiplayer embark no longer throws.
6. **Chapter reseed (Mysterious Door hang)** — UncappedSpire’s `DoSeedChange` still references
   `UInt64 GetDeterministicHashCode`. Harmony **cannot** patch that method (IL read throws the
   same MissingMethodException). v0.2.2 therefore never applied the seed fix. v0.2.3 prefixes
   the clean callers instead:
   - `DoLocalSeedChange` — local vote: broadcast `ChapterChangeMessage` + int-safe reseed
   - `HandleChapterChangeMessage` — remote peer reseed
   Each patch group is try/caught so one failure cannot abort the rest of this mod’s init.

## Install

Quit STS2, build, or unzip into `Slay the Spire 2/mods/UncappedChapterFix/`.

**All multiplayer clients must run the same version** of this mod (and UncappedSpire).

```bash
dotnet build mods/uncapped-chapter-fix -c Release
```

On load you should see five patch groups applied (four if UncappedSpire is absent).

**Load order:** this mod must initialize **after** UncappedSpire so it can replace the broken seed prefix. With the default local+workshop layout that is already the case; if you reorder mods manually, keep UncappedChapterFix after UncappedSpire.

## Relation to upstream

- Prefer a proper `SetEventFinished` in UncappedSpire `StartANewChapter`.
- Vanilla would also benefit from `num < 0 \|\| num >= cards.Count` in `FromChooseACardScreen` remote apply.
- This mod is a stopgap until those land; safe to leave enabled after.
