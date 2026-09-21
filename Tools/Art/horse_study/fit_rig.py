# Authoring notes
# Preserve heat weights where they work, then apply continuous anatomical masks.
# Midline skin belongs to the trunk, not two independently moving legs.
# Continuous side separation, with midline support rising into the trunk.
# Separate hooves permit later foot-plane control. Use a smooth coronet blend.
# Subtract the fifth influence before truncation: a group enters/leaves with
# zero weight, avoiding a discontinuity when equally weighted groups swap.
# Deliberate deformation probes, not claimed walk/gallop contact animation.

"""Original isolated quadruped rig study for b2przemo's CC-BY-3.0 horse.
This is a deformation prototype, not an accepted game asset or production gait.
Run Blender 4.5 with --factory-startup --disable-autoexec; output path after --.
"""
import bpy, sys, json, math, hashlib, shutil, time
from pathlib import Path
from mathutils import Vector, Quaternion, Matrix
base = Path(__file__).resolve().parents[3]
out = Path(sys.argv[sys.argv.index('--') + 1])
out.mkdir(parents=True, exist_ok=True)
source = base / 'ArtSource/HorseStudy/Source/StaticHorse39k.fbx'
assert hashlib.sha256(source.read_bytes()).hexdigest() == '3f4cd2e4a77dfa6a340602489037dcc8da7f3a82c441d9fdeb7d2847f4142e89'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(source), use_anim=False)
body = max((o for o in bpy.data.objects if o.type == 'MESH'), key=lambda o: len(o.data.vertices))
body.name = 'HeroHorseBody'
world = body.matrix_world.copy()
for v in body.data.vertices:
    v.co = world @ v.co
body.parent = None
body.matrix_world = Matrix.Identity(4)
for obj in list(bpy.data.objects):
    if obj != body:
        bpy.data.objects.remove(obj, do_unlink=True)
body.data.materials.clear()
body.vertex_groups.clear()
for m in list(body.modifiers):
    body.modifiers.remove(m)
rig_data = bpy.data.armatures.new('HeroHorseRig')
rig = bpy.data.objects.new('HeroHorseRig', rig_data)
bpy.context.collection.objects.link(rig)
rig.show_in_front = True
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
body.select_set(False)
bpy.ops.object.mode_set(mode='EDIT')
rows = []

def bone(name, head, tail, parent=None, deform=True):
    b = rig_data.edit_bones.new(name)
    b.head = head
    b.tail = tail
    b.use_deform = deform
    if parent:
        b.parent = rig_data.edit_bones[parent]
        b.use_connect = (b.head - b.parent.tail).length < 1e-05
    b.align_roll(Vector((1, 0, 0)))
    rows.append({'name': name, 'head': list(head), 'tail': list(tail), 'parent': parent, 'deform': deform})
    return name
bone('Root', (0, -0.35, 0), (0, -0.35, 0.25), deform=False)
bone('Pelvis', (0, -0.82, 1.38), (0, -0.43, 1.38), 'Root')
bone('Spine', (0, -0.43, 1.38), (0, -0.02, 1.43), 'Pelvis')
bone('Chest', (0, -0.02, 1.43), (0, 0.26, 1.47), 'Spine')
bone('NeckLower', (0, 0.26, 1.47), (0, 0.53, 1.72), 'Chest')
bone('NeckUpper', (0, 0.53, 1.72), (0, 0.87, 1.98), 'NeckLower')
bone('Head', (0, 0.87, 1.98), (0, 1.19, 1.58), 'NeckUpper')
bone('Jaw', (0, 0.86, 1.74), (0, 1.17, 1.51), 'Head')
for (side, s) in [('L', -1), ('R', 1)]:
    bone('Ear.' + side, (s * 0.075, 0.905, 2.005), (s * 0.103, 0.997, 2.102), 'Head')
