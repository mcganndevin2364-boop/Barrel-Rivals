"""Offline, in-place gait authoring for the existing CC0 nineteen-bone horse.

Targets describe hoof *sole geometry*, not bone tips. Two-link inverse kinematics
bakes rotation only; a small, explicitly reported torso compression creates reach.
No runtime solver, bone scale, object motion, reward or collision rule is added.
The sparse source rig has no independent hoof/pastern joints: this remains a
candidate for visual review, not production biomechanical or foot-planting proof.
"""
import math
import bpy
from mathutils import Vector, Quaternion

# Name labels in this source are inconsistent at the hindquarters. Anatomical
# labels below follow the final +Y-facing model's measured left/right positions.
LIMBS = (
    ('LF', 'Bone_R.001', 'Bone_R.002'),
    ('RF', 'Bone_L.001', 'Bone_L.002'),
    ('LH', 'Bone_L.004', 'Bone_L.005'),
    ('RH', 'Bone_R.004', 'Bone_R.005'),
)
SOURCES = (
    'https://pubs.extension.wsu.edu/product/beginning-horsemanship-mm/',
    'https://horses.extension.org/horse-gallop/',
    'https://animalrangeextension.montana.edu/equine/locomotion.html',
)
SPECS = {
    'Walk': dict(frames=32, speed=1.5, duty=.64, clearance=.10,
                 down=.110, bob=.006, offsets={'LH':0, 'LF':.25, 'RH':.5, 'RF':.75}),
    'Gallop': dict(frames=20, speed=8.0, duty=.20, clearance=.32,
                   down=.120, bob=.014, offsets={'RH':0, 'LH':.12, 'RF':.30, 'LF':.44}),
}


def _curves(action):
    # Blender 4.4+ actions may use layered channel bags.
    if action.is_action_legacy:
        return list(action.fcurves)
    return [f for layer in action.layers for strip in layer.strips
            for bag in strip.channelbags for f in bag.fcurves]


def _reset(arm):
    for bone in arm.pose.bones:
        bone.rotation_mode = 'QUATERNION'
        bone.rotation_quaternion = Quaternion()
        bone.location = (0, 0, 0)
        bone.scale = (1, 1, 1)


def _put_rotation(bone, rotation, head):
    matrix = rotation.to_matrix().to_4x4()
    matrix.translation = head
    bone.matrix = matrix
    bpy.context.view_layer.update()


def _solve(arm, limb, target):
    """Analytic two-link rotation IK in armature coordinates, with no stretch."""
    upper, lower = arm.pose.bones[limb['upper']], arm.pose.bones[limb['lower']]
    start = upper.head.copy()
    delta = target - start
    distance = delta.length
    a, b = limb['lengthA'], limb['lengthB']
    reachable = max(abs(a-b)+1e-5, min(a+b-1e-5, distance))
    direction = delta.normalized()
    # Both the fore carpus and hind hock fold toward the back of this rig;
    # the upper source joints stay in their authored anatomical plane.
    pole = Vector((0, 1, 0))
    pole -= direction * pole.dot(direction)
    pole.normalize()
    along = (a*a-b*b+reachable*reachable)/(2*reachable)
    height = math.sqrt(max(0, a*a-along*along))
    joint = start + direction*along + pole*height
    solved = start + direction*reachable
    qa = limb['restA'].rotation_difference(joint-start) @ limb['rotationA']
    _put_rotation(upper, qa, start)
    qb = limb['restB'].rotation_difference(solved-joint) @ limb['rotationB']
    _put_rotation(lower, qb, joint)
    return abs(distance-reachable)


def _vertices(body, limbs):
    evaluated = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = evaluated.to_mesh()
    matrix = body.matrix_world
    result = {}
    try:
        for limb in limbs:
            points = [matrix @ mesh.vertices[i].co for i in limb['soleIndices']]
            result[limb['name']] = dict(center=sum(points, Vector())/len(points),
                                        lowest=min((matrix @ mesh.vertices[i].co).z for i in limb['groundIndices']))
    finally:
        evaluated.to_mesh_clear()
    return result


