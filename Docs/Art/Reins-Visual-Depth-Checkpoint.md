# Reins visual depth — ground, stable and gait refinement

September 20, 2026. **Verified source checkpoint with actual Unity captures; visual target and mobile acceptance remain unfinished.** This continues the existing Reins game and free/original art. Native identity remains **0.4.0/build 4**, with `reins-lab-v1` rules. The approved **0.5/v2 moving alley, release and control migration is unimplemented**. This pass does not complete the reference-quality milestone or any full-release section in the eight-category/53-section plan.

## Changes in this source

**Drier arena materials and light separation.** `ReinsPremiumArenaBuilder` scales the existing roughness-derived ground smoothness to 0.34, raises soil normal strength to 0.78 and reduces distant terrain smoothness/detail. A deterministic original 128 × 128 linear detail map varies albedo across approximately 42 metres while keeping the existing 1.3-metre photographic clod scale. Its repeat edges match; the original reviewed CC0 maps remain unchanged. Ambient light and reflection intensity are lower, the sun angle is lower, and the horse-coat smoothness changes from 0.43 to 0.28 to reduce broad plastic-looking highlights. These are material and lighting changes, not new ground collision, displacement, footing rules or additional terrain themes.

**Warm stable with visible window tones.** `StableShowroomBuilder` replaces the clipped bright pane with one recessed physical quad, an original 64 × 128 frosted-transmission map and shallow timber reveals. Window diffuse contribution is near black and emission is bounded; the material no longer adds a strong specular reflection on top. Lantern emission is reduced. One shadowed sun, a softer neutral/cool fill and one short-range warm lantern pool provide contrast. Three lights are used in total, with only the sun shadowed. Sash and stall geometry cast the floor bands. There are no photograph backdrops, transparent shaft planes, new per-frame geometry or baked-GI claims. Final saved-scene captures show Copper readable and the window unclipped; the art remains visibly simpler than the reference.

**South-facing sky artifact diagnosis and fix.** The opt-in `ReinsSkyDiagnostic` separates full-scene, sky-only and world-only renders. The working diagnosis associates the bright seam with mipped panoramic sky sampling at wrapped longitude. The import change disables mipmaps and anisotropy for the 2K HDR sky only; surface maps retain their filtering. The downloaded HDR bytes and panorama exposure are unchanged. A same-camera before/after Unity comparison now shows the former full-height dashed stripe absent from the sky-only view. The final south-facing Drive/Finish captures also have no dashed stripe. Device filtering/performance review remains unperformed. Preserve that diagnostic comparison separately from the final gameplay evidence.

**Generated mesh persistence.** The prior character-motion checkpoint found that `CopySerialized` could change Mesh bounds while retaining stale native vertex data. A shared `PersistentMeshAsset.Save` now replaces that update pattern in the practice, premium arena, rider tack, stable tack, wardrobe, showroom and hair builders. It preserves asset identity, raw vertex streams/attribute formats, index format, submesh topology/ranges/base vertices/bounds, bindposes and variable bone influences. It rejects unsupported blend shapes or embedded mesh LODs rather than silently discarding them. Regression tests unload/reimport saved assets and compare actual channels, including clearing obsolete channels and preserving GUID/fileID. This is a regeneration repair, not evidence that every earlier mesh was corrupt.

## Original gait candidate and connected presentation

Candidate-06 retains the CC0 horse's **19 bones and 3,697 body vertices**. The current FBX is 1,048,076 bytes, SHA-256 `6951b81ac7bd0aed7c58e98822cbddc94641d877ea3873ce0c28f0ffce24efad`; the source and earlier FBX identities remain in [provenance](Reins-Reference-Graphics-Provenance.md). The four extracted source textures are unchanged.

