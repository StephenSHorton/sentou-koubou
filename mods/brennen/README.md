# Brennen — Family Meme Pack (char 1)

Playable character for **sentou-koubou**: your older brother, League energy, Corvette shaka vibes.

## Kit (vanilla-sized, intentionally OP meme)

| Piece | Detail |
|-------|--------|
| HP | 74 |
| Starter relic | **Duo Queue** — +1 Energy on combat turn 1 |
| Combat body | Blender flipbook (`images/combat/`) via `CreateCustomVisuals` — see `tools/char-anim-pipeline/BLENDER_PIPELINE.md` |
| Starting deck | 5× Strike, 4× Defend, **Feeding** |
| Reward pool | **20 Common / 35 Uncommon / 25 Rare** (80 cards) |
| Basics (non-reward) | Strike, Defend, Feeding |
| Powers | **8** generated power cards + custom hook powers |

### Pillars

1. **Snowball** — kills grant Strength, Fatal payoffs, First Blood  
2. **Tilt / int** — self-damage package (Tilt, Inting, Mental Boom, Inter)  
3. **Vision / peel** — Wards, Retain Block, Peel Bot  
4. **Teamfight** — AoE, Pentakill, Full Clear, Penta Secure  
5. **Chat control** — Weak / Frail / Mute All / Chat Mod / GG EZ  

### Signature basics

| Card | Effect |
|------|--------|
| Strike / Defend | 6 dmg / 5 Block |
| **Feeding** | Heal enemy to full HP. Exhaust. (meme tax in the opener) |

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

## Regenerating

```bash
python tools/generate_brennen_kit.py
```

Hand-authored keepers: Strike, Defend, Feed, Gank, Flash, Tilt, Ward, FirstBlood,
MainCharacter, MuteAll, Pentakill, AFK, Remake.

Custom powers live under `BrennenCode/Powers/` (Snowball, Macro, Mental Boom, Inter,
Penta Secure, Hard Stuck, Chat Mod, Main Character Syndrome).
