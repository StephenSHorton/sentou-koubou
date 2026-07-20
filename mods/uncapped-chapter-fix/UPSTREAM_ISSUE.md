## Summary

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

## Secondary issue same session (may be separate)

Host threw during a remote card reward apply:

```text
System.InvalidOperationException: Tried to get index from player choice result of type DeckCard!
  at PlayerChoiceResult.AsIndexOrNull()
  at CardReward.OnSelect ...
```

That can desync choice IDs even if chapter finish is fixed. Worth a separate look if you touch reward/choice code.

## Workaround / local compat

We shipped a tiny Harmony compat mod that postfixes `StartANewChapter` to force-finish the event instance on all peers (**UncappedChapterFix**). Happy to open a PR here with the in-tree `SetEventFinished` change if you want the fix upstream.

## Environment

- UncappedSpire v0.3.12
- RitsuLib 0.4.57 / BaseLib 3.3.7
- Other gameplay mods present (CardRanks, Ancients Awakened, Watcher, Downfall, YUILongMap, TradingPost, MpPlayerLimit) — same set on all peers; load order differed but ModelDb deterministic flags were on and SavedProperty mapHash matched.
