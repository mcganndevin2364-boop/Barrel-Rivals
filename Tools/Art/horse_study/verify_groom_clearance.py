"""Measure actual posed groom attachment and surface crossings."""
import bpy, sys, json, math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
folder = Path(sys.argv[sys.argv.index('--') + 1])
bpy.ops.wm.open_mainfile(filepath=str(folder / 'HeroHorse-GroomStudy.blend'), use_scripts=False)
spec = json.loads((folder / 'groom-study.json').read_text())
hair = bpy.data.objects['HeroHorseGroom']
body = bpy.data.objects['HeroHorseBody']
rig = bpy.data.objects['HeroHorseRig']
scene = bpy.context.scene
rig.data.pose_position = 'POSE'
body.data.calc_loop_triangles()
body_tri = [tuple(t.vertices) for t in body.data.loop_triangles]
hair.data.calc_loop_triangles()
hair_tri = [tuple(t.vertices) for t in hair.data.loop_triangles]

def posed(obj):
    ev = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    m = ev.to_mesh()
    p = [v.co.copy() for v in m.vertices]
    ev.to_mesh_clear()
    return p
rows = []
worst = []
for i in range(113):
    frame = 1 + i / 4
    scene.frame_set(int(frame), subframe=frame % 1)
    bpy.context.view_layer.update()
    b = posed(body)
    h = posed(hair)
    tree = BVHTree.FromPolygons(b, body_tri, all_triangles=True)
    distances = []
    root = []
    for a in spec['attachments']:
        p = sum((b[index] * w for (index, w) in zip(body_tri[a['face']], a['barycentric'])), Vector())
        root.append((h[a['vertex']] - p).length)
    for (kind, points) in [('vertex', h), ('face', [(h[a] + h[b] + h[c]) / 3 for (a, b, c) in hair_tri])]:
        for (index, p) in enumerate(points):
            (near, n, face, d) = tree.find_nearest(p)
            signed = (p - near).dot(n)
            distances.append(signed)
            if signed < -0.002:
                worst.append({'frame': frame, 'kind': kind, 'index': index, 'point': list(p), 'signedM': signed})
    rows.append({'frame': frame, 'minimumSignedDistanceM': min(distances), 'insideCountBeyond2mm': sum((v < -0.002 for v in distances)), 'rootMinM': min(root), 'rootMaxM': max(root), 'minimumGroomHeightM': min((v.z for v in h))})
result = {'scope': 'Diagnostic signed nearest-surface distances at hair vertices and triangle centers over 113 full-cycle poses. Not a continuous collision proof.', 'poses': rows, 'worst': sorted(worst, key=lambda a: a['signedM'])[:40]}
(folder / 'clearance-diagnostic.json').write_text(json.dumps(result, indent=2) + '\n')
print(json.dumps(rows))
print('WORST', result['worst'][:5])
assert max((r['insideCountBeyond2mm'] for r in rows)) == 0, 'Hair crosses body by more than the declared 2 mm tolerance'
assert max((r['rootMaxM'] for r in rows)) < 0.012, 'Detached groom root'
assert min((r['minimumGroomHeightM'] for r in rows)) > 0.08, 'Groom crosses diagnostic floor'
