## Summary

Several UncappedSpire multiplayer issues. Newest first.

### A. Multiplayer embark crash (Seed UInt64 → uint) — **blocks all MP runs**

Observed UncappedSpire **v0.3.15** (Workshop `3749824653`), STS2 **v0.107.1**.

```text
[ERROR] Exception starting multiplayer run : System.MissingMethodException:
  Method not found: 'UInt64 MegaCrit.Sts2.Core.Random.PlayerRngSet.get_Seed()'.
   at UncappedSpire...PlayerRngSetPatches.Patch_LoadFromSerializable.Prefix
   at Player.SyncWithSerializedPlayer → CombatStateSynchronizer.WaitForSync
```

Upstream prefix (still shipping):

```csharp
// PlayerRngSetPatches/Patch_LoadFromSerializable.cs
save.Seed = __instance.Seed; // compiled against UInt64 get_Seed()
```

Game API now:

```csharp
public uint Seed { get; }  // was UInt64 on older builds
```

**Suggested upstream fix:** rebuild against current `sts2.dll` so `Seed` is `uint` (source already assigns `Seed` — only the reference assembly age is wrong). `RunRngSet` prefix already uses `StringSeed` and is fine.

---

### B. Chapter → Neow unfinished event (state divergence)

Multiplayer state divergence when starting a **new UncappedSpire chapter** via **Closing the Chapter → Through the Mysterious Door**, then completing the following **Neow** event.

Observed on UncappedSpire **v0.3.12** (Workshop `3749824653`), STS2 ~0.107.x, RitsuLib 0.4.57, BaseLib 3.3.7, 3 players (host + 2 clients).

## Host log (smoking gun)

After all players vote **START_A_NEW_CHAPTER**, act transition proceeds, then:

```text
[WARN] [EventSynchronizer] Beginning new event EVENT.NEOW (...), but event EVENT.UNCAPPEDSPIRE-CLOSING_THE_CHAPTER (...) for player <id> is not yet finished!
```

(for **all three** players)

Then on Neow exit:

```text
[ERROR] State divergence detected! Checksum with ID 1098 ... Context: Exiting event room EVENT.NEOW
[DEBUG] [SteamHost] Disconnecting peer ..., reason: StateDivergence
```

RitsuLib dump shows peers agree on most inventory/RNG, but **choice next IDs** can already be drifted (e.g. host `71,52,44` vs client `71,52,45` for Watcher).

## Root cause in source

[`ClosingTheChapter.StartANewChapter`](https://github.com/Tobiline/UncappedSpire/blob/main/UncappedSpireCode/UncappedActs/ClosingTheChapter.cs):

```csharp
private Task StartANewChapter()
{
    if (!LocalContext.IsMe(Owner)) 
        return Task.CompletedTask;  // remote player instances never finish

    // ... seed / chapter / SetLocalPlayerReady ...
    return Task.CompletedTask;      // local instance also never SetEventFinished
}
```

Shared events run the option handler **once per player event model**. Only the local owner runs chapter mutation; **none** of the instances call `SetEventFinished`. Vanilla `EventSynchronizer.BeginEvent` then warns that the previous event is unfinished when Neow starts after the chapter transition.

Related: commit `063aae3` ("Race condition for mp chapter change") added `EnsureActChangeSynchronizerSuceeds`, but current game `ActChangeSynchronizer.OnPlayerReady(Player)` has **no actIndex argument**, so that prefix's `__args[1] = int.MaxValue` is a no-op on current builds.

## Suggested fix

At the top of `StartANewChapter` (before the `IsMe` early return):

```csharp
if (!IsFinished)
    SetEventFinished(Description);
```

So **every** shared-event instance is marked finished, then only the local owner performs seed/chapter/ready.

Optional hardening: ensure `ChapterChangeMessage` is applied on clients before `SetLocalPlayerReady` races into the act transition.

## Secondary issue (now primary remaining desync) — boss reward choices

Even with chapter finish fixed, act-2 boss rewards can still desync `choices.nextChoiceIds` (host +1 on peers who took interactive relics). Detected at **Exiting event room EVENT.NEOW**.

### Hefty Tablet (skip)

```text
Player obtained RELIC.HEFTY_TABLET
… choice ID N: NetPlayerChoiceResult indexes 3
System.ArgumentOutOfRangeException: Index was out of range
  at CardSelectCmd+<FromChooseACardScreen>d__13.MoveNext
  at HeftyTablet.AfterObtained()
```

Vanilla remote apply: `result = (num < 0) ? null : cards[num]`. Skip is usually `-1`, but peers sometimes send the reward-style sentinel `indexes == cards.Count` (3 on a 3-card offer) → OOB after the choice ID was reserved.

Suggested vanilla fix in `FromChooseACardScreen`:

```csharp
result = (num < 0 || num >= cards.Count) ? null : cards[num];
```

### Claws (wrong choice type)

```text
Player obtained RELIC.CLAWS
… choice ID N: NetPlayerChoiceResult indexes 3
System.InvalidOperationException: Tried to get deck cards from player choice result of type Index!
  at PlayerChoiceResult.AsDeckCards()
  at CardSelectCmd.FromDeckForTransformation(...)
  at Claws.AfterObtained()
```

`FromDeckForTransformation` always calls `AsDeckCards()`; Index (skip/misroute) throws. Empty-success is the safe host apply for cancel.

### Earlier same class

```text
System.InvalidOperationException: Tried to get index from player choice result of type DeckCard!
  at PlayerChoiceResult.AsIndexOrNull()
  at CardReward.OnSelect ...
```

## Workaround / local compat

**UncappedChapterFix v0.2.0** (Harmony compat):

1. Postfix `StartANewChapter` → force-finish event on all peers.
2. Prefix `FromChooseACardScreen` → bounds-checked skip.
3. Finalizer `AsDeckCards` → empty on wrong type.
4. Finalizer `AsIndexOrNull` → null on wrong type under reward/relic stacks.

Happy to open a PR with the in-tree `SetEventFinished` change if you want the chapter fix upstream.

## Environment

- UncappedSpire v0.3.12
- RitsuLib 0.4.57 / BaseLib 3.3.7
- Other gameplay mods present (CardRanks, Ancients Awakened, Watcher, Downfall, YUILongMap, TradingPost, MpPlayerLimit) — same set on all peers; load order differed but ModelDb deterministic flags were on and SavedProperty mapHash matched.
