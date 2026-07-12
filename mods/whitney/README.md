# Whitney — Atelier Witch (char 2)

Playable character for **sentou-koubou**: elemental seals, dual mana, Witch Hat Atelier vibes.

## Kit

| Piece | Detail |
|-------|--------|
| HP | 74 |
| Starter relic | **Traveler's Inkpot** — +3 Ink at combat start |
| Starting deck | 4× Spark, 3× Ripple, **Channel Ink**, **Novice Seal**, **Apprentice Seal** |
| Dual mana | **Energy** (turn) + **Ink** (banked power counter) |
| Compendium | Pool `SeenByDefault` — full kit visible without a run |
| Pillars | Fire / Water / Earth / Air + confluence spends |

### Dual purpose

Most cards do two jobs — e.g. damage **and** gain Ink, Block **and** Ink, Weak **and** Ink, damage **and** Attunement.

**Attunement** is real: attacks deal +Attunement damage while stacked.

### Ink loop (taught in starter)

```
Inkpot / Channel / Novice → bank Ink → Apprentice Seal / Seal Press / Grand Seal…
```

| Card | Role |
|------|------|
| Channel Ink | Gain 2 Ink |
| Novice Seal | 0-cost: dmg + Weak + gain Ink |
| **Apprentice Seal** | Spend 1 Ink → 8 dmg + Weak (starter spender) |
| **Seal Press** (C) | Spend 1 Ink → dmg + Block |
| Grand Seal / Focused Stroke / Cataclysm… | Bigger spenders |

Playability: cards require **Energy and Ink** (`base.IsPlayable && CanAfford`).

## Build

```bash
cp Directory.Build.props.example Directory.Build.props
# set GodotPath if publishing .pck
dotnet restore && dotnet build
```

Requires BaseLib + STS2 + MegaDot/Godot 4.5.1. Quit STS2 before build (DLL lock).

## Art

Character select / portraits: Whitney D3 lock (indigo hat matches dress, blue eyes, cyan quill).
Card regen via locked portrait + `tools/install_sts2_art.py`.
