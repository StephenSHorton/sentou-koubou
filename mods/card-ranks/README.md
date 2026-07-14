# Card Ranks

Manual campfire card combining for **Slay the Spire 2** — a ground-up rebuild of the
RankUpCards idea (not a fork).

## Ladder

| Tier | Badge | Multiplier | From |
|------|-------|------------|------|
| **I** (blue) | `rank1.png` | ×**1.5** damage/block | Two plain copies |
| **II** | `rank2.png` | ×**2** | Two Tier I |
| **III** | `rank3.png` | ×**3** | Two Tier II (max) |

- Same **card identity** and **same tier** only (picker dims illegal partners).
- Upgrade levels on the two cards are **summed** onto the survivor.
- Rank lives in the enchantment slot (icon + mult). It is **never cleared** for bonuses.

## Auto bonus (each new tier)

When a combine **reaches** Tier I, II, or III:

1. A random bonus is granted automatically (no dialog).
2. The card flashes in a reward-style preview, then settles.

Pool: **Clone**, **Soul's Power**, **Steady**, **Spiral**, **Imbued**, **Perfect Fit**, **Royally Approved**.

Bonuses use keywords / Replay / rank hooks so they **stack with rank** (one Enchantment object = the tier; bonuses layer beside it).

If every pool bonus is already on that card, no new bonus is granted (still shows the showcase).

Setting: **Auto-grant a random bonus when a card reaches a new tier**.

## Multiplayer

Every player needs the mod. Owner selects + rolls; peers mirror deck mutation + bonus.

## Build

```bash
cd mods/card-ranks
dotnet build -c Release
dotnet test tests/CardRanks.Tests.csproj -c Release
```

Copies `CardRanks.dll`, `.json`, `.pck` (rank icons), and rest-site PNG into the game Mods folder.

## Reference

Original art/strings: gitignored `reference/RankUpCards2`.
