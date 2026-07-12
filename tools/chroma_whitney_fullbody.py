"""Chroma-key Whitney full-body combat green plate to transparent PNG."""
from pathlib import Path
import numpy as np
from PIL import Image

sess = Path(r"C:\Users\4step\.grok\sessions\C%3A%5CUsers%5C4step\019f51e0-79c4-7431-bf9a-4c80e69a4e73\images")
docs = Path(r"C:\Users\4step\projects\sentou-koubou-brennen-kitpass\docs\assets\whitney\variants")
tools = Path(r"C:\Users\4step\projects\sentou-koubou-brennen-kitpass\tools\gen_out\whitney")
docs.mkdir(parents=True, exist_ok=True)
tools.mkdir(parents=True, exist_ok=True)

src_path = sess / "349.jpg"
img = Image.open(src_path).convert("RGBA")
arr = np.array(img).astype(np.float32)
r, g, b, a = arr[:, :, 0], arr[:, :, 1], arr[:, :, 2], arr[:, :, 3]

green_score = g - np.maximum(r, b)
is_green = (g > 90) & (green_score > 35) & (g > r * 1.15) & (g > b * 1.15)

alpha = a.copy()
alpha[is_green] = 0
near = (g > 70) & (green_score > 18) & (~is_green)
alpha[near] = np.clip(alpha[near] * (1.0 - (green_score[near] / 80.0)), 0, 255)

out = arr.copy()
out[:, :, 3] = alpha
# despill
mask = (alpha > 0) & (alpha < 250) & (g > r) & (g > b)
out[mask, 1] = np.minimum(out[mask, 1], (out[mask, 0] + out[mask, 2]) * 0.55)

out_img = Image.fromarray(out.astype(np.uint8), "RGBA")
bg = Image.new("RGBA", out_img.size, (18, 20, 26, 255))
comp = Image.alpha_composite(bg, out_img).convert("RGB")
green_rgb = img.convert("RGB")

names = {
    "png": [
        "whitney_combat_right.png",
        "whitney_fullbody_d3.png",
        "whitney_base_fullbody.png",
    ],
    "green": [
        "whitney_combat_right_green.jpg",
        "whitney_fullbody_d3_green.jpg",
    ],
    "preview": [
        "whitney_combat_right_preview.jpg",
        "whitney_fullbody_d3_preview.jpg",
    ],
}

for name in names["png"]:
    out_img.save(docs / name)
    out_img.save(tools / name)
for name in names["green"]:
    green_rgb.save(docs / name, quality=92)
    green_rgb.save(tools / name, quality=92)
for name in names["preview"]:
    comp.save(docs / name, quality=92)
    comp.save(tools / name, quality=92)

print("size", out_img.size)
print("transparent px", int((alpha == 0).sum()), "of", int(alpha.size))
print("saved combat right facing cutout")
