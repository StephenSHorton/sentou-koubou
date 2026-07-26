# Shared Combat Positions

In multiplayer combat, **vanilla always puts your own character in the front** of the party line. Each player therefore sees a different lineup.

This mod lays out characters by **lobby / host slot order** (`RunState.Players` index) so **everyone sees the same positions as the host**.

## Why

`NCombatRoom.PositionPlayersAndPets` builds the player list like this:

```csharp
if (LocalContext.IsMe(...))
    list.Insert(0, item);  // local always first
else
    list.Add(item);
```

We keep the same grid / spacing / pet / Osty logic, but sort by slot index instead of “me first”.

## Install

```bash
cd mods/shared-combat-positions
dotnet build -c Release
```

Enable **Shared Combat Positions**. No dependencies.

**Recommended:** enable on **all** multiplayer peers so everyone’s view matches. Visual-only if only some clients run it (each client only changes their own screen).

## Scope

| | |
|---|---|
| Combat party X/Y layout | yes — host slot order |
| Draw order (who is “in front”) | yes — same order |
| Enemy placement | unchanged |
| Singleplayer | no-op (one player) |
| Affects gameplay / targeting logic | no (visual positions only) |

## License

MIT — sentou-koubou.
