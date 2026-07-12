"""Chroma-key Blake full-body combat green plate to transparent PNG."""
from pathlib import Path
import numpy as np
from PIL import Image

# Source: latest combat-right green plate from session gen (override via CLI later if needed)
sess = Path(r"C:\Users\4step\.grok\sessions\C%3A%5CUsers%5C4step\019f547e-c73a-7e11-9949-f665f061efd6\images")
docs = Path(r"C:\Users\4step\projects\sentou-koubou-blake\docs\assets\blake\variants")
tools = Path(r"C:\Users\4step\projects\sentou-koubou-blake\tools\gen_out\blake")
docs.mkdir(parents=True, exist_ok=True)
tools.mkdir(parents=True, exist_ok=True)

# 41 = preferred lighter combat_right (user rejected dark pass 44)
# Prefer on-disk locked green plate if present so re-runs stay stable.
disk_green = docs / "blake_combat_right_green.jpg"
src_path = disk_green if disk_green.is_file() else (sess / "41.jpg")
if not src_path.is_file():
    raise SystemExit(f"missing source {src_path}")

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
        "blake_combat_right.png",
        "blake_fullbody_m.png",
        "blake_base_fullbody.png",
    ],
    "green": [
        "blake_combat_right_green.jpg",
        "blake_fullbody_m_green.jpg",
    ],
    "preview": [
        "blake_combat_right_preview.jpg",
        "blake_fullbody_m_preview.jpg",
        "blake_base_fullbody_preview.jpg",
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

# Also stash alt candidates if present
for alt_id, label in (
    ("40", "alt_front"),
    ("41", "alt_bright"),  # original lighter plate
    ("42", "alt_guard"),
    ("43", "alt_darker"),  # even moodier sibling of 44
):
    alt = sess / f"{alt_id}.jpg"
    if alt.is_file():
        Image.open(alt).convert("RGB").save(docs / f"blake_combat_{label}_green.jpg", quality=90)

print("size", out_img.size)
print("transparent px", int((alpha == 0).sum()), "of", int(alpha.size))
print("saved combat right facing cutout →", docs)
