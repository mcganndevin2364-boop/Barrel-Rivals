"""Original CC0 MakeHuman-derived glove study; run with Blender --disable-autoexec.
Args: MPFB source root, new output directory. Exported coordinates are metres,
x toward thumb, y dorsal, z toward fingers. No authoring program code is shipped.
"""
import sys, json, math, bmesh
from pathlib import Path
import bpy
from mathutils import Vector, Quaternion, Matrix
from mathutils.bvhtree import BVHTree
source, output=map(lambda p:Path(p).resolve(),sys.argv[sys.argv.index('--')+1:])
output.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(source/'src'))
import mpfb
mpfb.get_preference=lambda key: str(output/'mpfb-user') if key=='mpfb_user_data' else None
bpy.utils.extension_path_user=lambda *args,**kwargs:str(output/'mpfb-user')
mpfb.register()
from mpfb.services.humanservice import HumanService
from mpfb.services.targetservice import TargetService
from mpfb.services.exportservice import ExportService
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
macro=TargetService.get_default_macro_info_dict()
macro.update(gender=.15,age=.40,muscle=.60,weight=.46,height=.50,proportions=.60)
human=HumanService.create_human(macro_detail_dict=macro)
rig=HumanService.add_builtin_rig(human,'game_engine')
TargetService.bake_targets(human)
ExportService.bake_modifiers_remove_helpers(human,bake_masks=True,bake_subdiv=False,remove_helpers=True,also_proxy=True)
bpy.context.view_layer.update()
bones=rig.data.bones
wrist=rig.matrix_world@bones['hand_l'].head_local
index=rig.matrix_world@bones['index_01_l'].head_local
pinky=rig.matrix_world@bones['pinky_01_l'].head_local
middle=rig.matrix_world@bones['middle_01_l'].head_local
forward=(middle-wrist).normalized()
across=(index-pinky);across=(across-forward*across.dot(forward)).normalized()
up=-forward.cross(across).normalized()
def local(point):
 p=point-wrist
 return Vector((p.dot(across),p.dot(up),p.dot(forward)))*1.10

def pose(finger,angles):
 for segment,angle in enumerate(angles,1):
  pb=rig.pose.bones[f'{finger}_{segment:02d}_l']
  axis=pb.bone.matrix_local.to_3x3().inverted()@(rig.matrix_world.to_3x3().inverted()@across)
  pb.rotation_mode='QUATERNION';pb.rotation_quaternion=Quaternion(axis,math.radians(angle))