The repository's `export_reference_horse.py`, `author_horse_gaits.py` and `inspect_gait_candidate.py` author and inspect in-place motion offline in Blender 4.5.13 with automatic source-script execution disabled. Sole geometry supplies IK targets, with no runtime IK or deliberate bone stretching. Walk uses a 64% stance interval and four evenly spaced footfalls; gallop uses a 20% stance interval per limb, a fixed left lead and one intended suspension interval. Sequence guidance comes from [Montana State University](https://animalrangeextension.montana.edu/equine/locomotion.html) and [University of Kentucky / Extension Horses](https://horses.extension.org/horse-gallop/). Timing offsets and amplitudes are original parameters, not captured biomechanics.

The four parentless skeletal roots share body compression; the model, armature object and authoritative game root do not receive a locomotion translation curve. Walk's body offset is −110 ±6 mm; gallop's is −120 ±14 mm. `Western saddle` now follows the exact torso `Bone` with its authored world bind pose preserved, and regeneration removes old descendant saddles. A focused test checks the saved binding and sampled body/seat movement without moving the horse root.

Runtime presentation reads actual torso displacement for the hands and normal rider view. Reduced-motion view uses the blend's mean posture height without repeated stride bob. Gallop playback follows `clamp(speed/8, 1, 1.75)` so the full-gallop clip can match its authored travel through Drive. Import compression is disabled, and [curve resampling](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/ModelImporter-resampleCurves.html) is disabled where Unity supports preserving the authored curves. Actual imported geometry is measured below rather than inferred from that setting. FBX imports initially in Gallop frame zero, with the torso lowered 106 mm; the builder now explicitly samples Idle at zero before fitting hair/tack or caching rider bind offsets. Geometry measurements likewise select soles in the actual baked neutral pose, not raw mesh coordinates. The independent bind-matrix skin calculation agrees with Unity BakeMesh within 0.012 mm in the measured diagnostic. These choices preserve Core ownership; they do not prove planted blend-tree motion, natural turns, camera comfort or correct contact under acceleration.

### Existing Blender export observations — not Unity acceptance

The candidate-06 report samples the reimported FBX at 121 phases per cycle. Sole-centroid drift includes analytical forward travel at the authored speed; ground clearance considers strongly weighted distal-leg vertices. Half-frame FBX baking adds measurable error compared with the denser authoring solve.

| Clip | Duration | Authored speed | Designated-stance minimum-height range | Worst residual stance travel | Peak swing clearance |
|---|---:|---:|---:|---:|---:|
| Walk | 1.066667 s | 1.5 m/s | 5.763–6.922 mm | 0.081 mm | 10.52–10.53 cm |
| Gallop | 0.666667 s | 8.0 m/s | 2.980–7.774 mm | 7.119 mm | 32.57–32.60 cm |

The report records zero armature/empty object-matrix deviation and a maximum exported bone-scale deviation below 0.0000006. Original names and 60/32/20-frame Idle/Walk/Gallop durations at 30 fps remain. These are narrowly defined Blender measurements, not biological validation, full-sole planting or Unity/mobile results.

## Final verification

Use new `Evidence/VisualDepth-*` records for this source. Earlier `PremiumGraphics`, `StableReference` and `CharacterMotion` captures and test results retain their historical meaning. Capture tests that overwrite old filenames must have those bytes restored after the new evidence is copied out.

| Check | Status for this final source |
|---|---|
| Current FBX identity and unchanged extracted texture hashes | Checked against candidate-06 and recorded above |
| Generation, import and save/reopen of both scenes with candidate-06 and torso-bound saddle | Passed with neutral-pose binding and corrected native skin clearing; saved references verified in the final suites |
| Full final Editor tests, including persistence and saddle binding | **110/110 passed**, none skipped; saved mesh data, imported geometry and saddle binding included |
| Full final Play Mode tests and canonical v1 replay | **21/21 passed**, none skipped; canonical v1 replay remains **35,120 ms / 0 knocks / 300 style / 2,156 frames** |
| Isolated sky-fix comparison | Same-camera Unity before/after inspected: dashed stripe absent after the import change; source HDR unchanged |
| Final arena/stable/rider stills, including south-facing gameplay | Captured and inspected; window tones preserved, ground less glossy, sky stripe absent. Hair/tack/crowd/anatomy remain visibly provisional. |
| Unity moving study and imported geometry | 220 actual player-loop frames, encoded at 25 fps/8.8s; Drive leg range 40.061°, neck 2.274°; two body-only walk silhouettes differ by 6,286 lower-leg pixels. Imported clip metrics below; no collision/contact or full foot-planting acceptance claim. |
| Historical evidence restoration and source/hash audit | All 125 prior PNG/JSON/XML/MP4 records match baseline bytes; current source hash recorded; Core/contracts unchanged. Legacy scene reserialization restored. |
| New native builds, installation and phone performance/comfort | **Not performed for this source** |

