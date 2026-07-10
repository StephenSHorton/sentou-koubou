# Brennen — Family Meme Pack (char 1)

Playable character for **sentou-koubou**: your older brother, League energy, Corvette shaka vibes.

## Kit

| Piece | Detail |
|-------|--------|
| HP | 74 |
| Starter relic | **Duo Queue** — +1 Energy on combat turn 1 |
| Starting deck | 5× Strike, 4× Defend, **Feed** |

### Cards

| Rarity | Card | Effect (short) |
|--------|------|----------------|
| Basic | Strike / Defend | 6 dmg / 5 Block |
| Uncommon | **Feeding** | Heal enemy to full HP. Exhaust. |
| Common | Gank | 4 dmg ×2 random |
| Common | Flash | 6 Block, Exhaust |
| Common | Tilt | 9 dmg, take 2 |
| Common | Ward | 7 Block, draw 1 |
| Uncommon | Main Character | 14 dmg (20 if ≤50% HP) |
| Uncommon | Mute All | 2 Weak to ALL |
| Uncommon | First Blood | 3 dmg; if Fatal, +1 Energy + draw |
| Rare | Pentakill | 6 dmg ×3 to ALL |
| Rare | AFK | 20 Block, Exhaust |
| Rare | Remake | Draw 5, Exhaust |

## Catalog

Open the family pack display page:

```bash
open ../../docs/index.html
# or serve: python3 -m http.server -d ../../docs
```

## Build

```bash
cp Directory.Build.props.example Directory.Build.props
dotnet restore && dotnet build
# Publish for .pck after assets/loc changes
```

Requires BaseLib + STS2 + MegaDot/Godot 4.5.1.
