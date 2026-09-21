# Authoring notes
# FBX import normally adds one Blender frame. Explicit anim_offset=0 below
# aligns the exported 1..33 range; never mask a mismatch by relaxing tolerances.
# Both directions cover all distinct UV corners at a potentially split seam.
# Independent best rigid transform, rather than assuming the authored quaternion.

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
spec = json.loads((folder / 'walk-study.json').read_text())
cycle_frames = spec['frames']
speed = spec['authoredSpeedMps']
duration = cycle_frames / 30
duty = spec['dutyFraction']
roll_start = spec['rollStart']
assert spec['contactModel'] == 'toe-roll-v2'
frames = [1 + i / 8 for i in range(cycle_frames * 8 + 1)]

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
flat_indices = {}
toe_indices = {}
for name in ('ForeHoof.L', 'ForeHoof.R', 'HindHoof.L', 'HindHoof.R'):
    sole[name] = [i for (i, w) in enumerate(original_weights) if w.get(name, 0) > 0.99999]
    low = min((neutral[i, 2] for i in sole[name]))
    flat_indices[name] = [i for i in sole[name] if neutral[i, 2] < low + 0.008]
    toe_indices[name] = max(flat_indices[name], key=lambda i: neutral[i, 1])
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
rigidity_error = 0
flat_normal_error = 0
maximum_pitch = {n: 0.0 for n in sole}
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
        source_center = reference.mean(axis=0)
        dest_center = selected.mean(axis=0)
        (u, _, vt) = np.linalg.svd((reference - source_center).T @ (selected - dest_center))
        rotation_matrix = vt.T @ u.T
        assert np.linalg.det(rotation_matrix) > 0.999
        residual = selected - ((reference - source_center) @ rotation_matrix.T + dest_center)
        rigidity_error = max(rigidity_error, float(np.max(np.linalg.norm(residual, axis=1))))
        offset = {'HindHoof.L': 0, 'ForeHoof.L': 0.25, 'HindHoof.R': 0.5, 'ForeHoof.R': 0.75}[name]
        cycle = ((frame - 1) / cycle_frames - offset) % 1
        normal = rotation_matrix @ np.array([0.0, 0.0, 1.0])
        angle = math.acos(float(np.clip(normal[2], -1, 1)))
        maximum_pitch[name] = max(maximum_pitch[name], math.degrees(angle))
        if cycle < roll_start:
            flat_normal_error = max(flat_normal_error, float(np.linalg.norm(normal - [0.0, 0.0, 1.0])))
        anchor_indices = [toe_indices[name]] if roll_start <= cycle < duty else flat_indices[name]
        anchor = actual[[inverse[j] for j in anchor_indices]].mean(axis=0)
        rest_anchor = neutral[anchor_indices].mean(axis=0)
        foot_rows[name].append({'frame': frame, 'minimumHeightM': float(np.min(selected[:, 2])), 'anchorY': float(anchor[1]), 'restAnchorY': float(rest_anchor[1]), 'cycle': cycle, 'contact': cycle < duty, 'pitchDegrees': math.degrees(angle)})
print('ANIMATION_ERROR', maximum_error, byframe[:4], max(byframe, key=lambda a: a['maximumSkinnedPositionErrorM']), 'minHeight', minimum_height, 'rigidity', rigidity_error)
assert maximum_error < 0.0001, maximum_error
assert minimum_height > -0.001, minimum_height
assert rigidity_error < 0.0001, rigidity_error
assert flat_normal_error < 0.001, flat_normal_error
assert all((15 < v < 35 for v in maximum_pitch.values())), maximum_pitch
assert bone_scale_error < 1e-05
assert max((abs(v) for p in root_positions for v in p)) < 1e-05
scene.frame_set(1)
first = eval_points(body)
scene.frame_set(cycle_frames + 1)
last = eval_points(body)
loop_error = float(np.max(np.linalg.norm(first - last, axis=1)))
assert loop_error < 1e-05
stance_residual = {}
for (name, points) in foot_rows.items():
    offset = {'HindHoof.L': 0, 'ForeHoof.L': 0.25, 'HindHoof.R': 0.5, 'ForeHoof.R': 0.75}[name]
    positions = []
    for point in points:
        cycle = point['cycle']
        if cycle < duty:
            positions.append(point['anchorY'] - point['restAnchorY'] + speed * duration * cycle)
    stance_residual[name] = max(positions) - min(positions)
assert max(stance_residual.values()) < 0.0002, stance_residual
report = {'stanceResidualTravelM': stance_residual, 'scope': 'Independent FBX roundtrip, including half-step times between authored keys. Offline geometry proof, not Unity, biomechanical or device acceptance.', 'fbxSha256': hashlib.sha256((folder / 'HeroHorse-WalkStudy.fbx').read_bytes()).hexdigest(), 'blender': bpy.app.version_string, 'sampleCount': len(frames), 'vertices': len(imported), 'triangles': original_triangles, 'bones': len(imported_bones), 'maximumNeutralPositionErrorM': max(distances), 'vertexMapping': 'bijective nearest rest-position map; original order not assumed', 'maximumWeightDifference': max_weight_error, 'maximumUvCornerDifference': max_uv_error, 'maximumCornerNormalVectorDifference': max_normal_error, 'maximumInfluences': max((len(w) for w in iw)), 'minimumWeightSum': min((sum(w.values()) for w in iw)), 'maximumSkinnedPositionErrorM': maximum_error, 'minimumAnimatedHeightM': minimum_height, 'maximumRigidShapeErrorM': rigidity_error, 'maximumFlatNormalVectorError': flat_normal_error, 'maximumHoofPitchDegrees': maximum_pitch, 'maximumBoneScaleError': bone_scale_error, 'loopEndpointErrorM': loop_error, 'objectRootStationary': True, 'frames': byframe, 'feet': foot_rows}
(folder / 'roundtrip-verification.json').write_text(json.dumps(report, indent=2) + '\n')
print('ROUNDTRIP_VERIFIED', json.dumps({k: v for (k, v) in report.items() if k not in ('frames', 'feet')}))
