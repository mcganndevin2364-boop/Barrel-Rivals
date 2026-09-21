import bpy, json, hashlib, sys
from pathlib import Path
args = sys.argv[sys.argv.index('--') + 1:]
results = []
for path in args[:2]:
    bpy.ops.wm.open_mainfile(filepath=path, use_scripts=False)
    arm = bpy.data.objects['HeroHorseRig']
    body = bpy.data.objects['HeroHorseBody']
    arm.data.pose_position = 'REST'
    bpy.context.view_layer.update()
    action = arm.animation_data.action
    curves = list(action.fcurves) if action.is_action_legacy else [f for layer in action.layers for strip in layer.strips for bag in strip.channelbags for f in bag.fcurves]
    data = {'vertices': [list(v.co) for v in body.data.vertices], 'polygons': [list(p.vertices) for p in body.data.polygons], 'uv': [list(u.uv) for u in body.data.uv_layers.active.data], 'cornerNormals': [list(n.vector) for n in body.data.corner_normals], 'skin': [{body.vertex_groups[g.group].name: g.weight for g in v.groups} for v in body.data.vertices], 'bones': [{'name': b.name, 'parent': b.parent.name if b.parent else None, 'matrix': list((v for row in b.matrix_local for v in row)), 'length': b.length, 'deform': b.use_deform, 'connect': b.use_connect, 'inheritScale': b.inherit_scale} for b in arm.data.bones], 'curves': [{'path': f.data_path, 'index': f.array_index, 'keys': [(list(k.co), k.interpolation) for k in f.keyframe_points]} for f in sorted(curves, key=lambda f: (f.data_path, f.array_index))], 'modifiers': [(m.type, m.use_deform_preserve_volume) for m in body.modifiers if m.type == 'ARMATURE'], 'bodyWorld': [v for row in body.matrix_world for v in row], 'armatureWorld': [v for row in arm.matrix_world for v in row]}
    results.append(hashlib.sha256(json.dumps(data, sort_keys=True, separators=(',', ':')).encode()).hexdigest())
assert results[0] == results[1], results
Path(args[2]).write_text(json.dumps({'geometrySkinRigAndAnimationKeyDataIdentical': True, 'canonicalDataSha256': results[0], 'scope': 'Canonical neutral vertex/topology/UV/corner-normal data, named weights, rest hierarchy/matrices, linear animation keys, armature skinning mode and object matrices; excludes file serialization metadata.'}, indent=2) + '\n')
print('REBUILD_DATA_IDENTICAL', results[0])
