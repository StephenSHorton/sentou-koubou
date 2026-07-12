"""Install STS2 gen_out card arts into docs + mod portrait trees."""
from __future__ import annotations
import argparse, json, shutil
from pathlib import Path
from PIL import Image

BIG = (1000, 760)
SM = (250, 190)
RELIC_SM = (256, 256)
RELIC_BIG = (512, 512)

def fit_cover(im: Image.Image, size: tuple[int,int]) -> Image.Image:
    tw, th = size
    w, h = im.size
    scale = max(tw / w, th / h)
    nw, nh = int(w * scale + 0.5), int(h * scale + 0.5)
    im2 = im.resize((nw, nh), Image.Resampling.LANCZOS)
    left = (nw - tw) // 2
    top = (nh - th) // 2
    return im2.crop((left, top, left + tw, top + th))

def chroma_key_green(im: Image.Image, thresh: int = 90) -> Image.Image:
    im = im.convert("RGBA")
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            # green screen-ish: g high and greater than r/b
            if g > r + 40 and g > b + 40 and g > thresh:
                px[x, y] = (r, g, b, 0)
            elif r > 240 and g > 240 and b > 240:
                # near-white bg fallback
                px[x, y] = (r, g, b, 0)
    return im

def install_card(src: Path, docs_dir: Path, big_dir: Path, sm_dir: Path, stem: str):
    im = Image.open(src).convert("RGB")
    big = fit_cover(im, BIG)
    sm = fit_cover(im, SM)
    docs_dir.mkdir(parents=True, exist_ok=True)
    big_dir.mkdir(parents=True, exist_ok=True)
    sm_dir.mkdir(parents=True, exist_ok=True)
    big.save(docs_dir / f"{stem}.jpg", quality=90)
    big.save(big_dir / f"{stem}.png")
    sm.save(sm_dir / f"{stem}.png")

def install_relic(src: Path, relics_dir: Path, stem: str):
    im = Image.open(src)
    if im.mode != "RGBA":
        im = chroma_key_green(im)
    else:
        # still try to clear pure green if any
        im = chroma_key_green(im)
    relics_dir.mkdir(parents=True, exist_ok=True)
    (relics_dir / "big").mkdir(exist_ok=True)
    sm = fit_cover(im, RELIC_SM)
    bg = fit_cover(im, RELIC_BIG)
    sm.save(relics_dir / f"{stem}.png")
    bg.save(relics_dir / "big" / f"{stem}.png")
    # simple outline: alpha silhouette in dark gray
    outline = Image.new("RGBA", sm.size, (0, 0, 0, 0))
    sp = sm.load(); op = outline.load()
    for y in range(sm.size[1]):
        for x in range(sm.size[0]):
            if sp[x, y][3] > 20:
                op[x, y] = (40, 40, 40, 255)
    outline.save(relics_dir / f"{stem}_outline.png")

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--char", choices=["brennen", "whitney"], required=True)
    ap.add_argument("--gen-out", type=Path, required=True)
    ap.add_argument("--docs-assets", type=Path, required=True)
    ap.add_argument("--mod-root", type=Path, required=True)
    ap.add_argument("--only", nargs="*", default=None)
    args = ap.parse_args()
    scenes_name = "brennen_sts2_scenes.json" if args.char == "brennen" else "whitney_sts2_scenes.json"
    # find scenes near gen-out
    scenes_path = args.gen_out.parent / scenes_name
    stems = set(json.loads(scenes_path.read_text(encoding="utf-8")).keys()) if scenes_path.exists() else set()
    portraits_big = args.mod_root / "images" / "card_portraits" / "big"
    portraits_sm = args.mod_root / "images" / "card_portraits"
    # filename mapping for strike/defend
    alias = {}
    if args.char == "brennen":
        alias = {"strike": "strikebrennen", "defend": "defendbrennen", "feed": "feed"}
    n = 0
    for src in sorted(args.gen_out.glob("*.jpg")):
        stem = src.stem.lower()
        if stem.endswith("_base") or stem.startswith("sts2_ref") or stem.startswith("batch") or "anime" in stem or "portrait" in stem:
            continue
        if stem.startswith("relic_"):
            continue
        if args.only and stem not in args.only:
            continue
        if stems and stem not in stems and stem not in alias:
            # allow all gen_out jpg that look like cards
            pass
        game_stem = alias.get(stem, stem)
        install_card(src, args.docs_assets, portraits_big, portraits_sm, game_stem if args.char=="brennen" and stem in alias else stem)
        # docs always use catalog stem
        if game_stem != stem:
            # also write catalog-facing docs name
            im = Image.open(src).convert("RGB")
            fit_cover(im, BIG).save(args.docs_assets / f"{stem}.jpg", quality=90)
        n += 1
        print("card", stem, "->", game_stem)
    # relics
    for src in sorted(args.gen_out.glob("relic_*.png")) + sorted(args.gen_out.glob("relic_*.jpg")):
        stem = src.stem[len("relic_"):].lower()
        if args.only and f"relic_{stem}" not in args.only and stem not in (args.only or []):
            continue
        install_relic(src, args.mod_root / "images" / "relics", stem)
        # docs copy
        im = Image.open(args.mod_root / "images" / "relics" / f"{stem}.png")
        args.docs_assets.mkdir(parents=True, exist_ok=True)
        im.save(args.docs_assets / f"{stem}.png")
        print("relic", stem)
        n += 1
    print("installed", n)

if __name__ == "__main__":
    main()