bone('TailBase', (0, -0.93, 1.46), (0, -1.13, 1.29), 'Pelvis')
bone('TailTip', (0, -1.13, 1.29), (0, -1.18, 1.14), 'TailBase')
for (side, s) in [('L', -1), ('R', 1)]:

    def p(x, y, z):
        return (s * x, y, z)
    bone('ForeShoulder.' + side, p(0.075, 0.09, 1.45), p(0.16, 0.34, 1.2), 'Chest')
    bone('ForeUpper.' + side, p(0.16, 0.34, 1.2), p(0.16, 0.21, 0.98), 'ForeShoulder.' + side)
    bone('Forearm.' + side, p(0.16, 0.21, 0.98), p(0.16, 0.247, 0.625), 'ForeUpper.' + side)
    bone('ForeCannon.' + side, p(0.16, 0.247, 0.625), p(0.16, 0.195, 0.21), 'Forearm.' + side)
    bone('ForePastern.' + side, p(0.16, 0.195, 0.21), p(0.162, 0.237, 0.09), 'ForeCannon.' + side)
    bone('ForeHoof.' + side, p(0.162, 0.237, 0.09), p(0.162, 0.316, 0.035), 'ForePastern.' + side)
    bone('HindThigh.' + side, p(0.16, -0.81, 1.38), p(0.17, -0.68, 1.01), 'Pelvis')
    bone('HindShin.' + side, p(0.17, -0.68, 1.01), p(0.175, -0.994, 0.625), 'HindThigh.' + side)
    bone('HindCannon.' + side, p(0.175, -0.994, 0.625), p(0.178, -0.925, 0.21), 'HindShin.' + side)
    bone('HindPastern.' + side, p(0.178, -0.925, 0.21), p(0.178, -0.876, 0.09), 'HindCannon.' + side)
    bone('HindHoof.' + side, p(0.178, -0.876, 0.09), p(0.178, -0.79, 0.035), 'HindPastern.' + side)
bpy.ops.object.mode_set(mode='OBJECT')
body.select_set(True)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
start = time.time()
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
heat_seconds = time.time() - start

def smooth(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)

def trunk(y):
    chest = smooth(-0.35, 0.15, y)
    pelvis = 1 - smooth(-0.9, -0.45, y)
    return {'Chest': chest, 'Pelvis': pelvis, 'Spine': 1 - chest - pelvis}
group_names = {g.index: g.name for g in body.vertex_groups}
unweighted = []
pruned = 0
rigid_sole = 0
for v in body.data.vertices:
    weights = {group_names[g.group]: g.weight for g in v.groups if group_names[g.group] != 'Root' and g.weight > 1e-07}
    (x, y, z) = v.co
    side = 'L' if x < 0 else 'R'
    prefix = 'Fore' if y > -0.25 else 'Hind'
    removed = 0.0
    for (name, w) in list(weights.items()):
        if name.startswith(('Fore', 'Hind')):
            sign = -1 if name.endswith('.L') else 1
            lateral = smooth(-0.04, 0.08, x * sign)
            bridge = 1 - smooth(0.78, 0.94, z) * (1 - smooth(0.012, 0.13, abs(x)))
            factor = lateral * bridge
            weights[name] = w * factor
            removed += w * (1 - factor)
    for (n, w) in trunk(y).items():
        weights[n] = weights.get(n, 0) + removed * w
    hoof = prefix + 'Hoof.' + side
    rigidity = 1 - smooth(0.095, 0.18, z)
    if rigidity > 0:
        weights = {n: w * (1 - rigidity) for (n, w) in weights.items()}
        weights[hoof] = weights.get(hoof, 0) + rigidity
        if rigidity == 1:
            rigid_sole += 1
    pairs = sorted(((n, w) for (n, w) in weights.items() if w > 1e-07), key=lambda a: a[1], reverse=True)
    if not pairs:
        unweighted.append(v.index)
        continue
    if len(pairs) > 4:
        pruned += 1
        cutoff = pairs[4][1]
        pairs = [(n, w - cutoff) for (n, w) in pairs[:4] if w > cutoff + 1e-08]
    total = sum((w for (n, w) in pairs))
    assert total > 1e-08, ('ambiguous sparse weights', v.index)
    for group_index in [g.group for g in v.groups]:
        body.vertex_groups[group_index].remove([v.index])
    for (n, w) in pairs:
        body.vertex_groups[n].add([v.index], w / total, 'REPLACE')
assert not unweighted, {'unweightedAfterRegionCheck': len(unweighted)}
maximum_influences = max((len(v.groups) for v in body.data.vertices))
maximum_weight_error = max((abs(sum((g.weight for g in v.groups)) - 1) for v in body.data.vertices))
assert maximum_influences <= 4 and maximum_weight_error < 1e-05, (maximum_influences, maximum_weight_error)
for m in body.modifiers:
    if m.type == 'ARMATURE':
        m.use_deform_preserve_volume = False
for p in body.data.polygons:
    p.use_smooth = True
body.data.update()
body.data.calc_loop_triangles()
for pb in rig.pose.bones:
    pb.rotation_mode = 'QUATERNION'
