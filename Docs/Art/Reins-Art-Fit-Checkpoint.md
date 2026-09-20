# Reins art fit — hair, western tack and mountain depth

September 20, 2026. **Verified source checkpoint with actual Unity stills and moving capture; reference quality and phone acceptance remain unfinished.** This continues the existing Reins project and free/original art. Native identity remains **0.4.0/build 4** with `reins-lab-v1` rules. The approved **0.5/v2 moving alley and control migration remain unimplemented**. No full section of the eight-category/53-section plan is accepted by this art work alone.

## Source changes

**Hair fitted to the actual imported body.** `ReinsHorseSurface` bakes the explicit neutral Idle body, applies the renderer transform once, and converts the result into `Reference horse` local metres. The builder uses that live geometry's bounds center and triangle intersections to place the mane and forelock. Interpolated body weights supply the two strongest influences, normalized for `SkinQuality.Bone2`. The bone palette now maps the body's **19 bones**, including torso and ear influences where needed, rather than restricting all roots to the previous four-bone palette. Retaining two influences is a bounded approximation of skin that can have more influences. The final moving study was inspected, but exhaustive attachment/penetration across all clips, blends and views is not established.

The authored topology remains **118 cards, 2,748 vertices and 2,512 triangles**, with one alpha-clipped material and at most two influences per vertex. Mane roots follow the crest, side strands follow the neck surface, and the forelock samples the actual poll/forehead. Tail roots derive from the lower-tail bone's dock position, with the upper/lower tail weights changing along each card. The old rigid imported hair clumps remain disabled. This adds no physics solver, runtime mesh regeneration or animation authority.

The material uses a new original generated dark-hair atlas, with the native transparent PNG preserved. The source is **1,254 × 1,254 RGBA**; the Unity importer maximum is **1,024**, with clamp wrapping, mip coverage preservation and an alpha-test reference of 0.36. The source currently requests uncompressed import; this is a visual candidate, not a qualified mobile texture budget. Mane and tail ribbon winding now gives outward normals; the forelock keeps its upward winding. The earlier inward normals shaded strands as a black sheet under URP Lit even with culling disabled. A saved-normal regression checks the draped mane direction. Low smoothness and disabled environment reflections reduce broad shiny bands. See [the complete generation provenance](Original-natural-hair-provenance.md).

**Contoured western saddle and pad.** `StableTackBuilder` authors curved pad/skirt panels, a shallow seat, curved cantle, front swell, short horn, hanging fenders, open stirrups, rigging rings and a fitted cinch. Neutral body sampling supplies the back/belly envelope and forward origin. The saddle stays attached to the exact torso `Bone`, preserving its world bind pose. The generated saddle assembly has **11,360 triangles** across four existing material roles: woven pad, trim/stitching, leather and hardware. This count covers the authored saddle assembly, not the complete horse, rider or rein system; the four saved meshes were counted directly. Existing saddle/pad cosmetic IDs and their material bindings continue to supply appearance choices. A named saddle-horn anchor and the actual bit anchors define the stowed reins. Signed body cross-section rays place the neutral cord centers 28 mm outside the skin where needed, leaving 17 mm around the 11 mm radius at the sampled cross sections, and constrained smoothing preserves those clearances and endpoints. This is a static neutral drape; it does not deform with stable Idle motion. The new geometry does not create gear bonuses, ownership or progression.

**Original mountain ranges and foothill pines.** `ReinsMountainBuilder` replaces the coarse horizon forms with deterministic layered foothills, a main range and distant skyline, plus sparse pine geometry. The authored result is **21,728 triangles** in **six opaque material batches**, with 23,406 vertices and two generated mesh renderers. Lower ridges and local relief provide the shape. Two reviewed CC0 Poly Haven Rock Face source maps provide albedo/normal detail, and the shallow valley apron reuses the existing soil map; no external mountain scene image is used. [Rock provenance](../../Assets/_Project/Art/Reins/Premium/Mountains/Source-Provenance.md) records original bytes, authors, links and hashes. Scenery has no colliders or shadow casting and uses the existing lighting/fog. Rock layers use cylindrical horizontal UVs and actual-height vertical UVs at 35/50/70-metre feature scales, with integer full-circle wraps and periodic offsets preserving the seam. The valley alone retains floor mapping. This replaces the rejected floor-projected mapping that produced repeated diagonal texture rows on slopes. The design stays within the existing 1,000-metre far plane. These are source counts and settings, not measured draw-call or frame-time acceptance.

## Coordinate diagnosis and correction

The live Unity `Reference horse` neutral body was measured with bounds approximately **min (−0.4286, 0, −0.7656), max (0.4286, 2.12, 2.1694)**. A separate FBX/gait inspection rotates a parent around the normalized asset origin. The art builder instead rotates the imported root in place under the `Reference horse` wrapper. Those operations produce different forward origins: the live art representation is approximately **+0.70517 metres** ahead of the standalone inspection representation.

The early comparison that treated standalone forward coordinates as live model-local coordinates was therefore invalid. It **did not prove that the old hair table used world space or sat entirely beyond the head**. Do not repeat that diagnosis as an established defect. The useful correction is to fit the actual live imported geometry and its skin weights, removing the need to infer placement from a table measured in another hierarchy. The saddle uses the same measured-origin principle.

