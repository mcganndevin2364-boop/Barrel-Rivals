# Authoring notes
# This is an in-place walk. Static floor makes swing/plant orientation visible;
# travel residuals are measured separately at the cycle's authored 1.5m/s.

import bpy, sys, json
from pathlib import Path
from mathutils import Vector
folder = Path(sys.argv[sys.argv.index('--') + 1])
bpy.ops.wm.open_mainfile(filepath=str(folder / 'HeroHorse-WalkStudy.blend'), use_scripts=False)
scene = bpy.context.scene
scene.render.engine = 'BLENDER_WORKBENCH'
scene.render.resolution_x = 960
scene.render.resolution_y = 720
scene.render.resolution_percentage = 100
sh = scene.display.shading
sh.light = 'STUDIO'
sh.studiolight_rotate_z = 0.4
sh.color_type = 'OBJECT'
sh.show_shadows = True
sh.show_cavity = True
sh.cavity_type = 'BOTH'
sh.curvature_ridge_factor = 1.1
sh.curvature_valley_factor = 1
sh.background_type = 'WORLD'
sh.show_specular_highlight = True
if not scene.world:
    scene.world = bpy.data.worlds.new('DiagnosticWorld')
scene.world.color = (0.07, 0.07, 0.075)
scene.view_settings.view_transform = 'Standard'
scene.render.image_settings.file_format = 'PNG'
bpy.data.objects['HeroHorseBody'].color = (0.39, 0.28, 0.18, 1)
bpy.ops.mesh.primitive_plane_add(size=200)
floor = bpy.context.object
floor.name = 'DiagnosticFloor'
floor.color = (0.125, 0.13, 0.135, 1)
cd = bpy.data.cameras.new('DiagnosticCamera')
cam = bpy.data.objects.new('DiagnosticCamera', cd)
scene.collection.objects.link(cam)
scene.camera = cam
cd.type = 'ORTHO'
cd.ortho_scale = 3.4
for (view, pos) in [('side', (6, 0, 1.25)), ('quarter', (4, 4, 2.7))]:
    cam.location = pos
    cam.rotation_euler = (Vector((0, 0, 0.97)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    dest = folder / ('frames-' + view)
    dest.mkdir(exist_ok=True)
    for i in range(1, 34):
        scene.frame_set(i)
        scene.render.filepath = str(dest / ('%03d.png' % i))
        bpy.ops.render.render(write_still=True)
print('WALK_RENDERS_COMPLETE')
