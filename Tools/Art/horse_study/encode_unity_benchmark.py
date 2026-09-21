

"""Encode actual material review renders with an explicit offline-study label."""
import bpy, sys, json
from pathlib import Path
folder = Path(sys.argv[sys.argv.index('--') + 1])
bpy.ops.wm.read_factory_settings(use_empty=True)
s = bpy.context.scene
s.render.fps = 30
s.render.resolution_x = 800
s.render.resolution_y = 600
s.render.resolution_percentage = 100
s.view_settings.view_transform = 'Standard'
s.view_settings.look = 'None'
seq = s.sequence_editor_create()
start = 1
for view in ('quarter', 'rider'):
    strip = seq.strips.new_image(view, str(folder / ('frames-' + view) / '001.png'), channel=1, frame_start=start)
    for i in range(1, 112):
        strip.elements.append('%03d.png' % (i % 28 + 1))
    strip.frame_final_duration = 112
    label = seq.strips.new_effect('Unity benchmark ' + view, type='TEXT', channel=2, frame_start=start, frame_end=start + 112)
    label.text = 'UNITY HORSE / SADDLE BENCHMARK  |  ' + view.upper() + '\nControlled Unity capture - not a phone benchmark'
    label.font_size = 19
    label.location = (0.5, 0.91)
    label.color = (0.95, 0.93, 0.87, 1)
    label.use_shadow = True
    start += 112
s.frame_start = 1
s.frame_end = start - 1
s.render.image_settings.file_format = 'FFMPEG'
s.render.ffmpeg.format = 'MPEG4'
s.render.ffmpeg.codec = 'H264'
s.render.ffmpeg.constant_rate_factor = 'HIGH'
s.render.ffmpeg.audio_codec = 'NONE'
s.render.filepath = str(folder / 'HeroHorse-UnityBenchmark.mp4')
bpy.ops.render.render(animation=True)
(folder / 'video-spec.json').write_text(json.dumps({'frames': 224, 'fps': 30, 'width': 800, 'height': 600, 'views': ['quarter', 'rider'], 'scope': 'Controlled Unity capture, static floor, four repeated walk cycles per view. Not measured gameplay or phone performance.'}, indent=2) + '\n')
print('UNITY_BENCHMARK_VIDEO_ENCODED')
