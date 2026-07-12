import bpy
import bmesh
from mathutils import Vector, Euler
from math import radians
from pathlib import Path
from collections import defaultdict, deque

IMG_PATH = r"C:\Users\4step\projects\sentou-koubou-brennen-kitpass\docs\assets\whitney\variants\whitney_combat_right.png"
OUT_BLEND = r"C:\Users\4step\projects\sentou-koubou-brennen-kitpass\tools\char-anim-pipeline\blender\whitney_combat_rig.blend"
OUT2 = r"C:\Users\4step\projects\sentou-koubou-brennen-kitpass-combat-visuals\tools\char-anim-pipeline\blender\whitney_combat_rig.blend"
PLANE_W, PLANE_H = 1.5, 2.0
SUBDIVS = 6

# --- clean scene ---
bpy.ops.wm.read_homefile(use_empty=True)
scn = bpy.context.scene
# remove leftover
for o in list(bpy.data.objects):
    bpy.data.objects.remove(o, do_unlink=True)

img = bpy.data.images.load(IMG_PATH)
img.alpha_mode = "STRAIGHT"

# --- mesh plane XZ facing -Y (camera from -Y) ---
# Create grid in XZ: verts
bm = bmesh.new()
# start with simple plane
bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=0.5)
# rotate so face is XZ: currently XY plane at z=0, rotate -90 X => XZ with +Y normal
# want facing camera at -Y so normal should be -Y
bmesh.ops.rotate(bm, verts=bm.verts, cent=(0,0,0), matrix=Euler((radians(-90), 0, 0)).to_matrix().to_4x4())
# scale and position
for v in bm.verts:
    v.co.x *= PLANE_W
    v.co.z = (v.co.z + 0.5) * PLANE_H  # map from -0.5..0.5 to 0..H after rot... 
# After -90 X: original (x,y,0) -> (x, 0, -y). y was -0.5..0.5 so z is 0.5..-0.5
# Fix: rebuild cleanly
bm.clear()
# Manual quad
xs = [-PLANE_W/2, PLANE_W/2]
zs = [0.0, PLANE_H]
# create subdivided grid
sx, sz = 1, 1
for _ in range(SUBDIVS):
    sx *= 2
    sz *= 2
# create verts
verts = [[None]*(sz+1) for _ in range(sx+1)]
for ix in range(sx+1):
    for iz in range(sz+1):
        x = -PLANE_W/2 + PLANE_W * ix / sx
        z = PLANE_H * iz / sz
        verts[ix][iz] = bm.verts.new((x, 0.0, z))
bm.verts.ensure_lookup_table()
for ix in range(sx):
    for iz in range(sz):
        # winding so normal is -Y (for camera from -Y looking +Y)
        v0 = verts[ix][iz]
        v1 = verts[ix+1][iz]
        v2 = verts[ix+1][iz+1]
        v3 = verts[ix][iz+1]
        bm.faces.new((v0, v3, v2, v1))  # check normal

uv_layer = bm.loops.layers.uv.new()
for f in bm.faces:
    for loop in f.loops:
        co = loop.vert.co
        u = (co.x + PLANE_W/2) / PLANE_W
        v = co.z / PLANE_H
        loop[uv_layer].uv = (u, v)

mesh = bpy.data.meshes.new("WhitneyBody_mesh")
bm.to_mesh(mesh)
bm.free()
body = bpy.data.objects.new("WhitneyBody", mesh)
scn.collection.objects.link(body)

# material
mat = bpy.data.materials.new("WhitneyBody_mat")
mat.use_nodes = True
nt = mat.node_tree
for n in list(nt.nodes):
    nt.nodes.remove(n)
out = nt.nodes.new("ShaderNodeOutputMaterial")
bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
tex = nt.nodes.new("ShaderNodeTexImage")
tex.image = img
nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
nt.links.new(tex.outputs["Alpha"], bsdf.inputs["Alpha"])
if "Emission Color" in bsdf.inputs:
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Emission Color"])
    bsdf.inputs["Emission Strength"].default_value = 0.85
nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
try:
    mat.surface_render_method = "DITHERED"
except Exception:
    pass
body.data.materials.append(mat)

# --- armature ---
arm_data = bpy.data.armatures.new("WhitneyArmature")
arm = bpy.data.objects.new("WhitneyArmature", arm_data)
scn.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm
arm.select_set(True)
bpy.context.view_layer.update()
# ensure EDIT mode with context override if needed
try:
    bpy.ops.object.mode_set(mode="EDIT")
except Exception:
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.select_all(action='DESELECT')
    arm.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
