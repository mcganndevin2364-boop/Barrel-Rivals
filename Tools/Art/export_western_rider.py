"""Author a CC0-derived western rider; Blender 4.5 with --disable-autoexec.
Arguments after --: MPFB source root, system asset root, shoe asset root, output directory.
The MPFB tool is GPLv3; its generated assets are CC0. See rider provenance.
This original orchestration script does not bundle the MPFB program.
"""
import sys, json, math, bmesh
from pathlib import Path
import bpy
from mathutils import Vector

tool_root,system_root,shoe_root,work=map(lambda p:Path(p).resolve(),sys.argv[sys.argv.index('--')+1:])
work.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(tool_root/'src'))
import mpfb
# Keep this source-only authoring session's preferences/cache in the workspace.
mpfb.get_preference=lambda key: str(work/'mpfb-user') if key=='mpfb_user_data' else None
bpy.utils.extension_path_user=lambda *args,**kwargs:str(work/'mpfb-user')
mpfb.register()
from mpfb.services.humanservice import HumanService
from mpfb.services.targetservice import TargetService
from mpfb.services.exportservice import ExportService

bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
macro=TargetService.get_default_macro_info_dict()
macro.update(gender=.15,age=.40,muscle=.60,weight=.46,height=.50,proportions=.60)
human=HumanService.create_human(macro_detail_dict=macro)
human.name='Rider skin'
assetroot=system_root
HumanService.set_character_skin(str(assetroot/'skins/young_caucasian_female/young_caucasian_female.mhmat'),human,skin_type='GAMEENGINE')
rig=HumanService.add_builtin_rig(human,'game_engine');rig.name='Rider armature'
for subdir,name,kind in [('clothes','male_casualsuit03','Clothes'),('clothes','toigo_ankle_boots_male','Clothes'),('eyes','low-poly','Eyes'),('hair','ponytail01','Hair')]:
    root=shoe_root if name=='toigo_ankle_boots_male' else assetroot
    path=root/subdir/name/(name+'.mhclo')
    HumanService.add_mhclo_asset(str(path),human,asset_type=kind,subdiv_levels=0,material_type='GAMEENGINE')
TargetService.bake_targets(human)
ExportService.bake_modifiers_remove_helpers(human,bake_masks=True,bake_subdiv=False,remove_helpers=True,also_proxy=True)
# The race already has equipped gloves. Retain only exposed neck/head skin;
# don't send thousands of hidden hand/body triangles through mobile skinning.
bm=bmesh.new();bm.from_mesh(human.data)
neck_min=rig.data.bones['neck_01'].head_local.z-.18
relative=rig.matrix_world.inverted()@human.matrix_world
bmesh.ops.delete(bm,geom=[v for v in bm.verts if (relative@v.co).z<neck_min],context='VERTS')
bm.to_mesh(human.data);bm.free();human.data.update()
# Original compact felt cowboy hat: curled brim, creased crown and a leather band.
verts=[];faces=[];sides=48
top=max((relative@v.co).z for v in human.data.vertices)
cy=rig.data.bones['head'].tail_local.y+.012
def ring(rx,ry,z,curl=0,crease=0):
    start=len(verts)
    for i in range(sides):
        a=i*2*math.pi/sides;x=math.cos(a);y=math.sin(a)
        verts.append((rx*x,cy+ry*y,z+curl*x*x-crease*math.exp(-x*x*18)))
    if start:
        for i in range(sides):j=(i+1)%sides;faces.append((start-sides+i,start-sides+j,start+j,start+i))
ring(.185,.197,top-.095,.035)
ring(.184,.196,top-.090,.035)
ring(.101,.125,top-.079)
ring(.096,.119,top+.012,0,.004)
ring(.090,.112,top+.070,0,.020)
ring(.068,.090,top+.079,0,.021)
faces.append(tuple(range(len(verts)-sides,len(verts))))
mesh=bpy.data.meshes.new('Original felt cowboy hat');mesh.from_pydata(verts,[],faces);mesh.update()
hat=bpy.data.objects.new('Original cowboy hat',mesh);bpy.context.collection.objects.link(hat);hat.parent=rig
group=hat.vertex_groups.new(name='head');group.add(list(range(len(verts))),1,'REPLACE')
mod=hat.modifiers.new('Rider skinning','ARMATURE');mod.object=rig
material=bpy.data.materials.new('Dark brown felt');material.use_nodes=True
bsdf=material.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Base Color'].default_value=(.055,.027,.014,1);bsdf.inputs['Roughness'].default_value=.88
hat.data.materials.append(material)
# Only exported deforming surfaces, no body-part helper cages or authoring modifiers.
objects=[rig]+list(rig.children_recursive)
for obj in objects:
    if obj.type=='MESH':
        obj.data.calc_loop_triangles()
        for p in obj.data.polygons:p.use_smooth=True
report={'tool':'MPFB 2.0.17 / Blender '+bpy.app.version_string,'macro':macro,
 'objects':[{'name':o.name,'vertices':len(o.data.vertices),'triangles':len(o.data.loop_triangles),'materials':[m.name for m in o.data.materials]} for o in objects if o.type=='MESH'],
 'bones':{b.name:{'head':list(b.head_local),'tail':list(b.tail_local),'parent':b.parent.name if b.parent else None} for b in rig.data.bones}}
(work/'candidate-inspection.json').write_text(json.dumps(report,indent=2)+'\n')
bpy.ops.wm.save_as_mainfile(filepath=str(work/'Rider-candidate.blend'))
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(work/'Rider-candidate.fbx'),use_selection=True,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='COPY')
print('RIDER_CANDIDATE_READY',report['objects'])
