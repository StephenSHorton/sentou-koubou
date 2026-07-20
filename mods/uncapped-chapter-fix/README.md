# Uncapped Chapter Fix

Compat patch for **UncappedSpire** multiplayer chapter transitions.

## Problem

When all players pick **Through the Mysterious Door** on `EVENT.UNCAPPEDSPIRE-CLOSING_THE_CHAPTER`, UncappedSpire’s `StartANewChapter` returns early for non-local player event instances **without** marking the event finished. Host logs then show:

```text
Beginning new event EVENT.NEOW, but event EVENT.UNCAPPEDSPIRE-CLOSING_THE_CHAPTER is not yet finished!
```

…followed by multiplayer **state divergence** and forced disconnects (RitsuLib diagnostic dump).

Upstream source: [Tobiline/UncappedSpire](https://github.com/Tobiline/UncappedSpire)  
`ClosingTheChapter.StartANewChapter` only mutates chapter state when `LocalContext.IsMe(Owner)` and never calls `SetEventFinished`.

## Fix

Harmony postfix on `ClosingTheChapter.StartANewChapter`:

1. Mark **this** event model instance finished (`_isFinished` + `EnsureCleanup`) for every shared-event invocation (one per player).
2. No-op cleanly if UncappedSpire is not loaded.

Also soft-hardens remote **card reward** choice application: if a peer sends a deck-card choice where an index was expected, try to map it to an offered card index instead of throwing (seen in the same desync session as choice-ID drift).

## Install

Quit STS2, build, or unzip into `Slay the Spire 2/mods/UncappedChapterFix/`.

All multiplayer clients should run the same version of this mod **and** UncappedSpire.

```bash
dotnet build mods/uncapped-chapter-fix -c Release
```

## Relation to upstream

Prefer a proper fix in UncappedSpire (`SetEventFinished` at the start of `StartANewChapter`). This mod is a stopgap until that ships on Workshop.
