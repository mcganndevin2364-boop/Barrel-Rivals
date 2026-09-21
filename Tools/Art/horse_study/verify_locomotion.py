"""Independent preservation, loop, between-key sole and animation FBX checks."""
import bpy,sys,json,hashlib,math
import numpy as np
from pathlib import Path
source,folder=map(Path,sys.argv[sys.argv.index('--')+1:])
def curves(action):
 return list(action.fcurves) if action.is_action_legacy else [c for l in action.layers for s in l.strips for b in s.channelbags for c in b.fcurves]
def signature():
 data={}
 for o in bpy.data.objects:
  if o.type=='MESH':
   data[o.name]=dict(vertices=[list(v.co) for v in o.data.vertices],faces=[list(p.vertices) for p in o.data.polygons],skin=[[(o.vertex_groups[g.group].name,g.weight) for g in v.groups] for v in o.data.vertices],uv=[[list(v.uv) for v in layer.data] for layer in o.data.uv_layers])
 rig=bpy.data.objects['HeroHorseRig'];data['rig']=[(b.name,b.parent.name if b.parent else None,[list(r) for r in b.matrix_local]) for b in rig.data.bones]
 a=rig.animation_data.action;data['walk']=[(c.data_path,c.array_index,[(list(k.co),k.interpolation) for k in c.keyframe_points]) for c in curves(a)]
 data['atlases']=[(i.name,hashlib.sha256(bytes(i.packed_file.data)).hexdigest()) for i in bpy.data.images if i.packed_file]
 return hashlib.sha256(json.dumps(data,sort_keys=True).encode()).hexdigest()
def points(obj):
 ev=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());m=ev.to_mesh();p=np.array([list(obj.matrix_world@v.co) for v in m.vertices]);ev.to_mesh_clear();return p
bpy.ops.wm.open_mainfile(filepath=str(source),use_scripts=False);before=signature()
bpy.ops.wm.open_mainfile(filepath=str(folder/'HeroHorse-Locomotion.blend'),use_scripts=False);after=signature()
assert before==after,'Original geometry/rest/weights/UV/Walk/atlas changed'
rig=bpy.data.objects['HeroHorseRig'];body=bpy.data.objects['HeroHorseBody'];modifier=next(m for m in body.modifiers if m.type=='ARMATURE');original=rig.animation_data.action
hoof_ids=[]
for v in body.data.vertices:
 if any('Hoof.' in body.vertex_groups[g.group].name and g.weight>.99999 for g in v.groups):hoof_ids.append(v.index)
report={'preservedSourceSignature':before,'originalPreserved':True,'gaits':[],'scope':'Sampled Blender contact/loop/export checks; not biomechanics, continuous collision or phone acceptance.'}
for spec in json.loads((folder/'locomotion-authoring.json').read_text())['gaits']:
 rig.animation_data.action=bpy.data.actions[spec['name']];frames=[1+i/8 for i in range(spec['frames']*8+1)];expected=[];low=10
 for frame in frames:
  bpy.context.scene.frame_set(int(frame),subframe=frame%1);p=points(body);expected.append(p);low=min(low,float(p[hoof_ids,2].min()))
 loop=float(np.linalg.norm(expected[0]-expected[-1],axis=1).max());assert loop<1e-5,(spec['name'],loop)
 assert low>-.001,(spec['name'],low)
 prior=set(bpy.data.objects)
 bpy.ops.import_scene.fbx(filepath=str(folder/('HeroHorse-'+spec['name']+'.fbx')),use_anim=True,anim_offset=0)
 imported=next(o for o in bpy.data.objects if o not in prior and o.type=='ARMATURE');modifier.object=imported;error=0
 for frame,p in zip(frames,expected):
  bpy.context.scene.frame_set(int(frame),subframe=frame%1);error=max(error,float(np.linalg.norm(points(body)-p,axis=1).max()))
 assert error<.0001,(spec['name'],'FBX mismatch',error)
 modifier.object=rig
 for o in list(bpy.data.objects):
  if o not in prior:bpy.data.objects.remove(o,do_unlink=True)
 row={'name':spec['name'],'samples':len(frames),'minimumRigidHoofHeightM':low,'loopMaximumVertexErrorM':loop,'fbxMaximumVertexErrorM':error};report['gaits'].append(row);print('VERIFIED',json.dumps(row),flush=True)
 (folder/'locomotion-verification.json').write_text(json.dumps(report,indent=2)+'\n')
rig.animation_data.action=original
print('LOCOMOTION_VERIFIED',flush=True)
