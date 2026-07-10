# Brennen — Family Meme Pack (char 1)

Playable character for **sentou-koubou**: your older brother, League energy, Corvette shaka vibes.

## Kit (vanilla-sized)

| Piece | Detail |
|-------|--------|
| HP | 74 |
| Starter relic | **Duo Queue** — +1 Energy on combat turn 1 |
| Starting deck | 5× Strike, 4× Defend, **Feeding** |
| Reward pool | **20 Common / 35 Uncommon / 25 Rare** (80 cards) |
| Basics (non-reward) | Strike, Defend, Feeding |

Matches STS2 base-character pool size (Ironclad/Silent/etc.).

### Pillars

1. **Snowball** — kill rewards, openers, strength stacking  
2. **Tilt / int** — self-damage for payoff  
3. **Vision / peel** — Block + draw tools  
4. **Teamfight** — multi-hit and AoE  
5. **Chat control** — Weak / Vulnerable / Frail debuffs  

### Signature basics

| Card | Effect |
|------|--------|
| Strike / Defend | 6 dmg / 5 Block |
| **Feeding** | Heal enemy to full HP. Exhaust. (meme tax in the opener) |

## Catalog

```bash
# from repo root
python -m http.server -d docs 8765
# http://localhost:8765
```

Live: https://stephenshorton.github.io/sentou-koubou/

## Build

```bash
cp Directory.Build.props.example Directory.Build.props
dotnet restore && dotnet build
# Publish for .pck after assets/loc changes
```

Requires BaseLib + STS2 + MegaDot/Godot 4.5.1.

## Regenerating generated cards

New reward cards under `BrennenCode/Cards/{Common,Uncommon,Rare}/` (except hand-tuned kits)
are produced by:

```bash
python tools/generate_brennen_kit.py
```

Hand-authored keepers: Strike, Defend, Feed, Gank, Flash, Tilt, Ward, FirstBlood,
MainCharacter, MuteAll, Pentakill, AFK, Remake.
