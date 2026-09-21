"""Surface-fitted original mane/forelock/tail for the 34-bone horse candidate.
Reuses the project's original atlas PNGs unchanged. No bitmap generation/editing.
The existing body, eye materials and original bone animation remain untouched.
"""
import bpy, sys, math, json, random, hashlib
from pathlib import Path
from mathutils import Vector, Quaternion
from mathutils.bvhtree import BVHTree
args = sys.argv[sys.argv.index('--') + 1:]
source = Path(args[0])
out = Path(args[1])
repo = Path(args[2])
out.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source), use_scripts=False)
body = bpy.data.objects['HeroHorseBody']
rig = bpy.data.objects['HeroHorseRig']
scene = bpy.context.scene
rig.data.pose_position = 'REST'
bpy.context.view_layer.update()
body.data.calc_loop_triangles()
triangles = [tuple(t.vertices) for t in body.data.loop_triangles]
positions = [v.co.copy() for v in body.data.vertices]
tree = BVHTree.FromPolygons(positions, triangles, all_triangles=True)
weights = [{body.vertex_groups[g.group].name: g.weight for g in v.groups} for v in body.data.vertices]
randomizer = random.Random(617309)

def rand(a, b):
    return randomizer.uniform(a, b)

def smooth(x):
    x = max(0, min(1, x))
    return x * x * (3 - 2 * x)

def skin(face, point):
    (ia, ib, ic) = triangles[face]
    (a, b, c) = [positions[i] for i in (ia, ib, ic)]
    (e, f, g) = (b - a, c - a, point - a)
    ee = e.dot(e)
    ef = e.dot(f)
    ff = f.dot(f)
    den = ee * ff - ef * ef
    u = (ff * g.dot(e) - ef * g.dot(f)) / den
    v = (ee * g.dot(f) - ef * g.dot(e)) / den
    bc = (1 - u - v, u, v)
    values = {}
    for (i, amount) in zip((ia, ib, ic), bc):
        for (name, w) in weights[i].items():
            values[name] = values.get(name, 0) + w * amount
    pairs = sorted(((n, w) for (n, w) in values.items() if w > 1e-07), key=lambda p: p[1], reverse=True)[:4]
    total = sum((w for (_, w) in pairs))
    return ({n: w / total for (n, w) in pairs}, list(bc))

def ray(origin, direction):
    (point, normal, face, d) = tree.ray_cast(Vector(origin), Vector(direction))
    assert point is not None, (origin, direction)
    (w, bc) = skin(face, point)
    return (point, normal, w, face, bc)

def top(x, y):
    return ray((x, y, 3), (0, 0, -1))

def side(y, z):
    return ray((2, y, z), (-1, 0, 0))
# Helper bones affect only the groom; original body and eye weights remain untouched.
helpers = []
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
for index in range(4):
    (p, n, w, face, bc) = top(0, 0.14 + index * 0.2)
    name = 'GroomMane.' + str(index)
    bone = rig.data.edit_bones.new(name)
    bone.head = p
    bone.tail = p + Vector((0, 0, -0.15))
    bone.parent = rig.data.edit_bones[max(w, key=w.get)]
    bone.use_deform = True
    bone.align_roll(Vector((1, 0, 0)))
    helpers.append({'name': name, 'parent': bone.parent.name, 'head': list(p), 'axis': (0, 1, 0), 'phase': index * 0.55, 'kind': 'mane'})
