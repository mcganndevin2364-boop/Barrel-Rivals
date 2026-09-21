"""Encode the actual Unity in-place speed-blend review, without repeated frames."""
import bpy,sys,json
from pathlib import Path
folder=Path(sys.argv[sys.argv.index('--')+1]);bpy.ops.wm.read_factory_settings(use_empty=True)
s=bpy.context.scene;s.render.fps=30;s.render.resolution_x=800;s.render.resolution_y=600;s.render.resolution_percentage=100
s.view_settings.view_transform='Standard';s.view_settings.look='None';seq=s.sequence_editor_create()
for k,view in enumerate(['side','rider']):
 start=1+k*210;strip=seq.strips.new_image(view,str(folder/('frames-'+view)/'001.png'),channel=1,frame_start=start)
 for i in range(2,211):strip.elements.append('%03d.png'%i)
 strip.frame_final_duration=210
 for j,label in enumerate(['IDLE','WALK','TROT','GALLOP','SPRINT','DRIVE SPEED','STOP']):
  text=seq.strips.new_effect(view+label,type='TEXT',channel=2,frame_start=start+j*30,frame_end=start+(j+1)*30)
  text.text='UNITY GAIT STUDY  |  '+view.upper()+'  |  '+label+'\nIn-place speed blend — not phone footage'
  text.font_size=18;text.location=(.5,.94);text.color=(.95,.93,.87,1);text.use_shadow=True
s.frame_start=1;s.frame_end=420;s.render.image_settings.file_format='FFMPEG';s.render.ffmpeg.format='MPEG4';s.render.ffmpeg.codec='H264';s.render.ffmpeg.constant_rate_factor='HIGH';s.render.ffmpeg.audio_codec='NONE';s.render.filepath=str(folder/'HeroHorse-Locomotion.mp4')
bpy.ops.render.render(animation=True)
(folder/'video-spec.json').write_text(json.dumps({'frames':420,'fps':30,'width':800,'height':600,'views':['side','rider'],'scope':'420 actual Unity renders, in-place static floor, same live speed driver stepped at30Hz. Not gameplay frame rate, world foot planting or device acceptance.'},indent=2)+'\n')
