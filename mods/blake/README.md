# Blake — The Falcon

Playable STS2 character mod in the **sentou-koubou** monorepo. Companion to Brennen (tank) and Whitney (ink mage).

## Fantasy

A racer-brawler built around the fully charged punch. Blake spends turns **Revving** (doubling) **Charge**, protects the investment with Block, then **Unleashes**. The charge curve is exponential. Clean hits **Interrupt** (halve Charge).

Captain Falcon homage for internal design names; mechanics are original.

## Core keywords

| Keyword | Meaning |
|---------|---------|
| **Charge** | Stored fist damage (base 3 via starter relic) |
| **Rev** | Double Charge |
| **Unleash** | Deal Charge damage, reset to base |
| **Interrupt** | Unblocked enemy attack damage halves Charge (floor = base) |
| **Sweetspot** | Bonus if enemy intends to Attack |
| **Combo N** | Bonus if this is the Nth+ card this turn |
| **Follow-Through** | Overkill hits another enemy |

## Kit size

- Starting deck: Jab×4, Guard×4, Rev Up, Haymaker
- Starter relic: **Racer's Gauntlet**
- Rewards: 10 Common / 14 Uncommon / 10 Rare
- Relics: Racing Gloves, Pit Crew, Trophy Belt, Booster Coil

## Build

```bash
cd mods/blake
dotnet build -c Release
```

Requires STS2 + BaseLib. Copy `.dll` / `.json` / `.pck` into the game `Mods/Blake/` folder (MSBuild target does this when paths resolve).

Regenerate cards from the kit definition:

```bash
python tools/generate_blake_kit.py
```

## Art

Identity lock + card portraits follow the Brennen/Whitney STS2 drawn pipeline in `AGENTS.md`. Placeholder portraits ship until the lock pass is done.

Reference photos for Blake live in the design session; locked portrait should land under `docs/assets/blake/variants/`.
