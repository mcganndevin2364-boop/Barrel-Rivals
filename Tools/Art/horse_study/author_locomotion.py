"""Original idle/trot/gallop/sprint studies on the fitted 39-bone horse.
Preserves the existing mesh, rig, materials and active walk action. Exports
animation FBXs with a tiny bind-reference mesh, never object root motion.
Timing is authored for this arcade character, not motion-captured biology.
"""
import bpy, sys, math, json, hashlib
from pathlib import Path
from mathutils import Vector, Quaternion
args=sys.argv[sys.argv.index('--')+1:]
source=Path(args[0]);out=Path(args[1]);out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source),use_scripts=False)
rig=bpy.data.objects['HeroHorseRig'];body=bpy.data.objects['HeroHorseBody'];scene=bpy.context.scene
original=rig.animation_data.action;scene.render.fps=30
specs=[
 dict(name='Idle',frames=120,speed=0,duty=1,lift=0,compression=.022,bob=.002,offsets={'HL':0,'HR':0,'FL':0,'FR':0}),
 dict(name='Trot',frames=18,speed=3.5,duty=.45,lift=.14,compression=.120,bob=.012,offsets={'HL':0,'HR':.5,'FL':.5,'FR':0}),
 dict(name='Gallop',frames=18,speed=6,duty=.28,lift=.24,compression=.145,bob=.025,offsets={'HL':0,'HR':.12,'FL':.37,'FR':.52}),
 dict(name='Sprint',frames=13,speed=12,duty=.215,lift=.28,compression=.195,bob=.027,offsets={'HL':0,'HR':.19,'FL':.39,'FR':.60}),
]
limbs=[]
for family in ('Fore','Hind'):
 for side in ('L','R'):
  name=lambda part:family+part+'.'+side
  hoof=name('Hoof');upper=name('arm' if family=='Fore' else 'Shin');lower=name('Cannon');pastern=name('Pastern')
  group=body.vertex_groups[hoof].index
  rigid=[v.co.copy() for v in body.data.vertices if any(g.group==group and g.weight>.99999 for g in v.groups)]
  low=min(p.z for p in rigid);sole=[p for p in rigid if p.z<low+.008];center=sum(sole,Vector())/len(sole);toe=max(sole,key=lambda p:p.y)
  a,b=rig.data.bones[upper],rig.data.bones[lower]
  limbs.append(dict(label=('F' if family=='Fore' else 'H')+side,family=family,side=side,hoof=hoof,upper=upper,lower=lower,pastern=pastern,
   rigid=rigid,low=low,center=center,toe=toe,restA=a.tail_local-a.head_local,restB=b.tail_local-b.head_local,rotationA=a.matrix_local.to_quaternion(),rotationB=b.matrix_local.to_quaternion(),
   lengthA=a.length,lengthB=b.length,pasternVector=rig.data.bones[pastern].tail_local-rig.data.bones[pastern].head_local))

def smooth(x):
 x=max(0,min(1,x));return x*x*(3-2*x)
def reset():
 for pb in rig.pose.bones:
  pb.rotation_mode='QUATERNION';pb.rotation_quaternion=Quaternion();pb.location=(0,0,0);pb.scale=(1,1,1)
def axis_angle(name,axis,value):
 b=rig.data.bones[name];rig.pose.bones[name].rotation_quaternion=Quaternion(b.matrix_local.to_quaternion().inverted()@Vector(axis),value)
def rotation(pb,q):
 m=q.to_matrix().to_4x4();m.translation=pb.head;pb.matrix=m;bpy.context.view_layer.update()
def solve(limb,target):
 a,b=limb['lengthA'],limb['lengthB'];upper=rig.pose.bones[limb['upper']];start=upper.head.copy();delta=target-start
 distance=delta.length;reach=max(abs(a-b)+.00001,min(a+b-.00001,distance));direction=delta.normalized()
 pole=Vector((0,1 if limb['family']=='Fore' else -1,0));pole-=direction*pole.dot(direction);pole.normalize()
 along=(a*a-b*b+reach*reach)/(2*reach);joint=start+direction*along+pole*math.sqrt(max(0,a*a-along*along));end=start+direction*reach
 rotation(upper,limb['restA'].rotation_difference(joint-start)@limb['rotationA'])
 rotation(rig.pose.bones[limb['lower']],limb['restB'].rotation_difference(end-joint)@limb['rotationB'])
 return abs(reach-distance)
