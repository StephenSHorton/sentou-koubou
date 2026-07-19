# Multiplayer Player Limit

Clean **from-scratch** STS2 mod that raises the multiplayer lobby / host capacity above vanilla **4** (default **16**).

This is **not** the workshop “Remove Multiplayer Player Limit” (RMP) binary and **not** an IL patch of it. It only Harmony-hooks the vanilla entry points that hardcode `4`.

## Why

Vanilla host path:

```text
NetHostGameService.StartSteamHost(4)
NetHostGameService.StartENetHost(port, 4)
StartRunLobby(..., maxPlayers: 4)
```

Those `4`s are the entire multiplayer player cap. Changing them is enough for join + ready + start on current STS2.

## Install

1. Unzip so you have `Slay the Spire 2/mods/MpPlayerLimit/MpPlayerLimit.{dll,json}`
2. Enable **BaseLib** + **Multiplayer Player Limit** in the Mods list
3. **Do not** also enable workshop RMP (or any other capacity mod)

All players in a lobby should use the **same** max (default 16).

## Config

BaseLib settings for this mod:

| Setting | Default | Range |
|---------|---------|-------|
| `MaxPlayers` | 16 | 2–16 |

## Build

```bash
cd mods/mp-player-limit
dotnet build -c Release
```

## Release

```bash
python tools/release_mod.py mp-player-limit 0.1.0 --local-upload
```

## Notes / limitations

- Lobby **UI** may still be laid out for ~4 nameplates; extra players still join and start.
- Combat / rest-site layouts at high player counts are vanilla’s problem — we don’t reimplement RMP’s layout modules.
- Affects gameplay (more players in the run) → `affects_gameplay: true`.
