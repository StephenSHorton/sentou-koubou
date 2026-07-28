# Combat Queue Fix

Fixes a vanilla multiplayer softlock where cards queue but never play.

## Cause

After combat starts, a buffered **map vote** (`VoteForMapCoordAction`, type **NonCombat**) can still enqueue via `RunLocationTargetedMessageBuffer`. `ActionQueueSet.GetReadyAction` **skips** NonCombat during combat but does **not** dequeue it, so every later `PlayCardAction` / End Turn for that player sits forever behind the vote.

## Fix

- Cancel NonCombat heads while combat is active (before each `GetReadyAction` + on `CombatStarted`).
- Refuse new NonCombat enqueues while `_isInCombat`.

## Install

Unzip into `Slay the Spire 2/mods/`. No dependencies. Affects multiplayer reliability only.
