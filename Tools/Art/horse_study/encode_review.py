import bpy, sys
from pathlib import Path
folder = Path(sys.argv[sys.argv.index('--') + 1])
bpy.ops.wm.read_factory_settings(use_empty=True)
s = bpy.context.scene
s.render.fps = 30
s.render.resolution_x = 960
s.render.resolution_y = 720
s.render.resolution_percentage = 100
s.view_settings.view_transform = 'Standard'
s.view_settings.look = 'None'
seq = s.sequence_editor_create()
start = 1
for view in ('side', 'quarter'):
    first = folder / ('frames-' + view) / '001.png'
    strip = seq.strips.new_image(view, str(first), channel=1, frame_start=start)
    for cycle in range(4):
        for frame in range(1, 33):
            if cycle == 0 and frame == 1:
                continue
            strip.elements.append('%03d.png' % frame)
    strip.frame_final_duration = 128
    text = seq.strips.new_effect('Study label ' + view, type='TEXT', channel=2, frame_start=start, frame_end=start + 128)
    text.text = 'HORSE WALK STUDY  |  ' + view.upper() + ' VIEW\nOffline render - untextured - not final gameplay'
    text.font_size = 22
    text.location = (0.5, 0.91)
    text.color = (0.95, 0.93, 0.87, 1)
    text.use_shadow = True
    start += 128
s.frame_start = 1
s.frame_end = start - 1
s.render.image_settings.file_format = 'FFMPEG'
s.render.ffmpeg.format = 'MPEG4'
s.render.ffmpeg.codec = 'H264'
s.render.ffmpeg.constant_rate_factor = 'HIGH'
s.render.ffmpeg.audio_codec = 'NONE'
s.render.filepath = str(folder / 'HeroHorse-WalkStudy.mp4')
bpy.ops.render.render(animation=True)
print('ENCODE_REVIEW_COMPLETE')