poses = {'Neutral': {}, 'ForeFold': {'ForeShoulder.R': 10, 'ForeUpper.R': 12, 'Forearm.R': 28, 'ForeCannon.R': -62, 'ForePastern.R': 24, 'ForeHoof.R': -12}, 'HindFold': {'HindThigh.R': 28, 'HindShin.R': -46, 'HindCannon.R': 38, 'HindPastern.R': -15, 'HindHoof.R': -8}, 'Reach': {'ForeShoulder.R': -8, 'ForeUpper.R': 10, 'Forearm.R': 22, 'ForeCannon.R': -8, 'ForePastern.R': -3, 'ForeHoof.R': -10, 'HindThigh.R': -12, 'HindShin.R': 15, 'HindCannon.R': -8}, 'NeckLowered': {'NeckLower': -10, 'NeckUpper': -12, 'Head': 6}, 'TurnLeft': {'NeckLower': {'z': 10}, 'NeckUpper': {'z': 12}, 'Head': {'z': 8}, 'Spine': {'z': -3}, 'Chest': {'z': -3}}}

def set_pose(spec):
    for pb in rig.pose.bones:
        pb.rotation_quaternion = Quaternion()
        pb.location = (0, 0, 0)
        pb.scale = (1, 1, 1)
    for (name, value) in spec.items():
        axis = Vector((0, 0, 1)) if isinstance(value, dict) else Vector((1, 0, 0))
        angle = value['z'] if isinstance(value, dict) else value
        local_axis = rig.data.bones[name].matrix_local.to_quaternion().inverted() @ axis
        rig.pose.bones[name].rotation_quaternion = Quaternion(local_axis, math.radians(angle))
    bpy.context.view_layer.update()
rest = [v.co.copy() for v in body.data.vertices]
edges = [tuple(e.vertices) for e in body.data.edges]
rest_lengths = [(rest[a] - rest[b]).length for (a, b) in edges]
measurements = {}
for (label, spec) in poses.items():
    set_pose(spec)
    obj = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = obj.to_mesh()
    coords = [body.matrix_world @ v.co for v in mesh.vertices]
    stretch = [(coords[a] - coords[b]).length / l for ((a, b), l) in zip(edges, rest_lengths) if l > 0.0001]
    stretch.sort()
    measurements[label] = {'minimumHeight': min((p.z for p in coords)), 'maximumDisplacement': max(((p - q).length for (p, q) in zip(coords, rest))), 'maximumEdgeStretch': max(stretch), 'p99EdgeStretch': stretch[int(len(stretch) * 0.99)], 'minimumEdgeLengthRatio': min(stretch), 'objectMatrixUnchanged': list(body.matrix_world.translation) == [0, 0, 0]}
    obj.to_mesh_clear()
set_pose({})
for obj in bpy.context.selected_objects:
    obj.select_set(False)
body.select_set(True)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.wm.save_as_mainfile(filepath=str(out / 'HeroHorse-RigStudy.blend'))
bpy.ops.export_scene.fbx(filepath=str(out / 'HeroHorse-RigStudy.fbx'), use_selection=True, object_types={'MESH', 'ARMATURE'}, axis_forward='-Z', axis_up='Y', add_leaf_bones=False, bake_anim=False, path_mode='STRIP', use_custom_props=False)
shutil.copyfile(base / 'ArtSource/HorseStudy/Source/BlendSwap-13903-license.html', out / 'source-license.html')
report = {'source': 'Horse by b2przemo', 'license': 'CC-BY-3.0', 'sourceStaticFbxSha256': hashlib.sha256(source.read_bytes()).hexdigest(), 'coordinates': 'metres; +Y forward, +Z up, +X anatomical right', 'authoring': 'Original fitted 34-bone hierarchy including one non-deforming Root; heat weights with continuous limb/trunk and coronet masks, continuous four-influence sparsification and rigid sole regions', 'blender': bpy.app.version_string, 'bones': rows, 'boneCount': len(rows), 'vertices': len(rest), 'triangles': len(body.data.loop_triangles), 'automaticWeightSeconds': heat_seconds, 'prunedVertices': pruned, 'rigidSoleVertices': rigid_sole, 'unweightedVertices': len(unweighted), 'maximumInfluences': maximum_influences, 'maximumWeightSumError': maximum_weight_error, 'poses': poses, 'deformationMeasurements': measurements, 'scope': 'Isolated untextured deformation study; no gait/foot-plant/Unity/runtime or photographic acceptance', 'fbxSha256': hashlib.sha256((out / 'HeroHorse-RigStudy.fbx').read_bytes()).hexdigest()}
(out / 'rig-study.json').write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps(report))