The source poll also has substantial ear-bone influence, while lower crest points blend torso and neck. A fixed neck/head blend cannot exactly track every sampled surface point. The new fitter carries body-derived weights into the authored cards, with the strongest-two limit stated above. Attachment distance and appearance must be inspected across actual rendered motion; a neutral surface match alone is insufficient.

## Preserved source and historical evidence

This pass does **not** replace the horse FBX, alter its 19-bone rig or gait curves, or change Core rules, collision, input, timing or replay contracts. Candidate-06 identity and prior clip measurements remain recorded in [the visual-depth checkpoint](Reins-Visual-Depth-Checkpoint.md). Its **110 Editor / 21 Play Mode** results and moving evidence are historical results for that checkpoint, not fresh validation of this pass. The current horse still lacks independent hoof/pastern joints and finished turning, braking, lead-change and rider animation.

The original procedural strand atlas, earlier captures and source records remain historical assets. New evidence should use distinct **`ArtFit-*`** filenames; preserve earlier evidence bytes even when older test capture paths are reused. Free-asset research and the separately licensed b2przemo comparison remain isolated under `work/asset-evaluation`; they are not imported or shipped. No asset purchase, new paid service or user-reference image pixels are part of this pass.

## Final verification for this source

| Check | Current status |
|---|---|
| Native generated atlas dimensions, bytes and SHA-256 | Inspected and recorded in the linked provenance |
| Live Unity neutral body coordinate diagnostic | Bounds/origin difference recorded above; not final art acceptance |
| Generate both scenes; save, reopen and verify persisted references | **Passed** after final hair-normal, stowed-rein and terrain revisions |
| Final Editor tests: surface fitting, retained skin channels, outward normals, weights, saddle binding and scenery | **110/110 passed**, zero skipped |
| Final Play Mode tests, unchanged canonical v1 replay and cosmetic flow | **21/21 passed**, zero skipped; canonical replay remains **35,120 ms / 0 knocks / 300 style / 2,156 frames** |
| Actual race/stable stills and moving scene captures | Final race/stable/gear captures and 220-frame Unity study recorded; outward hair shading and body-fitted tack inspected. Terrain grid removed; regular range joins remain visible. |
| Hair attachment and ghost rendering | Neutral roots/clearance and normalized saved weights passed; actual skin deformation and ghost alpha/lifetime tests passed. Sampled moving frames inspected; no full-cycle penetration or device acceptance claimed. |
| Full source/hash audit and historical evidence integrity | All **163** earlier PNG/JSON/XML/MP4 records restored byte for byte against baseline `3775b134373f5a4469ce9d0e966357942cdaa64c`; current asset/source hashes recorded. Core/contracts and horse FBX unchanged. |
| New native export, installation and physical-phone performance/comfort | **Not performed for this pass** |

Test counts or attractive stills do not establish the user's reference quality. Remaining visual gaps include coarse horse anatomy, incomplete rider/hand animation, hair-card repetition and edge behavior, tack detail, regular terrace-like terrain joins, crowd depth and full-course consistency. Review the actual rider view and diagnostic side view in motion, then qualify the representative scene on the user's iPhone and a named Android handset before accepting mobile quality.

## Reproduction

Use one Unity **6000.6.0f1** Editor and `BarrelRivals.Editor.ReinsLabBuilder.Generate`. Hair, saddle and scenery authoring occurs at generation time; `PersistentMeshAsset.Save` retains generated asset identities and native mesh data. Keep the neutral Idle sampling step before fitting or caching bind offsets. Preserve source atlas bytes and control runtime resolution through Unity import settings.

Use a fresh output directory for any moving capture. The existing controlled capture pauses scaled time and steps presentation explicitly; its encoded 25 fps is a study playback rate, not measured real-time performance. The final motion record retains source identity, sample settings and measured pose changes. Encoding at 25 fps does not assert real-time device performance.

## Current evidence

- [MyStable](../../Evidence/ArtFit-MyStable.png), [saddle selection](../../Evidence/ArtFit-SaddlesPreview.png), [rider gear](../../Evidence/ArtFit-RiderPreview.png).
- [Racing home stretch](../../Evidence/ArtFit-Race-Drive.png), [third-barrel approach](../../Evidence/ArtFit-Race-Approach-3.png), [mixed footing](../../Evidence/ArtFit-Race-Mixed-Footing.png).
- [Actual moving Unity study](../../Evidence/ArtFit-Motion.mp4), [capture metadata](../../Evidence/ArtFit-Capture.json), [verification manifest](../../Evidence/ArtFit-Checkpoint.json).
- [Editor results](../../Evidence/ArtFit-EditMode.xml), [Play Mode results](../../Evidence/ArtFit-PlayMode.xml), [historical integrity](../../Evidence/ArtFit-Historical-Integrity.json).

The movie is 220 real captured player-loop frames, 1280 × 360, encoded as 8.8 seconds at 25 fps. It pairs the rider view with a diagnostic side view. Sampled Drive leg motion spans 40.061° and neck motion 2.274°; the two fixed-camera body-only walk silhouettes differ in 6,286 lower-leg pixels. These continue the same gait source; they do not prove natural biomechanics, full foot planting or real-time performance.
