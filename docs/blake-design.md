# BLAKE — The Falcon (New Character Design)

Companion to the Brennen & Whitney redesign doc. Same conventions: ~35-card curated core set, all numbers are tuning knobs, ⚡ marks cards that are "a little broken" by cleverness rather than stats.

---

## The fantasy

A racer-brawler built around the most hype move in fighting game history: the fully charged punch. Blake spends turns winding up while the fight rages around him, protecting the investment, and then deletes something. The charge curve is **exponential** — doubling, not adding — so the last turn of charging is always worth more than all the previous ones combined. And like the real Falcon Punch, the wind-up is *interruptible*: getting hit cleanly costs you half of everything you've built.

Where Brennen converts **risk** and Whitney converts **resources**, Blake converts **time**. Every fight is a question of how long you dare to hold.

## Core rules & keywords

- **Charge** — a stored damage value displayed as a fist meter beside Blake. Starts each combat at a base of 3 (the starter relic sets this).
- **Rev** — the charge verb: **double your Charge.** All charging is multiplicative, which is the entire exponential curve in one word: 3 → 6 → 12 → 24 → 48 → 96. The first Rev is worth +3; the fifth is worth +48. Charging literally gets faster the more you've done it.
- **Unleash** — deal damage equal to your Charge, then **reset Charge to base.** The punch spends the pool.
- **Interrupt (the risk rule)** — whenever you take unblocked attack damage, your Charge is **halved** (rounded down, never below base). Blocking isn't just survival for Blake — it's protecting the wind-up. This is the telegraphed-punch tension, mechanized: the enemy's attack intent is a threat *to your fist*.
- **Sweetspot** — a conditional bonus, usually keyed to **enemy intent** ("if the enemy intends to Attack..."). This is the read: punishing an opponent for pressing a button. Intent-reading is nearly untouched design space in StS and it's the most fighting-game thing a deckbuilder can do.
- **Combo N** — bonus if this is at least the Nth card you've played this turn. The speed half of the kit.
- **Follow-Through** — excess damage from this attack hits another enemy. Overkill carries through. (Appears on specific cards; it's what makes a 100+ damage punch feel correct in multi-enemy fights.)

**Why base Charge matters more than it looks:** because all growth is doubling, raising the base from 3 to 6 permanently doubles *every future punch* downstream. Base-Charge increases are the character's "charge faster" upgrades, and they're inherently exponential-friendly. Several rares and relics pull this lever.

## Starter relic

**Racer's Gauntlet** — Your Charge starts each combat at 3 and resets to 3 after you Unleash. *"Show me your moves."*
(The Interrupt rule lives in the Charge keyword itself, like Stance rules — not on the relic.)

## Starting deck (10)

| Card | Cost | Text |
|---|---|---|
| Jab ×4 (Attack) | 1 | Deal 6 damage. *Up: 9.* |
| Guard ×4 (Skill) | 1 | Gain 5 Block. *Up: 8.* |
| Rev Up (Skill) | 1 | **Rev.** *Up: Rev and draw 1.* |
| Haymaker (Attack) | 1 | **Unleash** — deal damage equal to your Charge. *Up: deal Charge + 6.* |

The full loop — build, protect, spend — is in the starter deck. Turn one Haymaker hits for a pathetic 3, which is itself the tutorial: this card is only as good as the time you feed it.

## Commons (10)

| Card | Type/Cost | Text | Role |
|---|---|---|---|
| **Warm Engine** | Skill, 1 | Rev. Gain 3 Block. *Up: 6 Block.* | The bread-and-butter: charge *safely*. |
| **Wind-Up** | Skill, 0, Exhaust | Rev. *Up: also draw 1.* | A free double, once. |
| **Raptor Boost** | Attack, 2 | Deal 6 damage. Rev. *Up: 9.* | Damage and charging in one motion. |
| **Guard Up** | Skill, 1 | Gain 6 Block. If your Charge is above base, gain 9 instead. *Up: 8 / 12.* | Blocks harder when there's something worth protecting. |
| **One-Two** | Attack, 1 | Deal 4 damage twice. **Combo 3:** draw 1. *Up: 6 twice.* | Combo-counter starter. |
| **Dash Attack** | Attack, 0 | Deal 3 damage. **Combo 3:** deal 7 instead. *Up: 5 / 10.* | 0-cost filler that rewards long turns. |
| **Shoulder Check** | Attack, 1 | Deal 8 damage. **Sweetspot** — enemy intends to Attack: apply 2 Weak. *Up: 11.* | Intro to intent-reading. |
| **Falcon Dive** | Attack, 1 | Deal 7 damage. **Fatal:** gain 1 Energy and Rev. *Up: 10.* | "YES!" — kill confirms fuel the next punch. |
| **Sidestep** | Skill, 0 | Gain 3 Block. Retain. *Up: 5.* | A pocketed dodge for the big enemy turn. |
| **Show Me Your Moves** | Skill, 0 | Apply 1 Weak. Draw 1 card. *Up: 2 Weak.* | The taunt cantrip. |

## Uncommons (14)

**Charge engines**
- **Full Throttle** — Skill, 2. Rev **twice**. *Up: costs 1.* One turn of pure commitment: ×4.
- **Ignition** — Skill, 0, Exhaust. Rev. *Up: also draw 1.* (Second copy pattern of Wind-Up so charge decks can hit density.)
- **Redline** — Skill, 1. Rev. If your Charge is 20 or more, Rev **again**. *Up: threshold 12.* ⚡ Acceleration on top of acceleration — the deeper you are, the faster you go. This card is why late charging feels like a runaway engine.
- **Slipstream** — Power, 1. Whenever you play your 4th card in a turn, Rev. *Up: 3rd card.* ⚡ The speed→charge bridge: cantrip-heavy combo turns now double you for free.
- **Perfect Shield** — Skill, 1. Gain 8 Block. If you block ALL attack damage this turn, Rev at end of turn. *Up: 11.* ⚡ The parry. Defense that accelerates you — the "powershield into punish" fantasy.

**Spending the Charge**
- **Pressure** — Attack, 1. Deal damage equal to **half** your Charge (rounded down). **Doesn't reset.** *Up: +4 flat.* The jab archetype in one card: at 48 Charge you're poking for 24 without ever firing the punch.
- **Stored Power** — Skill, 1. Gain Block equal to half your Charge. *Up: two-thirds.* The fist as a shield. With Pressure, enables the "never actually punch" build.
- **Falcon Kick** — Attack, 2. **Unleash** — deal damage equal to your Charge to ALL enemies. *Up: Charge + 8 to ALL.* The multi-enemy release. Clearing a hallway fight with one kick is the mid-run power fantasy.
- **Clean KO** — Attack, 1. Deal damage equal to your Charge. **If this kills the enemy, don't reset.** *Up: Charge + 8.* ⚡ The kill-confirm punch: size your Charge exactly, KO the target, and walk to the next enemy still fully loaded. Precision is the skill test.

**Reads & tech**
- **Knee of Justice** — Attack, 1. Deal 8 damage. **Sweetspot** — enemy intends to Attack: deal 22 instead. *Up: 10 / 28.* ⚡ The Knee. Sourspot when whiffed, electric when you call their button.
- **Grab** — Attack, 1. The target **loses ALL Block**, then deal 6 damage. *Up: 9.* ⚡ Grabs beat shields — the fighting-game triangle imported wholesale. Anti-Block tech barely exists in StS; here it's an identity.
- **Spot Dodge** — Skill, 1, Exhaust. The next time an enemy attacks you this turn, it **misses entirely**. *Up: costs 0.* Full invulnerability for one hit — the cleanest possible Charge protection.
- **YES!** — Skill, 0, Exhaust. If you Unleashed this turn: gain 2 Energy and draw 2 cards. *Up: gain 3 Energy.* ⚡ The victory-lap taunt: punch, flex, rebuild in the same turn.
- **Warm-Up Lap** — Skill, 1. Draw 2 cards. **Combo 4:** gain 1 Energy. *Up: draw 3.* Speed-deck smoothing.

## Rares (10)

- **FALCON PUNCH** — Attack, 3. **Unleash** — deal damage equal to **DOUBLE** your Charge. **Follow-Through** (excess damage hits another enemy). *Up: costs 2.* ⚡⚡ The signature. A built-in extra Rev at the moment of release, and overkill that decapitates the back rank. At Charge 48 this is 96, and whatever survives the target eats the rest.
- **Blue Falcon** — Attack, 3, Exhaust. Deal damage equal to your Charge to ALL enemies. **This doesn't reset your Charge.** *Up: costs 2.* ⚡ The Final Smash: run everyone over once per fight and keep the fist loaded.
- **G-Diffuser** — Power, 2. **At the start of your turn, Rev.** *Up: costs 1.* ⚡ The scariest card in the pool: a passive doubling engine. Left alone for five turns it wins the fight by itself — the counterplay is baked into the Interrupt rule, since every clean hit you take halves the snowball.
- **Super Armor** — Power, 2. Your Charge can no longer be halved. *Up: costs 1.* ⚡ Turns off the risk rule entirely. Enables the degenerate "never block, just wind up" build — on purpose, at rare, as the reward for finding it.
- **Champion's Fist** — Power, 1. Increase your **base** Charge by 3 (now, and after every reset). *Up: by 5.* ⚡ Deceptively simple: because all growth doubles, raising the floor multiplies every future punch. Stacked copies compound. This is the "you charge faster now, permanently" card.
- **Muscle Memory** — Skill, 1. Rev once, **plus once more for each time you've Unleashed this combat.** *Up: costs 0.* ⚡ Your third punch of a fight rebuilds almost instantly — charging literally accelerates the more you've released, at the combat scale.
- **Highlight Reel** — Power, 1. Whenever you Unleash, gain 2 Energy and draw 2 cards. *Up: costs 0.* The punch-often engine: every release refunds the tempo to start the next one.
- **Heat Haze** — Power, 2. Whenever you Rev, deal 4 damage to ALL enemies. *Up: 6.* ⚡ The fist gets so hot that *charging is the attack*. With G-Diffuser, passive AOE every turn; the jab build's second win condition.
- **Hard Read** — Skill, 1, Exhaust. **Sweetspot** — if the enemy intends to Attack: it is **Stunned** and does nothing this turn. Otherwise, draw 1 card. *Up: also gain 1 Energy on a successful read.* ⚡ The read of the century. Player-inflicted stuns are enormous, which is why it exhausts and lives at rare.
- **Photo Finish** — Attack, 1. Deal damage equal to your Charge. **Sweetspot** — this kills the last enemy: heal 8 HP and gain 15 Gold. *Up: heal 12, gain 25.* Crossing the line in style. A small rare that makes "end the fight with the punch" feel like taking the checkered flag.

## Non-starter relic sketches

- **Racing Gloves** (common) — Your base Charge is increased by 2.
- **Pit Crew** (uncommon) — The first time you Rev each turn, gain 3 Block.
- **Trophy Belt** (rare) — Your Charge **persists between combats** (it still resets when you Unleash). ⚡ Skip the punch in a hallway fight and walk into the elite pre-loaded at 48.
- **Booster Coil** (boss) — At the start of each combat, Rev twice.

## Combo lines the design wants players to discover

1. **The turbo turtle:** G-Diffuser + Super Armor + Champion's Fist. Base 6, auto-doubling, uninterruptible. Do nothing but block for four turns, then FALCON PUNCH for ~190 with Follow-Through carving into the next enemy. The purest form of the fantasy.
2. **The fist that never fires:** Champion's Fist + Redline to ~48, then Pressure jabs for 24, Stored Power blocks for 24, Heat Haze pings the room every Rev. You win without ever Unleashing — the threat of the punch *was* the punch.
3. **Machine-gun punches:** Highlight Reel + YES! + Muscle Memory. Unleash → refund energy and cards → Muscle Memory triple-Revs you back to strength → Haymaker again the same turn. By the end of a boss fight you're releasing every turn.
4. **The read deck:** Perfect Shield + Knee of Justice + Hard Read. Block everything, Rev off every parry, and punish every Attack intent with the electric Knee. Blake as a pure reaction character.
5. **Precision KOs:** Clean KO sized exactly lethal on enemy one, roll the intact Charge into enemy two, Blue Falcon to finish the room without ever resetting.

## Design risks / playtest first

1. **Doubling balance is knife-edge.** One extra Rev is +100%, so cost the Rev effects strictly (~1 energy per Rev baseline) and expect Redline, Slipstream, and G-Diffuser to need the most tuning. If the curve runs away, the levers are base Charge (3 → 2) and Falcon Punch's multiplier — never add a hard cap, which would kill the fantasy.
2. **Interrupt feel.** Halving must floor at base so a bad turn stings without a death spiral. If players feel the rule is invisible, animate it hard: the fist visibly dims and the meter cracks.
3. **Dead-hand turns.** If charge decks draw punches with nothing banked, they stall. Pressure, Stored Power, and Guard Up exist to make mid-charge turns feel productive — watch whether commons need one more Charge-reader.
4. **Stun and anti-Block power level.** Hard Read and Grab import fighting-game answers into a game not balanced around them; both are deliberately conservative (exhaust / small numbers) until proven safe.
5. **IP note for shipping:** this doc uses the homage names (FALCON PUNCH, Blue Falcon, Knee of Justice) to keep the fantasy vivid internally. For release they'd need a rename pass — the mechanics are all original, so it's purely cosmetic (e.g., KESTREL PUNCH, the Azure Comet, Knee of Conviction).

---

## Implementation notes (sentou-koubou)

- Mod path: `mods/blake/`
- Core helper: `BlakeCode/Charge.cs`
- Fist meter: `ChargePower` (Interrupt on unblocked enemy damage)
- Kit generator: `tools/generate_blake_kit.py`
- Branch: `feat/blake-character`
- Identity lock: **M** → `docs/assets/blake/variants/blake_locked_portrait.jpg` (+ `docs/assets/blake/portrait_sts2.jpg`)
- Combat lock: `docs/assets/blake/variants/blake_combat_right.png` (ready idle, faces right, 3/4 face)
- Green plate / chroma: `blake_combat_right_green.jpg` · `tools/chroma_blake_fullbody.py`
- Visual bible: `tools/blake_visual_bible.json`
- Card catalog JSON: `docs/blake-cards.json`
- Hard art rules: fists/gauntlets only (no sword); never use Brennen portrait as face style-ref
