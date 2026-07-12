# sentou-koubou agent notes

Monorepo of Slay the Spire 2 mods under `mods/`.

## Conventions

- One folder per mod: `mods/<mod-id>/` with matching `ModId`, `.csproj`, `.json` manifest, and asset root folder.
- Prefer **BaseLib** (`CustomCharacterModel` / `CustomCardModel` / etc.) over raw Harmony for content.
- Character mods: start from Alchyr’s Character template patterns; do not fork full character mods as bases.
- Local paths live in gitignored `Directory.Build.props` (copy from `*.example`). Set `Sts2Path` on Windows if discovery fails.
- **Build** copies `.dll` / `.json` / (with PckPacker) `.pck` into the game `Mods/` folder.
- **Publish** (Godot/MegaDot export) also regenerates `.pck` when you have MegaDot installed.
- Game is Early Access — expect BaseLib/game bumps to break mods; pin `min_game_version` and BaseLib `min_version` in manifests.
- **Edit only in git worktrees** branched from latest `origin/main` (or the feature branch). Never treat the main checkout as a shared write workspace.

## Mods

- `mods/brennen` — Full tank character kit. Regen generated cards with `python tools/generate_brennen_kit.py`.
- `mods/whitney` — Atelier witch, Energy + Ink dual mana, full 20C/35U/25R kit + combat flipbooks.
- `mods/blake` — Falcon racer-brawler (Charge / Rev / Unleash). Generate with `python tools/generate_blake_kit.py`.
- `mods/trading-post` — Co-op shop gold + campfire card trading.

## Releases (per mod)

- **Tag format:** `mod-id/vX.Y.Z` (e.g. `whitney/v0.2.0`, `trading-post/v0.1.0`) — one tag → one mod GitHub Release.
- **CI:** `.github/workflows/release-mod.yml` on tags `*/v*` (needs secret `STS2_REF_DLLS`; see `docs/releasing.md`).
- **Local:** `python tools/release_mod.py <mod> <version> [--push|--build-only|--local-upload]`.

## Local catalog

```bash
# From a worktree that has docs/
python -m http.server -d docs 8765
# Brennen: http://localhost:8765/#brennen
# Whitney: http://localhost:8765/#whitney
# Variant pickers: /brennen-variants.html , /whitney-variants.html
```

---

## Card / character art pipeline (learned 2026-07)

This is how we got **Brennen** and **Whitney** card portraits + relics to a shippable state. Future agents should follow this rather than inventing a new process.

### Goals that actually mattered

1. **STS2 drawn/painted look** — bold Mega Crit graphic (chunky brush, simplified shapes, limited facial detail, strong silhouette). **Not** anime, **not** photoreal, **not** soft “premium illustration” polish that drifts away from the game.
2. **One shared character universe** per kit — same face, outfit, weapon/tool, recurring props across cards.
3. **Pose variety** — when the character appears, each card needs a *different action beat*, not the same lean/ready pose cropped differently.
4. **Selective presence** — full / partial / still-life. Not every card needs the character; prop cards still share palette + props.
5. **Relics are cutouts** — single artifact on transparent (or chroma-keyed) background, not full scene paintings.
6. **Catalog + game install** — `docs/assets/<char>/` for HTML catalog; `mods/<char>/<Char>/images/card_portraits/` (+ `big/`) and `images/relics/` for the game; pack into `.pck` for runtime art.

### What failed (do not repeat)

| Approach | Why it failed |
|----------|----------------|
| Soft “restyle” of anime portraits toward STS2 | Still looked like anime headshots; files changed, style didn’t. |
| Pure `image_gen` per card with only a text description | Style and likeness drift; kit feels like 80 unrelated artists. |
| Using a green-screen full-body as the *only* edit reference for every card | Green chroma **leaked into card backgrounds**; also froze poses. |
| One “good” combat pose reused everywhere | Cookie-cutter: Scorch Line ≈ Refill ≈ Updraft (just zoom). Feels lazy. |
| Photoreal still-lifes for “none” cards | Broke kit cohesion (e.g. early Drench Seal). Force graphic paint language. |
| Header portrait = crop of full-body combat sprite | Reads as a zoomed sprite, not a hero portrait (Whitney D3 vs bad Brennen header). |

