# MP Drop Out

Multiplayer fix for **Slay the Spire 2**: when a peer disconnects mid-run, the rest of the party can keep playing.

## Problem

Vanilla only removes the leaver from the lobby connection set and peer-input UI. Shared waits still require **every** `RunState` player:

- Combat end-turn / begin-enemy-turn
- Map path votes
- Shared event option votes
- Act transition ready
- Treasure relic picks

So a disconnect softlocks the table (“they never take their turn”).

## What this mod does

1. **On remote disconnect** (every peer):
   - Combat: host enqueues `EndPlayerTurnAction` for the leaver (clients also mark end-turn locally as a fallback).
   - Act transition: marks the leaver ready.
   - Re-checks map / event / treasure gates for “all connected voted”.
2. **While disconnected**, shared “all players” checks **ignore leavers** via `RunLobby.ConnectedPlayerIds` (same set vanilla already updates).

Reconnect is still vanilla: if they rejoin the run lobby, they become a participant again.

## What it does *not* do

- **Host leave** still ends the session for everyone (no host migration — vanilla architecture).
- Clients leave with the existing **disconnect confirm** in the pause/settings UI; this mod makes that safe for the remaining players.
- Does not kill or remove the leaver’s character from the save (they stay in `Players` for reconnect / history).

## Install

Unzip into `Slay the Spire 2/mods/` → `mods/MpDropOut/`.

Everyone in the lobby should run the same version.

## Build

```bash
cd mods/mp-drop-out
dotnet build -c Release
```
