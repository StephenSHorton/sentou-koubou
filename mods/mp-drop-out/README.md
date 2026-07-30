# MP Drop Out

Multiplayer resilience for **Slay the Spire 2**: leavers stop softlocking the party, and **host migration** keeps the run alive when the host disconnects.

## Features

### 1. Drop-out (any peer)

When a **client** disconnects mid-run:

- Combat: host enqueues `EndPlayerTurnAction` for the leaver
- Map / shared event / treasure: once every **connected** player has acted, the host resolves
- Act transition: leaver is treated as ready
- Ongoing “all players ready” checks use `RunLobby.ConnectedPlayerIds`

### 2. Host migration (Steam)

When the **host** connection is lost (crash, quit, network):

1. Remaining players elect a successor: **lowest NetId** still in the run  
2. Successor **creates a new Steam lobby** and becomes host  
3. Other clients **auto-reconnect** via `ConnectToLobbyOwnedByFriend(successor)`  
4. Message handlers are copied onto the new net service; synchronizer `_netService` fields are rebound; `RunLobby` is recreated  
5. The dead host is dropped from waits like any other leaver  

Clients use the existing disconnect confirm to leave voluntarily. The old host going to menu is expected; the **remaining** party continues.

## Limits / requirements

| Topic | Detail |
|--------|--------|
| **Steam only** | Migration uses Steam lobbies + P2P. ENet/LAN not supported for promote. |
| **Same mods** | Everyone needs this mod (gameplay-affecting). |
| **Friends visibility** | Reconnect uses Steam friend lobby info — successor must be a Steam friend (normal STS2 co-op). |
| **Best-effort mid-combat** | Networking is rebound in place; edge cases mid-action may still desync — reconnect/rejoin path is the recovery. |
| **No host → empty** | If no remaining players, vanilla main-menu path runs. |
| **Abandon** | Host **abandon** still ends the run for everyone (not migrated). |

## Install

Unzip into `Slay the Spire 2/mods/` → `mods/MpDropOut/`.

## Build

```bash
cd mods/mp-drop-out
dotnet build -c Release
```

## Versions

- **v0.1.0** — drop-out only (non-host leavers)
- **v0.2.0** — host migration
