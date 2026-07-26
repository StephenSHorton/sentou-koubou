# Max HP Size

Scales each **playable character’s combat size** with their **current max HP**.

Based on Workshop **MaxHpSizeMod** by BittersweetGirlJay — same formula, local monorepo build so you can unsubscribe from the Steam Workshop item.

## Formula

```
scale = 1 + (maxHp - startingHp) / startingHp
scale = max(scale, 0.25)
```

- `startingHp` = character’s defined starting max HP  
- `maxHp` = current run max HP  
- At base HP → scale `1.0`  
- Double max HP → scale `2.0`  
- Below base → shrinks, not below `0.25`

## Behavior

- Updates when max HP changes (`SetMaxHpInternal`) with a short tween  
- Re-applies when a room is entered so combat nodes load at the right size  
- Only player creatures (all local multiplayer players)

## Install

1. Build / unzip into `Slay the Spire 2/mods/MaxHpSize/`
2. **Unsubscribe / disable** Workshop **MaxHpSizeMod** (duplicate if both run)
3. Enable **Max HP Size**

```bash
cd mods/max-hp-size
dotnet build -c Release
```

## License

MIT-style reimplementation for sentou-koubou; original Workshop idea by BittersweetGirlJay.
