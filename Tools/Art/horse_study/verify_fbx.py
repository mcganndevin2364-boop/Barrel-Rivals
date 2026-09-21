# Authoring notes
# FBX import normally adds one Blender frame. Explicit anim_offset=0 below
# aligns the exported 1..33 range; never mask a mismatch by relaxing tolerances.
# Both directions cover all distinct UV corners at a potentially split seam.

"""Independent saved Blender -> FBX -> Blender geometry/motion check.
Imports data only, with automatic script execution disabled by the caller.
Compares nearest neutral positions, skin groups, UV sets and skinned positions
at authored and between-key times. Does not assume FBX vertex order is stable.
"""
import bpy, sys, json, hashlib, math
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree
folder = Path(sys.argv[sys.argv.index('--') + 1])
bpy.ops.wm.open_mainfile(filepath=str(folder / 'HeroHorse-WalkStudy.blend'), use_scripts=False)
scene = bpy.context.scene
scene.render.fps = 30
arm = bpy.data.objects['HeroHorseRig']
body = bpy.data.objects['HeroHorseBody']
frames = [1 + i / 8 for i in range(257)]

def eval_points(obj):
    ev = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = ev.to_mesh()
    a = np.array([list(obj.matrix_world @ v.co) for v in mesh.vertices], dtype=np.float32)
    ev.to_mesh_clear()
    return a

def uv_sets(obj):
    result = [set() for v in obj.data.vertices]
    uv = obj.data.uv_layers.active
    assert uv
    for loop in obj.data.loops:
        result[loop.vertex_index].add(tuple(uv.data[loop.index].uv))
    return result

def normal_sets(obj):
    result = [[] for v in obj.data.vertices]
    matrix = obj.matrix_world.to_3x3().inverted().transposed()
    for loop in obj.data.loops:
        result[loop.vertex_index].append((matrix @ obj.data.corner_normals[loop.index].vector).normalized())
    return result

def weights(obj):
    return [{obj.vertex_groups[g.group].name: g.weight for g in v.groups if g.weight > 1e-07} for v in obj.data.vertices]
print('ORIGINAL_ACTION', arm.animation_data.action.name, list(arm.animation_data.action.frame_range))
arm.data.pose_position = 'REST'
bpy.context.view_layer.update()
neutral = eval_points(body)
original_weights = weights(body)
original_uvs = uv_sets(body)
original_normals = normal_sets(body)
original_bones = {b.name: b.parent.name if b.parent else None for b in arm.data.bones}
original_triangles = sum((len(p.vertices) - 2 for p in body.data.polygons))
sole = {}
for name in ('ForeHoof.L', 'ForeHoof.R', 'HindHoof.L', 'HindHoof.R'):
    sole[name] = [i for (i, w) in enumerate(original_weights) if w.get(name, 0) > 0.99999]
arm.data.pose_position = 'POSE'
bpy.context.view_layer.update()
original_samples = []
for frame in frames:
    scene.frame_set(int(frame), subframe=frame % 1)
    original_samples.append(eval_points(body))
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = 30
bpy.ops.import_scene.fbx(filepath=str(folder / 'HeroHorse-WalkStudy.fbx'), use_anim=True, anim_offset=0)
arm = next((o for o in bpy.data.objects if o.type == 'ARMATURE'))
body = next((o for o in bpy.data.objects if o.type == 'MESH'))
assert arm.animation_data and arm.animation_data.action
print('IMPORTED_ACTION', arm.animation_data.action.name, list(arm.animation_data.action.frame_range))
imported_bones = {b.name: b.parent.name if b.parent else None for b in arm.data.bones}
assert original_bones == imported_bones
arm.data.pose_position = 'REST'
bpy.context.view_layer.update()
imported = eval_points(body)
tree = KDTree(len(neutral))
for (i, v) in enumerate(neutral):
    tree.insert(Vector(v), i)
tree.balance()
mapping = []
distances = []
for v in imported:
    (_, index, d) = tree.find(Vector(v))
    mapping.append(index)
    distances.append(d)
assert max(distances) < 1e-05
assert len(set(mapping)) == len(neutral) == len(imported), 'Expected bijective nearest-position mapping for this specific export'
maparray = np.array(mapping)
iw = weights(body)
iu = uv_sets(body)
max_weight_error = 0
max_uv_error = 0
max_normal_error = 0
imported_normals = normal_sets(body)
for (i, index) in enumerate(mapping):
    (w, v) = (iw[i], original_weights[index])
    max_weight_error = max(max_weight_error, max((abs(w.get(n, 0) - v.get(n, 0)) for n in set(w) | set(v))))
    (a, b) = (iu[i], original_uvs[index])
    for (aa, bb) in ((imported_normals[i], original_normals[index]), (original_normals[index], imported_normals[i])):
        for n in aa:
            max_normal_error = max(max_normal_error, min(((n - v).length for v in bb)))
    for (aa, bb) in ((a, b), (b, a)):
        for u in aa:
            max_uv_error = max(max_uv_error, min((math.dist(u, t) for t in bb)))
