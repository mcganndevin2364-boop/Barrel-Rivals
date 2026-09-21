"""Compare saved surface data and shader graphs, ignoring serialization times.
Use compare_rebuild.py separately for the body rig and animation contract.
"""
import bpy
import hashlib
import json
import sys
from pathlib import Path

args = sys.argv[sys.argv.index('--') + 1:]
hashes = []

def value(socket):
    if not hasattr(socket, 'default_value'):
        return None
    default = socket.default_value
    if default is None or isinstance(default, (str, bool, float, int)):
        return default
    return list(default)

for path in args[:2]:
    bpy.ops.wm.open_mainfile(filepath=path, use_scripts=False)
    data = {'meshes': {}, 'materials': {}}
    for name in ('HeroHorseBody', 'HeroHorseEyes'):
        obj = bpy.data.objects[name]
        data['meshes'][name] = {
            'positions': [list(v.co) for v in obj.data.vertices],
            'polygons': [list(p.vertices) for p in obj.data.polygons],
            'colors': {a.name: {'type': a.data_type, 'domain': a.domain,
                              'values': [list(v.color) for v in a.data]}
                       for a in obj.data.color_attributes},
            'skin': [{obj.vertex_groups[g.group].name: g.weight for g in v.groups}
                     for v in obj.data.vertices],
            'materials': [m.name for m in obj.data.materials],
        }
    for name in ('OriginalBayCoat', 'OriginalHorseEyes'):
        material = bpy.data.materials[name]
        nodes = []
        for node in sorted(material.node_tree.nodes, key=lambda n: n.name):
            props = {key: getattr(node, key) for key in
                     ('operation', 'blend_type', 'noise_dimensions', 'normalize',
                      'use_clamp', 'interpolation_type', 'data_type', 'layer_name',
                      'clamp', 'invert') if hasattr(node, key)}
            nodes.append({'name': node.name, 'type': node.bl_idname,
                          'properties': props,
                          'inputs': [(s.identifier, value(s)) for s in node.inputs]})
        links = sorted((l.from_node.name, l.from_socket.identifier,
                        l.to_node.name, l.to_socket.identifier)
                       for l in material.node_tree.links)
        data['materials'][name] = {'nodes': nodes, 'links': links}
    hashes.append(hashlib.sha256(json.dumps(data, sort_keys=True,
                                            separators=(',', ':')).encode()).hexdigest())

assert hashes[0] == hashes[1], hashes
report = {'surfaceDataAndShaderGraphsIdentical': True,
          'canonicalDataSha256': hashes[0],
          'scope': 'Neutral body/eye mesh, vertex colors, skin and material assignment; '
                   'shader node types, authored properties, input defaults and links. '
                   'Body rig/animation checked separately. Not render or Unity acceptance.'}
Path(args[2]).write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps(report))
