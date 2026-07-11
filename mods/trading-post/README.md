# Trading Post

Co-op shop trading for **Slay the Spire 2** multiplayer. At every merchant, each player
gets **one trade per visit** — pick one, use it, and you're done trading (shopping with
the merchant stays open as usual).

## The three trades

| Trade | Cost to you | What happens |
|-------|-------------|--------------|
| **Give gold** | Nothing extra | Pick a friend and an amount; they receive it. |
| **Give a card** | Nothing extra | Pick a card from your deck; it moves to their deck (upgrades preserved). |
| **Request a relic** | **ALL of your gold** | Browse a friend's relics and make an offer. If they accept, the relic is yours and every coin you own is burned (the giver loses only the relic — they don't get your gold). If they decline, your trade is refunded. |

## How it works

- A **Trade** button appears in the bottom-left of the shop screen (co-op runs only).
- One trade per player per shop visit; the lockout resets at the next shop.
- Relic requests need the owner's consent via an accept/decline popup.
- **Every player in the lobby must have the mod installed** — trades are synced with
  custom network messages, and clients without the mod cannot decode them.

## Build

```bash
dotnet build   # copies TradingPost.dll + TradingPost.json into the game's mods folder
```

No `.pck` needed — the UI is built from stock Godot controls and localization is
injected at runtime.

## Implementation notes

- `TradeSynchronizer` mirrors the game's own `OneOffSynchronizer` pattern: the initiating
  client applies the change locally and broadcasts an `INetMessage`; peers mirror it.
  Mod message types are auto-registered by the game (`MessageTypes.Initialize` scans mods).
- Harmony patches: `RunManager.InitializeShared` / `RunManager.CleanUp` (synchronizer
  lifecycle) and `NMerchantRoom._Ready` (button + per-visit reset).
- Gold burned in relic trades uses the requester's gold *at acceptance time* on each
  client; message ordering is reliable and location-buffered, and the game's
  `ChecksumTracker` will flag any divergence.
