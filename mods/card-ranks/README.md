# Card Ranks

Manual campfire card combining for **Slay the Spire 2** — a ground-up rebuild of the
[RankUpCards2](https://github.com/moviejw/RankUpCards2) idea (not a fork).

## v1 scope

| Feature | Behavior |
|---------|----------|
| **Combine** rest-site tile | Pick **two** identical cards (same `ModelId`, same rank) |
| Rank 2 | ×**1.5** damage (powered attacks) and block |
| Rank 3 | ×**3** damage and block |
| Pairing | Only **same identity + same rank** (plain+plain → R2, R2+R2 → R3). Mixed tiers rejected. |
| Free action | Default **on** (toggle *Spend campfire action* to consume rest) |
| Strike/Defend | **On** by default; includes vanilla *and* modded Basic Strike/Defend |
| Multiplayer | Owner selects; deck mutation broadcasts; peers mirror via net messages |
| Auto-combine | **Not in v1** (no pile-change auto-merge) |

Every player in a co-op lobby should run the same mod version so messages decode correctly.

## Settings

- **Allow combining Strike and Defend** — default **on** (starter decks can combine immediately).
- **Spend campfire action when combining** — off = free Combine tile.

Open **Settings → Mods** (BaseLib mod config). Card Ranks only appears if settings use
**static** properties (BaseLib requirement); instance properties are ignored and hide the mod.

## Build

```bash
cd mods/card-ranks
dotnet build -c Release
# copies CardRanks.dll + .json + PNGs into the game Mods/CardRanks folder
```

```bash
dotnet test tests/CardRanks.Tests.csproj -c Release
```

Requires **BaseLib** ≥ 3.3.0 (Workshop / local).

## Reference

Design notes and original art/strings were read from gitignored `reference/RankUpCards2`
(clone of the upstream mod). Icons shipped here are adapted from that reference for local rebuild work.

## Not in v1

- STS1-style auto-combine on deck entry
- Ultimate Strike/Defend conversion
- Neow “Encyclopedian” relic / Neow pool rewrite
- Harmony scaling of non-damage DynamicVars