def target(limb,spec,phase):
 if spec['speed']==0:return limb['center']+Vector((0,0,.004-limb['low'])),0,True,0
 duty=spec['duty'];cycle=(phase-spec['offsets'][limb['label']])%1;sweep=spec['speed']*spec['frames']/30*duty
 if cycle<duty:fore=sweep*(.5-cycle/duty);lift=0
 else:
  t=(cycle-duty)/(1-duty)
  # Match planted velocity on entering/leaving swing; smooth reversal in between.
  tangent=-sweep*(1-duty)/duty;edge=.065;over=-tangent*edge*.5
  if t<edge:
   u=t/edge;fore=-sweep/2+tangent*edge*(u-u*u*.5)
  elif t>1-edge:
   u=(t-(1-edge))/edge;fore=sweep/2+over+tangent*edge*u*u*.5
  else:
   u=(t-edge)/(1-2*edge);fore=-sweep/2-over+(sweep+2*over)*smooth(u)
  lift=spec['lift']*(4*t*(1-t))**1.5
 p=limb['center'].copy();p.y+=fore;p.z+=.004-limb['low']+lift
 return p,cycle,cycle<duty,fore

def hoof_pose(limb,spec,p,cycle,contact):
 duty=spec['duty'];idle=spec['speed']==0;roll_start=duty*.72
 if idle:pitch=0;toe_weight=0
 elif contact:pitch=-math.radians(18)*smooth((cycle-roll_start)/(duty-roll_start));toe_weight=1 if cycle>=roll_start else 0
 else:
  t=(cycle-duty)/(1-duty);pitch=-math.radians(18)*(1-smooth(t/.22))-math.radians(38 if spec['name'] in ('Gallop','Sprint') else 26)*math.sin(math.pi*t)**2
  toe_weight=1-smooth(t/.35)
 q=Quaternion((1,0,0),pitch);qp=Quaternion((1,0,0),pitch*.45);anchor=limb['center'].lerp(limb['toe'],toe_weight)
 desired=p+(anchor-limb['center']);lowest=min((q@(v-anchor)).z for v in limb['rigid']);desired.z=p.z-(limb['center'].z-limb['low'])-lowest
 head=desired+q@(rig.data.bones[limb['hoof']].head_local-anchor)
 return head,q,qp,pitch

def pose(spec,phase):
 reset();name=spec['name'];wave=math.tau*phase
 root=rig.pose.bones['Root'];z=-spec['compression']+spec['bob']*math.cos(wave*(2 if name=='Trot' else 1)-.9*math.tau)
 root.location=rig.data.bones['Root'].matrix_local.to_3x3().inverted()@Vector((0,0,z))
 if name=='Idle':
  axis_angle('NeckLower',(1,0,0),.003*math.sin(wave));axis_angle('NeckUpper',(1,0,0),-.004*math.sin(wave))
  axis_angle('Head',(1,0,0),.004*math.sin(wave+.4))
 else:
  running=name in ('Gallop','Sprint');pitch=(.028 if running else .007)*math.sin(wave*(1 if running else 2)-.6)
  axis_angle('Pelvis',(1,0,0),pitch);axis_angle('Spine',(1,0,0),-pitch*.4);axis_angle('Chest',(1,0,0),-pitch*.35)
  axis_angle('NeckLower',(1,0,0),(-.08 if running else -.015)+.025*math.sin(wave-.3))
  axis_angle('NeckUpper',(1,0,0),.05 if running else .008)
  axis_angle('Head',(1,0,0),.015*math.sin(wave))
 for i in range(4):axis_angle('GroomMane.'+str(i),(0,1,0),-math.radians((1+math.sin(wave+i*.55))*(.6 if name=='Idle' else 2.1)))
 axis_angle('GroomTail',(0,1,0),math.radians((1.2 if name=='Idle' else 5)*math.sin(wave+.5)))
 bpy.context.view_layer.update();targets=[]
 for limb in limbs:
  p,cycle,contact,fore=target(limb,spec,phase);targets.append((limb,p,cycle,contact))
  if limb['family']=='Fore':
   axis_angle('ForeShoulder.'+limb['side'],(1,0,0),fore*.55);axis_angle('ForeUpper.'+limb['side'],(1,0,0),fore*.25)
  else:axis_angle('HindThigh.'+limb['side'],(1,0,0),fore*.65)
 bpy.context.view_layer.update();reach=0;minimum=10
 for limb,p,cycle,contact in targets:
  head,q,qp,pitch=hoof_pose(limb,spec,p,cycle,contact)
  reach=max(reach,solve(limb,head-qp@limb['pasternVector']))
  rotation(rig.pose.bones[limb['pastern']],qp@rig.data.bones[limb['pastern']].matrix_local.to_quaternion())
  rotation(rig.pose.bones[limb['hoof']],q@rig.data.bones[limb['hoof']].matrix_local.to_quaternion())
  skin=rig.pose.bones[limb['hoof']].matrix@rig.data.bones[limb['hoof']].matrix_local.inverted()
  minimum=min(minimum,min((skin@v).z for v in limb['rigid']))
 translation=max(pb.location.length for pb in rig.pose.bones if pb.name!='Root')
 return reach,minimum,translation

