import bpy, json, hashlib, sys
from pathlib import Path
args = sys.argv[sys.argv.index('--') + 1:]
results = []
baseline_bones = None
expected_extras = {"GroomMane."+str(i) for i in range(4)} | {"GroomTail"}
for path in args[:2]:
    bpy.ops.wm.open_mainfile(filepath=path, use_scripts=False)
    arm = bpy.data.objects['HeroHorseRig']
    body = bpy.data.objects['HeroHorseBody']
    arm.data.pose_position = 'REST'
    bpy.context.view_layer.update()
    names = {bone.name for bone in arm.data.bones}
    if baseline_bones is None:
        baseline_bones = names
        extras = set()
    else:
        assert baseline_bones <= names
        extras = names-baseline_bones
        assert extras == expected_extras, extras
        assert all(body.vertex_groups[g.group].name not in extras for v in body.data.vertices for g in v.groups)
    action = arm.animation_data.action
    curves = list(action.fcurves) if action.is_action_legacy else [f for layer in action.layers for strip in layer.strips for bag in strip.channelbags for f in bag.fcurves]
    curves = [f for f in curves if not any(('\"'+name+'\"') in f.data_path for name in extras)]
    data = {'vertices': [list(v.co) for v in body.data.vertices], 'polygons': [list(p.vertices) for p in body.data.polygons], 'uv': [list(u.uv) for u in body.data.uv_layers.active.data], 'cornerNormals': [list(n.vector) for n in body.data.corner_normals], 'skin': [{body.vertex_groups[g.group].name: g.weight for g in v.groups} for v in body.data.vertices], 'bones': [{'name': b.name, 'parent': b.parent.name if b.parent else None, 'matrix': list((v for row in b.matrix_local for v in row)), 'length': b.length, 'deform': b.use_deform, 'connect': b.use_connect, 'inheritScale': b.inherit_scale} for b in arm.data.bones if b.name in baseline_bones], 'curves': [{'path': f.data_path, 'index': f.array_index, 'keys': [(list(k.co), k.interpolation) for k in f.keyframe_points]} for f in sorted(curves, key=lambda f: (f.data_path, f.array_index))], 'modifiers': [(m.type, m.use_deform_preserve_volume) for m in body.modifiers if m.type == 'ARMATURE'], 'bodyWorld': [v for row in body.matrix_world for v in row], 'armatureWorld': [v for row in arm.matrix_world for v in row]}
    results.append(hashlib.sha256(json.dumps(data, sort_keys=True, separators=(',', ':')).encode()).hexdigest())
assert results[0] == results[1], results
Path(args[2]).write_text(json.dumps({'originalBodySkinBonesAndKeysPreserved': True, 'addedHairOnlyBones': sorted(expected_extras), 'canonicalDataSha256': results[0], 'scope': 'Original body and pre-existing bone/action subset; five explicit hair-only helper bones excluded. Canonical neutral vertex/topology/UV/corner-normal data, named weights, rest hierarchy/matrices, linear animation keys, armature skinning mode and object matrices; excludes file serialization metadata.'}, indent=2) + '\n')
print('REBUILD_DATA_IDENTICAL', results[0])
