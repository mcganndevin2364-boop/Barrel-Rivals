# Neutral daylight exposes color/roughness without the warm studio treatment.

import bpy, sys, math
from pathlib import Path
from mathutils import Vector
folder = Path(sys.argv[sys.argv.index('--') + 1])
bpy.ops.wm.open_mainfile(filepath=str(folder / 'HeroHorse-SurfaceStudy.blend'), use_scripts=False)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 20
scene.cycles.use_denoising = True
scene.render.resolution_x = 900
scene.render.resolution_y = 900
scene.render.resolution_percentage = 100
scene.view_settings.view_transform = 'AgX'
scene.world = bpy.data.worlds.new('SurfaceReviewWorld')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs[0].default_value = (0.15, 0.18, 0.23, 1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value = 0.4

def light(name, pos, energy, color, size, target):
    d = bpy.data.lights.new(name, 'AREA')
    o = bpy.data.objects.new(name, d)
    scene.collection.objects.link(o)
    o.location = pos
    d.energy = energy
    d.color = color
    d.shape = 'DISK'
    d.size = size
    o.rotation_euler = (Vector(target) - o.location).to_track_quat('-Z', 'Y').to_euler()
    return o
key = light('Broad key', (2, 2, 4), 450, (1, 0.83, 0.65), 3, (0, 0, 1.2))
fill = light('Cool fill', (-2, 1.5, 2.8), 200, (0.66, 0.8, 1), 2, (0, 0, 1.4))
rim = light('Rim', (1, -2, 3.4), 500, (1, 0.88, 0.72), 2, (0, 0, 1.4))
bpy.ops.mesh.primitive_plane_add(size=200)
floor = bpy.context.object
floor.name = 'ReviewFloor'
m = bpy.data.materials.new('ReviewFloorMaterial')
m.diffuse_color = (0.055, 0.05, 0.046, 1)
floor.data.materials.append(m)
cd = bpy.data.cameras.new('ReviewCamera')
cam = bpy.data.objects.new('ReviewCamera', cd)
scene.collection.objects.link(cam)
scene.camera = cam
rig = bpy.data.objects['HeroHorseRig']
rig.data.pose_position = 'REST'
bpy.context.view_layer.update()
if '--motion' in sys.argv:
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 8
    scene.cycles.use_denoising = True
    scene.render.resolution_x = 800
    scene.render.resolution_y = 600
    rig.data.pose_position = 'POSE'
    for (view, pos, target, scale) in [('quarter', (4, 4, 2.7), (0, 0, 1.03), 3.3), ('rider', (0, -0.35, 2.25), (0, 4, 1.9), 3.3)]:
        cam.location = pos
        cam.rotation_euler = (Vector(target) - cam.location).to_track_quat('-Z', 'Y').to_euler()
        cd.type = 'PERSP' if view == 'rider' else 'ORTHO'
        cd.ortho_scale = scale
        cd.lens = 24
        dest = folder / ('frames-' + view)
        dest.mkdir(exist_ok=True)
        for frame in range(1, 29):
            scene.frame_set(frame)
            scene.render.filepath = str(dest / ('%03d.png' % frame))
            bpy.ops.render.render(write_still=True)
    print('SURFACE_MOTION_COMPLETE')
    sys.exit(0)
for (name, pos, target, scale) in [('head', (2, 3, 2.15), (0, 1.06, 1.83), 0.8), ('side', (3, 1, 1.85), (0, 1.06, 1.83), 0.72), ('body', (4, 4, 2.7), (0, 0, 1.08), 3.3)]:
    cd.type = 'ORTHO'
    cd.ortho_scale = scale
    cam.location = pos
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    scene.render.filepath = str(folder / (name + '.png'))
    bpy.ops.render.render(write_still=True)
key.data.color = (1, 1, 1)
fill.data.color = (1, 1, 1)
rim.data.color = (1, 1, 1)
cam.location = (2, 3, 2.15)
cd.ortho_scale = 0.8
cam.rotation_euler = (Vector((0, 1.06, 1.83)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
scene.render.filepath = str(folder / 'head-neutral.png')
bpy.ops.render.render(write_still=True)
print('SURFACE_REVIEW_COMPLETE')