### Actual imported Unity clip measurements

With compression off and authored curves preserved, 121 samples per clip measure:

| Clip | Stance ground-height range | Maximum nominal stance drift | Peak swing clearance |
|---|---:|---:|---:|
| Walk | 5.763–6.927 mm | 2.961 mm | 10.524–10.532 cm |
| Gallop | 3.046–7.829 mm | 6.167 mm | 32.570–32.600 cm |

Object-root matrix deviation and first/last loop vertex difference are zero in this check. Maximum bone-scale deviation is 2.39e−7. Unity initially resampled the curves and measured 27.062 mm gallop drift; preserving authored curves resolved it without relaxing the 25 mm regression budget. These are nominal straight-line clip measurements; blending, turns, acceleration and flat hoof orientation are not accepted by this result.

Unity splits the source’s 3,697 body vertices into 4,349 imported vertices for attributes such as UVs/normals. The same bone-weight/neutral-height predicate selects repeated positions in Unity, changing arithmetic sole-centroid weighting compared with Blender. Each report records its own selection; minimum ground height is unaffected by repeated positions. Exact numerical equality between the two reports is not claimed.

The decoded movie's frame 60 matches the original Unity capture with RGB mean absolute errors 2.03/0.83/2.15 levels out of 255. This verifies orientation/content, not real-time performance. The source test captures paused, explicitly stepped animation; the displayed 25 fps is the encoding rate.

- [Actual moving Unity study](../../Evidence/VisualDepth-Motion.mp4), [capture metadata](../../Evidence/VisualDepth-Capture.json), [imported geometry](../../Evidence/VisualDepth-Gait-Unity.json).
- [MyStable](../../Evidence/VisualDepth-MyStable.png), [saddles](../../Evidence/VisualDepth-SaddlesPreview.png), [rider gear](../../Evidence/VisualDepth-RiderPreview.png), [south-facing Drive](../../Evidence/VisualDepth-Race-Drive.png).
- [Verification manifest](../../Evidence/VisualDepth-Checkpoint.json), [historical integrity](../../Evidence/VisualDepth-Historical-Integrity.json).

The previous [character-motion checkpoint](Reins-Character-Motion-Checkpoint.md) remains a separate verified result. Do not reuse its test counts or motion measurements as this pass's verification.

## Remaining visual limits and next acceptance

The source skeleton has **no independent hoof/pastern joint**. A low toe or sole-centroid target does not establish a flat planted hoof. There is only one gallop lead; lead changes, natural tight turns, braking, transitional blends and variable-speed planting remain unfinished. The simple horse anatomy, strip-like hair, saddle/glove modeling, incomplete rider, repeated crowd and surface detail still fall short of the user's reference. Stowed stable rein endpoints also require review against the newly moving saddle/neck; the current fixed curves are not a finished stable rein simulation.

Review actual first-person and side-view motion together, including full-speed Drive, body/hand/camera alignment, turns and reduced motion. Then qualify the completed representative scene on the connected iPhone and a named Android handset. Record real frame-time, memory, thermal and touch/comfort results; a controlled Editor movie's encoding rate is not real-time performance. No paid assets or services were purchased, and this source adds no trusted progression, multiplayer deployment or release acceptance.

Reproduce with one Unity 6000.6.0f1 Editor and `BarrelRivals.Editor.ReinsLabBuilder.Generate`. The gait exporter operates on an isolated candidate output; preserve the source `.blend` and previous records. Use the existing opt-in motion capture workflow with a fresh directory, then publish only inspected `VisualDepth-*` results after validation.
