"""Run in Blender 4.5 with --disable-autoexec; argv: -- source.blend output-directory.
Exports approved mesh/rig/image data only. CC0 upstream provenance is in Docs/Art.
Original basic animation clips are a first-pass gait study, not production motion capture.
"""
import bpy,sys,math,json
from pathlib import Path
from mathutils import Vector,Matrix
source,output=sys.argv[sys.argv.index('--')+1:];out=Path(output);out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=source,load_ui=False,use_scripts=False)
meshes=[o for o in bpy.data.objects if o.type=='MESH'];arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
body=bpy.data.objects['Plane']
bpy.context.view_layer.objects.active=arm
if bpy.context.object.mode!='OBJECT':bpy.ops.object.mode_set(mode='OBJECT')
coords=[body.matrix_world@Vector(v) for v in body.bound_box]
low=Vector([min(v[i] for v in coords) for i in range(3)]);high=Vector([max(v[i] for v in coords) for i in range(3)])
scale=2.12/(high.z-low.z);center=Vector(((low.x+high.x)/2,(low.y+high.y)/2,low.z))
# A common parent preserves mesh/armature bind relations while setting metres and nose +Y.
root=bpy.data.objects.new('RodeoHorse',None);bpy.context.collection.objects.link(root)
for o in meshes+[arm]:
 world=o.matrix_world.copy();o.parent=root;o.matrix_world=world
root.matrix_world=Matrix.Scale(scale,4)@Matrix.Rotation(math.pi,4,'Z')@Matrix.Translation(-center)
bpy.context.view_layer.update()
rename={'Plane':'HorseBody','BezierCurve':'HorseHairA','BezierCurve.005':'HorseHairB','Sphere':'HorseEyeL','Sphere.002':'HorseEyeR'}
for o in meshes:o.name=rename.get(o.name,o.name)
# Keep detached eyes on the same head bone, preserving the original rest transform.
for o in meshes:
 if o.name.startswith('HorseEye'):
  world=o.matrix_world.copy();o.parent=arm;o.parent_type='BONE';o.parent_bone='Bone.002';o.matrix_world=world
# Export the packed source maps losslessly; unused missing reference photos stay out.
for original,name in [('HorseMain4k00.png','HorseAlbedo.png'),('HorseMain4k00Norm00.p','HorseNormal.png'),('Hair12Main2k.png','HorseHair.png'),('eye_texture.bmp.001','HorseEye.png')]:
 im=bpy.data.images.get(original)
 if im and im.size[0]:im.filepath_raw=str(out/name);im.file_format='PNG';im.save()
# Explicit, in-place clips. No animation root translation controls racing motion.
scene=bpy.context.scene;scene.render.fps=30;arm.animation_data_create()
for name,frames,moving in [('Idle',60,0),('Walk',32,.4),('Gallop',20,1)]:
 action=bpy.data.actions.new(name);arm.animation_data.action=action
 for f in range(1,frames+2):
  phase=(f-1)/frames*math.tau
  for b in arm.pose.bones:
   b.rotation_mode='XYZ';b.rotation_euler=(0,0,0);b.location=(0,0,0)
  if moving:
   for upper,knee,offset in [('Bone_L.001','Bone_L.002',0),('Bone_R.001','Bone_R.002',math.pi),('Bone_L.004','Bone_L.005',2.0),('Bone_R.004','Bone_R.005',5.14)]:
    arm.pose.bones[upper].rotation_euler.x=math.sin(phase+offset)*.42*moving
    arm.pose.bones[knee].rotation_euler.x=max(0,math.cos(phase+offset))*.62*moving
  arm.pose.bones['Bone.001'].rotation_euler.x=math.sin(phase)*(.025+.025*moving)
  arm.pose.bones['Bone.002'].rotation_euler.x=math.sin(phase+.6)*.025
  arm.pose.bones['Bone.004'].rotation_euler.z=math.sin(phase)*.07
  for b in arm.pose.bones:b.keyframe_insert(data_path='rotation_euler',frame=f,group=b.name)
 action.use_fake_user=True
arm.animation_data.action=None
for b in arm.pose.bones:b.rotation_euler=(0,0,0)
scene.frame_set(1)
bpy.context.view_layer.update()
for o in list(bpy.context.view_layer.objects):o.select_set(False)
for o in meshes+[arm,root]:o.select_set(True)
bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=str(out/'RodeoHorse.fbx'),use_selection=True,object_types={'MESH','ARMATURE','EMPTY'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='STRIP',use_custom_props=False)
report={'source':Path(source).name,'heightMetres':2.12,'bodyVertices':len(body.data.vertices),'bones':len(arm.data.bones),'clips':['Idle','Walk','Gallop'],'animationQuality':'Original first-pass gait studies; foot planting/turn/rider synchronization still require review','autoExecuteScripts':False,'blender':bpy.app.version_string}
(out/'source-inspection.json').write_text(json.dumps(report,indent=2)+'\n')
print('HORSE_EXPORT',json.dumps(report))
