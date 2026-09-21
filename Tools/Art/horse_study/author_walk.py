# Authoring notes
# The lowest original sole ring remains a rigid plane under each hoof bone.
# A specific original toe surface vertex is a reproducible roll pivot.
# Keep the lowest actual rigid hoof vertex at the requested ground/lift
# height even as the heel rolls up. Never substitute a bone tip for contact.
# Small original weight-transfer accents, not motion-captured biomechanics.

"""Original sole-plane-controlled walk study for the fitted 34-bone horse.
Offline rotation IK, measured toe-pivot roll, geodesically refined skin.
No constraints, bone scale, runtime solver or object root motion.
The signed Core race root is not represented or changed by this art study.
"""
import bpy, sys, json, math, hashlib
from pathlib import Path
from mathutils import Vector, Quaternion
folder = Path(sys.argv[sys.argv.index('--') + 1])
out = Path(sys.argv[sys.argv.index('--') + 2])
out.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(folder / 'HeroHorse-RigStudy.blend'), use_scripts=False)
arm = bpy.data.objects['HeroHorseRig']
body = bpy.data.objects['HeroHorseBody']
scene = bpy.context.scene
scene.render.fps = 30
frames = 28
speed = 1.5
duration = frames / 30
duty = 0.62
clearance = 0.075
limbs = []
for family in ('Fore', 'Hind'):
    for side in ('L', 'R'):

        def name(part):
            return family + part + '.' + side
        hoof = name('Hoof')
        upper = name('arm') if family == 'Fore' else name('Shin')
        lower = name('Cannon')
        pastern = name('Pastern')
        group = body.vertex_groups[hoof].index
        rigid = [v for v in body.data.vertices if any((g.group == group and g.weight > 0.99999 for g in v.groups))]
        low = min((v.co.z for v in rigid))
        sole = [v for v in rigid if v.co.z < low + 0.008]
        center = sum((v.co for v in sole), Vector()) / len(sole)
        (a, b) = (arm.data.bones[upper], arm.data.bones[lower])
        limbs.append(dict(label=('F' if family == 'Fore' else 'H') + side, family=family, side=side, upper=upper, lower=lower, pastern=pastern, hoof=hoof, restA=a.tail_local - a.head_local, restB=b.tail_local - b.head_local, rotationA=a.matrix_local.to_quaternion(), rotationB=b.matrix_local.to_quaternion(), lengthA=a.length, lengthB=b.length, soleIndices=[v.index for v in sole], rigidIndices=[v.index for v in rigid], restCenter=center, lowest=low, hoofOffset=center - arm.data.bones[hoof].head_local, pasternVector=arm.data.bones[pastern].tail_local - arm.data.bones[pastern].head_local))
for limb in limbs:
    points = [body.data.vertices[i].co for i in limb['rigidIndices']]
    low = [p for p in points if p.z < limb['lowest'] + 0.008]
    toe = max(low, key=lambda p: p.y).copy()
    limb['toe'] = toe
    limb['toeIndex'] = min(limb['rigidIndices'], key=lambda i: (body.data.vertices[i].co - toe).length)
    limb['rigidRest'] = [p.copy() for p in points]

def smooth(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)

def hoof_pose(limb, p, contact, cycle):
    roll_start = 0.5
    if contact:
        pitch = -math.radians(12) * smooth(roll_start, duty, cycle)
        toe_weight = 1 if cycle >= roll_start else 0
        contact_kind = 'toe' if toe_weight else 'flat'
    else:
        t = (cycle - duty) / (1 - duty)
        pitch = -math.radians(12) * (1 - smooth(0, 0.22, t)) - math.radians(18) * math.sin(math.pi * t) ** 2
        toe_weight = 1 - smooth(0, 0.35, t)
        contact_kind = 'swing'
    q = Quaternion((1, 0, 0), pitch)
    qp = Quaternion((1, 0, 0), pitch * 0.45)
    anchor = limb['restCenter'].lerp(limb['toe'], toe_weight)
    desired_anchor = p + (anchor - limb['restCenter'])
    lowest = min(((q @ (v - anchor)).z for v in limb['rigidRest']))
    desired_anchor.z = p.z - (limb['restCenter'].z - limb['lowest']) - lowest
    head = desired_anchor + q @ (arm.data.bones[limb['hoof']].head_local - anchor)
    return (head, q, qp, contact_kind, anchor, desired_anchor, pitch)
offsets = {'HL': 0, 'FL': 0.25, 'HR': 0.5, 'FR': 0.75}

def reset():
    for pb in arm.pose.bones:
        pb.rotation_mode = 'QUATERNION'
        pb.rotation_quaternion = Quaternion()
        pb.location = (0, 0, 0)
        pb.scale = (1, 1, 1)

def rotation(pb, q):
    m = q.to_matrix().to_4x4()
    m.translation = pb.head
    pb.matrix = m
    bpy.context.view_layer.update()

