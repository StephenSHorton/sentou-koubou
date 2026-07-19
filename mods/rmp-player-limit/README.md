# DEPRECATED

**Use [`mods/mp-player-limit`](../mp-player-limit/) instead** — clean Harmony rewrite of the multiplayer capacity raise.

This folder is the old IL-patched workshop RMP binary and is no longer recommended.

---
# Remove Multiplayer Player Limit (Sentou fix)

Fork/fix of [Rain156/sts2-RMP-Mods](https://github.com/Rain156/sts2-RMP-Mods) workshop build **0.1.8**.

## Why this exists

On current STS2, the workshop DLL hard-crashes when a multiplayer lobby tries to start:

```text
MissingMethodException:
  UInt64 StringHelper.GetDeterministicHashCode(String)
  at ExtendedLobbyModule.NormalizeRandomCharacters / BuildActsForBeginRun
```

The game API is now:

- `int StringHelper.GetDeterministicHashCode(string)`
- `Rng(uint seed, int counter = 0)` (not `Rng(ulong)`)

That exception runs every frame in `LobbyManagerNode._Process`, so everyone stays stuck on character select after ready-up.

## What we ship

| Path | Role |
|------|------|
| `vendor/RemoveMultiplayerPlayerLimit.original.dll` | Unmodified workshop 0.1.8 binary |
| `vendor/RemoveMultiplayerPlayerLimit.pck` | Unmodified assets/PCK |
| `tools/PatchRmp/` | Mono.Cecil IL patcher (re-run against new `sts2.dll`) |
| `dist/RemoveMultiplayerPlayerLimit.dll` | Patched output (build artifact) |

License of upstream work remains whatever Rain_G published (README claims CC0 on GitHub). We only redistribute a binary patch of the same mod.

## Build / install

```bash
cd mods/rmp-player-limit
dotnet build -c Release
```

Installs to `Slay the Spire 2/mods/RemoveMultiplayerPlayerLimit/`.

## Important: disable the Steam Workshop copy

If both the workshop item and this local folder load, you can get double-init or the broken DLL again.

1. In Steam: unsubscribe **or** leave unsubscribed / disabled in the in-game Mods list.
2. This repo already renames the workshop manifest to `*.DISABLED_*` on machines that hit the bug; Steam may restore it on update.

**Every player in the lobby needs this fixed build** (or no RMP at all). Mixing broken workshop + fixed local will soft-lock ready-up again.

## Re-patch after a game update

```bash
dotnet run --project tools/PatchRmp -c Release -- \
  vendor/RemoveMultiplayerPlayerLimit.original.dll \
  "C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64/sts2.dll" \
  dist/RemoveMultiplayerPlayerLimit.dll
```

If patch sites go missing (`hashFixes=0`), the upstream DLL changed — re-vendor from workshop and update the patcher.

