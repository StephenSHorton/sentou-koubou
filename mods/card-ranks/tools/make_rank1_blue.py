from pathlib import Path
import subprocess
import sys

try:
    from PIL import Image
except ImportError:
    subprocess.check_call([sys.executable, "-m", "pip", "install", "pillow", "-q"])
    from PIL import Image

root = Path(__file__).resolve().parents[1]
src = root / "assets" / "rank2.png"
img = Image.open(src).convert("RGBA")
pixels = img.load()
w, h = img.size
for y in range(h):
    for x in range(w):
        r, g, b, a = pixels[x, y]
        if a < 8:
            continue
        nr = int(r * 0.35 + 40)
        ng = int(g * 0.45 + 90)
        nb = int(min(255, b * 0.55 + 160))
        pixels[x, y] = (nr, ng, nb, a)
for dest in [root / "assets" / "rank1.png", root / "card_ranks" / "rank1.png"]:
    dest.parent.mkdir(parents=True, exist_ok=True)
    img.save(dest)
    print("wrote", dest, dest.stat().st_size)