def _limbs(arm, body):
    inverse = arm.matrix_world.inverted()
    limbs = []
    for name, upper_name, lower_name in LIMBS:
        index = body.vertex_groups[lower_name].index
        weighted = [v for v in body.data.vertices if any(g.group==index and g.weight>.8 for g in v.groups)]
        lowest = min((body.matrix_world @ v.co).z for v in weighted)
        sole = [v for v in weighted if (body.matrix_world @ v.co).z < lowest+.023]
        center = sum((body.matrix_world @ v.co for v in sole), Vector())/len(sole)
        sole_local = inverse @ center
        upper, lower = arm.data.bones[upper_name], arm.data.bones[lower_name]
        limbs.append(dict(name=name, upper=upper_name, lower=lower_name,
                          soleIndices=[v.index for v in sole], groundIndices=[v.index for v in weighted], restCenter=center,
                          neutralY=(arm.matrix_world @ upper.head_local).y,
                          chainRoot=upper.parent.name,
                          soleHeight=center.z-lowest, restA=upper.tail_local-upper.head_local,
                          restB=sole_local-lower.head_local, lengthA=upper.length,
                          lengthB=(sole_local-lower.head_local).length,
                          rotationA=upper.matrix_local.to_quaternion(),
                          rotationB=lower.matrix_local.to_quaternion()))
    return limbs


def _target(limb, spec, phase):
    cycle = (phase-spec['offsets'][limb['name']]) % 1
    duty = spec['duty']
    sweep = spec['speed']*(spec['frames']/30)*duty
    target = limb['restCenter'].copy()
    contact = cycle < duty
    if contact:
        fore = sweep*(.5-cycle/duty)
        lift = 0
    else:
        swing = (cycle-duty)/(1-duty)
        # Cubic Hermite preserves the backward stance velocity at toe-off and
        # touchdown. The raised arc has zero vertical velocity at each end.
        tangent = -sweep*(1-duty)/duty
        edge=.045; over=-tangent*edge*.5
        if swing<edge:
            u=swing/edge
            fore=-sweep/2+tangent*edge*(u-u*u*.5)
        elif swing>1-edge:
            u=(swing-(1-edge))/edge
            fore=sweep/2+over+tangent*edge*u*u*.5
        else:
            u=(swing-edge)/(1-2*edge)
            fore=(-sweep/2-over)+(sweep+2*over)*(u*u*(3-2*u))
        lift=spec['clearance']*(4*swing*(1-swing))**1.5
    target.y = limb['neutralY']+fore
    target.z = .006+limb['soleHeight']+lift
    return target, contact, cycle


def _pose(arm, body, limbs, name, phase):
    _reset(arm)
    if name=='Idle':
        arm.pose.bones['Bone.001'].rotation_quaternion=Quaternion((1,0,0),math.sin(phase*math.tau)*.025)
        arm.pose.bones['Bone.002'].rotation_quaternion=Quaternion((1,0,0),math.sin(phase*math.tau+.6)*.025)
        arm.pose.bones['Bone.004'].rotation_quaternion=Quaternion((0,0,1),math.sin(phase*math.tau)*.07)
        bpy.context.view_layer.update()
        return {},0,0
    spec=SPECS[name]
    scale=arm.matrix_world.to_scale().z
    compression=-spec['down']+spec['bob']*math.cos(phase*math.tau)
    for bone in arm.pose.bones:
        if bone.parent is None:
            # All independent skeletal roots share the same vertical compression;
            # the armature/model/GameObject transforms themselves stay fixed.
            offset=Vector((0,0,compression/scale))
            bone.location=bone.bone.matrix_local.to_3x3().inverted() @ offset
    arm.pose.bones['Bone.001'].rotation_quaternion=Quaternion((1,0,0),math.sin(phase*math.tau)*(.018 if name=='Walk' else .035))
    arm.pose.bones['Bone.002'].rotation_quaternion=Quaternion((1,0,0),-math.sin(phase*math.tau+.25)*(.012 if name=='Walk' else .022))
    arm.pose.bones['Bone.004'].rotation_quaternion=Quaternion((0,0,1),math.sin(phase*math.tau+.5)*.045)
    bpy.context.view_layer.update()
    # Share stride extension with the existing shoulder/thigh segment rather
    # than demanding all reach from two nearly straight distal segments.
    for limb in limbs:
        target,_,_=_target(limb,spec,phase)
        shift=(target.y-limb['neutralY'])*.23
        chain=arm.pose.bones[limb['chainRoot']]
        vertical=abs((arm.matrix_world.to_3x3() @ (chain.bone.tail_local-chain.bone.head_local)).z)
        chain.rotation_quaternion=Quaternion((1,0,0),-math.atan2(shift,vertical))
    bpy.context.view_layer.update()
    inverse=arm.matrix_world.inverted(); targets={}; contacts={}; max_reach=0
    for limb in limbs:
        target,contact,cycle=_target(limb,spec,phase)
        targets[limb['name']]=target;contacts[limb['name']]=(contact,cycle)
    # The source has no hoof pitch joint. Correct against actual weighted sole
    # geometry, so changing lower-leg angle cannot silently bury the toe.
    for iteration in range(24):
        max_reach=0
        for limb in limbs:
            reach=_solve(arm,limb,inverse @ targets[limb['name']])
            max_reach=max(max_reach,reach*scale)
        actual=_vertices(body,limbs)
        if iteration<23:
            for limb in limbs:
                label=limb['name']; desired,_,_=_target(limb,spec,phase)
                targets[label].y += .6*(desired.y-actual[label]['center'].y)
                targets[label].z += .6*(desired.z-limb['soleHeight']-actual[label]['lowest'])
    for limb in limbs:
        label=limb['name']; actual[label]['contact'],actual[label]['cycle']=contacts[label]
    return actual,compression,max_reach


