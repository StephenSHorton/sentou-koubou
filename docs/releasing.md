# Releasing mods (local, per-mod tags)

Each playable mod ships its **own** GitHub Release — not a monorepo-wide `v1.0.0`.

Build and package **on a machine with STS2 installed** (same as MarisaMod and most character mods). No cloud CI.

## Tag format

```text
<mod-id>/v<semver>
```

| Tag | Package |
|-----|---------|
| `whitney/v0.2.0` | `Whitney.zip` |
| `brennen/v0.1.1` | `Brennen.zip` |
| `blake/v0.1.0` | `Blake.zip` |
| `trading-post/v0.1.0` | `TradingPost.zip` |
| `card-ranks/v0.1.0` | `CardRanks.zip` |

## Zip layout (Mods drop-in)

```text
Whitney.zip
└── Whitney/
    ├── Whitney.dll
    ├── Whitney.json
    └── Whitney.pck          # when has_pck / packer produced one
```

Players unzip into `Slay the Spire 2/mods/` → `mods/Whitney/…`.  
Requires [BaseLib](https://github.com/Alchyr/BaseLib-StS2/releases).

## Prerequisites

- STS2 installed (so `dotnet build` can reference `sts2.dll` / Harmony)
- [.NET SDK 9+](https://dotnet.microsoft.com/download)
- [GitHub CLI](https://cli.github.com/) (`gh auth login`) for uploading releases
- Quit the game before building (DLL lock)

## One-liner (recommended)

From a worktree with the mod ready (ideally on `main` or a release branch):

```bash
python tools/release_mod.py whitney 0.2.1 --local-upload
```

That will:

1. Set `mods/whitney/Whitney.json` → `"version": "v0.2.1"`
2. `dotnet build -c Release` (copies into game `Mods/` when discovery works)
3. Stage `tools/gen_out/releases/Whitney/` and zip `Whitney.zip`
4. Commit the version bump if needed
5. Create annotated tag `whitney/v0.2.1`
6. `gh release create` with the zip attached

### Other flags

```bash
# Build + zip only (no git / no GitHub)
python tools/release_mod.py trading-post 0.1.0 --build-only

# Tag + push branch/tag only (no Release asset — prefer --local-upload instead)
python tools/release_mod.py blake 0.1.0 --push

# Reuse last build artifacts
python tools/release_mod.py whitney 0.2.1 --skip-build --local-upload
```

## Manual steps (if you skip the script)

```bash
cd mods/whitney
# bump "version" in Whitney.json to v0.2.1
dotnet build -c Release
# collect Whitney.dll / .json / .pck from Mods/Whitney or bin output
# zip as Whitney/Whitney.*

git tag -a whitney/v0.2.1 -m "Whitney v0.2.1"
git push origin whitney/v0.2.1

gh release create whitney/v0.2.1 Whitney.zip \
  --title "Whitney v0.2.1" \
  --generate-notes
```

## Checklist

- [ ] Quit STS2
- [ ] Smoke-test the build in-game once if the change is non-trivial
- [ ] Manifest `version` matches the tag (`vX.Y.Z`)
- [ ] Zip contains only the mod folder (dll + json + pck/assets as needed)
- [ ] Release notes mention BaseLib if it’s a character mod
