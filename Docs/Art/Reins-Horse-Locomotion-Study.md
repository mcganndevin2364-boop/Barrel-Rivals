# Reins — candidate locomotion and moving capture

September 21, 2026. The fitted development horse now has original Idle, Trot, Gallop and Sprint studies alongside its preserved Walk. A speed-driven Unity blend changes the gait and rider lean together. This is still the isolated development scene: it has not replaced the racing, stable or ghost character, or changed either mobile package.

[Actual Unity transition movie](../../Evidence/HorseLocomotion-Motion.mp4) · [sprint pose](../../Evidence/HorseLocomotion-Sprint.png) · [riding view](../../Evidence/HorseLocomotion-Riding.png).

## Implementation

The editable `HeroHorse-Locomotion.blend` preserves the original geometry, rest rig, skin weights, UVs, Walk curves and packed atlases. Four original actions use rotation-only limb solving, measured rigid sole surfaces, toe roll, folded swing, restrained torso/neck motion and the existing five hair helper bones. Root offsets are visual body movement; the actor never gains animation root motion. The sprint is a separate authored action rather than a sped-up Walk. These are authored arcade studies, not motion capture or validated equine biomechanics.

| Clip | Loop seconds | Reference speed | Authoring stance fraction |
|---|---:|---:|---:|
| Idle | 4.000 | 0 m/s | 1.000 |
| Preserved Walk | 0.933 | 1.5 m/s | 0.620 |
| Trot | 0.600 | 3.5 m/s | 0.450 |
| Gallop | 0.600 | 6 m/s | 0.280 |
| Sprint | 0.433 | 12 m/s | 0.215 |

The new gait FBXs include a tiny 39-triangle binding carrier. The first bare-armature export lost the neutral bind matrices and Unity collapsed the model root, producing mismatched deformation/binding paths. The carrier preserves those transforms and is never instantiated with the visible horse. Imported animation paths are checked against the actual existing hierarchy. Compression/resampling remain disabled; the original full horse FBX remains byte-identical.

`HeroHorseLocomotion` feeds a five-child blend tree using a 0.25-second exponential speed response. Reference thresholds are 0/1.5/3.5/6/12 m/s. Above 12 m/s the sprint rate scales to the bounded 14 m/s inspection maximum. The same smoothed speed controls the existing seated rider solver; hands, camera and reins follow actual bone/support transforms. NaN/infinite speeds resolve safely to zero. The driver controls presentation only and has no Core, input, timer, reward or replay writes.

`HeroHorseGrounding` then corrects blend-induced floor penetration against the actual rigid hoof vertices. It rotates each lower two-bone chain while preserving pastern/hoof orientation; it does not stretch bones, move the torso/actor, use runtime mesh baking, or affect Core collision. The benchmark has a flat floor in model space. This is not terrain following or world-space stance locking. The uncorrected sequence reached 59.932 mm below the floor and remains recorded as the reason for this correction.

## Capture correction

Visual inspection exposed a real review bug: repeated `Camera.Render` calls within one Editor frame could reuse an earlier GPU-skinned pose even while the skeleton and CPU-baked measurements advanced. `Capture` now temporarily requests skin-matrix recalculation for each render and restores the previous flags in `finally`. This does not change the live player's normal skinning policy.

The fresh moving frames show the resulting limb motion and rider lean. Earlier HorseUnity/HorseRider movies remain unchanged as historical files; they must **not** be used as reliable proof of frame-by-frame skinned deformation. Their separate numerical and live tests retain their stated scope. The correction also exposed a low first-person camera intersecting the rider's clothing during fast lean. The candidate camera is now 0.60 m above the measured horn, with an 18-degree downward pitch and 72-degree FOV; live grip framing is checked during acceleration as well as walking.

## Verification

**8/8 focused Play Mode tests pass, with zero skips.** They cover live sprint/idle selection, safe speed handling, actor immobility, measured floor correction, rider reach, grip framing, hidden-bone updates, reduced motion and existing race/ghost regressions. All **639 previous evidence files remain unchanged**. No new full-suite or device result is claimed.

Verification results and current capture limits are recorded in [the checkpoint](../../Evidence/HorseLocomotion-Checkpoint.json), [Blender verification](../../Evidence/HorseLocomotion-Blender.json), [Unity measurements](../../Evidence/HorseLocomotion-Review.json) and [Play Mode results](../../Evidence/HorseLocomotion-PlayMode.xml).

Independent Blender verification samples 1,356 poses across the four new clips, including between authored quarter-frame keys. Original source signatures match, loop vertex error is zero, rigid hooves stay at least 1.697 mm above the floor and maximum FBX body-deformation difference is 2.280 micrometres. This checks these exports, not Unity-versus-Blender fidelity. The historical seven-pose Walk discrepancy remains open at 0.364 mm against its retained 0.2 mm target.

Unity samples 33 poses per clip for all five gaits and measures transition hoof heights separately. Static loop/ground and rider-reach checks pass. Across the 420 transition frames, the uncorrected minimum is −59.932 mm; the correction brings the independently CPU-baked surface minimum to +3.9995 mm. Maximum visual leg correction is 63.932 mm, maximum rider reach error is 0.000585 mm and maximum visual Root step at 30 Hz is 16.949 mm. These sample bounds are not a continuous contact guarantee. The controlled movie steps the actual speed driver and Animator at 30 Hz through Idle → Walk → Trot → Gallop → Sprint → 14 m/s → Stop, once from the side and once from the riding camera. It uses 420 unique 800×600 frames, 14 seconds, a static floor and no audio. It is an in-place presentation study, not world foot-planting evidence, measured game FPS or phone footage.

The character remains 92,402 triangles / 20 renderers / 25 slots, over the 60k/six targets. Hat/clothing, coat/groom detail, shoulder/groin deformation and natural movement still need work. Tight turns, braking, canter/contact/Wrap and authored rider effort are missing. A smoothed blend does not establish natural grounded transitions, and the fixed floor cannot establish world-space stance contact. Device performance, comfort and reference quality remain open.

## Reproduction and next work

With the pinned Blender 4.5.13 and auto-execution disabled, run `Tools/Art/horse_study/author_locomotion.py` with the preserved GroomStudy blend and a fresh output directory; then run `verify_locomotion.py` with the same arguments. Copy only reviewed new blend/report and four FBXs into their existing candidate/development paths, preserving Unity metadata.

Run `HeroHorseBenchmarkBuilder.Build`, then `CaptureLocomotion` with `BARREL_HORSE_BENCHMARK_OUTPUT` set to a fresh private directory. Use one Unity 6000.6.0f1 Editor per project. `encode_locomotion.py` encodes the actual frames; `verify_video.swift` verifies native decoding. In the saved development scene, play and adjust `HeroHorseLocomotion.targetSpeed`; use the existing rider-view/reduced-motion controls.

Continue visible art and gait/turn/contact refinement, reduce character geometry/materials and add LODs, then explicitly adopt the approved character into Core-driven racing, saved stable cosmetics and ghost ownership/visibility. Any player replacement needs a new build identity, native rebuilds and actual device qualification. Both existing 0.5 artifacts remain uninstalled/unqualified; last verified installed iPhone is 0.4.0/build 4. All eight categories and 53 IDs, progression and multiplayer remain in scope.
