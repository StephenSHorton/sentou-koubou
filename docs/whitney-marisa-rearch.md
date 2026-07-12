# Whitney re-architecture (Marisa → ink)

## Decision

Scrap the prior Whitney seal/element dual-mana kit. Rebuild Whitney on the **MarisaMod**
character architecture (complete Amplify / enchant / power suite), rethemed to
**atelier ink** with violet card chrome.

## Mapping

| Marisa | Whitney |
|--------|---------|
| Starlit enchantment/power | **Inkbound** |
| Charge-Up | **Saturate** (loc); class still `ChargeUpPower` until rename pass |
| Amplify | Amplify (kept) |
| Spark tag/cards | Spark (ink sparks) |
| Blue frames / orbs | Violet Whitney energy + recolored frames |
| Spine combat | Whitney Blender flipbook (`WhitneyCombatVisuals`) |

## Source

Mechanics adapted from local `STS2_MarisaMod` (authors: Flynn, Hell, Hohner_257, Kishin, Samsara).
Artwork is temporary placeholders from that pack until Whitney-generated art ships.

## Art still TODO

- Card portraits for every card (STS2 graphic, D3 Whitney lock)
- Violet recolor of card frames (attack/skill/power)
- Power/relic cutouts in ink palette
- Cookie / merchant / hand UI if we keep those paths
- Optional custom energy counter scene

## Build

```bash
cd mods/whitney
dotnet build -c Release
```
