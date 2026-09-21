"""Independent saved groom and FBX fidelity checks, including between-key poses.
No photographic, continuous collision, Unity shader or device acceptance implied.
"""
import bpy, json, hashlib, math, sys
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree

folder = Path(sys.argv[sys.argv.index('--') + 1])
bpy.ops.wm.open_mainfile(filepath=str(folder / 'HeroHorse-GroomStudy.blend'), use_scripts=False)
scene = bpy.context.scene
rig = bpy.data.objects['HeroHorseRig']
hair = bpy.data.objects['HeroHorseGroom']
spec = json.loads((folder / 'groom-study.json').read_text())
assert len(rig.data.bones) == 39
assert len(hair.data.materials) == 2
hair.data.calc_loop_triangles()
assert all(t.area > 1e-10 for t in hair.data.loop_triangles)
for atlas in spec['atlases']:
    image = bpy.data.images[Path(atlas['file']).name]
    assert image.packed_file
    assert hashlib.sha256(bytes(image.packed_file.data)).hexdigest() == atlas['sha256']
    assert image.colorspace_settings.name == 'sRGB'
for material in hair.data.materials:
    nodes = material.node_tree.nodes
    cutoff = [n for n in nodes if n.type == 'MATH' and n.operation == 'GREATER_THAN']
    assert len(cutoff) == 1 and abs(cutoff[0].inputs[1].default_value - .36) < 1e-6
    assert cutoff[0].inputs[0].links[0].from_socket.name == 'Alpha'

def weights(obj):
    return [{obj.vertex_groups[g.group].name: g.weight for g in v.groups if g.weight > 1e-7}
            for v in obj.data.vertices]

def uvs(obj):
    values = [set() for v in obj.data.vertices]
    for loop in obj.data.loops:
        values[loop.vertex_index].add(tuple(obj.data.uv_layers.active.data[loop.index].uv))
    return values

def points(obj):
    ev = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = ev.to_mesh()
    result = np.array([list(obj.matrix_world @ v.co) for v in mesh.vertices])
    ev.to_mesh_clear()
    return result

def normals(obj):
    matrix = obj.matrix_world.to_3x3().inverted().transposed()
    values = [[] for v in obj.data.vertices]
    for loop in obj.data.loops:
        values[loop.vertex_index].append((matrix @ obj.data.corner_normals[loop.index].vector).normalized())
    return values

def canonical_face(indices):
    # Preserve winding while ignoring only the choice of first corner.
    indices = tuple(indices)
    return min(indices[i:] + indices[:i] for i in range(len(indices)))

def faces(obj, mapping=None):
    result = []
    for poly in obj.data.polygons:
        ids = [mapping[i] if mapping is not None else i for i in poly.vertices]
        result.append((canonical_face(ids), obj.data.materials[poly.material_index].name))
    return sorted(result)

ow = weights(hair)
assert all(1 <= len(w) <= 4 and abs(sum(w.values()) - 1) < 1e-6 for w in ow)
assert all(all(math.isfinite(w) and w > 0 for w in skin.values()) for skin in ow)
assert all(not any(n.startswith('Groom') for n in ow[a['vertex']]) for a in spec['attachments'])
rig.data.pose_position = 'REST'
bpy.context.view_layer.update()
neutral = points(hair)
ou = uvs(hair)
on = normals(hair)
of = faces(hair)
assert np.isfinite(neutral).all()
for card in spec['cards']:
    start, count, cols = card['startVertex'], card['vertexCount'], card['columns']
    for i in range(start, start + count):
        assert len(ou[i]) == 1
        u, v = next(iter(ou[i]))
        assert (card['atlasCell'] + .02) / 8 <= u <= (card['atlasCell'] + .98) / 8
        assert .0299 <= v <= .9691
    root_v = next(iter(ou[start]))[1]
    tip_v = next(iter(ou[start + count - cols - 1]))[1]
    assert root_v > tip_v, 'Atlas roots must be at V top'

frames = [1 + i / 8 for i in range(225)]
original = []
rig.data.pose_position = 'POSE'
root_matrix = np.array(rig.matrix_world)
helper_rotations = {b['name']: [] for b in spec['helpers']}
for frame in frames:
    scene.frame_set(int(frame), subframe=frame % 1)
    bpy.context.view_layer.update()
    assert np.max(np.abs(np.array(rig.matrix_world) - root_matrix)) < 1e-8
    original.append(points(hair))
    for name in helper_rotations:
        helper_rotations[name].append(np.array(rig.pose.bones[name].rotation_quaternion))
