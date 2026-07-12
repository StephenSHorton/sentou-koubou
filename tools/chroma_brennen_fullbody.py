"""Chroma-key Brennen full-body green plate to transparent PNG."""
from pathlib import Path
import numpy as np
from PIL import Image

sess = Path(r"C:\Users\4step\.grok\sessions\C%3A%5CUsers%5C4step\019f51e0-79c4-7431-bf9a-4c80e69a4e73\images")
base = Path(r"C:\Users\4step\projects\sentou-koubou-brennen-kitpass\tools\gen_out\cohesion\variants")
docs = Path(r"C:\Users\4step\projects\sentou-koubou-brennen-kitpass\docs\assets\brennen\variants")
docs.mkdir(parents=True, exist_ok=True)
base.mkdir(parents=True, exist_ok=True)

# Prefer combat stance source; fall back to 153 front pose if missing
src_candidates = [sess / "156.jpg", sess / "155.jpg", sess / "153.jpg"]
src_path = next(p for p in src_candidates if p.exists())
print("source", src_path)

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

mask = (alpha > 0) & (alpha < 250) & (g > r) & (g > b)
out[mask, 1] = np.minimum(out[mask, 1], (out[mask, 0] + out[mask, 2]) * 0.55)

out_img = Image.fromarray(out.astype(np.uint8), "RGBA")

# Main locked combat base names
names_png = [
    "brennen_fullbody_c6.png",
    "brennen_base_fullbody.png",
    "brennen_combat_right.png",
]
names_green = [
    "brennen_fullbody_c6_green.jpg",
    "brennen_combat_right_green.jpg",
]
names_preview = [
    "brennen_fullbody_c6_preview.jpg",
    "brennen_base_fullbody_preview.jpg",
    "brennen_combat_right_preview.jpg",
]

bg = Image.new("RGBA", out_img.size, (18, 20, 26, 255))
comp = Image.alpha_composite(bg, out_img).convert("RGB")
green_rgb = img.convert("RGB")

for name in names_png:
    out_img.save(base / name)
    out_img.save(docs / name)
for name in names_green:
    green_rgb.save(base / name, quality=92)
    green_rgb.save(docs / name, quality=92)
for name in names_preview:
    comp.save(base / name, quality=92)
    comp.save(docs / name, quality=92)

# Keep previous front-facing as archive if present
front_green = docs / "brennen_fullbody_front_green.jpg"
# already overwritten main — fine

print("size", out_img.size)
print("transparent px", int((alpha == 0).sum()), "of", int(alpha.size))
print("saved combat right facing cutout")
