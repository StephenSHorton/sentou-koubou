# Battle Ball

Combat-only **ball toss** for Slay the Spire 2 (solo or multiplayer).

## What it does

During battles a basketball spawns above a **mid-screen floor**, with one **side-view hoop on the right** (opening faces left). Balls are clamped so they **cannot leave the play area**.

| Input | Action |
|-------|--------|
| **Click-drag** a ball | Grab, then release to **throw** |
| **+** / **−** (bottom-left) | Spawn / remove balls (1–8) |

**Scoring:** drop a ball **down through** the rim → score ticks up and **confetti** bursts.

Does **not** change combat rules, damage, or rewards.

## Multiplayer

- Grab / throw / spawn / despawn / score are **reliable**.
- Free-ball **and held-cursor** motion stream at ~20–30 Hz from the holder/thrower (remotes track grabs, not freeze at grab point).
- Ball ids are peer-unique so multiple balls stay in sync.

## Build

```bash
dotnet build -c Release
```

Copies `BattleBall.dll` + `.json` + assets into `mods/BattleBall/`.