loop_error = float(np.max(np.linalg.norm(original[0] - original[-1], axis=1)))
assert loop_error < 1e-6
assert all(np.max(np.ptp(np.array(rotations), axis=0)) > .005 for rotations in helper_rotations.values())

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = 30
bpy.ops.import_scene.fbx(filepath=str(folder / 'HeroHorse-GroomStudy.fbx'), use_anim=True, anim_offset=0, colors_type='LINEAR')
hair = bpy.data.objects['HeroHorseGroom']
rig = bpy.data.objects['HeroHorseRig']
rig.data.pose_position = 'REST'
bpy.context.view_layer.update()
imported = points(hair)
iw, iu, inn = weights(hair), uvs(hair), normals(hair)
assert len(imported) == len(neutral)
tree = KDTree(len(neutral))
for i, p in enumerate(neutral):
    tree.insert(Vector(p), i)
tree.balance()
mapping, used = [], set()
max_rest = max_uv = max_skin = 0.0
min_normal_dot = 1.0
for i, p in enumerate(imported):
    matches = []
    for _, j, d in tree.find_range(Vector(p), 1e-5):
        if j in used or set(iw[i]) != set(ow[j]):
            continue
        uv_error = max(max(min(max(abs(a-b) for a,b in zip(u,v)) for v in ou[j]) for u in iu[i]),
                       max(min(max(abs(a-b) for a,b in zip(u,v)) for v in iu[i]) for u in ou[j]))
        skin_error = max(abs(iw[i][n]-ow[j][n]) for n in iw[i])
        if uv_error < 1e-6 and skin_error < 1e-5:
            matches.append((d,j,uv_error,skin_error))
    assert matches, ('No unused position/UV/skin match', i, list(p))
    d,j,uv_error,skin_error = min(matches)
    mapping.append(j); used.add(j)
    max_rest = max(max_rest,d); max_uv = max(max_uv,uv_error); max_skin = max(max_skin,skin_error)
    dot = min(min(max(a.dot(b) for b in on[j]) for a in inn[i]),
              min(max(a.dot(b) for b in inn[i]) for a in on[j]))
    min_normal_dot = min(min_normal_dot,dot)
assert len(used) == len(neutral)
assert min_normal_dot > .99999
assert faces(hair,mapping) == of, 'Face winding/material assignment mismatch'
rig.data.pose_position = 'POSE'
max_motion = 0.0
for frame, expected in zip(frames,original):
    scene.frame_set(int(frame), subframe=frame % 1)
    bpy.context.view_layer.update()
    actual = points(hair)
    max_motion = max(max_motion,float(np.max(np.linalg.norm(actual-expected[mapping],axis=1))))
assert max_motion < 1e-5, max_motion
hair.data.calc_loop_triangles()
report = {
    'scope': 'Saved groom topology, UV/skin/material identity and independent FBX fidelity at 225 authored and between-key poses. Not photographic, continuous collision, Unity or device acceptance.',
    'hairVertices': len(neutral), 'hairTriangles': len(hair.data.loop_triangles),
    'materials': 2, 'cards': len(spec['cards']), 'bones': len(rig.data.bones),
    'sampleCount': len(frames), 'packedAtlasHashesVerified': True,
    'normalizedMaximumFourWeights': True, 'rootsExcludeHelperWeights': True,
    'alphaCutoutAndAtlasOrientationVerified': True, 'fixedObjectRoot': True,
    'distinctHelperMotion': True, 'loopErrorM': loop_error,
    'fbx': {'bijectivePositionUVSkinMapping': True, 'faceWindingMaterialsPreserved': True,
            'maximumNeutralPositionErrorM': max_rest, 'maximumMotionErrorM': max_motion,
            'maximumUVError': max_uv, 'maximumSkinError': max_skin,
            'minimumCornerNormalDot': min_normal_dot},
}
(folder/'groom-verification.json').write_text(json.dumps(report,indent=2)+'\n')
print('GROOM_VERIFIED',json.dumps(report))
