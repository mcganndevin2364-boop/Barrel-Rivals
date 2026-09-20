# Reins reference graphics — provenance and checkpoint limits

Original record: September 19, 2026. Updated September 20 with the candidate-06 gait export identity; original export identity is preserved below. This records sources and authoring work, not completed R2 visual-quality, gameplay or mobile-performance acceptance. The candidate's final Unity verification is tracked separately in [the visual-depth checkpoint](Reins-Visual-Depth-Checkpoint.md).

## Reference and intended use

The user supplied a rider-view rodeo image as the desired direction throughout gameplay: a believable horse in the foreground, granular arena dirt, packed stands, western structures, warm natural light and clear race controls. The reference is guidance for composition, materials and atmosphere. Its pixels and sponsor artwork are not bundled as game assets. No commercial sponsor signage or implied sponsorship is part of this checkpoint; use original game-world signs instead.

All art is integrated into the existing real-time Unity scene. A reference image, still render or generated background is not evidence that the actual moving game has achieved that appearance. Graphics observe the shared race state; they do not change rules, scoring, collision or replay authority.

## Horse source and extraction

- Source: [Rigged Horse on OpenGameArt](https://opengameart.org/node/10771), [original download](https://opengameart.org/sites/default/files/riggedHorse.blend).
- Source creators: **Lyndon Daniels / LyndonDaniels and ChadM**. The underlying [Realtime Rancher's model release](https://opengameart.org/content/realtime-ranchers-3d-model-pack) is by Lyndon Daniels; ChadM's derived asset supplies the rig. Preserve both names in the art records.
- Published source license: **CC0**. Keep the source URLs and license record with the project even though attribution is not a condition of CC0.
- Downloaded source: `riggedHorse.blend`, 20,194,164 bytes, Blender 2.63-era format.
- Source SHA-256: `9cca670b93a74d50e89263e50d55ab035a6c46aa7d2b21e354bdac6987037f4a`.
- Extraction tool: **Blender 4.5.13 LTS, Intel macOS**, run from the official DMG mounted read-only. Recorded DMG SHA-256: `43caddd07d0917cb5bac288180e6bcb0374fac4fdc31cca9dc230a2e3dec752f`.
- Blender was launched with automatic script execution disabled. The export script additionally opens the source with `use_scripts=False`; no embedded source script was intentionally executed.

The repository-owned [export script](../../Tools/Art/export_reference_horse.py) selects mesh/armature data and the named loaded texture maps and exports FBX plus PNGs. The original [source-inspection.json](../../Assets/_Project/Art/Reins/Horse/source-inspection.json) describes the first export; its original animation-quality wording is not evidence of a candidate-06 Unity run. The exporter now also writes a candidate inspection and gait metrics into its selected output directory. It does not export the original scene's reference photographs, UI, custom properties, or embedded scripts. A common parent sets the horse's source bounding height to 2.12 metres, places its base at zero and preserves mesh/armature relationships. Detached eyes are parented to the head bone while preserving their rest transforms. That height is an authoring normalization, not a verified horse-withers measurement.

The inspection record reports **3,697 body vertices and 19 armature bones**. Body vertices are not the total character triangle count: hair/eyes, exported triangulation, future tack and the future rider must still be included when checking the character budget.

### Extracted files

All files below are under `Assets/_Project/Art/Reins/Horse`, outside builder-owned generated content.

| File | Source or role | Dimensions / bytes | SHA-256 |
|---|---|---|---|
| `RodeoHorse.fbx` | Current candidate-06 export: existing mesh/rig with original sole-target gait curves | 1,048,076 bytes | `6951b81ac7bd0aed7c58e98822cbddc94641d877ea3873ce0c28f0ffce24efad` |
| `HorseAlbedo.png` | Source `HorseMain4k00.png` | 2048 × 2048 / 4,527,924 bytes | `96a4538d2774b79ad37a731c9b19960ecfc97974b37637a12e77a273eb5ea3c1` |
| `HorseNormal.png` | Source `HorseMain4k00Norm00.p` | 2048 × 2048 / 2,356,147 bytes | `2aa2dcd4214f91b1b5c9a5cc742586c80b0b87cf8acb15bd596baac8ce5342b3` |
| `HorseHair.png` | Source `Hair12Main2k.png` | 2048 × 2048 / 7,979,316 bytes | `4a21ef40e4f3fd17b30a9ebccc2023e4cfd2bd9dc32c584c482dca03f06e3312` |
| `HorseEye.png` | Source `eye_texture.bmp.001` | 400 × 400 / 221,906 bytes | `69c89ef7840d5c51768dbccb52166ea4007c413a39b18ed26054fd0bb701a75e` |

The **historical September 19 FBX** was 929,868 bytes, SHA-256 `3cca79306d9c5d57f363518d761810b83e8ab06409fdc9840b7b1d151aa5d614`. It remains in the earlier source history and its checkpoint evidence; it is not the current file's hash. Candidate-06 and the current tracked FBX were compared byte for byte on September 20. The four texture files above were rehashed and remain unchanged.

These are file-inspection observations. They do not establish correct Unity normal-map orientation, alpha filtering, bone deformation, render appearance or on-device texture residency.

## Animation work and remaining character work

The inspected source had no supplied locomotion animation. The original September 19 exporter authored **Idle, Walk and Gallop** studies using explicit in-place bone rotations at 30 frames per second, with nominal cycles of 60, 32 and 20 frames plus a closing pose. That first export remains historical. No animation root translation controls the race.

These are a first-pass integration and gait study, **not production animation or motion capture**. Foot planting, gait phase/anatomical accuracy, clip blending, speed matching, head motion, acceleration, braking, tight turns, contact reactions, mane/tail behavior and rider synchronization need review and further authoring. The exported horse does not include a finished rider, western saddle, bridle or reins. Existing procedural rider/tack elements, where retained, are placeholders and do not make the character complete.

The horse is now extracted into project art assets rather than an unopened candidate. That is a concrete advance in the art pipeline; it is not an assertion that its anatomy, skinning or appearance meets the user's reference throughout gameplay.

### Candidate-06 gait authoring follow-up — September 20

[author_horse_gaits.py](../../Tools/Art/author_horse_gaits.py) replaces the old sinusoidal limb study with original offline rotation IK using measured sole vertices, bounded stance travel, swing arcs and shared vertical skeletal compression. [inspect_gait_candidate.py](../../Tools/Art/inspect_gait_candidate.py) reimports the exported FBX to inspect geometry and contact samples. The current export retains **19 bones, 3,697 body vertices**, original names, and the same Idle/Walk/Gallop durations. The exporter bakes at half-frame steps with simplification disabled. There is no runtime IK solver, deliberately stretched bone or object/root-motion curve.

Walk is authored for 1.5 m/s and a four-beat LH→LF→RH→RF sequence; gallop is authored for 8 m/s and fixed left lead RH→LH→RF→LF. The sequence choices follow [Montana State University's locomotion reference](https://animalrangeextension.montana.edu/equine/locomotion.html) and [Ashley Griffin, University of Kentucky, on gallop](https://horses.extension.org/horse-gallop/). Numeric duty cycles, timing offsets, target heights and amplitudes are original design choices, not motion-capture or published biomechanical measurements. No reference photograph or animation frames were copied into the asset.

Candidate-06's Blender reimport report sampled 121 phases per cycle. At the authored constant speed, worst residual sole-centroid stance travel was **0.081 mm for Walk** and **7.119 mm for Gallop**. Those values use analytical forward travel to assess an in-place clip; they are **not Unity foot-sliding measurements**. The report also found no armature/empty object-matrix drift and no deliberate scale animation. Detailed method, runtime integration and pending checks belong in [the visual-depth checkpoint](Reins-Visual-Depth-Checkpoint.md).

The source still has no independent hoof/pastern joint, so sole clearance does not prove flat hoof contact. There is one gallop lead, no authored lead changes, and no accepted blend/turn/brake planting or finished rider synchronization. Keep the CC0 attribution/source record, the original FBX identity and the earlier checkpoint evidence. The new export does not complete the visual milestone or authorize claims of production animation.

## Original generated environment assets

- [Arena dirt generation](Reins-Dirt-Generation.md): full prompt, native dimensions/hash, base-color-only status and edge inspection. The source PNG was copied unchanged from the built-in generator. Tiling and grazing-angle quality still need scene/device review; no verified normal/roughness/displacement set is supplied.
- [Rodeo crowd generation](Reins-Crowd-Generation.md): full prompt, native dimensions/hash and measured alpha distribution. Fourteen fictional seated adults form a distant cutout strip. It is crowd-impostor art, not foreground 3D people or a full-scene backdrop; oblique views, repetition and alpha edges require review.

No paid asset purchase or API/CLI image-generation fallback was used for these images. Generated art provenance is recorded without claiming exclusive copyright or treating a generated image as a runtime screenshot.

## Acceptance still required

No new Unity test, scene validation, native build, phone installation or measured performance result is asserted by this provenance record. Attach separate dated evidence when those checks run.

Required graphics review includes correct imported materials/bones/clips, horse-camera framing throughout a complete race, no detached eyes or stretched hair, acceptable gait/ground contact, readable barrels, dirt seams, crowd transparency/repetition, preservation of authored references on scene regeneration, and measured draw/triangle/texture/frame-time costs. Compare actual Unity captures and physical-phone footage with the user reference; one attractive still does not satisfy the throughout-gameplay requirement.

The previously approved R2 limits and sequence remain: one finished horse/rider/alley, full-course presentation, then physical-device qualification. More arenas and crowd detail do not replace that acceptance work.

## Original skinned hair follow-up

The earlier character-motion checkpoint kept the CC0 horse/rig unchanged and deactivated its two unweighted rigid hair objects. `ReinsHairBuilder` authors a separate skinned mesh and a deterministic 256 × 512 strand atlas from code, without reference pixels or a new downloaded asset. Four existing bones and at most two weights per vertex drive 118 cards (2,748 vertices; 2,512 triangles). Generated source lives under `Assets/_Project/Art/Reins/Hair`. The [motion checkpoint](Reins-Character-Motion-Checkpoint.md) retains that checkpoint's geometry, hashes, tests, moving review and limitations; candidate-06 updates the horse animation after that historical result.
