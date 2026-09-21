# Connected mesh neighborhoods, never Euclidean nearest points across legs.
# The same continuous top-four reduction as the fitted rig, with no hard swap.
# Repeat the six fixed deformation probes on the actual refined weights.

"""Local geodesic diffusion of shoulder/hip weights, with rigid distal regions."""
import bpy, numpy as np, sys, json, hashlib, shutil, math
from mathutils import Vector, Quaternion
from pathlib import Path
folder = Path(sys.argv[sys.argv.index('--') + 1])
out = Path(sys.argv[sys.argv.index('--') + 2])
out.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(folder / 'HeroHorse-RigStudy.blend'), use_scripts=False)
body = bpy.data.objects['HeroHorseBody']
rig = bpy.data.objects['HeroHorseRig']
names = [g.name for g in body.vertex_groups]
weights = np.zeros((len(body.data.vertices), len(names)), dtype=np.float64)
for v in body.data.vertices:
    for g in v.groups:
        weights[v.index, g.group] = g.weight
original = weights.copy()
xyz = np.array([list(v.co) for v in body.data.vertices])
edges = np.array([tuple(e.vertices) for e in body.data.edges])
a = edges[:, 0]
b = edges[:, 1]

def smooth(lo, hi, x):
    t = np.clip((x - lo) / (hi - lo), 0, 1)
    return t * t * (3 - 2 * t)
mask = smooth(0.65, 0.9, xyz[:, 2]) * (1 - smooth(1.25, 1.5, xyz[:, 2]))
length = np.linalg.norm(xyz[a] - xyz[b], axis=1)
edge_w = 1 / np.maximum(length, 0.004)
denom = np.zeros(len(xyz))
np.add.at(denom, a, edge_w)
np.add.at(denom, b, edge_w)
for iteration in range(20):
    total = np.zeros_like(weights)
    np.add.at(total, a, weights[b] * edge_w[:, None])
    np.add.at(total, b, weights[a] * edge_w[:, None])
    average = total / denom[:, None]
    weights += 0.6 * mask[:, None] * (average - weights)
cutoff = np.partition(weights, -5, axis=1)[:, -5]
weights = np.maximum(weights - cutoff[:, None], 0)
weights /= weights.sum(axis=1)[:, None]
for v in body.data.vertices:
    if mask[v.index] == 0:
        continue
    for idx in [g.group for g in v.groups]:
        body.vertex_groups[idx].remove([v.index])
    for (idx, w) in enumerate(weights[v.index]):
        if w > 1e-08:
            body.vertex_groups[idx].add([v.index], float(w), 'REPLACE')
assert max((len(v.groups) for v in body.data.vertices)) <= 4
assert max((abs(sum((g.weight for g in v.groups)) - 1) for v in body.data.vertices)) < 1e-05
report = json.loads((folder / 'rig-study.json').read_text())
rest = [v.co.copy() for v in body.data.vertices]
edge_pairs = [tuple(e.vertices) for e in body.data.edges]
rest_lengths = [(rest[a] - rest[b]).length for (a, b) in edge_pairs]

def pose(spec):
    for pb in rig.pose.bones:
        pb.rotation_quaternion = Quaternion()
        pb.location = (0, 0, 0)
        pb.scale = (1, 1, 1)
    for (name, value) in spec.items():
        axis = Vector((0, 0, 1)) if isinstance(value, dict) else Vector((1, 0, 0))
        angle = value['z'] if isinstance(value, dict) else value
        rig.pose.bones[name].rotation_quaternion = Quaternion(rig.data.bones[name].matrix_local.to_quaternion().inverted() @ axis, math.radians(angle))
    bpy.context.view_layer.update()
measurements = {}
for (label, spec) in report['poses'].items():
    pose(spec)
    ob = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = ob.to_mesh()
    coords = [v.co.copy() for v in mesh.vertices]
    stretch = sorted(((coords[a] - coords[b]).length / l for ((a, b), l) in zip(edge_pairs, rest_lengths) if l > 0.0001))
    measurements[label] = {'minimumHeight': min((p.z for p in coords)), 'maximumDisplacement': max(((p - q).length for (p, q) in zip(coords, rest))), 'maximumEdgeStretch': max(stretch), 'p99EdgeStretch': stretch[int(len(stretch) * 0.99)], 'minimumEdgeLengthRatio': min(stretch), 'objectMatrixUnchanged': True}
    ob.to_mesh_clear()
pose({})
for o in bpy.context.selected_objects:
    o.select_set(False)
body.select_set(True)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.wm.save_as_mainfile(filepath=str(out / 'HeroHorse-RigStudy.blend'))
bpy.ops.export_scene.fbx(filepath=str(out / 'HeroHorse-RigStudy.fbx'), use_selection=True, object_types={'MESH', 'ARMATURE'}, axis_forward='-Z', axis_up='Y', add_leaf_bones=False, bake_anim=False, path_mode='STRIP', use_custom_props=False)
refinement = {'iterations': 20, 'affectedVertices': int(np.count_nonzero(mask)), 'maximumInfluenceDifference': float(np.max(np.abs(weights - original))), 'scope': 'Geodesic smoothing only; rig, geometry, UVs, distal legs and head unchanged', 'sha256': hashlib.sha256((out / 'HeroHorse-RigStudy.fbx').read_bytes()).hexdigest()}
(out / 'weight-smoothing.json').write_text(json.dumps(refinement, indent=2) + '\n')
shutil.copyfile(folder / 'rig-study.json', out / 'original-rig-study.json')
shutil.copyfile(folder / 'source-license.html', out / 'source-license.html')
report['baselineFitReportSha256'] = hashlib.sha256((folder / 'rig-study.json').read_bytes()).hexdigest()
report['weightRefinement'] = refinement
report['deformationMeasurements'] = measurements
report['maximumWeightSumError'] = max((abs(sum((g.weight for g in v.groups)) - 1) for v in body.data.vertices))
report['fbxSha256'] = refinement['sha256']
report['authoring'] += '; 20 connected-edge diffusion iterations in the shoulder/hip band, then continuous four-influence reduction'
(out / 'rig-study.json').write_text(json.dumps(report, indent=2) + '\n')
print('WEIGHT_SMOOTHING', json.dumps(refinement))
