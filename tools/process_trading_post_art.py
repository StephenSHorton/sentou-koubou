"""Process Trading Post UI art: black-bg cutouts, resize, export assets."""
from __future__ import annotations

from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "mods" / "trading-post" / "assets"
SESSION_IMAGES = Path.home() / ".grok" / "sessions" / "C%3A%5CUsers%5C4step%5C019f51e0-79c4-7431-bf9a-4c80e69a4e73" / "images"


def cut_dark_bg(im: Image.Image, lum_thresh: float = 22.0, max_chan: float = 36.0) -> Image.Image:
    a = np.array(im.convert("RGBA"), dtype=np.float32)
    rgb = a[:, :, :3]
    lum = 0.2126 * rgb[:, :, 0] + 0.7152 * rgb[:, :, 1] + 0.0722 * rgb[:, :, 2]
    bg = (lum < lum_thresh) & (rgb.max(axis=2) < max_chan)
    alpha = np.where(bg, 0.0, a[:, :, 3])
    # Soften near-black fringe so cut edges aren't hard pixels
    fringe = (~bg) & (lum < 48) & (rgb.max(axis=2) < 70)
    alpha = np.where(fringe, np.minimum(alpha, np.clip(lum * 5.5, 0, 255)), alpha)
    a[:, :, 3] = alpha
    return Image.fromarray(a.astype(np.uint8))


def crop_alpha(im: Image.Image, pad: int = 10) -> Image.Image:
    a = np.array(im)
    mask = a[:, :, 3] > 8
    if not mask.any():
        return im
    ys, xs = np.where(mask)
    y0 = max(0, int(ys.min()) - pad)
    y1 = min(a.shape[0], int(ys.max()) + pad + 1)
    x0 = max(0, int(xs.min()) - pad)
    x1 = min(a.shape[1], int(xs.max()) + pad + 1)
    return im.crop((x0, y0, x1, y1))


def to_square(im: Image.Image) -> Image.Image:
    w, h = im.size
    side = max(w, h)
    out = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    out.paste(im, ((side - w) // 2, (side - h) // 2), im)
    return out


def save_thumb(im: Image.Image, path: Path, max_side: int) -> None:
    out = im.copy()
    out.thumbnail((max_side, max_side), Image.Resampling.LANCZOS)
    path.parent.mkdir(parents=True, exist_ok=True)
    out.save(path, optimize=True)
    print(f"wrote {path.name} {out.size} bytes={path.stat().st_size}")


def process_source(path: Path, lum: float = 22.0, max_chan: float = 36.0) -> Image.Image:
    im = Image.open(path).convert("RGBA")
    im = cut_dark_bg(im, lum_thresh=lum, max_chan=max_chan)
    im = crop_alpha(im, pad=8)
    return im


def main() -> None:
    ASSETS.mkdir(parents=True, exist_ok=True)

    # Original campfire / trade illustration
    original = ASSETS / "option_trade.png"
    if not original.exists():
        raise SystemExit(f"missing {original}")

    # Backup original once
    bak = ASSETS / "option_trade_source.png"
    if not bak.exists():
        Image.open(original).save(bak)

    plate = process_source(bak if bak.exists() else original, lum=18, max_chan=28)
    # Menu banner: keep wide plate art
    banner = plate.copy()
    banner.thumbnail((960, 560), Image.Resampling.LANCZOS)
    banner.save(ASSETS / "menu_banner.png", optimize=True)
    print(f"wrote menu_banner.png {banner.size}")

    # Campfire option icon: square transparent cutout (no black frame)
    icon = to_square(plate)
    save_thumb(icon, ASSETS / "option_trade.png", 384)

    # Generated session icons if present
    gen_map = {
        "icon_gold.png": ["465.jpg", "465.png"],
        "icon_card.png": ["467.jpg", "467.png"],
        "icon_trade.png": ["466.jpg", "466.png"],
    }
    # Also search latest images folder loosely
    candidates: dict[str, Path] = {}
    for out_name, names in gen_map.items():
        for n in names:
            p = SESSION_IMAGES / n
            if p.exists():
                candidates[out_name] = p
                break

    # Fallback: any session images numbered high
    if len(candidates) < 3 and SESSION_IMAGES.exists():
        files = sorted(SESSION_IMAGES.glob("*.jpg"), key=lambda p: p.stat().st_mtime, reverse=True)
        print("recent gen images:", [f.name for f in files[:6]])

    for out_name, src in candidates.items():
        cut = process_source(src, lum=28, max_chan=45)
        cut = to_square(cut)
        save_thumb(cut, ASSETS / out_name, 256)

    # If gens missing, derive simple icons from plate regions (still better than nothing)
    if not (ASSETS / "icon_trade.png").exists():
        save_thumb(to_square(plate), ASSETS / "icon_trade.png", 256)
        print("icon_trade fallback from plate")

    # Report alpha health
    for name in ["option_trade.png", "menu_banner.png", "icon_gold.png", "icon_card.png", "icon_trade.png"]:
        p = ASSETS / name
        if not p.exists():
            print("missing", name)
            continue
        im = Image.open(p).convert("RGBA")
        px = list(im.getdata())
        z = sum(1 for r, g, b, a in px if a == 0)
        black = sum(1 for r, g, b, a in px if a > 200 and r < 20 and g < 20 and b < 20)
        print(f"{name}: transparent={z} near_black_opaque={black} mode={im.mode} size={im.size}")


if __name__ == "__main__":
    main()
