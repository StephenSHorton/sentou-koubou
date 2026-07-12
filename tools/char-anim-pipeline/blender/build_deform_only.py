"""
Single-mesh armature deform only (no part splits).
Subdivides the fullbody plane so bones can bend the painting.
"""
import bmesh
import bpy
import math
from mathutils import Vector, Matrix, Euler
from pathlib import Path

IMG_PATH = Path(
    r"C:\Users\4step\projects\sentou-koubou-brennen-kitpass"
    r"\docs\assets\brennen\variants\brennen_combat_right.png"
)

arm = bpy.data.objects.get("BrennenArmature")
if not arm:
    raise RuntimeError("BrennenArmature missing")

# Remove hybrid parts
removed = []
for obj in list(bpy.data.objects):
    if obj.name.startswith("Part_"):
        removed.append(obj.name)
        bpy.data.objects.remove(obj, do_unlink=True)

# Remove parts collection if empty
if "Brennen_Parts" in bpy.data.collections:
    col = bpy.data.collections["Brennen_Parts"]
    if len(col.objects) == 0 and len(col.children) == 0:
        bpy.data.collections.remove(col)

# Find or recreate body
body = bpy.data.objects.get("BrennenBody") or bpy.data.objects.get("BrennenBody_REF")
img = bpy.data.images.load(str(IMG_PATH), check_existing=True)
try:
    img.alpha_mode = "STRAIGHT"
except Exception:
    pass

aspect = img.size[0] / max(1, img.size[1])
H = 2.0
W = H * aspect

bpy.ops.object.mode_set(mode="OBJECT")

if body is None:
    mesh = bpy.data.meshes.new("BrennenBody_mesh")
    body = bpy.data.objects.new("BrennenBody", mesh)
    bpy.context.scene.collection.objects.link(body)
else:
    body.name = "BrennenBody"
    body.hide_render = False
    body.hide_set(False)
    body.display_type = "TEXTURED"
    try:
        body.visible_camera = True
    except Exception:
        pass
    mesh = body.data

# Clear parent / modifiers
body.parent = None
body.parent_type = "OBJECT"
body.parent_bone = ""
while body.modifiers:
    body.modifiers.remove(body.modifiers[0])

# Rebuild upright subdivided plane (XZ, normal +Y)
bm = bmesh.new()
# grid: more cuts = smoother deform
cuts_x, cuts_z = 24, 32
for iz in range(cuts_z + 1):
    for ix in range(cuts_x + 1):
        u = ix / cuts_x
        v = iz / cuts_z
        x = -W / 2 + u * W
        z = v * H
        bm.verts.new((x, 0.0, z))
bm.verts.ensure_lookup_table()

def vid(ix, iz):
    return iz * (cuts_x + 1) + ix

for iz in range(cuts_z):
    for ix in range(cuts_x):
        v0 = bm.verts[vid(ix, iz)]
        v1 = bm.verts[vid(ix + 1, iz)]
        v2 = bm.verts[vid(ix + 1, iz + 1)]
        v3 = bm.verts[vid(ix, iz + 1)]
        bm.faces.new((v0, v1, v2, v3))

bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
bm.to_mesh(mesh)
bm.free()
mesh.update()

# UVs
if not mesh.uv_layers:
    mesh.uv_layers.new(name="UVMap")
# Rebuild UVs from vertex positions
uv_layer = mesh.uv_layers.active.data
for poly in mesh.polygons:
    for li in poly.loop_indices:
        loop = mesh.loops[li]
        co = mesh.vertices[loop.vertex_index].co
        u = (co.x + W / 2) / W
        v = co.z / H
        uv_layer[li].uv = (u, v)

# Material
mat = bpy.data.materials.get("BrennenBody_mat") or bpy.data.materials.new("BrennenBody_mat")
mat.use_nodes = True
nt = mat.node_tree
nt.nodes.clear()
out = nt.nodes.new("ShaderNodeOutputMaterial")
bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
tex = nt.nodes.new("ShaderNodeTexImage")
tex.image = img
nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
nt.links.new(tex.outputs["Alpha"], bsdf.inputs["Alpha"])
nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
for attr, val in (("blend_method", "CLIP"), ("alpha_threshold", 0.05), ("use_backface_culling", False)):
    if hasattr(mat, attr):
        try:
            setattr(mat, attr, val)
        except Exception:
            pass