def angle(name, value):
    b = arm.data.bones[name]
    arm.pose.bones[name].rotation_quaternion = Quaternion(b.matrix_local.to_quaternion().inverted() @ Vector((1, 0, 0)), value)

def solve(limb, target):
    (a, b) = (limb['lengthA'], limb['lengthB'])
    upper = arm.pose.bones[limb['upper']]
    start = upper.head.copy()
    delta = target - start
    distance = delta.length
    reach = max(abs(a - b) + 1e-05, min(a + b - 1e-05, distance))
    direction = delta.normalized()
    pole = Vector((0, 1 if limb['family'] == 'Fore' else -1, 0))
    pole -= direction * pole.dot(direction)
    pole.normalize()
    along = (a * a - b * b + reach * reach) / (2 * reach)
    joint = start + direction * along + pole * math.sqrt(max(0, a * a - along * along))
    end = start + direction * reach
    rotation(upper, limb['restA'].rotation_difference(joint - start) @ limb['rotationA'])
    rotation(arm.pose.bones[limb['lower']], limb['restB'].rotation_difference(end - joint) @ limb['rotationB'])
    return abs(reach - distance)

def target(limb, phase):
    cycle = (phase - offsets[limb['label']]) % 1
    sweep = speed * duration * duty
    if cycle < duty:
        fore = sweep * (0.5 - cycle / duty)
        lift = 0
    else:
        t = (cycle - duty) / (1 - duty)
        tangent = -sweep * (1 - duty) / duty
        edge = 0.045
        over = -tangent * edge * 0.5
        if t < edge:
            u = t / edge
            fore = -sweep / 2 + tangent * edge * (u - u * u * 0.5)
        elif t > 1 - edge:
            u = (t - (1 - edge)) / edge
            fore = sweep / 2 + over + tangent * edge * u * u * 0.5
        else:
            u = (t - edge) / (1 - 2 * edge)
            fore = -sweep / 2 - over + (sweep + 2 * over) * (u * u * (3 - 2 * u))
        lift = clearance * (4 * t * (1 - t)) ** 1.5
    p = limb['restCenter'].copy()
    p.y += fore
    p.z += 0.004 - limb['lowest'] + lift
    return (p, cycle < duty, cycle, fore)
maxreach = 0
samples = []
maxlocaltranslation = 0
minheight = 1000000000.0
maxstrain = 0
rest = [v.co.copy() for v in body.data.vertices]
edges = [tuple(e.vertices) for e in body.data.edges]
lengths = [(rest[a] - rest[b]).length for (a, b) in edges]
arm.animation_data_create()
action = bpy.data.actions.new('WalkStudy')
action.use_fake_user = True
arm.animation_data.action = action
for i in range(frames * 4 + 1):
    phase = i / (frames * 4)
    frame = 1 + i / 4
    reset()
    arm.pose.bones['Root'].location = arm.data.bones['Root'].matrix_local.to_3x3().inverted() @ Vector((0, 0, -0.087 + 0.008 * math.cos(phase * math.tau * 2)))
    sway = 0.006 * math.sin(phase * math.tau)
    arm.pose.bones['Root'].location += arm.data.bones['Root'].matrix_local.to_3x3().inverted() @ Vector((sway, 0, 0))
    for (name, amplitude) in [('Pelvis', 0.01), ('Spine', -0.004), ('Chest', -0.004)]:
        axis = arm.data.bones[name].matrix_local.to_quaternion().inverted() @ Vector((0, 1, 0))
        arm.pose.bones[name].rotation_quaternion = Quaternion(axis, amplitude * math.sin(phase * math.tau))
    angle('NeckLower', 0.023 * math.sin(phase * math.tau * 2))
    angle('NeckUpper', -0.018 * math.sin(phase * math.tau * 2 + 0.3))
    bpy.context.view_layer.update()
    targets = {}
    for limb in limbs:
        (p, contact, cycle, fore) = target(limb, phase)
        targets[limb['label']] = (p, contact, cycle)
        if limb['family'] == 'Fore':
            angle('ForeShoulder.' + limb['side'], fore * 0.55)
            angle('ForeUpper.' + limb['side'], fore * 0.25)
        else:
            angle('HindThigh.' + limb['side'], fore * 0.65)
    bpy.context.view_layer.update()
    for limb in limbs:
        (p, contact, cycle) = targets[limb['label']]
        (head, q, qp, kind, anchor, desired_anchor, pitch) = hoof_pose(limb, p, contact, cycle)
        limb['contactDetails'] = (kind, anchor, desired_anchor, pitch, q)
        fetlock = head - qp @ limb['pasternVector']
        maxreach = max(maxreach, solve(limb, fetlock))
        rotation(arm.pose.bones[limb['pastern']], qp @ arm.data.bones[limb['pastern']].matrix_local.to_quaternion())
        rotation(arm.pose.bones[limb['hoof']], q @ arm.data.bones[limb['hoof']].matrix_local.to_quaternion())
    for pb in arm.pose.bones:
        if pb.name != 'Root':
            maxlocaltranslation = max(maxlocaltranslation, pb.location.length)
        pb.keyframe_insert(data_path='rotation_quaternion', frame=frame, group=pb.name)
        pb.keyframe_insert(data_path='location', frame=frame, group=pb.name)
    obj = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = obj.to_mesh()
    coords = [v.co.copy() for v in mesh.vertices]
    row = {'phase': phase, 'hooves': {}}
    for limb in limbs:
        pts = [coords[n] for n in limb['soleIndices']]
        center = sum(pts, Vector()) / len(pts)
        rigid = [coords[n] for n in limb['rigidIndices']]
        (p, contact, cycle) = targets[limb['label']]
        (kind, anchor, desired_anchor, pitch, q) = limb['contactDetails']
        toe = coords[limb['toeIndex']]
        actual_anchor = toe if kind == 'toe' else center
        pivot_rest = limb['toe'] if kind == 'toe' else limb['restCenter']
        origin_error = coords[limb['rigidIndices'][0]] - q @ rest[limb['rigidIndices'][0]]
        rigidity_error = max(((coords[n] - q @ rest[n] - origin_error).length for n in limb['rigidIndices']))
        row['hooves'][limb['label']] = {'center': list(center), 'lowest': min((v.z for v in rigid)), 'contact': contact, 'cycle': cycle, 'rigidShapeErrorM': rigidity_error, 'contactKind': kind, 'anchorY': actual_anchor.y, 'restAnchorY': pivot_rest.y, 'pitchRadians': pitch}
    minheight = min(minheight, min((v.z for v in coords)))
    maxstrain = max(maxstrain, max(((coords[a] - coords[b]).length / l for ((a, b), l) in zip(edges, lengths) if l > 0.0001)))
    obj.to_mesh_clear()
    samples.append(row)