ebs = arm_data.edit_bones

def ab(name, head, tail, parent=None):
    b = ebs.new(name)
    b.head = Vector(head)
    b.tail = Vector(tail)
    if parent:
        b.parent = ebs[parent]
    return b

ab("root", (0,0,0.05), (0,0,0.18))
ab("hip", (0,0,0.72), (0,0,0.88), "root")
ab("torso", (0,0,0.88), (0,0,1.08), "hip")
ab("chest", (0,0,1.08), (0,0,1.28), "torso")
ab("neck", (0,0,1.28), (0.02,0,1.38), "chest")
ab("head", (0.02,0,1.38), (0.02,0,1.58), "neck")
ab("hat", (0.02,0,1.58), (0.05,0,1.82), "head")
ab("shoulder_r", (0.12,0,1.26), (0.28,0,1.32), "chest")
ab("upper_arm_r", (0.28,0,1.32), (0.32,0,1.48), "shoulder_r")
ab("forearm_r", (0.32,0,1.48), (0.30,0,1.58), "upper_arm_r")
ab("hand_r", (0.30,0,1.58), (0.28,0,1.64), "forearm_r")
ab("weapon", (0.28,0,1.64), (0.38,0,1.95), "hand_r")
ab("shoulder_l", (-0.12,0,1.26), (-0.28,0,1.22), "chest")
ab("upper_arm_l", (-0.28,0,1.22), (-0.22,0,1.02), "shoulder_l")
ab("forearm_l", (-0.22,0,1.02), (-0.08,0,0.98), "upper_arm_l")
ab("hand_l", (-0.08,0,0.98), (0.02,0,0.96), "forearm_l")
ab("thigh_r", (0.10,0,0.72), (0.18,0,0.42), "hip")
ab("shin_r", (0.18,0,0.42), (0.16,0,0.12), "thigh_r")
ab("foot_r", (0.16,0,0.12), (0.28,0,0.08), "shin_r")
ab("thigh_l", (-0.10,0,0.72), (-0.20,0,0.40), "hip")
ab("shin_l", (-0.20,0,0.40), (-0.18,0,0.10), "thigh_l")
ab("foot_l", (-0.18,0,0.10), (-0.30,0,0.06), "shin_l")

bpy.ops.object.mode_set(mode="OBJECT")

# parent auto weights
body.select_set(True)
arm.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.parent_set(type="ARMATURE_AUTO")

me = body.data
Wimg, Himg = img.size
px = list(img.pixels[:])

def rgba(x,y):
    i=(y*Wimg+x)*4
    return px[i],px[i+1],px[i+2],px[i+3]

def is_green(r,g,b,a):
    return a<0.08 or (g>0.45 and g>r*1.3 and g>b*1.3)

def is_skin(r,g,b,a):
    if a<0.1 or is_green(r,g,b,a): return False
    return r>0.45 and 0.25<g<0.8 and b>0.15 and r>g*0.9 and (r-b)>0.08

def is_quill(r,g,b,a):
    if a<0.1 or is_green(r,g,b,a) or is_skin(r,g,b,a): return False
    # bright cyan/teal plume (measured ~r0.18 g0.58 b0.72)
    cyan = (b > 0.40 and g > 0.30 and b >= r * 1.2 and r < 0.55)
    lum = 0.2126*r + 0.7152*g + 0.0722*b
    dark = lum < 0.28 and a > 0.35 and r < 0.35
    spark = b > 0.50 and g > 0.35 and r < 0.55
    return cyan or dark or spark

bm = bmesh.new(); bm.from_mesh(me)
uv_lay = bm.loops.layers.uv.active
vert_uv = {}
for f in bm.faces:
    for loop in f.loops:
        if loop.vert.index not in vert_uv:
            vert_uv[loop.vert.index] = loop[uv_lay].uv.copy()
adj = defaultdict(set)
for e in bm.edges:
    a,b = e.verts[0].index, e.verts[1].index
    adj[a].add(b); adj[b].add(a)
bm.free()

body_imw = body.matrix_world.inverted()
arm_mw = arm.matrix_world
wb = arm.data.bones["weapon"]
w_head = body_imw @ (arm_mw @ wb.head_local)
w_tail = body_imw @ (arm_mw @ wb.tail_local)
axis_n = (w_tail - w_head).normalized()
axis_len = max((w_tail-w_head).length, 1e-6)
head_p = body_imw @ (arm_mw @ arm.data.bones["head"].head_local)

