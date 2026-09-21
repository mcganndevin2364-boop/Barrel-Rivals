# RGB is neutral linear coat reflectance; alpha masks bare muzzle/hoof skin.
# The material remains opaque. Fine coat relief must not grow over bare skin.
# Neutral reflectance variations follow anatomy; lighting is never baked in.
# Two closed globes share one material and rigid Head skin. Rest centers are
# measured against the existing eyelid cavities, not guessed from a bone tip.
# Latitude clustering preserves a continuous limbal ring and horizontal pupil.

"""Original bay coat/eye study on the attributed b2przemo horse.
Procedural materials and mesh vertex colors; no downloaded or generated bitmap.
Coordinates are metres, +Y forward, +Z up. Does not change body/rig/motion.
"""
import bpy, bmesh, math, sys, json, hashlib
from pathlib import Path
from mathutils import Vector
args = sys.argv[sys.argv.index('--') + 1:]
source = Path(args[0])
out = Path(args[1])
out.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source), use_scripts=False)
body = bpy.data.objects['HeroHorseBody']
rig = bpy.data.objects['HeroHorseRig']
rig.data.pose_position = 'REST'
bpy.context.view_layer.update()

def smooth(a, b, x):
    t = max(0, min(1, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)

def mix(a, b, t):
    return tuple((x * (1 - t) + y * t for (x, y) in zip(a, b)))

def material(name, attr, roughness):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    n = m.node_tree.nodes
    l = m.node_tree.links
    p = n.get('Principled BSDF')
    p.inputs['Roughness'].default_value = roughness
    p.inputs['IOR'].default_value = 1.45
    a = n.new('ShaderNodeVertexColor')
    a.layer_name = attr
    l.new(a.outputs['Color'], p.inputs['Base Color'])
    return (m, n, l, p)
(coat, n, l, p) = material('OriginalBayCoat', 'CoatColor', 0.38)
p.inputs['Sheen Weight'].default_value = 0.07
p.inputs['Sheen Roughness'].default_value = 0.65
p.inputs['Specular IOR Level'].default_value = 0.3
uv = n.new('ShaderNodeTexCoord')
mapping = n.new('ShaderNodeVectorMath')
mapping.operation = 'MULTIPLY'
mapping.inputs[1].default_value = (850, 260, 1)
l.new(uv.outputs['UV'], mapping.inputs[0])
noise = n.new('ShaderNodeTexNoise')
noise.inputs['Scale'].default_value = 1
noise.inputs['Detail'].default_value = 2
l.new(mapping.outputs['Vector'], noise.inputs['Vector'])
bump = n.new('ShaderNodeBump')
bump.inputs['Strength'].default_value = 0.28
bump.inputs['Distance'].default_value = 0.00035
l.new(noise.outputs['Fac'], bump.inputs['Height'])
l.new(bump.outputs['Normal'], p.inputs['Normal'])
rough = n.new('ShaderNodeMapRange')
rough.inputs['To Min'].default_value = 0.48
rough.inputs['To Max'].default_value = 0.6
l.new(noise.outputs['Fac'], rough.inputs['Value'])
l.new(rough.outputs['Result'], p.inputs['Roughness'])
attribute = next((node for node in n if node.type == 'VERTEX_COLOR'))
bare = n.new('ShaderNodeMixRGB')
bare.inputs[2].default_value = (0.36, 0.36, 0.36, 1)
l.new(attribute.outputs['Alpha'], bare.inputs[0])
l.new(rough.outputs['Result'], bare.inputs[1])
l.new(bare.outputs[0], p.inputs['Roughness'])
strength = n.new('ShaderNodeMapRange')
strength.inputs['To Min'].default_value = 0.28
strength.inputs['To Max'].default_value = 0
l.new(attribute.outputs['Alpha'], strength.inputs['Value'])
l.new(strength.outputs['Result'], bump.inputs['Strength'])
grain = n.new('ShaderNodeMapRange')
grain.inputs['To Min'].default_value = 0.9
grain.inputs['To Max'].default_value = 1.04
l.new(noise.outputs['Fac'], grain.inputs['Value'])
tint = n.new('ShaderNodeMixRGB')
tint.blend_type = 'MULTIPLY'
tint.inputs[0].default_value = 1
l.new(attribute.outputs['Color'], tint.inputs[1])
l.new(grain.outputs['Result'], tint.inputs[2])
l.new(tint.outputs[0], p.inputs['Base Color'])
body.data.materials.clear()
body.data.materials.append(coat)
col = body.data.color_attributes.get('CoatColor') or body.data.color_attributes.new(name='CoatColor', type='FLOAT_COLOR', domain='POINT')
colors = []
for v in body.data.vertices:
    (x, y, z) = v.co
    warm = smooth(0.15, 0.36, abs(x)) * (1 - smooth(1.35, 1.65, z))
    c = mix((0.125, 0.038, 0.014), (0.245, 0.085, 0.029), warm)
    belly = (1 - smooth(0.68, 1.12, z)) * smooth(-1.05, -0.65, y) * (1 - smooth(0.06, 0.3, y))
    c = mix(c, (0.185, 0.079, 0.031), belly * 0.45)
    muzzle = 1 - smooth(0.72, 1.0, math.sqrt(((y - 1.24) / 0.21) ** 2 + ((z - 1.54) / 0.17) ** 2))
    c = mix(c, (0.024, 0.018, 0.015), muzzle)
    leg = 1 - smooth(0.35, 0.72, z)
    c = mix(c, (0.02, 0.013, 0.01), leg)
    hoof = 1 - smooth(0.093, 0.125, z)
    c = mix(c, (0.047, 0.035, 0.025), hoof)
    eye = 1 - smooth(0.9, 1.55, math.sqrt(((y - 1.049) / 0.041) ** 2 + ((z - 1.852) / 0.029) ** 2))
    c = mix(c, (0.02, 0.012, 0.008), eye * 0.78)
    c = (*c, max(muzzle, hoof, eye * 0.8))
    col.data[v.index].color = c
    colors.append(c)
body.data.color_attributes.active_color = col
(eye_mat, n, l, p) = material('OriginalHorseEyes', 'EyeColor', 0.095)
p.inputs['IOR'].default_value = 1.38
p.inputs['Coat Weight'].default_value = 0.3
p.inputs['Coat Roughness'].default_value = 0.06
verts = []
faces = []
ec = []
for side in [-1, 1]:
    center = Vector((side * 0.088, 1.048, 1.849))
    normal = Vector((side * 0.94, 0.34, 0.04)).normalized()
    horizontal = Vector((-side * 0.34, 0.94, 0)).normalized()
    vertical = normal.cross(horizontal) * side
    rings = [0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.78, 0.88, 1.04, 1.28, 1.57, 1.9, 2.3, 2.7, math.pi]
    ring_indices = []
    segs = 48
    for (i, angle) in enumerate(rings):
        indices = []
        for j in range(1 if i in (0, len(rings) - 1) else segs):
            a = math.tau * j / segs
            u = math.sin(angle) * math.cos(a)
            v = math.sin(angle) * math.sin(a)
            w = math.cos(angle)
            indices.append(len(verts))
            verts.append(center + normal * (w * 0.03) + horizontal * (u * 0.029) + vertical * (v * 0.022))
            radius = math.sqrt(u * u + v * v)
            iris = 1 - smooth(0.69, 0.75, radius) if w > 0 else 0
            fiber = 0.8 + 0.2 * math.sin(a * 37 + radius * 22)
            c = mix((0.009, 0.007, 0.005), (0.028 * fiber, 0.01 * fiber, 0.003 * fiber), iris)
            pupil = 1 - smooth(0.88, 1.12, math.sqrt((u / 0.48) ** 2 + (v / 0.17) ** 2)) if w > 0 else 0
            c = mix(c, (0.0015, 0.0013, 0.001), pupil)
            ec.append((*c, 1))
        ring_indices.append(indices)
    for i in range(len(rings) - 1):
        for j in range(segs):
            k = (j + 1) % segs
            a = ring_indices[i]
            b = ring_indices[i + 1]
            if len(a) == 1:
                quad = (a[0], b[k], b[j])
            elif len(b) == 1:
                quad = (a[j], a[k], b[0])
            else:
                quad = (a[j], a[k], b[k], b[j])
            if side < 0:
                quad = quad[::-1]
            faces.append(quad)
mesh = bpy.data.meshes.new('FittedEyes')
mesh.from_pydata(verts, [], faces)
mesh.update()
eyes = bpy.data.objects.new('HeroHorseEyes', mesh)
bpy.context.collection.objects.link(eyes)
mesh.materials.append(eye_mat)
attr = mesh.color_attributes.new(name='EyeColor', type='FLOAT_COLOR', domain='POINT')
for (i, c) in enumerate(ec):
    attr.data[i].color = c
mesh.color_attributes.active_color = attr
bm = bmesh.new()
bm.from_mesh(mesh)
bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
bm.to_mesh(mesh)
bm.free()
mesh.update()
for poly in mesh.polygons:
    poly.use_smooth = True
group = eyes.vertex_groups.new(name='Head')
group.add(list(range(len(mesh.vertices))), 1, 'REPLACE')
mod = eyes.modifiers.new('HorseRig', 'ARMATURE')
mod.object = rig
eyes.parent = rig
rig.data.pose_position = 'POSE'
bpy.context.scene.frame_set(1)
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(out / 'HeroHorse-SurfaceStudy.blend'))
for o in bpy.context.selected_objects:
    o.select_set(False)
for o in (body, rig, eyes):
    o.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(filepath=str(out / 'HeroHorse-SurfaceStudy.fbx'), use_selection=True, object_types={'MESH', 'ARMATURE'}, axis_forward='-Z', axis_up='Y', add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False, bake_anim_step=0.25, bake_anim_simplify_factor=0, path_mode='STRIP', use_custom_props=False, colors_type='LINEAR')
mesh.calc_loop_triangles()
report = {'sourceBlendSha256': hashlib.sha256(source.read_bytes()).hexdigest(), 'bodyVertexCount': len(body.data.vertices), 'eyeVertices': len(mesh.vertices), 'eyeTriangles': len(mesh.loop_triangles), 'bodyGeometryRigAndAnimationChanged': False, 'materials': ['OriginalBayCoat', 'OriginalHorseEyes'], 'bitmapTextures': 0, 'scope': 'Offline original surface study; procedural Blender shaders require an explicit Unity material conversion before adoption.'}
report['fbxSha256'] = hashlib.sha256((out / 'HeroHorse-SurfaceStudy.fbx').read_bytes()).hexdigest()
(out / 'surface-study.json').write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps(report))