if action.is_action_legacy:
    curves = list(action.fcurves)
else:
    curves = [f for layer in action.layers for strip in layer.strips for bag in strip.channelbags for f in bag.fcurves]
for curve in curves:
    for key in curve.keyframe_points:
        key.interpolation = 'LINEAR'
scene.frame_start = 1
scene.frame_end = frames + 1
scene.frame_set(1)
report = {'scope': '28-frame walk with lower knee lift, toe roll and small weight-transfer accents. Offline art candidate; not physiological or game acceptance.', 'groundClearanceM': 0.004, 'sourceRigSha256': hashlib.sha256((folder / 'HeroHorse-RigStudy.fbx').read_bytes()).hexdigest(), 'fps': 30, 'frames': frames, 'durationSeconds': duration, 'authoredSpeedMps': speed, 'dutyFraction': duty, 'swingClearanceM': clearance, 'samples': len(samples), 'objectRootMotion': False, 'boneScaleAnimation': False, 'hoofOrientation': 'Flat midstance, measured toe pivot during last 12% of cycle, folded swing; geometric support correction.', 'contactModel': 'toe-roll-v2', 'rollStart': 0.5, 'toeRollDegrees': 12, 'swingFlexDegrees': 18, 'rootCompressionM': 0.087, 'rootBobM': 0.008, 'rootSwayM': 0.006, 'footfallOffsets': offsets, 'maximumReachClampingM': maxreach, 'maximumNonRootBoneTranslationM': maxlocaltranslation, 'minimumBodyHeightM': minheight, 'maximumEdgeStretch': maxstrain, 'feet': {}}
for limb in limbs:
    label = limb['label']
    points = [s['hooves'][label] for s in samples]
    stance = [p for p in points if p['contact']]
    positions = [p['anchorY'] - p['restAnchorY'] + speed * duration * p['cycle'] for p in stance]
    report['feet'][label] = {'stanceResidualTravelM': max(positions) - min(positions), 'minimumStanceHeightM': min((p['lowest'] for p in stance)), 'maximumStanceHeightM': max((p['lowest'] for p in stance)), 'maximumRigidShapeErrorM': max((p['rigidShapeErrorM'] for p in points)), 'peakSwingHeightM': max((p['lowest'] for p in points))}
assert maxreach < 0.002, report
assert maxlocaltranslation < 1e-05, report
assert minheight > -0.001, report
bpy.ops.wm.save_as_mainfile(filepath=str(out / 'HeroHorse-WalkStudy.blend'))
for o in bpy.context.selected_objects:
    o.select_set(False)
arm.select_set(True)
body.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(filepath=str(out / 'HeroHorse-WalkStudy.fbx'), use_selection=True, object_types={'MESH', 'ARMATURE'}, axis_forward='-Z', axis_up='Y', add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False, bake_anim_step=0.25, bake_anim_simplify_factor=0, path_mode='STRIP', use_custom_props=False)
report['fbxSha256'] = hashlib.sha256((out / 'HeroHorse-WalkStudy.fbx').read_bytes()).hexdigest()
(out / 'walk-study.json').write_text(json.dumps(report, indent=2) + '\n')
(out / 'walk-samples.json').write_text(json.dumps(samples, separators=(',', ':')) + '\n')
print('WALK_STUDY', json.dumps(report))