(tail_p, tail_n, tail_w, tail_face, tail_bc) = ray((0, -3, 1.37), (0, 1, 0))
name = 'GroomTail'
bone = rig.data.edit_bones.new(name)
bone.head = tail_p
bone.tail = tail_p + Vector((0, 0, -0.22))
bone.parent = rig.data.edit_bones['TailTip']
bone.use_deform = True
bone.align_roll(Vector((1, 0, 0)))
helpers.append({'name': name, 'parent': 'TailTip', 'head': list(tail_p), 'axis': (0, 1, 0), 'phase': 0.5, 'kind': 'tail'})
bpy.ops.object.mode_set(mode='OBJECT')
materials = []
atlas_rows = []
for (label, filename, expected, tint) in [('Dense', 'Original natural strand atlas.png', '76d95b131e996f01fac36ea5a8b6df0dd0ac04184944116e29c3716650158a21', (0.24, 0.22, 0.2, 1)), ('Outer', 'Original separated strand atlas.png', '7e9ffd61284340429ca8fae65ab6cde07ac515953a4f921fd2fd7efa69e1f236', (0.12, 0.11, 0.1, 1))]:
    path = repo / 'Assets/_Project/Art/Reins/Hair' / filename
    assert hashlib.sha256(path.read_bytes()).hexdigest() == expected
    image = bpy.data.images.load(str(path), check_existing=True)
    image.pack()
    m = bpy.data.materials.new('FittedGroom' + label)
    m.use_nodes = True
    n = m.node_tree.nodes
    l = m.node_tree.links
    n.clear()
    output = n.new('ShaderNodeOutputMaterial')
    p = n.new('ShaderNodeBsdfPrincipled')
    p.inputs['Roughness'].default_value = 0.55
    p.inputs['Specular IOR Level'].default_value = 0.08
    p.inputs['Anisotropic'].default_value = 0.2
    p.inputs['Sheen Weight'].default_value = 0.02
    p.inputs['Specular Tint'].default_value = (0.5, 0.36, 0.24, 1)
    p.inputs['Sheen Tint'].default_value = (0.25, 0.15, 0.1, 1)
    tex = n.new('ShaderNodeTexImage')
    tex.image = image
    tex.extension = 'CLIP'
    uv = n.new('ShaderNodeTexCoord')
    l.new(uv.outputs['UV'], tex.inputs['Vector'])
    color = n.new('ShaderNodeMixRGB')
    color.blend_type = 'MULTIPLY'
    color.inputs[0].default_value = 1
    color.inputs[2].default_value = tint
    l.new(tex.outputs['Color'], color.inputs[1])
    l.new(color.outputs[0], p.inputs['Base Color'])
    bump = n.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value = 0.3
    bump.inputs['Distance'].default_value = 0.001
    l.new(tex.outputs['Color'], bump.inputs['Height'])
    l.new(bump.outputs['Normal'], p.inputs['Normal'])
    tangent = n.new('ShaderNodeTangent')
    tangent.direction_type = 'UV_MAP'
    tangent.uv_map = 'UVMap'
    geometry = n.new('ShaderNodeNewGeometry')
    cross = n.new('ShaderNodeVectorMath')
    cross.operation = 'CROSS_PRODUCT'
    l.new(geometry.outputs['Normal'], cross.inputs[0])
    l.new(tangent.outputs[0], cross.inputs[1])
    l.new(cross.outputs[0], p.inputs['Tangent'])
    # Atlas alpha is coverage; it remains separate from the dark strand tint.
    cutoff = n.new('ShaderNodeMath')
    cutoff.operation = 'GREATER_THAN'
    cutoff.inputs[1].default_value = 0.36
    l.new(tex.outputs['Alpha'], cutoff.inputs[0])
    transparent = n.new('ShaderNodeBsdfTransparent')
    mix = n.new('ShaderNodeMixShader')
    l.new(cutoff.outputs[0], mix.inputs[0])
    l.new(transparent.outputs[0], mix.inputs[1])
    l.new(p.outputs[0], mix.inputs[2])
    l.new(mix.outputs[0], output.inputs['Surface'])
    materials.append(m)
    atlas_rows.append({'file': str(path.relative_to(repo)), 'sha256': expected, 'bitmapEdited': False, 'packedInBlend': True})
vertices = []
faces = []
uvs = []
vertex_weights = []
face_materials = []
cards = []
attachments = []

def blend_helper(w, name, amount):
    result = {n: v * (1 - amount) for (n, v) in w.items()}
    result[name] = result.get(name, 0) + amount
    values = sorted(((n, v) for (n, v) in result.items() if v > 1e-07), key=lambda p: p[1], reverse=True)[:4]
    total = sum((v for (n, v) in values))
    return {n: v / total for (n, v) in values}

def card(kind, fn, rows, columns, bundle, material, helper=None):
    start = len(vertices)
    root_ids = []
    # Dense sampling around the crest/dock prevents flat faces cutting curved skin.
    times = [0, 0.04, 0.08, 0.125, 0.17, 0.21, 0.25, 0.375, 0.5, 0.625, 0.75, 0.875, 1] if kind == 'mane' else [0, 0.04, 0.08, 0.12, 0.16, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1] if kind == 'tail' else [i / rows for i in range(rows + 1)]
    rows = len(times) - 1
    for i in range(rows + 1):
        t = times[i]
        for j in range(columns + 1):
            q = j / columns - 0.5
            (point, w, attachment) = fn(t, q)
            if helper and i > 0:
                w = blend_helper(w, helper, (0.35 if kind == 'mane' else 0.88) * smooth((t - 0.12) / 0.88))
            index = len(vertices)
            vertices.append(point)
            vertex_weights.append(w)
            uvs.append(((bundle + 0.03 + 0.94 * j / columns) / 8, 0.969 - 0.939 * t))
            if i == 0:
                root_ids.append(index)
                if attachment:
                    attachments.append({'vertex': index, 'face': attachment[0], 'barycentric': attachment[1], 'normalOffset': attachment[2], 'kind': kind})
    for i in range(rows):
        for j in range(columns):
            a = start + i * (columns + 1) + j
            b = a + columns + 1
            faces.append((a, a + 1, b + 1, b))
            face_materials.append(material)
    cards.append({'kind': kind, 'startVertex': start, 'vertexCount': (rows + 1) * (columns + 1), 'rootVertices': root_ids, 'rows': rows, 'times': times, 'columns': columns, 'atlasCell': bundle, 'material': material, 'helper': helper})