mesh.materials.clear()
mesh.materials.append(mat)

# Place body at origin upright (armature should already match character)
body.location = (0, 0, 0)
body.rotation_euler = (0, 0, 0)
body.scale = (1, 1, 1)

# --- Armature deform with automatic weights ---
for o in bpy.context.view_layer.objects:
    o.select_set(False)
body.select_set(True)
arm.select_set(True)
bpy.context.view_layer.objects.active = arm

# Parent with automatic weights
try:
    bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    parent_method = "ARMATURE_AUTO"
except Exception as e:
    # Fallback: manual armature modifier + heat-ish weights from bone proximity
    parent_method = f"fallback:{e}"
    body.parent = arm
    body.parent_type = "OBJECT"
    mod = body.modifiers.new(name="Armature", type="ARMATURE")
    mod.object = arm
    mod.use_vertex_groups = True

    # Create vertex groups per bone and assign by distance to bone segment in rest pose
    bpy.context.view_layer.update()
    # Clear old groups
    body.vertex_groups.clear()
    for bone in arm.data.bones:
        body.vertex_groups.new(name=bone.name)

    # Rest bone head/tail in body local space
    arm_mw = arm.matrix_world
    body_iw = body.matrix_world.inverted()
    bone_segs = []
    for bone in arm.data.bones:
        h = body_iw @ (arm_mw @ bone.head_local.to_4d())
        t = body_iw @ (arm_mw @ bone.tail_local.to_4d())
        # head_local is Vector 3
        h = body_iw @ arm_mw @ bone.head_local
        t = body_iw @ arm_mw @ bone.tail_local
        bone_segs.append((bone.name, h, t))

    # Assign weights: inverse-distance to nearest points on bone segments
    for vi, vert in enumerate(mesh.vertices):
        p = vert.co
        weights = {}
        for name, h, t in bone_segs:
            d = t - h
            L2 = d.length_squared
            if L2 < 1e-10:
                dist = (p - h).length
            else:
                u = max(0.0, min(1.0, (p - h).dot(d) / L2))
                proj = h + d * u
                dist = (p - proj).length
            # falloff
            w = 1.0 / (dist * dist + 1e-4)
            weights[name] = w
        # keep top 4 bones, normalize
        top = sorted(weights.items(), key=lambda x: x[1], reverse=True)[:4]
        s = sum(w for _, w in top) or 1.0
        for name, w in top:
            body.vertex_groups[name].add([vi], w / s, "REPLACE")

# Ensure armature modifier exists and is first
has_arm_mod = any(m.type == "ARMATURE" for m in body.modifiers)
if not has_arm_mod:
    mod = body.modifiers.new(name="Armature", type="ARMATURE")
    mod.object = arm
else:
    for m in body.modifiers:
        if m.type == "ARMATURE":
            m.object = arm
            m.use_vertex_groups = True

arm.show_in_front = True

# Material preview
for window in bpy.context.window_manager.windows:
    for area in window.screen.areas:
        if area.type == "VIEW_3D":
            for space in area.spaces:
                if space.type == "VIEW_3D":
                    space.shading.type = "MATERIAL"

# Select armature for posing
for o in bpy.context.view_layer.objects:
    o.select_set(False)
arm.select_set(True)
bpy.context.view_layer.objects.active = arm

bpy.ops.wm.save_mainfile()

vg_count = len(body.vertex_groups)
weighted = 0
for vg in body.vertex_groups:
    # count roughly
    pass

result = {
    "mode": "armature_deform_only",
    "removed_parts": removed,
    "parent_method": parent_method,
    "subdiv": [cuts_x, cuts_z],
    "vertex_groups": [g.name for g in body.vertex_groups],
    "modifiers": [m.type for m in body.modifiers],
    "parent": body.parent.name if body.parent else None,
    "verts": len(mesh.vertices),
    "saved": bpy.data.filepath,
}