def clear_set(i, pairs):
    for ge in list(me.vertices[i].groups):
        body.vertex_groups[ge.group].remove([i])
    for n,w in pairs:
        if n not in body.vertex_groups:
            body.vertex_groups.new(name=n)
        if w>0.001:
            body.vertex_groups[n].add([i], float(w), "REPLACE")

quill=set()
for i,v in enumerate(me.vertices):
    if i not in vert_uv: continue
    uv = vert_uv[i]
    if uv.x < 0.48 or uv.y < 0.48: continue
    x=int(max(0,min(Wimg-1,uv.x*Wimg))); y=int(max(0,min(Himg-1,uv.y*Himg)))
    r,g,b,a = rgba(x,y)
    if not is_quill(r,g,b,a): continue
    if (v.co-head_p).length < 0.12: continue
    t = (v.co-w_head).dot(axis_n)/axis_len
    proj = w_head + axis_n*((v.co-w_head).dot(axis_n))
    d = (v.co-proj).length
    if t < -0.2 or t > 1.5: continue
    if d > 0.35: continue
    clear_set(i, [("weapon", 1.0)])
    quill.add(i)

q = deque(quill)
while q:
    i = q.popleft()
    for j in adj[i]:
        if j in quill or j not in vert_uv: continue
        uv = vert_uv[j]
        if uv.x < 0.48 or uv.y < 0.48: continue
        x=int(max(0,min(Wimg-1,uv.x*Wimg))); y=int(max(0,min(Himg-1,uv.y*Himg)))
        r,g,b,a = rgba(x,y)
        if not is_quill(r,g,b,a): continue
        if (me.vertices[j].co-head_p).length < 0.12: continue
        clear_set(j, [("weapon", 1.0)])
        quill.add(j); q.append(j)

# head reinforce
for i,v in enumerate(me.vertices):
    if i not in vert_uv: continue
    if (v.co-head_p).length > 0.24: continue
    uv = vert_uv[i]
    x=int(max(0,min(Wimg-1,uv.x*Wimg))); y=int(max(0,min(Himg-1,uv.y*Himg)))
    r,g,b,a = rgba(x,y)
    ww=0
    if "weapon" in body.vertex_groups:
        gi=body.vertex_groups["weapon"].index
        for ge in me.vertices[i].groups:
            if ge.group==gi: ww=ge.weight
    if ww >= 0.5: continue
    if is_skin(r,g,b,a) or (uv.y>0.72 and uv.x<0.65 and not is_quill(r,g,b,a)):
        clear_set(i, [("head", 0.65), ("hat", 0.35)] if uv.y>0.78 else [("head", 1.0)])

# --- animations ---
def ensure_action(name):
    act = bpy.data.actions.get(name)
    if act:
        bpy.data.actions.remove(act)
    return bpy.data.actions.new(name)

if not arm.animation_data:
    arm.animation_data_create()

def clear_pose():
    for pb in arm.pose.bones:
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = (0,0,0)
        pb.location = (0,0,0)
        pb.scale = (1,1,1)

def insert_pose(frame):
    bpy.context.scene.frame_set(frame)
    for pb in arm.pose.bones:
        pb.keyframe_insert("rotation_euler", frame=frame)
        pb.keyframe_insert("location", frame=frame)
        pb.keyframe_insert("scale", frame=frame)

# IDLE
arm.animation_data.action = ensure_action("idle")
def set_idle(frame, breath=0.0, knee=0.0):
    clear_pose()
    arm.pose.bones["torso"].rotation_euler = (0.02+breath, 0, 0.01)
    arm.pose.bones["chest"].rotation_euler = (breath*0.5, 0, 0)
    arm.pose.bones["head"].rotation_euler = (-breath*0.3, 0, 0)
    arm.pose.bones["hat"].rotation_euler = (-breath*0.15, 0, 0)
    k = 0.035+knee; s = -0.055-knee*1.4
    for side in ("l","r"):
        arm.pose.bones[f"thigh_{side}"].rotation_euler[0] = k
        arm.pose.bones[f"shin_{side}"].rotation_euler[0] = s
    arm.pose.bones["weapon"].rotation_euler = (0, 0, breath*0.5)
    arm.pose.bones["forearm_r"].rotation_euler = (breath*0.35, 0, breath*0.25)
    insert_pose(frame)

set_idle(1, 0, 0)
set_idle(24, 0.035, 0.03)
set_idle(48, 0, 0)

