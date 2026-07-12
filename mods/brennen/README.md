# Brennen — Family Meme Pack (char 1)

Playable character for **sentou-koubou**: your older brother, League energy, tank main vibes.

## Kit (vanilla-sized)

| Piece | Detail |
|-------|--------|
| HP | **82** |
| Starter relic | **Duo Queue** — +1 Energy on combat turn 1 |
| Combat body | Blender flipbook (`images/combat/`) via `CreateCustomVisuals` — see `tools/char-anim-pipeline/BLENDER_PIPELINE.md` |
| Starting deck | **5 Strike / 5 Defend** (Feed is a reward card, not a starter) |
| Reward pool | ~20 Common / 35 Uncommon / 25 Rare (+ kit-pass package cards) |
| Basics (non-reward) | Strike, Defend |
| Signature reward | **Feeding** (Uncommon) — full heal enemy **and** self. Exhaust |

### Pillars

1. **Barricade wall** — **Proxy Camp** (keep Block EOC) + **Tower Dive** (damage = Block)  
2. **Tilt / int** — self-damage package (Tilt, Inting, Mental Boom, Inter) + heals (**Second Wind**, **Recall**) + HP→Energy (**Buyback**, **Blood for Blue**, Throw)  
3. **Vision / peel** — Wards, Retain Block, Peel Bot  
4. **Teamfight / tank** — AoE, Pentakill; **Main Tank** (solo double dmg taken + SotT Block); **Frontline** (MP TankPower)  
5. **Chat control** — Weak / Frail / Mute All / Chat Mod / GG EZ  

### Signature cards (kit pass)

| Card | Effect |
|------|--------|
| Strike / Defend | 5 dmg / 6 Block (tank basics) |
| **Proxy Camp** | Barricade — Block not removed at turn start |
| **Tower Dive** | Deal damage equal to Block (×2 if upgraded) |
| **Main Tank** | Take double attack damage; SotT gain Block |
| **Feeding** | Full heal enemy and self. Exhaust. Uncommon reward |

### Role Diffs (rares)

| Diff | Job |
|------|-----|
| TOP Diff | Island 1v1 — big hit + Block, bonus if only 1 enemy |
| JG Diff | Pathing — random multi-hit + draw |
| MID Diff | Prio — draw + energy (Exhaust) |
| ADC Diff | DPS — high multi-hit |
| SUP Diff | Peel — Block + Weak ALL |

## Catalog

```bash
python -m http.server -d docs 8765
# http://localhost:8765
```

Live: https://stephenshorton.github.io/sentou-koubou/

## Build

```bash
cp Directory.Build.props.example Directory.Build.props
dotnet restore && dotnet build
```

Quit STS2 before build — the game locks `Mods/*.dll`.

## Regenerating

```bash
python tools/generate_brennen_kit.py
```

Hand-authored keepers: Strike, Defend, Feed, Gank, Flash, Tilt, Ward, FirstBlood,
MainCharacter, MuteAll, Pentakill, AFK, Remake, Peel package, Frontline, kit-pass cards.

Custom powers under `BrennenCode/Powers/` (Snowball, Macro, Mental Boom, Inter,
Penta Secure, Hard Stuck, Chat Mod, Main Character Syndrome, **Main Tank**).