reports=[]
# A skin binding carrier preserves rest matrices and the FBX model root across
# Blender/Unity. It is never instantiated with the rendered horse. Exporting a
# bare armature loses its bind pose and Unity collapses the extra root node.
vertices=[];faces=[]
for bone in rig.data.bones:
 start=len(vertices);p=bone.head_local
 vertices.extend([tuple(p),tuple(p+Vector((.001,0,0))),tuple(p+Vector((0,.001,0)))])
 faces.append((start,start+1,start+2))
mesh=bpy.data.meshes.new('Animation bind reference');mesh.from_pydata(vertices,[],faces)
carrier=bpy.data.objects.new('AnimationBindReference',mesh);scene.collection.objects.link(carrier)
for i,bone in enumerate(rig.data.bones):carrier.vertex_groups.new(name=bone.name).add([i*3,i*3+1,i*3+2],1,'REPLACE')
modifier=carrier.modifiers.new('Export bind pose','ARMATURE');modifier.object=rig
for spec in specs:
 action=bpy.data.actions.new(spec['name']);action.use_fake_user=True;rig.animation_data.action=action
 maximum_reach=0;minimum=10;translation=0
 for i in range(spec['frames']*4+1):
  phase=i/(spec['frames']*4);frame=1+i/4
  reach,low,shift=pose(spec,phase);maximum_reach=max(maximum_reach,reach);minimum=min(minimum,low);translation=max(translation,shift)
  for pb in rig.pose.bones:
   pb.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=pb.name);pb.keyframe_insert(data_path='location',frame=frame,group=pb.name)
 curves=list(action.fcurves) if action.is_action_legacy else [c for layer in action.layers for strip in layer.strips for bag in strip.channelbags for c in bag.fcurves]
 for curve in curves:
  for key in curve.keyframe_points:key.interpolation='LINEAR'
 row=dict(spec,maximumReachClampingM=maximum_reach,minimumRigidHoofHeightM=minimum,maximumNonRootTranslationM=translation)
 reports.append(row);print('GAIT_AUTHORED',json.dumps(row),flush=True)
 # Keep a diagnostic source on failure rather than quietly changing the acceptance tolerance.
 (out/'locomotion-authoring.json').write_text(json.dumps({'gaits':reports,'scope':'Original authored studies, not biomechanical or device acceptance.'},indent=2)+'\n')
 if maximum_reach>.002 or minimum<-.001 or translation>1e-5:
  bpy.ops.wm.save_as_mainfile(filepath=str(out/('Diagnostic-'+spec['name']+'.blend')))
  raise RuntimeError('Gait contact/reach gate failed: '+spec['name'])
 scene.frame_start=1;scene.frame_end=spec['frames']+1;scene.frame_set(1)
 for o in bpy.context.selected_objects:o.select_set(False)
 rig.select_set(True);carrier.select_set(True);bpy.context.view_layer.objects.active=rig
 bpy.ops.export_scene.fbx(filepath=str(out/('HeroHorse-'+spec['name']+'.fbx')),use_selection=True,object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,
  bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_step=.25,bake_anim_simplify_factor=0,path_mode='STRIP',use_custom_props=False)
bpy.data.objects.remove(carrier,do_unlink=True);bpy.data.meshes.remove(mesh)
rig.animation_data.action=original;scene.frame_start=1;scene.frame_end=29;scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(out/'HeroHorse-Locomotion.blend'))
report={'sourceSha256':hashlib.sha256(source.read_bytes()).hexdigest(),'blender':bpy.app.version_string,'gaits':reports,'geometryOrRestRigChanged':False,'objectRootMotion':False,'scope':'Original arcade locomotion studies. Not motion capture, production biomechanics or device acceptance.'}
(out/'locomotion-authoring.json').write_text(json.dumps(report,indent=2)+'\n')
print('LOCOMOTION_AUTHORED',flush=True)
