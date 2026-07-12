from pathlib import Path
from PIL import Image
import shutil

sess = Path(r"C:\Users\4step\.grok\sessions\C%3A%5CUsers%5C4step\019f547e-c73a-7e11-9949-f665f061efd6\images")
src = Image.open(sess / "92.jpg").convert("RGB")
charui = Path("mods/blake/Blake/images/charui")
variants = Path("docs/assets/blake/variants")
variants.mkdir(parents=True, exist_ok=True)
old = charui / "char_select_bg_blake.png"
if old.exists():
    shutil.copy2(old, variants / "char_select_bg_blake_centered_rejected.png")

def fit_cover(im, size):
    tw, th = size
    w, h = im.size
    scale = max(tw / w, th / h)
    nw, nh = int(w * scale + 0.5), int(h * scale + 0.5)
    im2 = im.resize((nw, nh), Image.Resampling.LANCZOS)
    left = (nw - tw) // 2
    top = (nh - th) // 2
    return im2.crop((left, top, left + tw, top + th))

out = fit_cover(src, (3840, 2160))
out.save(charui / "char_select_bg_blake.png")
out.save(variants / "char_select_bg_blake.png")
print("ok", out.size, (charui / "char_select_bg_blake.png").stat().st_size)
