# BLAKE — The Falcon (Character Design)

Companion to the Brennen & Whitney kits. Same conventions: ~35-card curated core set, all numbers are tuning knobs, ⚡ marks cards that are "a little broken" by cleverness rather than stats.

## The fantasy

A racer-brawler built around the most hype move in fighting game history: the fully charged punch. Blake spends turns winding up while the fight rages around him, protecting the investment, and then deletes something. The charge curve is **exponential** — doubling, not adding — so the last turn of charging is always worth more than all the previous ones combined. And like the real Falcon Punch, the wind-up is *interruptible*: getting hit cleanly costs you half of everything you've built.

Where Brennen converts **risk** and Whitney converts **resources**, Blake converts **time**. Every fight is a question of how long you dare to hold.

## Implementation notes (sentou-koubou)

- Mod path: `mods/blake/`
- Core helper: `BlakeCode/Charge.cs`
- Fist meter: `ChargePower` (Interrupt on unblocked enemy damage)
- Kit generator: `tools/generate_blake_kit.py`
- Branch: `feat/blake-character`

Full card text lives in the design spec and `docs/blake-cards.json`.
