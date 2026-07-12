"""
Hybrid Brennen: split fullbody into limb/torso/head/weapon pieces + armature deform.
Each piece is a small plane with UVs into the same texture, weighted to its bone(s).
"""
import bmesh
import bpy
import math
from mathutils import Vector, Matrix
from pathlib import Path

IMG_PATH = Path(
    r"C:\Users\4step\projects\sentou-koubou-brennen-kitpass"
    r"\docs\assets\brennen\variants\brennen_combat_right.png"
)

arm = bpy.data.objects.get("BrennenArmature")
old_body = bpy.data.objects.get("BrennenBody")
if not arm:
    raise RuntimeError("BrennenArmature missing")

# --- image ---
img = bpy.data.images.load(str(IMG_PATH), check_existing=True)
try:
    img.alpha_mode = "STRAIGHT"
except Exception:
    pass
try:
    img.colorspace_settings.name = "sRGB"
except Exception:
    pass

aspect = img.size[0] / max(1, img.size[1])
H = 2.0
W = H * aspect

# Collection
col_name = "Brennen_Parts"
if col_name in bpy.data.collections:
    col = bpy.data.collections[col_name]
else:
    col = bpy.data.collections.new(col_name)
    bpy.context.scene.collection.children.link(col)


def link(obj):
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    col.objects.link(obj)
    return obj


# Remove previous generated parts
for obj in list(bpy.data.objects):
    if obj.name.startswith("Part_"):
        bpy.data.objects.remove(obj, do_unlink=True)

# Hide / unparent old single body (keep as reference, semi-transparent)
if old_body:
    mw = old_body.matrix_world.copy()
    old_body.parent = None
    old_body.matrix_world = mw
    old_body.name = "BrennenBody_REF"
    old_body.hide_render = True
    # leave visible but muted — user can hide
    old_body.display_type = "WIRE"
    try:
        old_body.visible_camera = False
    except Exception:
        pass

# Shared material (full texture; UVs crop each piece)
mat_name = "BrennenParts_mat"
mat = bpy.data.materials.get(mat_name) or bpy.data.materials.new(mat_name)
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
for attr, val in (("blend_method", "CLIP"), ("alpha_threshold", 0.08), ("use_backface_culling", False)):
    if hasattr(mat, attr):
        try:
            setattr(mat, attr, val)
        except Exception:
            pass


def uv_to_local(u, v):
    """Map UV (0-1) to mesh local space matching original upright plane (XZ, normal +Y)."""
    x = -W / 2 + u * W
    z = v * H
    return Vector((x, 0.0, z))


def make_part(name, u0, v0, u1, v1, primary_bone, soft_bones=None):
    """
    soft_bones: list of (bone_name, weight) for hybrid blend at joints.
    primary gets remaining weight so totals ~1.
    """
    soft_bones = soft_bones or []
    mesh = bpy.data.meshes.new(f"Part_{name}_mesh")
    # 4 corners in UV space -> local 3D
    corners_uv = [(u0, v0), (u1, v0), (u1, v1), (u0, v1)]
    verts = [uv_to_local(u, v) for u, v in corners_uv]
    mesh.from_pydata(verts, [], [(0, 1, 2, 3)])
    mesh.update()

    # UVs
    mesh.uv_layers.new(name="UVMap")
    uv = mesh.uv_layers.active.data
    for i, loop_i in enumerate(mesh.polygons[0].loop_indices):
        uv[loop_i].uv = corners_uv[i]

    obj = bpy.data.objects.new(f"Part_{name}", mesh)
    link(obj)
    obj.data.materials.append(mat)

    # Place in world at origin same as original design; parent armature with deform
    obj.parent = arm
    obj.parent_type = "OBJECT"
    obj.parent_bone = ""

    # Armature modifier
    mod = obj.modifiers.new(name="Armature", type="ARMATURE")
    mod.object = arm
    mod.use_vertex_groups = True

    # Vertex groups
    n = len(mesh.vertices)
    primary_w = 1.0 - sum(w for _, w in soft_bones)
    if primary_w < 0.05:
        primary_w = 0.5
        # renormalize soft
        soft_bones = [(b, w * 0.5) for b, w in soft_bones]

    groups = {primary_bone: primary_w}
    for b, w in soft_bones:
        groups[b] = groups.get(b, 0.0) + w

    for bname, w in groups.items():
        if bname not in arm.data.bones:
            continue
        vg = obj.vertex_groups.new(name=bname)
        vg.add(list(range(n)), w, "REPLACE")

    # Ensure all armature bone groups exist empty (optional)
    for bone in arm.data.bones:
        if bone.name not in obj.vertex_groups:
            obj.vertex_groups.new(name=bone.name)

    return obj


