# Whitney — Atelier Witch (char 2)

Playable character for **sentou-koubou**: elemental seals, dual mana, Witch Hat Atelier vibes.

## Kit (vertical slice)

| Piece | Detail |
|-------|--------|
| HP | 74 |
| Starter relic | **Traveler's Inkpot** — +3 Ink at combat start |
| Starting deck | 4× Spark, 4× Ripple, **Channel Ink**, **Novice Seal** |
| Dual mana | **Energy** (turn) + **Ink** (banked power counter) |
| Pillars | Fire / Water / Earth / Air + confluence spends |

### Dual purpose

Most cards do two jobs — e.g. damage **and** gain Ink, Block **and** Ink, Weak **and** Ink, damage **and** Attunement.

Ink spenders: **Grand Seal** (3), **Focused Stroke** (2 + Attunement scale), **Cataclysm Seal** (5).

### Signature basics

| Card | Effect |
|------|--------|
| Spark / Ripple | 6 dmg / 5 Block |
| Channel Ink | Gain 2 Ink |
| Novice Seal | 5 dmg + Weak + 1 Ink (0 cost) |

## Build

```bash
cp Directory.Build.props.example Directory.Build.props
# set GodotPath if publishing .pck
dotnet restore && dotnet build
```

Requires BaseLib + STS2 + MegaDot/Godot 4.5.1.

## Art

Character select / portraits: Whitney likeness + Witch Hat Atelier outfit language
(tall soft hat, layered atelier dress, elemental ink seals).
