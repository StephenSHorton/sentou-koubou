# Card Ranks

Manual campfire card combining for **Slay the Spire 2** — a ground-up rebuild of the
RankUpCards idea (not a fork).

## Ladder

| Tier | Badge | Multiplier | From |
|------|-------|------------|------|
| **I** (blue) | `rank1.png` | ×**1.5** damage/block | **3** plain copies |
| **II** | `rank2.png` | ×**2** | **3** Tier I |
| **III** | `rank3.png` | ×**3** | **3** Tier II (max) |

- Same **card identity** and **same tier** only.
- Combine picker only lists cards that already have **3+ matching copies** (no permanent dim clutter).
- After the first pick, illegal partners stay dimmed.
- Select **3** cards: keep the highest-upgrade copy, sacrifice the other two.
- Upgrade levels on all three are **summed** onto the survivor.
- Rank lives as an enchantment (icon + mult). Bonuses stack beside it when multi-enchant is available (e.g. UncappedSpire).

## Auto bonus (each new tier)

When a combine **reaches** Tier I, II, or III, a random bonus is granted automatically:

**Clone**, **Soul's Power**, **Steady**, **Spiral**, **Imbued**, **Perfect Fit**, **Royally Approved**.

These are applied as **real vanilla enchantments** only when the card can stack them (UncappedSpire MultiEnchantment). Bonuses from sacrificed copies are **merged** onto the survivor.

Eligibility / fair rolls:
- **Soul's Power** only if the card has **local Exhaust** (vanilla `SoulsPower.CanEnchant`).
- **Spiral** only on basic Strike/Defend.
- **Royally Approved** only on **Attack/Skill** (vanilla `CanEnchantCardType` — Powers cannot take it).
- **Imbued** only on **Skill**.
- Rolls are **uniform among bonuses that can actually land** as a real Multi/vanilla leaf. **No keyword-only fallbacks** (Innate/Retain from `ApplyKeyword` alone are not in `SerializableCard` and desync multiplayer hand order).
- No “flag-only” Perfect Fit / Clone / Steady / Royally Approved counting as success.

**Clone** uses the **native** rest-site Clone button (`CLONE`, game art, **spends** the campfire action). We only inject that vanilla option when your deck has a Clone-enchanted card (same path as Paels’ Growth).

**Combine spends the campfire action** by default (mod setting can make it free again).

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