assert max_weight_error < 1e-05 and max_uv_error < 1e-05
assert max_normal_error < 0.002, max_normal_error
assert max((len(w) for w in iw)) <= 4 and min((sum(w.values()) for w in iw)) > 0.99999
assert sum((len(p.vertices) - 2 for p in body.data.polygons)) == original_triangles
arm.data.pose_position = 'POSE'
bpy.context.view_layer.update()
maximum_error = 0
minimum_height = 1000000000.0
byframe = []
plane_error = 0
root_positions = []
bone_scale_error = 0
foot_rows = {n: [] for n in sole}
inverse = {j: i for (i, j) in enumerate(mapping)}
for (frame, expected) in zip(frames, original_samples):
    scene.frame_set(int(frame), subframe=frame % 1)
    actual = eval_points(body)
    errors = np.linalg.norm(actual - expected[maparray], axis=1)
    m = float(np.max(errors))
    maximum_error = max(maximum_error, m)
    minimum_height = min(minimum_height, float(np.min(actual[:, 2])))
    byframe.append({'frame': frame, 'maximumSkinnedPositionErrorM': m})
    root_positions.append(list(arm.matrix_world.translation))
    bone_scale_error = max(bone_scale_error, max((abs(c - 1) for b in arm.pose.bones for c in b.scale)))
    for (name, indices) in sole.items():
        selected = actual[[inverse[j] for j in indices]]
        reference = neutral[indices]
        height_delta = selected[:, 2] - reference[:, 2]
        plane_error = max(plane_error, float(np.ptp(height_delta)))
        foot_rows[name].append({'frame': frame, 'minimumHeightM': float(np.min(selected[:, 2])), 'centerY': float(np.mean(selected[:, 1]))})
print('ANIMATION_ERROR', maximum_error, byframe[:4], max(byframe, key=lambda a: a['maximumSkinnedPositionErrorM']), 'minHeight', minimum_height, 'plane', plane_error)
assert maximum_error < 0.0001, maximum_error
assert minimum_height > -0.001, minimum_height
assert plane_error < 0.0001, plane_error
assert bone_scale_error < 1e-05
assert max((abs(v) for p in root_positions for v in p)) < 1e-05
scene.frame_set(1)
first = eval_points(body)
scene.frame_set(33)
last = eval_points(body)
loop_error = float(np.max(np.linalg.norm(first - last, axis=1)))
assert loop_error < 1e-05
stance_residual = {}
for (name, points) in foot_rows.items():
    offset = {'HindHoof.L': 0, 'ForeHoof.L': 0.25, 'HindHoof.R': 0.5, 'ForeHoof.R': 0.75}[name]
    positions = []
    for point in points:
        cycle = ((point['frame'] - 1) / 32 - offset) % 1
        if cycle < 0.62:
            positions.append(point['centerY'] + 1.5 * (32 / 30) * cycle)
    stance_residual[name] = max(positions) - min(positions)
assert max(stance_residual.values()) < 0.0002, stance_residual
report = {'stanceResidualTravelM': stance_residual, 'scope': 'Independent FBX roundtrip, including half-step times between authored keys. Offline geometry proof, not Unity, biomechanical or device acceptance.', 'fbxSha256': hashlib.sha256((folder / 'HeroHorse-WalkStudy.fbx').read_bytes()).hexdigest(), 'blender': bpy.app.version_string, 'sampleCount': len(frames), 'vertices': len(imported), 'triangles': original_triangles, 'bones': len(imported_bones), 'maximumNeutralPositionErrorM': max(distances), 'vertexMapping': 'bijective nearest rest-position map; original order not assumed', 'maximumWeightDifference': max_weight_error, 'maximumUvCornerDifference': max_uv_error, 'maximumCornerNormalVectorDifference': max_normal_error, 'maximumInfluences': max((len(w) for w in iw)), 'minimumWeightSum': min((sum(w.values()) for w in iw)), 'maximumSkinnedPositionErrorM': maximum_error, 'minimumAnimatedHeightM': minimum_height, 'maximumRigidSolePlaneErrorM': plane_error, 'maximumBoneScaleError': bone_scale_error, 'loopEndpointErrorM': loop_error, 'objectRootStationary': True, 'frames': byframe, 'feet': foot_rows}
(folder / 'roundtrip-verification.json').write_text(json.dumps(report, indent=2) + '\n')
print('ROUNDTRIP_VERIFIED', json.dumps({k: v for (k, v) in report.items() if k not in ('frames', 'feet')}))
