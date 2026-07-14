"""Generate distinct Tier I / II / III ribbon numerals for Card Ranks.

Outputs:
  assets/rank1.png  blue I
  assets/rank2.png  purple II
  assets/rank3.png  gold III
  card_ranks/rank*.png  (PckPacker source)
"""
from __future__ import annotations

import subprocess
import sys
from pathlib import Path

try:
    from PIL import Image, ImageDraw, ImageFont, ImageFilter
except ImportError:
    subprocess.check_call([sys.executable, "-m", "pip", "install", "pillow", "-q"])
    from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
SIZE = 128
PAD = 8

# (roman, fill RGB, highlight RGB, shadow RGB)
TIERS = {
    1: ("I", (70, 150, 255), (180, 220, 255), (20, 50, 120)),
    2: ("II", (170, 80, 230), (230, 180, 255), (70, 20, 110)),
    3: ("III", (255, 170, 40), (255, 230, 140), (120, 60, 10)),
}


def find_font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = [
        r"C:\Windows\Fonts\arialbd.ttf",
        r"C:\Windows\Fonts\segoeuib.ttf",
        r"C:\Windows\Fonts\calibrib.ttf",
        r"C:\Windows\Fonts\arial.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
    ]
    for path in candidates:
        p = Path(path)
        if p.exists():
            try:
                return ImageFont.truetype(str(p), size=size)
            except OSError:
                pass
    return ImageFont.load_default()


def draw_tier(roman: str, fill, hi, shadow) -> Image.Image:
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    # Slightly larger font for single "I", smaller for "III"
    font_size = 96 if len(roman) == 1 else (78 if len(roman) == 2 else 64)
    font = find_font(font_size)
    draw = ImageDraw.Draw(img)

    # Measure text
    bbox = draw.textbbox((0, 0), roman, font=font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    x = (SIZE - tw) // 2 - bbox[0]
    y = (SIZE - th) // 2 - bbox[1] - 4

    # Soft shadow layer
    shadow_layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow_layer)
    sd.text((x + 3, y + 4), roman, font=font, fill=(*shadow, 200))
    shadow_layer = shadow_layer.filter(ImageFilter.GaussianBlur(radius=2))
    img = Image.alpha_composite(img, shadow_layer)
    draw = ImageDraw.Draw(img)

    # Outline for readability on cards
    outline = (*shadow, 255)
    for dx, dy in ((-2, 0), (2, 0), (0, -2), (0, 2), (-2, -2), (2, -2), (-2, 2), (2, 2)):
        draw.text((x + dx, y + dy), roman, font=font, fill=outline)

    # Main fill
    draw.text((x, y), roman, font=font, fill=(*fill, 255))

    # Top highlight stroke (simple second pass offset up)
    draw.text((x, y - 1), roman, font=font, fill=(*hi, 90))

    return img


def main() -> None:
    for n, (roman, fill, hi, shadow) in TIERS.items():
        img = draw_tier(roman, fill, hi, shadow)
        for dest_dir in (ROOT / "assets", ROOT / "card_ranks"):
            dest_dir.mkdir(parents=True, exist_ok=True)
            path = dest_dir / f"rank{n}.png"
            img.save(path)
            print(f"wrote {path} ({path.stat().st_size} bytes)  {roman}")


if __name__ == "__main__":
    main()
