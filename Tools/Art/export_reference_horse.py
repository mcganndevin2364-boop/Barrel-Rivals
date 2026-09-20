"""Run in Blender 4.5 with --disable-autoexec; argv: -- source.blend output-directory.
Exports approved mesh/rig/image data only. CC0 upstream provenance is in Docs/Art.
Original sole-target gait candidate; exported movement still needs runtime visual review.
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
# Original offline animation, using the existing rig and measured sole targets.
sys.dont_write_bytecode=True
sys.path.insert(0,str(Path(__file__).resolve().parent))
from author_horse_gaits import author_gaits
gait_report=author_gaits(arm,body)
(out/'gait-metrics.json').write_text(json.dumps(gait_report,indent=2)+'\n')
for o in list(bpy.context.view_layer.objects):o.select_set(False)
for o in meshes+[arm,root]:o.select_set(True)
bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=str(out/'RodeoHorse.fbx'),use_selection=True,object_types={'MESH','ARMATURE','EMPTY'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,bake_anim_step=.5,path_mode='STRIP',use_custom_props=False)
report={'source':Path(source).name,'heightMetres':2.12,'bodyVertices':len(body.data.vertices),'bones':len(arm.data.bones),'clips':['Idle','Walk','Gallop'],'animationQuality':'Original sole-target gait candidate; Unity import, torso/tack integration, foot orientation and visual acceptance pending','autoExecuteScripts':False,'blender':bpy.app.version_string}
(out/'source-inspection.json').write_text(json.dumps(report,indent=2)+'\n')
print('HORSE_EXPORT',json.dumps(report))
