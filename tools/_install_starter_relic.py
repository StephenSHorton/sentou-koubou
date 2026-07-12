from pathlib import Path
from PIL import Image
import numpy as np
import shutil

sess = Path(r"C:\Users\4step\.grok\sessions\C%3A%5CUsers%5C4step\019f547e-c73a-7e11-9949-f665f061efd6\images")
# Prefer charged fist gauntlet (94)
src_path = sess / "94.jpg"
relics = Path("mods/blake/Blake/images/relics")
docs = Path("docs/assets/blake")
docs.mkdir(parents=True, exist_ok=True)
(relics / "big").mkdir(parents=True, exist_ok=True)

def chroma_black(im: Image.Image, thresh: int = 28) -> Image.Image:
    im = im.convert("RGBA")
    arr = np.array(im).astype(np.float32)
    r, g, b, a = arr[:, :, 0], arr[:, :, 1], arr[:, :, 2], arr[:, :, 3]
    # near-black background
    is_dark = (r < thresh) & (g < thresh) & (b < thresh)
    # also pure green placeholders if any
    green_score = g - np.maximum(r, b)
    is_green = (g > 90) & (green_score > 35) & (g > r * 1.15) & (g > b * 1.15)
    alpha = a.copy()
    alpha[is_dark | is_green] = 0
    # soft edge on near-dark
    near = (r < thresh + 20) & (g < thresh + 20) & (b < thresh + 20) & (~is_dark)
    alpha[near] = np.clip(alpha[near] * ((np.maximum(np.maximum(r, g), b)[near] - thresh) / 20.0), 0, 255)
    out = arr.copy()
    out[:, :, 3] = alpha
    return Image.fromarray(out.astype(np.uint8), "RGBA")

def fit_contain(im: Image.Image, size: int) -> Image.Image:
    """Fit inside size x size keeping aspect, transparent pad."""
    im = im.convert("RGBA")
    # crop tight to alpha
    bbox = im.getbbox()
    if bbox:
        im = im.crop(bbox)
    im.thumbnail((size - 16, size - 16), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    x = (size - im.width) // 2
    y = (size - im.height) // 2
    canvas.paste(im, (x, y), im)
    return canvas

def make_outline(im: Image.Image) -> Image.Image:
    outline = Image.new("RGBA", im.size, (0, 0, 0, 0))
    sp, op = im.load(), outline.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            if sp[x, y][3] > 20:
                op[x, y] = (40, 40, 40, 255)
    return outline

cut = chroma_black(Image.open(src_path))
sm = fit_contain(cut, 256)
bg = fit_contain(cut, 512)
sm.save(relics / "racersgauntlet.png")
bg.save(relics / "big" / "racersgauntlet.png")
make_outline(sm).save(relics / "racersgauntlet_outline.png")
# catalog
bg.convert("RGBA").save(docs / "racersgauntlet.png")
# raw for regen
Path("tools/gen_out/blake/relics").mkdir(parents=True, exist_ok=True)
shutil.copy2(src_path, Path("tools/gen_out/blake/relics/relic_racersgauntlet.jpg"))
print("racersgauntlet installed", sm.size, sm.stat().st_size if hasattr(sm, "stat") else "")
print("file sizes", (relics/"racersgauntlet.png").stat().st_size, (relics/"big"/"racersgauntlet.png").stat().st_size)
# alpha check
arr = np.array(sm)
print("alpha nonzero", int((arr[:,:,3] > 0).sum()), "corner", tuple(arr[0,0]))
