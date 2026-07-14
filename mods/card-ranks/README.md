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

## Optional bonus roll (each new tier)

When a combine **reaches** Tier I, II, or III, a popup offers:

- **Roll** — random bonus from a positive-only pool  
- **Skip** — keep the tier only  

Pool: **Clone**, **Soul's Power**, **Steady**, **Spiral**, **Imbued**, **Perfect Fit**, **Royally Approved**.

Bonuses use keywords / Replay / rank-enchantment hooks so they **stack with rank** (game still allows only one *Enchantment* object — rank stays that object; bonuses are layered).

Setting: **Offer optional bonus roll when a card reaches a new tier**.

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
