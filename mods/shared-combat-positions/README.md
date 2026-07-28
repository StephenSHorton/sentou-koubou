# Shared Combat Positions

Multiplayer combat visual QoL:

1. **Shared lineup** — characters use **lobby / host slot order** instead of always putting you first  
2. **Always-visible teammate state** — HP, block, and power/status icons stay on screen **without hovering**  
3. **HP above bodies** — ally state UI uses a higher canvas z-index so multi-row / overlapping characters do not bury health bars

## Shared lineup

Vanilla builds the combat party list like this:

```csharp
if (LocalContext.IsMe(...))
    list.Insert(0, item);  // local always first
else
    list.Add(item);
```

We keep the same grid / spacing / pet / Osty logic, but sort by `RunState.GetPlayerSlotIndex` so every client matches the host’s view.

## Always-visible HP / statuses

For remote players (and their pets), vanilla:

- `HideImmediately()` on spawn  
- `AnimateIn` only on hover  
- `AnimateOut` on unhover  

This mod forces `AnimateIn` after setup and after unhover, and stops hiding your own bar while you hover a teammate — **only while combat is active**.

When combat ends (or the combat room exits), remote state UI is force-hidden so HP/status cannot paint through the map.

Nameplates still appear on hover (less clutter). Hover tips still work.

## Install

```bash
cd mods/shared-combat-positions
dotnet build -c Release
```

Enable **Shared Combat Positions**. No dependencies.

**Recommended:** enable on **all** multiplayer peers so everyone’s view matches.

## Scope

| | |
|---|---|
| Combat party X/Y layout | host slot order |
| Draw order | same order |
| Teammate HP / block / powers | always visible |
| Ally HP vs overlapping sprites | drawn on top |
| Enemy placement | unchanged |
| Singleplayer | no-op for lineup; no remote allies |
| Gameplay / targeting | visual only |

## License

MIT — sentou-koubou.
