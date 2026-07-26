# Ping Rage

Spices up the multiplayer **Ping** button (next to End Turn).

## Features

1. **Random funny lines** — ~30 impatient one-liners, shuffled (no immediate repeat) instead of the vanilla character banter string  
2. **Rage scaling** — mash Ping quickly and the speech bubble grows (up to ~2.8×)  
3. **Unhinged wiggle** — higher rage → stronger position thrash and rotation shake  
4. **Faster mash allowed** — debounce ~160ms (vanilla is 1s) so the ramp feels responsive  

Rage decays if you stop mashing for a couple of seconds.

## Install

```bash
cd mods/ping-rage
dotnet build -c Release
```

Enable **Ping Rage**. No dependencies. Visual / flavor only (still sends the normal end-turn ping net message).

## License

MIT — sentou-koubou.