### What worked

#### 1. Lock a character base *before* mass card gen

Interactive variant pages (`docs/*-variants.html`) → user picks:

- Style family (e.g. Brennen **C** bold graphic → tank/greatsword evolution → combat ready).
- Whitney **D3** indigo ink mage, blue eyes, hat matches dress.

**Assets to keep as gospel:**

| Char | Identity lock | Combat / sprite lock |
|------|----------------|----------------------|
| Brennen | `docs/assets/brennen/variants/brennen_locked_portrait.jpg` (+ `portrait_sts2.jpg` header) | `brennen_combat_right.png` (transparent), green plate for regen |
| Whitney | `docs/assets/whitney/variants/whitney_locked_d3.jpg` (+ `portrait_sts2.jpg`) | `whitney_combat_right.png` ready stance, 3/4 face readable |
| Blake | `docs/assets/blake/variants/blake_locked_portrait.jpg` (variant **M**) (+ `portrait_sts2.jpg`) | `blake_combat_right.png` ready idle faces right; chroma via `tools/chroma_blake_fullbody.py` |

Visual bibles: `tools/brennen_visual_bible.json`, `tools/whitney_visual_bible.json`.

**Combat base rules we settled on:**

- Face the fight **toward the right** (STS2 left-side fighter), but use **three-quarter** so the face stays readable — full profile hides the face too much.
- Default base is **ready/idle**, not attacking.
- Style of the full-body must match the portrait paint language (Whitney’s first combat pass was too cartoony; re-roll until brushes match D3).

#### 2. Prompt structure per card

Build a JSON of prompts (`tools/brennen_tank_c6_prompts.json`, `tools/whitney_d3_prompts.json`) with:

- `presence`: `full` | `partial` | `none`
- A **unique scene beat** (one sentence that only fits that card)
- Shared style block + character lock description + recurring props

**Character block (edit reference in, don’t only describe):**

- Use `image_edit` with the locked portrait (and sometimes combat green plate) as `image[]`.
- Lead with the **action**, then “SAME character as reference…”, then style, then “No text, no UI, no green screen.”

**Still-life / `none`:**

- Prefer `image_gen` with explicit “STS2 bold painted graphic, NOT photoreal, no person.”
- Shared props (wards, seals, inkwells, headsets, coins) keep the universe even without a face.

**Relics:**

- `image_gen` / edit: single object, pure green `#00FF00` background.
- Install via `tools/install_sts2_art.py` chroma → transparent PNG + outline.

#### 3. Batching and install

1. Generate into `tools/gen_out/<pass>/cards/*.jpg` and `relic_*.jpg`.
2. Install:
   ```bash
   python tools/install_sts2_art.py --char brennen|whitney \
     --gen-out tools/gen_out/<pass> \
     --docs-assets docs/assets/<char> \
     --mod-root mods/<char>/<CharFolder>
   ```
   - Cards → docs `*.jpg` + game `card_portraits/` and `big/` (1000×760 / 250×190).
   - Relics → transparent PNGs under `images/relics/`.
3. **Spot-check for clones** before calling the kit done. Near-duplicates get a forced re-gen with opposite composition (close desk vs wide environment vs lunge, etc.).
4. Scan for green bleed on catalog JPGs; re-edit “replace green with painted vignette” if chroma leaked.
5. Pack for game:
   ```bash
   # In mods/<char>/
   # PackageReference BSchneppe.StS2.PckPacker (quick .pck of Godot assets)
   dotnet build -c Release
   # Copies .dll .json .pck → STS2 Mods/<Name>/
   ```
   Manifests set `"has_pck": true` — without a `.pck`, art won’t show.

