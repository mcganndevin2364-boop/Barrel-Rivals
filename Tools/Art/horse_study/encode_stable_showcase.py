"""Encode 120 actual Unity showroom frames; this is not measured game FPS."""
import bpy, sys, json
from pathlib import Path
folder = Path(sys.argv[sys.argv.index('--') + 1])
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = 30
scene.render.resolution_x, scene.render.resolution_y = 960, 540
scene.render.resolution_percentage = 100
scene.view_settings.view_transform = 'Standard'
scene.view_settings.look = 'None'
sequence = scene.sequence_editor_create()
strip = sequence.strips.new_image('Actual saved MyStable', str(folder/'frames/001.png'), channel=1, frame_start=1)
for frame in range(2,121): strip.elements.append('%03d.png' % frame)
strip.frame_final_duration = 120
label = sequence.strips.new_effect('Capture scope', type='TEXT', channel=2, frame_start=1, frame_end=121)
label.text = 'UNITY SHOWROOM REVIEW  |  Controlled idle cycle — not phone footage'
label.font_size = 14
label.location = (.5,.014)
label.color = (.95,.93,.87,1)
label.use_shadow = True
scene.frame_start, scene.frame_end = 1,120
scene.render.image_settings.file_format = 'FFMPEG'
scene.render.ffmpeg.format = 'MPEG4'
scene.render.ffmpeg.codec = 'H264'
scene.render.ffmpeg.constant_rate_factor = 'HIGH'
scene.render.ffmpeg.audio_codec = 'NONE'
scene.render.filepath = str(folder/'StableShowcase.mp4')
bpy.ops.render.render(animation=True)
(folder/'video-spec.json').write_text(json.dumps({'frames':120,'fps':30,'width':960,'height':540,'views':['stable'],
    'scope':'Saved MyStable idle sampled at 120 phases, real rig-following reins, skin refresh and real UI. Four-second encoded cycle is a controlled review, not game FPS or phone performance.'},indent=2)+'\n')
