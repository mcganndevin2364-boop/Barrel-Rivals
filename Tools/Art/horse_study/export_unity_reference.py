import bpy,json,sys,hashlib,struct
from pathlib import Path
args=sys.argv[sys.argv.index('--')+1:];source=Path(args[0]);out=Path(args[1]);out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source),use_scripts=False)
rig=bpy.data.objects['HeroHorseRig'];rig.data.pose_position='REST';bpy.context.view_layer.update()
# Canonical game direction after the reviewed FBX import + 180-degree art turn.
# Unity performs the handedness change inside imported mesh coordinates.
# After the measured 180-degree art turn, canonical metres are (x,z,y).
def canonical(p):return (p.x,p.z,p.y)
with (out/'UnitySurfaceReference.bytes').open('wb') as f:
 f.write(b'BRHCOL02');f.write(struct.pack('<i',2))
 for name,attribute in [('HeroHorseBody','CoatColor'),('HeroHorseEyes','EyeColor')]:
  o=bpy.data.objects[name];namebytes=name.encode();f.write(struct.pack('<i',len(namebytes)));f.write(namebytes);f.write(struct.pack('<i',len(o.data.vertices)))
  for v,c in zip(o.data.vertices,o.data.color_attributes[attribute].data):f.write(struct.pack('<7f',*canonical(o.matrix_world@v.co),*c.color))
body=bpy.data.objects['HeroHorseBody'];rig.data.pose_position='POSE'
with (out/'UnitySurfaceReference.bytes').open('ab') as f:
 frames=list(range(0,28,4));f.write(struct.pack('<ii',len(frames),len(body.data.vertices)))
 for frame in frames:
  bpy.context.scene.frame_set(1+frame);bpy.context.view_layer.update()
  evaluated=body.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=evaluated.to_mesh()
  f.write(struct.pack('<f',frame/30))
  for v in mesh.vertices:f.write(struct.pack('<3f',*canonical(body.matrix_world@v.co)))
  evaluated.to_mesh_clear()
(out/'UnitySurfaceReference.json').write_text(json.dumps({'format':'BRHCOL02; little-endian int32 mesh count, name UTF-8 byte count/name, vertex count, float32 x/y/z/r/g/b/a per vertex. Then int32 pose count/body vertex count; for each pose float32 seconds followed by x/y/z float32 for every body vertex. Coordinates are canonical game metres, not renderer-local import units.','sourceBlendSha256':hashlib.sha256(source.read_bytes()).hexdigest(),'referenceSha256':hashlib.sha256((out/'UnitySurfaceReference.bytes').read_bytes()).hexdigest(),'animatedBodySamples':7,'sourceVertices':{'HeroHorseBody':19502,'HeroHorseEyes':1444},'scope':'Independent neutral surface and linear color reference for Unity import; alpha is a bare-surface mask, never opacity.'},indent=2)+'\n')