for (layer, count) in [(0, 24), (1, 30)]:
    for i in range(count):
        f = (i + 0.5 + rand(-0.25, 0.25)) / count
        root_y = 0.04 + f * 0.86
        root_x = -0.047 + 0.016 * math.sin(f * 12) if layer == 0 else 0.002 + 0.009 * math.sin(f * 9 + 1)
        length = rand(0.24, 0.32) if layer == 0 else rand(0.3, 0.4)
        length *= 1 - 0.18 * f
        width = rand(0.082, 0.108) if layer == 0 else rand(0.061, 0.086)
        sweep = -length * (0.16 + 0.16 * (0.5 + 0.5 * math.sin(f * 8)))
        wave = rand(0, math.tau)
        loose = 0.004 if layer == 0 else rand(0.009, 0.02)

        def fitted(t, q, root_y=root_y, root_x=root_x, length=length, width=width, sweep=sweep, wave=wave, loose=loose):
            y = root_y + sweep * smooth(t) + q * width * (1 - 0.55 * t * t) + 0.009 * math.sin(t * math.pi) * math.sin(wave)
            if t <= 0.25:
                x = root_x + (0.07 - root_x) * t / 0.25
                (p, n, w, face, bc) = top(x, y)
            else:
                crest = top(0.07, y)[0]
                drop = length * (t - 0.25) / 0.75
                (p, n, w, face, bc) = side(y, crest.z - drop)
            offset = 0.0025 + 0.0035 * smooth(t / 0.15) + loose * math.sin(t * math.pi) + 0.003 * (1 - 4 * q * q) * math.sin(t * math.pi) + 0.008 * smooth((t - 0.4) / 0.6)
            return (p + n * offset, w, (face, bc, offset))
        card('mane', fitted, 8, 2, randomizer.randrange(8), layer, 'GroomMane.' + str(min(3, int(f * 4))))
for i in range(12):
    root_x = rand(-0.027, 0.027)
    root_y = rand(0.91, 0.96)
    end_y = rand(1.095, 1.145)
    width = rand(0.026, 0.041)
    sweep = rand(-0.018, 0.018)

    def fitted(t, q, root_x=root_x, root_y=root_y, end_y=end_y, width=width, sweep=sweep):
        x = root_x + sweep * smooth(t) + q * width * (1 - 0.7 * t * t)
        y = root_y + (end_y - root_y) * t
        (p, n, w, face, bc) = top(x, y)
        offset = 0.007 + 0.01 * math.sin(t * math.pi)
        return (p + n * offset, w, (face, bc, offset))
    card('forelock', fitted, 6, 2, i % 8, 0 if i < 5 else 1)
for ring in range(3):
    for i in range(12):
        angle = math.tau * (i / 12 + ring * 0.031)
        radial = Vector((math.cos(angle), math.sin(angle), 0))
        across = Vector((-math.sin(angle), math.cos(angle), 0))
        radius = 0.012 + ring * 0.011
        root = tail_p + Vector((0, -0.012, 0)) + radial * radius
        root_hit = ray((root.x, -3, root.z), (0, 1, 0))
        root = root_hit[0] + root_hit[1] * 0.007
        end = Vector((rand(-0.025, 0.025), tail_p.y - 0.18, rand(0.2, 0.34))) + radial * rand(0.035, 0.065)
        width = rand(0.049, 0.072)

        def fitted(t, q, root=root, root_hit=root_hit, end=end, width=width, across=across, angle=angle):
            if t == 0:
                hit = ray((root.x + (1 if across.x >= 0 else -1) * q * width * 0.32, -3, root.z), (0, 1, 0))
                return (hit[0] + hit[1] * 0.007, hit[2], (hit[3], hit[4], 0.007))
            p = root.lerp(end, t) + Vector((0.012 * math.sin(t * math.tau + angle) * math.sin(t * math.pi), -0.07 * math.sin(t * math.pi), 0.05 * math.sin(t * math.pi)))
            # Start transverse to the fitting ray, then fan out. A depth-facing root
            # would project all width vertices to one point and collapse the face.
            surface_across = Vector((1 if across.x >= 0 else -1, 0, 0)).lerp(across, smooth(t / 0.4)).normalized()
            p += surface_across * q * width * (0.32 + 0.9 * math.sin(t * math.pi * 0.77))
            w = dict(root_hit[2])
            if t <= 0.32 and p.z > 1.12:
                hit = ray((p.x, -3, p.z), (0, 1, 0))
                clearance = 0.009 + 0.023 * smooth(t / 0.32)
                if (p - hit[0]).dot(hit[1]) < clearance:
                    p = hit[0] + hit[1] * clearance
                w = hit[2]
            return (p, w, None)
        card('tail', fitted, 10, 4, randomizer.randrange(8), 0 if ring == 0 else 1, 'GroomTail')
