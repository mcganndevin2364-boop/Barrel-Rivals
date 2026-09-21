"""Render and locate edge-strain diagnostics on the actual fitted Blender rig."""
import bpy, json, sys, math
from pathlib import Path
from mathutils import Vector, Quaternion
folder = Path(sys.argv[sys.argv.index('--') + 1])
data = json.loads((folder / 'rig-study.json').read_text())
bpy.ops.wm.open_mainfile(filepath=str(folder / 'HeroHorse-RigStudy.blend'), use_scripts=False)
rig = bpy.data.objects['HeroHorseRig']
body = bpy.data.objects['HeroHorseBody']
scene = bpy.context.scene
scene.render.engine = 'BLENDER_WORKBENCH'
scene.render.resolution_x = 1050
scene.render.resolution_y = 760
scene.render.resolution_percentage = 100
sh = scene.display.shading
sh.light = 'STUDIO'
sh.studiolight_rotate_z = 0.4
sh.color_type = 'SINGLE'
sh.single_color = (0.39, 0.28, 0.18)
sh.show_shadows = True
sh.show_cavity = True
sh.cavity_type = 'BOTH'
sh.curvature_ridge_factor = 1.15
sh.curvature_valley_factor = 1.0
sh.show_specular_highlight = True
sh.background_type = 'WORLD'
sh.background_color = (0.075, 0.075, 0.085)
if scene.world is None:
    scene.world = bpy.data.worlds.new('DiagnosticWorld')
scene.world.color = (0.075, 0.075, 0.085)
scene.view_settings.view_transform = 'Standard'
camdata = bpy.data.cameras.new('DiagnosticCamera')
cam = bpy.data.objects.new('DiagnosticCamera', camdata)
scene.collection.objects.link(cam)
scene.camera = cam
camdata.type = 'ORTHO'
rest = [v.co.copy() for v in body.data.vertices]
edges = [tuple(e.vertices) for e in body.data.edges]
lengths = [(rest[a] - rest[b]).length for (a, b) in edges]
report = {}

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

def render(name, pos, target, scale):
    cam.location = pos
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    camdata.ortho_scale = scale
    scene.render.filepath = str(folder / (name + '.png'))
    bpy.ops.render.render(write_still=True)
for (name, spec) in data['poses'].items():
    pose(spec)
    obj = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = obj.to_mesh()
    coords = [v.co.copy() for v in mesh.vertices]
    scored = sorted([((coords[a] - coords[b]).length / l, a, b, l) for ((a, b), l) in zip(edges, lengths) if l > 0.0001], reverse=True)
    report[name] = [{'ratio': s, 'restLength': l, 'midpoint': list((rest[a] + rest[b]) * 0.5), 'weightsA': {body.vertex_groups[g.group].name: g.weight for g in body.data.vertices[a].groups}, 'weightsB': {body.vertex_groups[g.group].name: g.weight for g in body.data.vertices[b].groups}} for (s, a, b, l) in scored[:12]]
    obj.to_mesh_clear()
    render(name + '-side', (6, 0, 1.12), (0, 0, 1.12), 3.25)
    if name == 'Neutral':
        render(name + '-front', (0, 6, 1.12), (0, 0, 1.12), 3.05)
        render(name + '-quarter', (4, 4, 2.8), (0, 0, 1.05), 3.4)
    if name in ('ForeFold', 'HindFold'):
        y = 0.22 if name == 'ForeFold' else -0.8
        render(name + '-joint', (4, y, 0.6), (0, y, 0.6), 1.55)
    if name == 'TurnLeft':
        render(name + '-front', (0, 6, 1.12), (0, 0, 1.12), 3.05)
(folder / 'strain-diagnostics.json').write_text(json.dumps(report, indent=2) + '\n')
print('DIAGNOSTIC_RENDERS_COMPLETE')
