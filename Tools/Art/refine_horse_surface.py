"""Original constrained surface refinement of the licensed CC0 horse.

Run inside the existing exporter, after metre normalization and before gait
authoring. Smooth upper-body facets while retaining authored sole landmarks,
eye rims and ear tips. This does not add joints or certify anatomical realism.
"""
import math
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree


def smoothstep(a, b, x):
    t = max(0.0, min(1.0, (x-a)/(b-a)))
    return t*t*(3-2*t)


def refine_surface(body, eyes):
    matrix = body.matrix_world.copy()
    inverse = matrix.inverted()
    eye_centers = [o.matrix_world.translation.copy() for o in eyes]
    rig = next(m.object for m in body.modifiers if m.type == 'ARMATURE')
    valid_groups = {g.index for g in body.vertex_groups if g.name in rig.data.bones}
    source_count = len(body.data.vertices)
    body.data.calc_loop_triangles()
    original_surface = BVHTree.FromPolygons(
        [matrix@v.co for v in body.data.vertices],
        [tuple(t.vertices) for t in body.data.loop_triangles], all_triangles=True)
    # Shape corrections are in the normalized authoring frame: +Y forward, Z up.
    # Keep the jaw's muscle mass, but reduce its oversized lateral roundness.
    for vertex in body.data.vertices:
        p = matrix @ vertex.co
        eye_clearance = min((p-e).length for e in eye_centers)
        eye_mask = smoothstep(.055, .115, eye_clearance)
        cheek = math.exp(-((p.y-1.055)/.15)**2-((p.z-1.735)/.16)**2)
        p.x *= 1-.19*cheek*eye_mask
        belly = math.exp(-((p.y+.43)/.50)**2-((p.z-1.10)/.25)**2)
        p.x *= 1-.055*belly
        p.z += .022*belly
        vertex.co = inverse @ p

    # Both subdivision modes have identical topology/UV indexing. Simple mode
    # retains source corners. Non-planar quad interiors need an additional
    # projection onto original triangles to preserve contact-vertex locations.
    # Retessellated triangle interiors can still differ; imported gait geometry
    # must be measured again instead of assuming a completely identical sole.
    simple = body.copy()
    simple.data = body.data.copy()
    bpy.context.collection.objects.link(simple)
    for modifier in list(simple.modifiers):
        simple.modifiers.remove(modifier)
    bpy.context.view_layer.objects.active = simple
    subdivision = simple.modifiers.new('Contact-preserving topology', 'SUBSURF')
    subdivision.subdivision_type = 'SIMPLE'
    subdivision.levels = subdivision.render_levels = 1
    bpy.ops.object.modifier_apply(modifier=subdivision.name)

    bpy.context.view_layer.objects.active = body
    subdivision = body.modifiers.new('Upper body surface', 'SUBSURF')
    subdivision.subdivision_type = 'CATMULL_CLARK'
    subdivision.levels = subdivision.render_levels = 1
    # Apply before the armature; no current animation is baked into the mesh.
    bpy.ops.object.modifier_move_up(modifier=subdivision.name)
    bpy.ops.object.modifier_apply(modifier=subdivision.name)
    assert len(body.data.vertices) == len(simple.data.vertices)
    assert all(tuple(a.vertices) == tuple(b.vertices)
               for a,b in zip(body.data.polygons, simple.data.polygons))
    maximum_shift = 0.0
    preserved_sole_vertices = 0
    maximum_sole_distance = 0.0
    for smooth, linear in zip(body.data.vertices, simple.data.vertices):
        p = matrix @ linear.co
        eye_clearance = min((p-e).length for e in eye_centers)
        blend = smoothstep(.16, .52, p.z)
        blend *= 1-smoothstep(2.02, 2.085, p.z)
        blend *= smoothstep(.045, .095, eye_clearance)
        smooth.co = linear.co.lerp(smooth.co, blend)
        if p.z < .32:
            current = matrix@smooth.co
            nearest = original_surface.find_nearest(current)[0]
            current = current.lerp(nearest, 1-smoothstep(.16, .32, p.z))
            smooth.co = inverse@current
        maximum_shift = max(maximum_shift, (matrix@smooth.co-p).length)
        if p.z <= .16:
            distance = original_surface.find_nearest(matrix@smooth.co)[3]
            assert distance < 1e-6
            maximum_sole_distance = max(maximum_sole_distance, distance)
            preserved_sole_vertices += 1
    for polygon in body.data.polygons:
        polygon.use_smooth = True
    # Match the intended mobile four-weight skin explicitly. Do this before
    # authoring/measuring gaits so Blender and the Unity import use the same
    # influence policy, including exclusion of the source's orphan Bone.005.
    maximum_dropped_weight = 0.0
    limited_vertices = 0
    for vertex in body.data.vertices:
        valid = sorted([(g.group,g.weight) for g in vertex.groups
                        if g.group in valid_groups and g.weight > 0],
                       key=lambda pair: (-pair[1],pair[0]))
        assert valid, 'Refined vertex has no deform-bone influence'
        total = sum(w for _,w in valid)
        kept = valid[:4]
        dropped = 1-sum(w for _,w in kept)/total
        maximum_dropped_weight = max(maximum_dropped_weight, dropped)
        limited_vertices += len(valid)>4
        # Snapshot integer indices, not RNA group elements invalidated by removal.
        for index in [g.group for g in vertex.groups]:
            body.vertex_groups[index].remove([vertex.index])
        retained_total = sum(w for _,w in kept)
        for index,weight in kept:
            body.vertex_groups[index].add([vertex.index],weight/retained_total,'REPLACE')
    mesh = simple.data
    bpy.data.objects.remove(simple, do_unlink=True)
    bpy.data.meshes.remove(mesh)
    body.data.calc_loop_triangles()
    return dict(sourceVertices=source_count, vertices=len(body.data.vertices),
                triangles=len(body.data.loop_triangles), uvLayers=len(body.data.uv_layers),
                maximumSmoothingShiftMetres=maximum_shift,
                protectedSoleVertices=preserved_sole_vertices,
                maximumProtectedSoleDistanceMetres=maximum_sole_distance,
                maximumSkinInfluences=4, limitedVertices=limited_vertices,
                maximumDroppedNormalizedInfluence=maximum_dropped_weight,
                method='Original cheek/flank contour correction; one constrained subdivision level',
                limits='Same sparse rig and inherited anatomy; no new hoof joints or production-realism acceptance')