mesh = bpy.data.meshes.new('FittedManeForelockTail')
mesh.from_pydata(vertices, [], faces)
mesh.update()
uv = mesh.uv_layers.new(name='UVMap')
for loop in mesh.loops:
    uv.data[loop.index].uv = uvs[loop.vertex_index]
for (p, material) in zip(mesh.polygons, face_materials):
    p.use_smooth = True
    p.material_index = material
hair = bpy.data.objects.new('HeroHorseGroom', mesh)
bpy.context.collection.objects.link(hair)
for m in materials:
    mesh.materials.append(m)
for name in sorted({n for w in vertex_weights for n in w}):
    hair.vertex_groups.new(name=name)
for (i, w) in enumerate(vertex_weights):
    for (name, value) in w.items():
        hair.vertex_groups[name].add([i], value, 'REPLACE')
hair.parent = rig
modifier = hair.modifiers.new('HorseRig', 'ARMATURE')
modifier.object = rig
modifier.use_deform_preserve_volume = False
rig.data.pose_position = 'POSE'
for h in helpers:
    pb = rig.pose.bones[h['name']]
    pb.rotation_mode = 'QUATERNION'
    axis = rig.data.bones[h['name']].matrix_local.to_quaternion().inverted() @ Vector(h['axis'])
    for i in range(113):
        frame = 1 + i / 4
        phase = (frame - 1) / 28 * math.tau
        angle = -(1 + math.sin(phase + h['phase'])) * 1.25 if h['kind'] == 'mane' else math.sin(phase + h['phase']) * 4
        pb.rotation_quaternion = Quaternion(axis, math.radians(angle))
        pb.keyframe_insert(data_path='rotation_quaternion', frame=frame)
action = rig.animation_data.action
curves = list(action.fcurves) if action.is_action_legacy else [f for layer in action.layers for strip in layer.strips for bag in strip.channelbags for f in bag.fcurves]
for c in curves:
    if 'Groom' in c.data_path:
        for k in c.keyframe_points:
            k.interpolation = 'LINEAR'
scene.frame_set(1)
bpy.context.view_layer.update()
mesh.calc_loop_triangles()
bpy.ops.wm.save_as_mainfile(filepath=str(out / 'HeroHorse-GroomStudy.blend'))
for o in bpy.context.selected_objects:
    o.select_set(False)
for o in (body, rig, hair, bpy.data.objects['HeroHorseEyes']):
    o.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(filepath=str(out / 'HeroHorse-GroomStudy.fbx'), use_selection=True, object_types={'MESH', 'ARMATURE'}, axis_forward='-Z', axis_up='Y', add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False, bake_anim_step=0.25, bake_anim_simplify_factor=0, path_mode='STRIP', use_custom_props=False, colors_type='LINEAR')
report = {'scope': 'Offline surface-fitted groom; no Unity/device or reference-quality acceptance.', 'sourceBlendSha256': hashlib.sha256(source.read_bytes()).hexdigest(), 'hairVertices': len(mesh.vertices), 'hairTriangles': len(mesh.loop_triangles), 'hairMaterials': 2, 'bones': len(rig.data.bones), 'helpers': helpers, 'atlases': atlas_rows, 'cards': cards, 'attachments': attachments, 'alphaCutoff': 0.36, 'packedImages': True, 'fbxSha256': hashlib.sha256((out / 'HeroHorse-GroomStudy.fbx').read_bytes()).hexdigest()}
(out / 'groom-study.json').write_text(json.dumps(report, indent=2) + '\n')
print('GROOM_AUTHORED', len(cards), len(mesh.vertices), len(mesh.loop_triangles))
