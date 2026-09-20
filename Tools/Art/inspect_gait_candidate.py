"""Read an exported FBX, measure rendered sole paths and optionally render QA poses.
Blender --background --factory-startup --disable-autoexec --python this.py -- model.fbx output-directory [--render]
The monochrome preview is a diagnostic view of the exported model, not gameplay art.
"""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector, Matrix
sys.dont_write_bytecode=True
sys.path.insert(0,str(Path(__file__).resolve().parent))
from author_horse_gaits import _limbs, _vertices, _stats, SPECS
args=sys.argv[sys.argv.index('--')+1:];model,out=Path(args[0]),Path(args[1]);out.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(model),use_anim=True)
arm=next(o for o in bpy.data.objects if o.type=='ARMATURE');body=bpy.data.objects['HorseBody']
arm.animation_data.action=None
for bone in arm.pose.bones:bone.matrix_basis=Matrix.Identity(4)
bpy.context.view_layer.update();limbs=_limbs(arm,body)
report={'fbx':str(model),'boneCount':len(arm.data.bones),'bodyVertices':len(body.data.vertices),'clips':{}}
scene=bpy.context.scene;scene.render.fps=30
for name in ['Walk','Gallop']:
 action=next(a for a in bpy.data.actions if a.name.endswith('|'+name) or a.name==name)
 arm.animation_data.action=action
 if action.slots:arm.animation_data.action_slot=action.slots[0]
 samples=[];first,end=action.frame_range
 roots={o:o.matrix_world.copy() for o in bpy.data.objects if o.type in ('ARMATURE','EMPTY')}
 root_error=0;bone_scale_error=0;loop_poses=[]
 for i in range(121):
  phase=i/120;frame=first+phase*(end-first)
  scene.frame_set(int(frame),subframe=frame-int(frame));bpy.context.view_layer.update()
  actual=_vertices(body,limbs)
  root_error=max(root_error,max(abs(o.matrix_world[r][c]-m[r][c]) for o,m in roots.items() for r in range(4) for c in range(4)))
  for limb in limbs:
   label=limb['name'];cycle=(phase-SPECS[name]['offsets'][label])%1
   actual[label]['contact']=cycle<SPECS[name]['duty'];actual[label]['cycle']=cycle
  for bone in arm.pose.bones:bone_scale_error=max(bone_scale_error,max(abs(v-1) for v in bone.scale))
  if i in (0,120):loop_poses.append({b.name:b.matrix.copy() for b in arm.pose.bones})
  samples.append({'phase':phase,'hooves':{label:{'center':list(p['center']),'lowest':p['lowest'],'contact':p['contact'],'cycle':p['cycle']} for label,p in actual.items()}})
 report['clips'][name]={'frameRange':list(action.frame_range),'seconds':(end-first)/30,
  'maximumScaleDeviation':bone_scale_error,'maximumObjectWorldMatrixDeviation':root_error,
  'loopMaximumBoneTranslationDifference':max((loop_poses[0][b].translation-loop_poses[1][b].translation).length for b in loop_poses[0]),
  'hoofMetrics':_stats(samples,name),'samples':samples}
(out/'fbx-roundtrip.json').write_text(json.dumps(report,indent=2)+'\n')
print('ROUNDTRIP_METRICS',json.dumps({name:{k:v for k,v in clip.items() if k!='samples'} for name,clip in report['clips'].items()}))
if '--render' in args:
 for obj in bpy.data.objects:
  if obj.type=='MESH':
   obj.hide_render=obj!=body
 body.color=(.55,.36,.19,1)
 scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.color_type='OBJECT';scene.display.shading.light='STUDIO'
 scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True;scene.display.shading.cavity_type='BOTH'
 scene.display.shading.background_type='WORLD';scene.world.color=(.10,.10,.10)
 scene.render.resolution_x=800;scene.render.resolution_y=450;scene.render.resolution_percentage=100
 bpy.ops.object.camera_add(location=(4.7,0,2.25));camera=bpy.context.object
 camera.rotation_euler=(Vector((0,0,1.03))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=4.6;scene.camera=camera
 # A grid and flat plane make sole heights visible without hiding them in grass.
 bpy.ops.mesh.primitive_plane_add(size=30,location=(0,0,-.002));ground=bpy.context.object;ground.color=(.16,.18,.19,1)
 for name in ['Walk','Gallop']:
  arm.animation_data.action=next(a for a in bpy.data.actions if a.name.endswith('|'+name) or a.name==name)
  if arm.animation_data.action.slots:arm.animation_data.action_slot=arm.animation_data.action.slots[0]
  first,end=arm.animation_data.action.frame_range
  for i in range(8):
   phase=i/8;frame=first+phase*(end-first);scene.frame_set(int(frame),subframe=frame-int(frame));bpy.context.view_layer.update()
   scene.render.filepath=str(out/(name.lower()+'-'+str(i)+'.png'));bpy.ops.render.render(write_still=True)
 print('POSE_PREVIEWS',str(out))