# UV regions tuned for fullbody combat-right portrait (approximate; tweakable)
# Image: character centered-right, sword on right side of frame, head upper, legs lower
PARTS = [
    # name, u0, v0, u1, v1, primary, soft
    ("head", 0.28, 0.72, 0.72, 0.98, "head", [("neck", 0.25), ("chest", 0.1)]),
    ("torso", 0.22, 0.38, 0.72, 0.78, "torso", [("chest", 0.35), ("hip", 0.2)]),
    ("pelvis", 0.28, 0.28, 0.68, 0.45, "hip", [("torso", 0.3)]),
    ("leg_r", 0.48, 0.02, 0.78, 0.40, "thigh_r", [("shin_r", 0.35), ("hip", 0.15)]),
    ("leg_l", 0.18, 0.02, 0.48, 0.40, "thigh_l", [("shin_l", 0.35), ("hip", 0.15)]),
    ("arm_r", 0.55, 0.42, 0.88, 0.78, "upper_arm_r", [("forearm_r", 0.3), ("chest", 0.15)]),
    ("arm_l", 0.12, 0.38, 0.42, 0.72, "upper_arm_l", [("forearm_l", 0.25), ("chest", 0.15)]),
    ("weapon", 0.62, 0.35, 0.98, 0.95, "weapon", [("hand_r", 0.25), ("forearm_r", 0.1)]),
]

created = []
for spec in PARTS:
    name, u0, v0, u1, v1, primary, soft = spec
    if primary not in arm.data.bones:
        # skip if bone missing
        continue
    soft = [(b, w) for b, w in soft if b in arm.data.bones]
    obj = make_part(name, u0, v0, u1, v1, primary, soft)
    created.append(obj.name)

# Match old body world placement: original upright plane at roughly z 0..2
# Parts already in same local space as original mesh design.
# If REF body exists, copy its world matrix to a root empty and parent parts? 
# Simpler: leave parts in armature object space at rest matching original bone-layout coords.

# Align parts collection to sit like REF if present
ref = bpy.data.objects.get("BrennenBody_REF")
if ref:
    # Parts are in armature-local space matching original unparented body at origin.
    # Move each part so its center roughly matches ref's world placement by
    # applying ref's matrix relative to original design (identity at origin).
    # Use: part.matrix_world = ref.matrix_world @ part.matrix_local (local already set)
    for name in created:
        obj = bpy.data.objects[name]
        # store local mesh as rest relative to armature object
        # bake ref transform onto object location/rotation/scale without breaking armature parent
        # Clear parent temporarily
        local_mesh_ok = True
    # Parent parts to armature object (already), set matrix_basis so world matches
    # ref was free at world; original parts built in same space as body before bone parent.
    # If ref.matrix is not identity, apply same transform to each part's matrix_basis.
    R = ref.matrix_world.copy()
    for name in created:
        obj = bpy.data.objects[name]
        # object parented to arm: matrix_world = arm.matrix_world @ matrix_local
        # want matrix_world ≈ R @ mesh_local (mesh_local currently identity-ish)
        # so matrix_local = arm.matrix_world.inverted() @ R @ Matrix.Identity
        obj.matrix_local = arm.matrix_world.inverted() @ R

# Armature binding: also set deform on armature
arm.show_in_front = True

# Pose mode smoke: ensure modifiers visible
for name in created:
    obj = bpy.data.objects[name]
    for mod in obj.modifiers:
        if mod.type == "ARMATURE":
            mod.show_in_editmode = True
            mod.show_on_cage = True

# Viewport material
for window in bpy.context.window_manager.windows:
    for area in window.screen.areas:
        if area.type == "VIEW_3D":
            for space in area.spaces:
                if space.type == "VIEW_3D":
                    space.shading.type = "MATERIAL"

# Select armature
for o in bpy.context.view_layer.objects:
    o.select_set(False)
arm.select_set(True)
bpy.context.view_layer.objects.active = arm

bpy.ops.wm.save_mainfile()

result = {
    "parts": created,
    "ref_hidden_wire": bool(ref),
    "saved": bpy.data.filepath,
    "note": "Hybrid paper-doll + armature deform. Pose armature — limbs should move with soft joint blend.",
}
