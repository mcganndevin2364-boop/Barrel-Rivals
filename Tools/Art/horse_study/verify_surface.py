

"""Check actual saved face/coat geometry and animated attachment, not realism."""
import bpy, bmesh, json, sys, math
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree
folder = Path(sys.argv[sys.argv.index('--') + 1])
bpy.ops.wm.open_mainfile(filepath=str(folder / 'HeroHorse-SurfaceStudy.blend'), use_scripts=False)
body = bpy.data.objects['HeroHorseBody']
eyes = bpy.data.objects['HeroHorseEyes']
rig = bpy.data.objects['HeroHorseRig']
scene = bpy.context.scene
bm = bmesh.new()
bm.from_mesh(eyes.data)
assert all((e.is_manifold for e in bm.edges)), 'Eye globe boundary or nonmanifold edge'
assert all((f.calc_area() > 1e-12 for f in bm.faces)), 'Degenerate eye faces'
components = []
remaining = set(bm.verts)
while remaining:
    seed = remaining.pop()
    part = {seed}
    pending = [seed]
    while pending:
        v = pending.pop()
        for e in v.link_edges:
            w = e.other_vert(v)
            if w in remaining:
                remaining.remove(w)
                part.add(w)
                pending.append(w)
    components.append(part)
assert len(components) == 2
for part in components:
    center = sum((v.co for v in part), Vector()) / len(part)
    assert all(((v.co - center).dot(v.normal) > 0 for v in part)), 'Inward globe normal'
bm.free()
for (obj, attr) in [(body, 'CoatColor'), (eyes, 'EyeColor')]:
    a = obj.data.color_attributes[attr]
    assert a.domain == 'POINT' and len(a.data) == len(obj.data.vertices)
    assert all((all((math.isfinite(c) and 0 <= c <= 1 for c in v.color)) for v in a.data))
    assert len(obj.data.materials) == 1
assert all((len(v.groups) == 1 and eyes.vertex_groups[v.groups[0].group].name == 'Head' and (abs(v.groups[0].weight - 1) < 1e-07) for v in eyes.data.vertices))
rest = [v.co.copy() for v in eyes.data.vertices]
rest_head = rig.data.bones['Head'].matrix_local.inverted()
rig.data.pose_position = 'POSE'
max_error = 0
max_distance_error = 0
first = None
last = None
original_samples = []
for i in range(113):
    t = 1 + i / 4
    scene.frame_set(int(t), subframe=t - int(t))
    bpy.context.view_layer.update()
    evaluated = eyes.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = evaluated.to_mesh()
    expected = rig.pose.bones['Head'].matrix @ rest_head
    max_error = max(max_error, max(((v.co - expected @ r).length for (v, r) in zip(mesh.vertices, rest))))
    if i == 0:
        first = [v.co.copy() for v in mesh.vertices]
    if i == 112:
        last = [v.co.copy() for v in mesh.vertices]
    original_samples.append(np.array([list(v.co) for v in mesh.vertices]))
    evaluated.to_mesh_clear()
assert max_error < 1e-05
loop = max(((p - q).length for (p, q) in zip(first, last)))
assert loop < 1e-06
eyes.data.calc_loop_triangles()
body.data.calc_loop_triangles()
report = {'eyeComponents': 2, 'closedOutwardNondegenerateGlobes': True, 'sampleCount': 113, 'maximumHeadAttachmentErrorM': max_error, 'loopEndpointErrorM': loop, 'eyeVertices': len(eyes.data.vertices), 'eyeTriangles': len(eyes.data.loop_triangles), 'bodyTriangles': len(body.data.loop_triangles), 'totalTriangles': len(body.data.loop_triangles) + len(eyes.data.loop_triangles), 'materialSlots': 2, 'bodyColorMin': [min((v.color[j] for v in body.data.color_attributes['CoatColor'].data)) for j in range(3)], 'bodyColorMax': [max((v.color[j] for v in body.data.color_attributes['CoatColor'].data)) for j in range(3)], 'scope': 'Offline topology, finite reflectance and head attachment checks. Not a physiological, Unity material, photographic or phone performance acceptance.'}
original = {}
for (obj, name) in [(body, 'CoatColor'), (eyes, 'EyeColor')]:
    original[obj.name] = {'positions': [v.co.copy() for v in obj.data.vertices], 'color': [tuple(v.color) for v in obj.data.color_attributes[name].data], 'attribute': name}
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.scene.render.fps = 30
bpy.ops.import_scene.fbx(filepath=str(folder / 'HeroHorse-SurfaceStudy.fbx'), use_anim=True, anim_offset=0, colors_type='LINEAR')
rig = bpy.data.objects['HeroHorseRig']
rig.data.pose_position = 'REST'
bpy.context.view_layer.update()
max_color = 0
max_rest = 0
maps = {}
for (name, spec) in original.items():
    obj = bpy.data.objects[name]
    tree = KDTree(len(spec['positions']))
    for (i, p) in enumerate(spec['positions']):
        tree.insert(p, i)
    tree.balance()
    mapping = []
    for v in obj.data.vertices:
        (_, i, d) = tree.find(obj.matrix_world @ v.co)
        mapping.append(i)
        max_rest = max(max_rest, d)
    assert len(set(mapping)) == len(spec['positions']) == len(mapping)
    maps[name] = np.array(mapping)
    a = obj.data.color_attributes[spec['attribute']]
    for loop in obj.data.loops:
        color = a.data[loop.index if a.domain == 'CORNER' else loop.vertex_index].color
        max_color = max(max_color, max((abs(x - y) for (x, y) in zip(color, spec['color'][mapping[loop.vertex_index]]))))
assert max_rest < 1e-05 and max_color < 1e-05, (max_rest, max_color)
rig.data.pose_position = 'POSE'
scene = bpy.context.scene
max_export = 0
eyes = bpy.data.objects['HeroHorseEyes']
assert all((len(v.groups) == 1 and eyes.vertex_groups[v.groups[0].group].name == 'Head' and (abs(v.groups[0].weight - 1) < 1e-06) for v in eyes.data.vertices))
for (i, expected) in enumerate(original_samples):
    t = 1 + i / 4
    scene.frame_set(int(t), subframe=t - int(t))
    bpy.context.view_layer.update()
    ev = eyes.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = ev.to_mesh()
    points = np.array([list(eyes.matrix_world @ v.co) for v in mesh.vertices])
    ev.to_mesh_clear()
    max_export = max(max_export, float(np.max(np.linalg.norm(points - expected[maps['HeroHorseEyes']], axis=1))))
assert max_export < 1e-05, max_export
report['fbx'] = {'linearVertexColorMaximumError': max_color, 'maximumNeutralPositionErrorM': max_rest, 'eyeMaximumAnimatedPositionErrorM': max_export, 'eyeHeadWeightsPreserved': True, 'sampleCount': len(original_samples), 'shaderGraphsTransferred': False}
(folder / 'surface-verification.json').write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps(report))
