# Horse coat and western hat — source checkpoint

This increment refines the visible character toward the supplied racing/stable/gear references. The development horse gains an anatomical directional coat reflection and restrained surface variation. An original cattleman hat replaces the earlier simple hat in both the development character and the saved Reins racing scene. No extra catalog slot, progression, ownership or performance bonus is introduced.

## What changed

`HeroHorseCoatBuilder` duplicates the imported body with authored tangent directions that follow the trunk, neck and forehead. GPU skinning carries those directions with the horse. Positions, normals, vertex reflectance, UVs, indices, all four skin influences and bind poses are retained exactly. Rebuilding an existing native mesh through the generic variable-influence setter changed its packed weight representation; this builder now restores the original four-weight layout after persistence. The reopened-mesh regression protects that fix.

The opaque `Horse Surface` shader uses the installed render pipeline's anisotropic GGX function, actual light direction and shadow attenuation. Short-coat grain fades before becoming subpixel. RGB remains linear reflectance and vertex alpha remains a bare-surface mask, never transparency. Eye materials disable coat sheen. Shadow, depth and depth-normal passes remain present. No bitmap, paid asset or new third-party dependency is added.

`WesternHatBuilder` authors a closed cattleman crown, rolled brim, leather ribbon and small sewn stitches. It measures the preserved rider FBX, so rebuilding does not repeatedly enlarge its own output. The 1,200-triangle / 842-vertex mesh uses the existing head binding and one opaque material; felt and leather use separate vertex mask values. Racing keeps the head/hat in shadow-only mode for first person. The shared hat is also available to the existing presentation ghost. It is fixed outfit art, not an owned/equippable hat category.

The saved racing scene changes only the hat mesh/material binding and its bounds. The coat and replacement horse remain in the isolated development scene, excluded from player builds. The stable render temporarily places that candidate in the actual barn; this neutral inspection is not saved over MyStable and is not catalog or animated stowed-rein integration. The twenty existing local cosmetics/five slots and one real horse remain unchanged.

## Verification and visible limits

Actual Unity 6000.6.0f1 renders include coat comparison, hat close-up, character side/quarter views, a neutral stable inspection and the saved racing rider. The material comparison uses the current shader with the previous settings and original tangents as its control; it is not a byte-exact previous-version render. Directional pixel probes show 2.333 mean RGB-byte change when the flow is rotated, zero when coat sheen is disabled, and 4.0 with the effect enabled versus disabled. All compared geometry/color/skin/UV channels are identical; maximum tangent/normal dot is 0.00001248.

The new moving review captures the real five-gait speed driver and flat-floor correction from side and first-person views. Scoped per-render skin refresh remains required. It is an in-place, silent controlled review, not measured phone FPS or natural world foot planting. The prior source fidelity discrepancy was retested and remains open: 0.364 mm versus the retained 0.2 mm target. No tolerance was widened.

Candidate character cost is now **93,076 triangles / 20 renderers / 25 material slots**. The live saved-rider proof records **79,288 triangles / 21 renderers / 26 slots**, an increase of 674 triangles from the hat replacement. Both remain above the 60k/six-slot targets, with no lower LODs. The additional coat mesh and lighting calculations require mobile memory/GPU measurement.

The coat highlights and western silhouette improve, but the current art remains below the photographic references. Mane roots/card edges, face and clothing, saddle construction, shoulder deformation, natural turns/braking/Wrap and stable rein draping remain visible work. The simple hat crown also needs further authored surface refinement. These renders are actual engine output; no reference pixels or generated promotional image substitute for the game.

**10/10 focused Play Mode checks pass, zero skipped**, covering reopened body channels/opaque passes, rigid head attachment/bodycam shadows, live gait/ground/camera, rider reach and existing ghost behavior. All **655 prior evidence files remain unchanged**. No new full-suite result is claimed.

Fresh test results, counts, source/capture hashes and historical preservation are recorded in [CharacterFinish-Checkpoint.json](../../Evidence/CharacterFinish-Checkpoint.json). Older evidence remains at its original revision. Neither existing verified 0.5 native artifact was rebuilt or installed. The last verified installed iPhone remains 0.4.0/build 4; both 0.5 packages remain unqualified on phones.

## Reproduce and continue

Set `BARREL_HORSE_BENCHMARK_OUTPUT` to a fresh scratch directory and use one Unity Editor per project. `HeroHorseBenchmarkBuilder.RebuildFinish` updates the saved development coat/hat and captures the material inspection. `UpdateSavedPlayerHat` updates only the racing hat before applying unsaved inspection poses. `CaptureLocomotion` records the current moving character. Encode those frames with `Tools/Art/horse_study/encode_locomotion.py` and verify decoding with `verify_video.swift`.

Keep the accepted Reins input, Core authority, replay/save boundary, catalog IDs and free-asset policy intact. Next prioritize visible grooming/clothing/tack and natural action refinement, reduce character costs with measured LODs, then explicitly integrate the replacement into race/stable/ghost presentation and qualify new native builds. Preserve all eight categories/53 section IDs and the progression/multiplayer/release scope; no section or R2 quality gate is completed here.
