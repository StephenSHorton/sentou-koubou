"""
Fix BrennenBody plane: add UVs + material that shows the PNG in Material Preview.
Run inside Blender: Scripting workspace -> Open this file -> Run Script
Does NOT close Blender.
"""
import bmesh
import bpy
from pathlib import Path

IMG_PATH = Path(
    r"C:\Users\4step\projects\sentou-koubou-brennen-kitpass"
    r"\docs\assets\brennen\variants\brennen_combat_right.png"
)

body = bpy.data.objects.get("BrennenBody")
if body is None or body.type != "MESH":
    raise RuntimeError("BrennenBody mesh not found")

# --- UVs (missing UVs = invisible/wrong texture in Material Preview) ---
mesh = body.data
if not mesh.uv_layers:
    mesh.uv_layers.new(name="UVMap")
uv_layer = mesh.uv_layers.active.data
# Quad UV: bottom-left, bottom-right, top-right, top-left
if len(mesh.polygons) >= 1:
    poly = mesh.polygons[0]
    uvs = [(0.0, 0.0), (1.0, 0.0), (1.0, 1.0), (0.0, 1.0)]
    for li, loop_index in enumerate(poly.loop_indices):
        uv_layer[loop_index].uv = uvs[li % 4]

# --- Image ---
img = None
if IMG_PATH.exists():
    img = bpy.data.images.load(str(IMG_PATH), check_existing=True)
else:
    for im in bpy.data.images:
        if "brennen" in im.name.lower() or "combat" in im.name.lower():
            img = im
            break

# --- Material ---
if body.data.materials:
    mat = body.data.materials[0]
else:
    mat = bpy.data.materials.new("BrennenBody_mat")
    body.data.materials.append(mat)

mat.use_nodes = True
nt = mat.node_tree
nt.nodes.clear()

out = nt.nodes.new("ShaderNodeOutputMaterial")
out.location = (400, 0)
bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
bsdf.location = (100, 0)
tex = nt.nodes.new("ShaderNodeTexImage")
tex.location = (-250, 0)
if img is not None:
    tex.image = img
    try:
        tex.image.alpha_mode = "STRAIGHT"
    except Exception:
        pass
    try:
        tex.image.colorspace_settings.name = "sRGB"
    except Exception:
        pass

nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
if "Alpha" in tex.outputs and "Alpha" in bsdf.inputs:
    nt.links.new(tex.outputs["Alpha"], bsdf.inputs["Alpha"])
nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])

# Prefer CLIP cutout so we don't go fully invisible with bad alpha
for attr, val in (
    ("blend_method", "CLIP"),
    ("alpha_threshold", 0.05),
    ("use_backface_culling", False),
):
    if hasattr(mat, attr):
        try:
            setattr(mat, attr, val)
        except Exception:
            pass

if not body.data.materials:
    body.data.materials.append(mat)
else:
    body.data.materials[0] = mat

# Recalc normals
bm = bmesh.new()
bm.from_mesh(mesh)
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
bm.to_mesh(mesh)
bm.free()
mesh.update()

# Viewport: Material Preview
for window in bpy.context.window_manager.windows:
    for area in window.screen.areas:
        if area.type == "VIEW_3D":
            for space in area.spaces:
                if space.type == "VIEW_3D":
                    space.shading.type = "MATERIAL"

bpy.ops.wm.save_mainfile()
print("FIXED: UVs + material. Image:", img.filepath if img else None)
