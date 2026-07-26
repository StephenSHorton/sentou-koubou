"""Chroma-key green + split icon sheet / panel for Battle Draw toolbar."""
from __future__ import annotations

import os
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "assets"
SESSION = Path(
    r"C:\Users\4step\.grok\sessions\C%3A%5CUsers%5C4step\019f7809-0265-73c1-b31b-1b1edf349dbb\images"
)


def punch_green(img: Image.Image, thr: int = 90) -> Image.Image:
    img = img.convert("RGBA")
    px = img.load()
    w, h = img.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if g > 120 and g > r + thr and g > b + thr:
                px[x, y] = (r, g, b, 0)
            elif g > 100 and r < 80 and b < 80:
                px[x, y] = (r, g, b, 0)
    return img


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)

    sheet = punch_green(Image.open(SESSION / "3.jpg"))
    w, h = sheet.size
    cw, ch = w // 2, h // 2
    pad = 12
    cells = [
        ("icon_brush.png", (pad, pad, cw - pad, ch - pad)),
        ("icon_eraser.png", (cw + pad, pad, w - pad, ch - pad)),
        ("icon_clear.png", (pad, ch + pad, cw - pad, h - pad)),
        ("icon_tab.png", (cw + pad, ch + pad, w - pad, h - pad)),
    ]
    for name, box in cells:
        cell = sheet.crop(box).resize((96, 96), Image.Resampling.LANCZOS)
        cell.save(OUT / name, "PNG")
        print("wrote", name, cell.size)

    panel = punch_green(Image.open(SESSION / "2.jpg"))
    bbox = panel.getbbox()
    if bbox:
        panel = panel.crop(bbox)
    panel = panel.resize((480, 220), Image.Resampling.LANCZOS)
    panel.save(OUT / "panel_tools.png", "PNG")
    print("wrote panel_tools.png", panel.size)
    print("assets:", sorted(p.name for p in OUT.iterdir()))


if __name__ == "__main__":
    main()