#### 4. Build / launch checklist (Windows)

```text
STS2:  .../Steam/steamapps/common/Slay the Spire 2
Mods:  .../Mods/BaseLib   (Workshop 3737335127; junction or copy)
       .../Mods/Brennen   (.dll + .json + .pck)
       .../Mods/Whitney   (.dll + .json + .pck)
```

Enable BaseLib + character mods in-game. BaseLib is required.

### Opinions / product judgment (keep these)

1. **Lock identity first, mass-generate second.** Skipping the variant picker wastes a full kit pass.
2. **Reference image > prose.** `image_edit` from the locked base beats a perfect paragraph every time for likeness.
3. **Composition is content.** Two cards with the same pose and different props still “feel lazy.” Force different camera, scale, and verb per card.
4. **Still-lifes are allowed and good** when they share props/palette — they reduce cookie-cutter faces *and* sell the kit fantasy (wards, seals, mute disc, inkpot).
5. **Green screen is for cutouts only** (full-body sprite, relics). Never leave it on card portraits.
6. **Header portraits are a product surface.** They need a dedicated half-body hero shot (Whitney D3 quality), not a crop of the combat PNG.
7. **In-game character select ≠ catalog header.** BaseLib loads:
   - `images/charui/char_select_<id>.png` (+ `_locked`) — picker tiles (typically ~864×1152)
   - `images/charui/character_icon_<id>.png` — small icon (~256×256)
   - `CustomCharacterSelectBg` → scene at `res://scenes/screens/char_select/char_select_bg_<class>.tscn` (else **Ironclad** backdrop is reused)
   - Catalog `docs/assets/<char>/portrait_sts2.jpg` is HTML-only until you also overwrite those charui files and rebuild the `.pck`.
8. **Asset filenames strip underscores.** `Id.Entry` is like `BRENNEN-DUO_QUEUE`; files must be `duoqueue.png`. Always map with `RemovePrefix().Replace("_","").ToLowerInvariant()` for cards, relics, and powers.
7. **User taste beats model defaults.** Whitney “younger” + “more like Brennen’s drawn C” + “blue eyes” + “ready not attack” + “face readable 3/4” were all necessary corrections; bake them into the bible.
8. **Re-roll is normal.** Budget time for a “dupe audit” pass after the first full gen (Scorch/Refill/Updraft, Moss Coat/Zephyr Draft, Spark/Ripple/Novice Seal/Ember Armor, photoreal Drench Seal).
9. **Catalog HTML is the QA tool.** Tabbed `docs/index.html` + hard refresh is faster than launching the game for art review; then build `.pck` when art is locked.

### Tooling map

| Path | Role |
|------|------|
| `tools/build_*_prompts.py` | Emit per-card presence + scene JSON |
| `tools/*_visual_bible.json` | Locked look, props, combat rules |
| `tools/install_sts2_art.py` | Resize/install cards + chroma relics |
| `tools/chroma_*_fullbody.py` | Green plate → transparent character PNG |
| `tools/gen_out/` | Working generations (often gitignored; re-gen from locks) |
| `docs/assets/<char>/` | Catalog arts |
| `docs/*-variants.html` | Character base pickers |
| `mods/<char>/<Assets>/images/` | Runtime Godot assets packed into `.pck` |

### Suggested order for a new character’s art

1. Likeness + style variants page → user locks base (portrait).
2. Full-body ready stance (3/4 face, faces right) + transparent cutout.
3. Header portrait from the lock (half-body, not combat crop).
4. Prompt JSON (full/partial/none) + prop list.
5. Generate in small batches; install continuously.
6. Dupe audit + green scan.
7. `dotnet build` with PckPacker → playtest in STS2.

---

## Card code regen (Brennen)

Generated kit content (not art):

```bash
python tools/generate_brennen_kit.py
```

Hand-authored keepers and custom powers live under `BrennenCode/` — see `mods/brennen/README.md`.
