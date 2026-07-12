# Releasing mods (per-mod tags)

Each playable mod ships its **own** GitHub Release, not a monorepo-wide `vX.Y.Z`.

## Tag format

```text
<mod-id>/v<semver>
```

| Tag | Builds |
|-----|--------|
| `whitney/v0.2.0` | `mods/whitney` → `Whitney.zip` |
| `brennen/v0.1.1` | `mods/brennen` → `Brennen.zip` |
| `blake/v0.1.0` | `mods/blake` → `Blake.zip` |
| `trading-post/v0.1.0` | `mods/trading-post` → `TradingPost.zip` |

## Zip layout (Mods drop-in)

Same shape as MarisaMod releases:

```text
Whitney.zip
└── Whitney/
    ├── Whitney.dll
    ├── Whitney.json
    └── Whitney.pck          # when the mod has_pck / packer produced one
```

Unzip into `Slay the Spire 2/mods/` so you get `mods/Whitney/…`. Requires [BaseLib](https://github.com/Alchyr/BaseLib-StS2/releases).

## One-time CI setup (GitHub Actions)

Cloud runners do not have the game installed. The workflow needs **reference assemblies** only (compile-time; not shipped in the zip).

1. From your install:
   ```text
   …/Steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64/
   ```
2. Zip at least:
   - `sts2.dll`
   - `0Harmony.dll`
   - `GodotSharp.dll` (trading-post)
3. Create a **repository secret** `STS2_REF_DLLS` = base64 of that zip:

   ```powershell
   Compress-Archive sts2.dll,0Harmony.dll,GodotSharp.dll sts2-ref-dlls.zip
   [Convert]::ToBase64String([IO.File]::ReadAllBytes("sts2-ref-dlls.zip")) | Set-Clipboard
   ```

   Then: GitHub → repo → Settings → Secrets and variables → Actions → New repository secret.

## Release flows

### A) Tag push (recommended once secret is set)

```bash
# On main (or a merged branch), after the mod is ready:
python tools/release_mod.py whitney 0.2.1 --push
```

That bumps `mods/whitney/Whitney.json` version, commits if needed, creates annotated tag `whitney/v0.2.1`, pushes branch + tag.  
Workflow [`.github/workflows/release-mod.yml`](../.github/workflows/release-mod.yml) builds and publishes the Release.

### B) Manual workflow dispatch

Actions → **Release mod** → Run workflow → pick mod + version.

### C) Fully local (no CI secret)

```bash
python tools/release_mod.py whitney 0.2.1 --build-only
# zip at tools/gen_out/releases/Whitney.zip

python tools/release_mod.py whitney 0.2.1 --local-upload
# builds, tags, and gh release create with the local zip
```

## Notes

- Do **not** use a single monorepo tag like `v1.0.0` for all mods.
- Manifest `version` is set to `vX.Y.Z` to match the tag.
- Character mods use Godot.NET.Sdk + PckPacker; `trading-post` may ship loose PNGs and no `.pck`.