def surface(name):
 bpy.context.view_layer.update()
 evaluated=human.evaluated_get(bpy.context.evaluated_depsgraph_get())
 mesh=bpy.data.meshes.new_from_object(evaluated)
 for v in mesh.vertices:v.co=local(evaluated.matrix_world@v.co)
 bm=bmesh.new();bm.from_mesh(mesh)
 bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.co.length>.30],context='VERTS')
 bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-6,plane_co=(0,0,-.045),plane_no=(0,0,1),clear_inner=True,clear_outer=False)
 # Remove isolated fragments from the coarse region cut; the connected hand remains.
 bm.verts.ensure_lookup_table();visited=set();components=[]
 for seed in bm.verts:
  if seed in visited:continue
  comp=set([seed]);stack=[seed];visited.add(seed)
  while stack:
   v=stack.pop()
   for edge in v.link_edges:
    n=edge.other_vert(v)
    if n not in visited:visited.add(n);comp.add(n);stack.append(n)
  components.append(comp)
 keep=max(components,key=len)
 bmesh.ops.delete(bm,geom=[v for v in bm.verts if v not in keep],context='VERTS')
 bmesh.ops.reverse_faces(bm,faces=list(bm.faces))
 bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
 # A thin leather shell softens nail borders and small skin folds.
 for _ in range(2):
  targets={v:sum((e.other_vert(v).co for e in v.link_edges),Vector())/len(v.link_edges) for v in bm.verts if len(v.link_edges)>2 and not v.is_boundary}
  for v,p in targets.items():v.co=v.co.lerp(p,.18)
 bm.normal_update()
 for v in bm.verts:v.co+=v.normal*.0015
 # Continue the existing wrist rim into a short rolled leather cuff.
 boundary=[e for e in bm.edges if e.is_boundary and all(v.co.z<-.042 for v in e.verts)]
 for dz,radial in [(-.003,.0012),(-.005,0),(-.002,-.0012)]:
  result=bmesh.ops.extrude_edge_only(bm,edges=boundary)
  fresh=[v for v in result['geom'] if isinstance(v,bmesh.types.BMVert)]
  for v in fresh:
   v.co.z+=dz
   direction=Vector((v.co.x,v.co.y,0)).normalized();v.co+=direction*radial
  fresh_set=set(fresh);boundary=[e for e in result['geom'] if isinstance(e,bmesh.types.BMEdge) and all(v in fresh_set for v in e.verts)]
 bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
 bm.to_mesh(mesh);bm.free();mesh.update()
 obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
 for p in mesh.polygons:p.use_smooth=True
 mesh.calc_loop_triangles()
 original_points=[v.co.copy() for v in mesh.vertices]
 original=BVHTree.FromPolygons(original_points,[tuple(t.vertices) for t in mesh.loop_triangles],all_triangles=True)
 bpy.context.view_layer.objects.active=obj
 reduction=obj.modifiers.new('Bounded glove density','DECIMATE');reduction.ratio=.40 if name.startswith('Closed') else .68
 bpy.ops.object.modifier_apply(modifier=reduction.name)
 mesh=obj.data
 for polygon in mesh.polygons:polygon.use_smooth=True
 mesh.calc_loop_triangles()
 reduced=BVHTree.FromPolygons([v.co for v in mesh.vertices],[tuple(t.vertices) for t in mesh.loop_triangles],all_triangles=True)
 deviation=max(max(original.find_nearest(v.co)[3] for v in mesh.vertices),max(reduced.find_nearest(v)[3] for v in original_points))
 print('REDUCTION',name,len(mesh.loop_triangles),deviation)
 assert deviation<.003,'Glove reduction moved sampled surface over 3mm'

 bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.20,island_margin=.02,scale_to_bounds=True);bpy.ops.object.mode_set(mode='OBJECT')
 mesh.calc_loop_triangles()
 # Export split UV vertices so Unity has the exact original seam normals.
 verts=[];normals=[];uv=[];indices=[];lookup={}
 for tri in mesh.loop_triangles:
  for loop in tri.loops:
   vert=mesh.vertices[mesh.loops[loop].vertex_index]
   tex=mesh.uv_layers.active.data[loop].uv
   key=(vert.index,round(tex.x,7),round(tex.y,7))
   if key not in lookup:
    lookup[key]=len(verts)
    verts.append(dict(zip(('x','y','z'),vert.co)));normals.append(dict(zip(('x','y','z'),vert.normal)));uv.append({'x':tex.x,'y':tex.y})
   indices.append(lookup[key])
 record={'name':name,'vertices':verts,'normals':normals,'uv':uv,'triangles':indices,'sourceVertexCount':len(mesh.vertices),'sourceTriangles':len(mesh.loop_triangles),'maximumSampledReductionErrorMetres':deviation}
 (output/(name+'.json')).write_text(json.dumps(record,separators=(',',':'))+'\n')
 return obj

for f in ['index','middle','ring','pinky']:pose(f,[-6,-10,-5])
opened=surface('Open riding glove')
for f in ['index','middle','ring','pinky']:pose(f,[-45,-65,-25])
# Thumb adduction/opposition uses its rest joint frame; inspect before accepting.
pose('thumb',[0,-35,-22])
bpy.context.view_layer.update()
# Oppose the thumb tip toward the forefinger around the rein. All rotations
# remain authoring-only; the game uses baked pose meshes, not a second hand rig.
point=wrist+(across*.026+up*(-.045)+forward*.099)/1.10
goal=rig.matrix_world.inverted()@point
for _ in range(18):
 for segment in [3,2]:
  pb=rig.pose.bones[f'thumb_{segment:02d}_l'];pivot=pb.head.copy()
  tip=rig.pose.bones['thumb_03_l'].tail.copy()
  rotation=(tip-pivot).rotation_difference(goal-pivot)
  pb.matrix=Matrix.Translation(pivot)@rotation.to_matrix().to_4x4()@Matrix.Translation(-pivot)@pb.matrix
  bpy.context.view_layer.update()
closed=surface('Closed riding glove')
for p in rig.pose.bones:p.rotation_quaternion=Quaternion()
bpy.context.view_layer.update()
report={'source':'MPFB 2.0.17 CC0 base mesh and game_engine weights','macro':macro,'units':'metres; x thumb, y dorsal, z fingers','shellThicknessMetres':.0015,'vertices':{o.name:len(o.data.vertices) for o in [opened,closed]},'wrist':list(wrist),'forward':list(forward),'across':list(across),'dorsal':list(up)}
(output/'inspection.json').write_text(json.dumps(report,indent=2)+'\n')
# Keep only two extracted surfaces in the review scene.
for o in list(bpy.data.objects):
 if o not in [opened,closed]:bpy.data.objects.remove(o,do_unlink=True)
opened.location.x=-.15;closed.location.x=.15
bpy.ops.wm.save_as_mainfile(filepath=str(output/'Glove-study.blend'))
print('GLOVE_EXPORT',report)
