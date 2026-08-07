#!/usr/bin/env python3
"""
Local per-mod release helper for sentou-koubou.

Build on a machine with STS2 installed; upload the zip to GitHub Releases.
No cloud CI — same model as MarisaMod and most STS2 character mods.

Tag format:  <mod-id>/v<semver>   e.g. whitney/v0.2.0
Zip layout:  <Assembly>/<Assembly>.{dll,json,pck}  (Mods drop-in)

Usage:
  # Preferred: build, tag, create GitHub Release with zip
  python tools/release_mod.py whitney 0.2.1 --local-upload

  # Build + zip only (no git / no gh)
  python tools/release_mod.py whitney 0.2.1 --build-only

  # Tag + push only (no Release asset)
  python tools/release_mod.py whitney 0.2.1 --push

Known mods: whitney, brennen, blake, trading-post, card-ranks, mp-player-limit,
rmp-player-limit, uncapped-chapter-fix, gilded-ledger, …
See docs/releasing.md and AGENTS.md.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import subprocess
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

MODS: dict[str, dict[str, str]] = {
    "whitney": {
        "folder": "whitney",
        "project": "Whitney.csproj",
        "assembly": "Whitney",
    },
    "brennen": {
        "folder": "brennen",
        "project": "Brennen.csproj",
        "assembly": "Brennen",
    },
    "blake": {
        "folder": "blake",
        "project": "Blake.csproj",
        "assembly": "Blake",
    },
    "trading-post": {
        "folder": "trading-post",
        "project": "TradingPost.csproj",
        "assembly": "TradingPost",
    },
    "card-ranks": {
        "folder": "card-ranks",
        "project": "CardRanks.csproj",
        "assembly": "CardRanks",
    },
    # Clean Harmony rewrite (preferred)
    "mp-player-limit": {
        "folder": "mp-player-limit",
        "project": "MpPlayerLimit.csproj",
        "assembly": "MpPlayerLimit",
    },
    # Legacy: IL-patched workshop RMP (superseded by mp-player-limit)
    "rmp-player-limit": {
        "folder": "rmp-player-limit",
        "project": "",  # custom build via build.ps1
        "assembly": "RemoveMultiplayerPlayerLimit",
        "build": "ps1",
    },
    # UncappedSpire MP chapter/seed/potion compat (soft-depends on UncappedSpire)
    "uncapped-chapter-fix": {
        "folder": "uncapped-chapter-fix",
        "project": "UncappedChapterFix.csproj",
        "assembly": "UncappedChapterFix",
    },
    "mp-teammate-view": {
        "folder": "mp-teammate-view",
        "project": "MpTeammateView.csproj",
        "assembly": "MpTeammateView",
    },
    "max-hp-size": {
        "folder": "max-hp-size",
        "project": "MaxHpSize.csproj",
        "assembly": "MaxHpSize",
    },
    "character-cursors": {
        "folder": "character-cursors",
        "project": "CharacterCursors.csproj",
        "assembly": "CharacterCursors",
    },
    "shared-combat-positions": {
        "folder": "shared-combat-positions",
        "project": "SharedCombatPositions.csproj",
        "assembly": "SharedCombatPositions",
    },
    "ping-rage": {
        "folder": "ping-rage",
        "project": "PingRage.csproj",
        "assembly": "PingRage",
    },
    "battle-draw": {
        "folder": "battle-draw",
        "project": "BattleDraw.csproj",
        "assembly": "BattleDraw",
    },
    "combat-queue-fix": {
        "folder": "combat-queue-fix",
        "project": "CombatQueueFix.csproj",
        "assembly": "CombatQueueFix",
    },
    "mp-drop-out": {
        "folder": "mp-drop-out",
        "project": "MpDropOut.csproj",
        "assembly": "MpDropOut",
    },
    "gilded-ledger": {
        "folder": "gilded-ledger",
        "project": "GildedLedger.csproj",
        "assembly": "GildedLedger",
    },
}


def run(cmd: list[str], *, cwd: Path | None = None, check: bool = True) -> subprocess.CompletedProcess:
    print("+", " ".join(cmd))
    return subprocess.run(cmd, cwd=cwd or ROOT, check=check)


def set_manifest_version(mod_dir: Path, assembly: str, version: str) -> Path:
    manifest = mod_dir / f"{assembly}.json"
    data = json.loads(manifest.read_text(encoding="utf-8-sig"))
    data["version"] = f"v{version}"
    # preserve readable formatting
    manifest.write_text(
        json.dumps(data, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )
    print(f"Updated {manifest.relative_to(ROOT)} version -> v{version}")
    return manifest


def build_mod(mod_dir: Path, project: str, *, build_kind: str = "dotnet") -> Path:
    """Build Release; return directory that should contain the dll (mods copy or bin)."""
    if build_kind == "ps1":
        script = mod_dir / "build.ps1"
        if not script.is_file():
            raise SystemExit(f"Missing {script}")
        run(
            [
                "powershell",
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                str(script),
            ],
            cwd=mod_dir,
        )
        dist = mod_dir / "dist"
        if dist.is_dir() and any(dist.glob("*.dll")):
            return dist
        raise SystemExit(f"Could not find patched dll under {dist}")

    # Prefer normal local discovery via Sts2PathDiscovery.props
    run(["dotnet", "build", project, "-c", "Release"], cwd=mod_dir)

    # Godot SDK output
    candidates = [
        mod_dir / ".godot" / "mono" / "temp" / "bin" / "Release",
        mod_dir / "bin" / "Release" / "net9.0",
        mod_dir / "bin" / "Release",
    ]
    for c in candidates:
        if c.is_dir() and any(c.glob("*.dll")):
            return c
    raise SystemExit(f"Could not find build output under {mod_dir}")


def stage_release(
    mod_dir: Path,
    assembly: str,
    build_dir: Path,
    out_root: Path,
    *,
    include_trading_assets: bool,
) -> Path:
    staging = out_root / assembly
    if staging.exists():
        shutil.rmtree(staging)
    staging.mkdir(parents=True)

    # Prefer game Mods folder copy if present (has pck from packer + inject)
    steam_mods = None
    # Sts2PathDiscovery is local; also check common path
    for base in [
        Path(os.environ.get("STS2_MODS", "")),
        Path(r"C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods"),
        Path(os.path.expandvars(r"%ProgramFiles(x86)%\Steam\steamapps\common\Slay the Spire 2\mods")),
    ]:
        if base and (base / assembly).is_dir():
            steam_mods = base / assembly
            break

    sources: list[Path] = []
    if steam_mods:
        sources.append(steam_mods)
    sources.append(build_dir)

    # Ship the same essentials as MarisaMod.zip: dll + json + pck (+ png assets).
    # Skip .deps.json / runtimeconfig / pdb noise.
    wanted_names_or_ext = {".dll", ".pck", ".png"}
    for src_dir in sources:
        if not src_dir.is_dir():
            continue
        for f in src_dir.iterdir():
            if not f.is_file():
                continue
            name = f.name.lower()
            if name.endswith(".deps.json") or name.endswith(".runtimeconfig.json"):
                continue
            if name.endswith(".pdb"):
                continue
            if f.suffix.lower() not in wanted_names_or_ext and not name.endswith(".json"):
                continue
            # Only the mod manifest JSON, not arbitrary json
            if f.suffix.lower() == ".json" and f.stem != assembly:
                continue
            dest = staging / f.name
            # Prefer pck/dll from steam mods (post-inject) over bin
            if dest.exists() and f.suffix.lower() in {".dll", ".pck"} and steam_mods and src_dir == build_dir:
                continue
            shutil.copy2(f, dest)

    # Always use repo manifest (version already bumped)
    shutil.copy2(mod_dir / f"{assembly}.json", staging / f"{assembly}.json")

    # RMP-style: pck lives under vendor/ (not produced by the patcher)
    vendor_pck = mod_dir / "vendor" / f"{assembly}.pck"
    if vendor_pck.is_file() and not (staging / f"{assembly}.pck").is_file():
        shutil.copy2(vendor_pck, staging / f"{assembly}.pck")

    if include_trading_assets:
        assets = mod_dir / "assets"
        if assets.is_dir():
            for f in assets.glob("*.png"):
                shutil.copy2(f, staging / f.name)

    dll = staging / f"{assembly}.dll"
    if not dll.is_file():
        raise SystemExit(f"Missing {dll} after staging")
    return staging


def zip_mod(staging: Path, zip_path: Path) -> None:
    if zip_path.exists():
        zip_path.unlink()
    # Zip contains top-level Assembly/ folder (Marisa-style)
    with zipfile.ZipFile(zip_path, "w", compression=zipfile.ZIP_DEFLATED) as zf:
        for f in staging.rglob("*"):
            if f.is_file():
                arc = f.relative_to(staging.parent).as_posix()
                zf.write(f, arc)
    print(f"Wrote {zip_path} ({zip_path.stat().st_size} bytes)")


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("mod", choices=sorted(MODS.keys()), help="Mod id")
    ap.add_argument("version", help="Semver without leading v, e.g. 0.2.0")
    ap.add_argument("--build-only", action="store_true", help="Build + zip only; no git tag")
    ap.add_argument("--push", action="store_true", help="Create and push git tag to origin")
    ap.add_argument(
        "--local-upload",
        action="store_true",
        help="Create GitHub Release from this machine with the local zip (requires gh auth)",
    )
    ap.add_argument("--skip-build", action="store_true", help="Reuse last local build; only package")
    args = ap.parse_args()

    version = args.version.lstrip("vV")
    if not re.match(r"^\d+\.\d+\.\d+", version):
        raise SystemExit("version must look like 0.2.0")

    info = MODS[args.mod]
    mod_dir = ROOT / "mods" / info["folder"]
    assembly = info["assembly"]
    tag = f"{args.mod}/v{version}"
    build_kind = info.get("build", "dotnet")

    set_manifest_version(mod_dir, assembly, version)

    out_root = ROOT / "tools" / "gen_out" / "releases"
    out_root.mkdir(parents=True, exist_ok=True)

    if not args.skip_build:
        build_dir = build_mod(mod_dir, info["project"], build_kind=build_kind)
    else:
        if build_kind == "ps1":
            build_dir = mod_dir / "dist"
        else:
            build_dir = mod_dir / ".godot" / "mono" / "temp" / "bin" / "Release"
            if not build_dir.is_dir():
                build_dir = mod_dir / "bin" / "Release" / "net9.0"

    staging = stage_release(
        mod_dir,
        assembly,
        build_dir,
        out_root,
        include_trading_assets=(args.mod in ("trading-post", "card-ranks")),
    )
    zip_path = out_root / f"{assembly}.zip"
    zip_mod(staging, zip_path)

    print(f"\nPackage ready: {zip_path}")
    print(f"Install: unzip into STS2/mods/  →  mods/{assembly}/")

    if args.build_only and not args.push and not args.local_upload:
        return

    if args.push or args.local_upload:
        # Commit manifest bump if dirty
        status = subprocess.run(
            ["git", "status", "--porcelain", str(mod_dir / f"{assembly}.json")],
            cwd=ROOT,
            capture_output=True,
            text=True,
        )
        if status.stdout.strip():
            run(["git", "add", str(mod_dir / f"{assembly}.json")])
            run(
                [
                    "git",
                    "commit",
                    "-m",
                    f"chore({args.mod}): bump version to v{version}",
                ]
            )

        # Create annotated tag
        existing = subprocess.run(
            ["git", "rev-parse", "-q", "--verify", f"refs/tags/{tag}"],
            cwd=ROOT,
            capture_output=True,
        )
        if existing.returncode == 0:
            raise SystemExit(f"Tag {tag} already exists")

        run(["git", "tag", "-a", tag, "-m", f"{assembly} v{version}"])
        print(f"Created tag {tag}")

        if args.push or args.local_upload:
            # Push current branch + tag so the Release points at a remote commit
            branch = subprocess.check_output(
                ["git", "rev-parse", "--abbrev-ref", "HEAD"], cwd=ROOT, text=True
            ).strip()
            run(["git", "push", "origin", branch])
            run(["git", "push", "origin", tag])
            print(f"Pushed tag {tag}")

        if args.local_upload:
            notes = (
                f"**{assembly}** `v{version}`\n\n"
                f"Unzip into `Slay the Spire 2/mods/` "
                f"(character mods need [BaseLib](https://github.com/Alchyr/BaseLib-StS2/releases))."
            )
            if args.mod == "rmp-player-limit":
                notes = (
                    f"**{assembly}** `v{version}` (Sentou IL-patched RMP)\n\n"
                    "Fixes multiplayer ready-up hang on current STS2 "
                    "(`GetDeterministicHashCode` int + `Rng(uint)`).\n\n"
                    "**Install:** unzip into `Slay the Spire 2/mods/` → "
                    f"`mods/{assembly}/`.\n\n"
                    "**Important:** disable/unsubscribe the Steam Workshop "
                    "copy of Remove Multiplayer Player Limit (same mod id) "
                    "so only this fixed build loads. All players need the fix."
                )
            run(
                [
                    "gh",
                    "release",
                    "create",
                    tag,
                    str(zip_path),
                    "--title",
                    f"{assembly} v{version}",
                    "--notes",
                    notes,
                ]
            )
            print(f"GitHub Release created for {tag}")


if __name__ == "__main__":
    main()