def _stats(samples, name):
    spec=SPECS[name]; duration=spec['frames']/30
    result={}
    for label,_,_ in LIMBS:
        points=[s['hooves'][label] for s in samples]
        stance=[p for p in points if p['contact']]
        world_stance=[p['center'][1]+spec['speed']*p['cycle']*duration for p in stance]
        result[label]={
            'stanceMinimumHeightMetres':min(p['lowest'] for p in stance),
            'stanceMaximumHeightMetres':max(p['lowest'] for p in stance),
            'stanceResidualTravelAtAuthoredSpeedMetres':max(world_stance)-min(world_stance),
            'minimumHeightMetres':min(p['lowest'] for p in points),
            'peakSwingClearanceMetres':max(p['lowest'] for p in points if not p['contact']),
        }
    return result


def author_gaits(arm, body):
    scene=bpy.context.scene;scene.render.fps=30;arm.animation_data_create()
    _reset(arm);bpy.context.view_layer.update();limbs=_limbs(arm,body)
    report={'method':'Offline sole-target rotation IK; no stretch or runtime/root motion',
            'sources':list(SOURCES),'rootObjectAnimation':False,'independentHoofJoints':False,
            'skeletalRootsCompressed':[b.name for b in arm.pose.bones if b.parent is None],
            'soleVertexCounts':{l['name']:len(l['soleIndices']) for l in limbs},
            'groundCheckedDistalVertexCounts':{l['name']:len(l['groundIndices']) for l in limbs},'clips':{},
            'limitations':['Fixed left-lead gallop only; lead changes and tight-turn gait remain unimplemented.',
                'No independent pastern/hoof joints: planted sole position does not guarantee flat hoof orientation.',
                'Static saddle/pad attachments require matching torso follow before candidate acceptance.',
                'Authored speeds are references; current variable-speed blend tree may still slide.',
                'Metrics are authoring-space measurements, not Unity import, visual or device acceptance.']}
    for name,frames in [('Idle',60),('Walk',32),('Gallop',20)]:
        action=bpy.data.actions.new(name);arm.animation_data.action=action;action.use_fake_user=True
        samples=[];max_reach=0;compressions=[];angles={b.name:0 for b in arm.pose.bones};scale_error=0
        # Four authoring samples per 30fps frame; FBX uses half-frame bake steps.
        for i in range(frames*4+1):
            phase=i/(frames*4);frame=1+i/4
            actual,compression,reach=_pose(arm,body,limbs,name,phase)
            max_reach=max(max_reach,reach);compressions.append(compression)
            for bone in arm.pose.bones:
                bone.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=bone.name)
                bone.keyframe_insert(data_path='location',frame=frame,group=bone.name)
                angles[bone.name]=max(angles[bone.name],math.degrees(bone.rotation_quaternion.angle))
                scale_error=max(scale_error,max(abs(v-1) for v in bone.scale))
            if actual:
                samples.append({'phase':phase,'bodyCompressionMetres':compression,'hooves':{
                    label:{'center':list(p['center']),'lowest':p['lowest'],'contact':p['contact'],'cycle':p['cycle']}
                    for label,p in actual.items()}})
        for curve in _curves(action):
            for key in curve.keyframe_points:key.interpolation='LINEAR'
        entry={'frames':frames,'seconds':frames/30,'bodyMinimumOffsetMetres':min(compressions),
               'bodyMaximumOffsetMetres':max(compressions),'maximumFinalSolveReachClampMetres':max_reach,
               'maximumScaleDeviation':scale_error,'maximumBoneBasisRotationDegrees':angles}
        if name!='Idle':
            support=[sum(p['contact'] for p in s['hooves'].values()) for s in samples[:-1]]
            entry.update(spec=SPECS[name],hoofMetrics=_stats(samples,name),samples=samples,
                minimumContactCount=min(support),suspensionFraction=sum(n==0 for n in support)/len(support),
                suspensionIntervalCount=sum(support[i]==0 and support[i-1]!=0 for i in range(len(support))))
        report['clips'][name]=entry
    arm.animation_data.action=None;_reset(arm);scene.frame_set(1);bpy.context.view_layer.update()
    return report