# ATTACK cast flourish
arm.animation_data.action = ensure_action("attack")
clear_pose(); insert_pose(1)
clear_pose()
arm.pose.bones["torso"].rotation_euler = (0,0,-0.08)
arm.pose.bones["chest"].rotation_euler = (0.05,0,-0.1)
arm.pose.bones["upper_arm_r"].rotation_euler = (-0.15,0,-0.2)
arm.pose.bones["forearm_r"].rotation_euler = (-0.1,0,-0.15)
arm.pose.bones["weapon"].rotation_euler = (0,0,-0.25)
insert_pose(6)
clear_pose()
arm.pose.bones["torso"].rotation_euler = (0.05,0,0.12)
arm.pose.bones["chest"].rotation_euler = (0.08,0,0.15)
arm.pose.bones["upper_arm_r"].rotation_euler = (0.25,0,0.35)
arm.pose.bones["forearm_r"].rotation_euler = (0.2,0,0.3)
arm.pose.bones["weapon"].rotation_euler = (0.1,0.15,0.45)
arm.pose.bones["head"].rotation_euler = (0,0,0.05)
insert_pose(12)
clear_pose(); insert_pose(20)

# HIT
arm.animation_data.action = ensure_action("hit")
clear_pose(); insert_pose(1)
clear_pose()
arm.pose.bones["torso"].rotation_euler = (-0.08,0,-0.12)
arm.pose.bones["chest"].rotation_euler = (-0.05,0,-0.08)
arm.pose.bones["head"].rotation_euler = (-0.1,0,-0.15)
arm.pose.bones["upper_arm_r"].rotation_euler = (-0.1,0,-0.1)
insert_pose(5)
clear_pose(); insert_pose(12)

# DEAD
arm.animation_data.action = ensure_action("dead")
clear_pose(); insert_pose(1)
clear_pose()
arm.pose.bones["hip"].rotation_euler = (0.4,0,0.3)
arm.pose.bones["torso"].rotation_euler = (0.5,0,0.2)
arm.pose.bones["chest"].rotation_euler = (0.3,0,0.1)
arm.pose.bones["head"].rotation_euler = (0.4,0,0.2)
arm.pose.bones["thigh_l"].rotation_euler = (0.3,0,0.2)
arm.pose.bones["thigh_r"].rotation_euler = (0.2,0,-0.1)
arm.pose.bones["upper_arm_r"].rotation_euler = (0.5,0,0.4)
arm.pose.bones["upper_arm_l"].rotation_euler = (0.3,0,-0.3)
insert_pose(12)
insert_pose(20)

# smooth keys
for act in bpy.data.actions:
    if not getattr(act, "is_action_layered", False):
        continue
    for layer in act.layers:
        for strip in layer.strips:
            if not hasattr(strip, "channelbags"): continue
            for bag in strip.channelbags:
                for fc in bag.fcurves:
                    for kp in fc.keyframe_points:
                        kp.interpolation = "BEZIER"
                        kp.handle_left_type = "AUTO_CLAMPED"
                        kp.handle_right_type = "AUTO_CLAMPED"
                    fc.update()

# camera
cam_data = bpy.data.cameras.new("Camera")
cam = bpy.data.objects.new("Camera", cam_data)
scn.collection.objects.link(cam)
cam.data.type = "ORTHO"
cam.data.ortho_scale = 2.3
cam.location = (0, -5, 1.0)
cam.rotation_euler = (radians(90), 0, 0)
scn.camera = cam

light_data = bpy.data.lights.new("Light", "SUN")
light = bpy.data.objects.new("Light", light_data)
scn.collection.objects.link(light)
light.location = (1, -2, 3)
light.data.energy = 3

arm.animation_data.action = bpy.data.actions["idle"]
scn.frame_start = 1
scn.frame_end = 48
scn.frame_current = 24
scn.render.fps = 24

bpy.context.view_layer.objects.active = arm
for o in bpy.context.view_layer.objects:
    o.select_set(False)
arm.select_set(True)
bpy.ops.object.mode_set(mode="POSE")

for area in bpy.context.screen.areas:
    if area.type == "VIEW_3D":
        for sp in area.spaces:
            if sp.type == "VIEW_3D":
                sp.shading.type = "MATERIAL"
                sp.overlay.show_bones = True

Path(OUT_BLEND).parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
try:
    Path(OUT2).parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=OUT2, copy=True)
    bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
except Exception:
    pass

result = {
    "saved": OUT_BLEND,
    "verts": len(me.vertices),
    "bones": len(arm.data.bones),
    "quill_verts": len(quill),
    "actions": {a.name: [float(a.frame_range[0]), float(a.frame_range[1])] for a in bpy.data.actions},
    "frame": scn.frame_current,
    "action": "idle",
}
print("RESULT", result)



