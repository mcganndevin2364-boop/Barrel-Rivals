# Fitted horse, movement, surface and groom studies

This editable candidate advances S10/S12 in an isolated Unity benchmark but is **not adopted by runtime or visually accepted**. Current race/MyStable scenes and both verified 0.5 mobile artifacts still use the existing CC0 horse.

- `Source/StaticHorse39k.fbx`: pinned reduced b2przemo body, 19,502 vertices / 39,000 triangles, existing UVs, no supplied maps.
- `Candidate/HeroHorse-RigStudy.fbx`: 34 bones (one non-deforming root), at most four influences, no locomotion.
- `Candidate/HeroHorse-WalkStudy.blend` and `.fbx`: editable 0.933-second walk, independently controlled pasterns/hooves and toe roll at push-off. Object root stays fixed; the visual skeletal root compresses by 79–95 mm and sways laterally by ±6 mm. This is not Core/root motion.
- [Attribution](ATTRIBUTION.md) and [provenance](PROVENANCE.json) travel with every derivative.

[Checkpoint and limits](../../Docs/Art/Reins-Horse-Motion-Refinement.md) · [motion preview](../../Evidence/HorseMotion-Walk.mp4) · [roundtrip results](../../Evidence/HorseMotion-Roundtrip.json).

The newer `Candidate/HeroHorse-SurfaceStudy.blend` and `.fbx` add original fitted eyes and a bay coat while preserving the body and walk. [Surface checkpoint](../../Docs/Art/Reins-Horse-Surface-Study.md) covers verification and the required Unity material conversion. This is 41,880 triangles/two slots before hair/tack/rider, not a finished hero character.

## Rebuild and inspect

Use Blender 4.5.13 LTS with automatic script execution disabled. Run from the repository root; choose a scratch output directory outside Unity Assets. No add-ons, downloaded Python packages or network access are needed. The tools use Blender's bundled Python, NumPy, mathutils and FBX exporter.

```bash
BARREL_BLENDER="/path/to/Blender.app/Contents/MacOS/Blender"
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/fit_rig.py -- /tmp/barrel-horse-study/base-rig
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/refine_weights.py -- /tmp/barrel-horse-study/base-rig /tmp/barrel-horse-study/rig
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/author_walk.py -- /tmp/barrel-horse-study/rig /tmp/barrel-horse-study/walk
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/verify_fbx.py -- /tmp/barrel-horse-study/walk
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/review_poses.py -- /tmp/barrel-horse-study/rig
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/render_walk.py -- /tmp/barrel-horse-study/walk
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/encode_review.py -- /tmp/barrel-horse-study/walk
```

To compare an editable saved candidate with a rebuild (ignoring file timestamps):

```bash
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/compare_rebuild.py -- ArtSource/HorseStudy/Candidate/HeroHorse-WalkStudy.blend /tmp/barrel-horse-study/walk/HeroHorse-WalkStudy.blend /tmp/barrel-horse-study/rebuild.json
```

`verify_fbx.py` explicitly disables Blender's default one-frame animation import offset, then compares geometry at the same time. It checks hierarchy, neutral vertex mapping, skin weights, UV corners/normals, 225 animated samples, loop closure, scale, rigid hoof shape, flat midstance, toe-pivot travel and reference-speed contact travel. A passing result does not accept biomechanics, every self-intersection, Unity conversion or phone cost. File hashes can change with FBX exporter metadata; validate semantic measurements as well as pinning distributed binaries.

The fixed 28-frame study is intentional. If changing duration or gait, update the renderer, encoder and independent check's expected cycle/phase contract together. The macOS `verify_video.swift` helper separately reads `video-spec.json` and decodes six movie frames with async AVFoundation; it is not required to generate the 3D asset.

## Integration gate

Continue improving shoulder/groin deformation and the crouched walk/weight transfer; some diagnostic edges still stretch up to 2.317-fold, improved from 3.011 but still visibly unresolved. Refine the current eye/coat/groom treatment and hoof roll-over, then add the remaining gait/turn/brake motions; continue rider-view reviews. The 39k body is already 9,436 triangles above the current 29,564 body before any other character parts; create appropriate topology/LODs and profile the combined character.

Do not rename the 34-bone hierarchy to imitate the old 19-bone rig. Add an explicit presentation bone-role binding and deliberately refit `ReinsHorseSurface`, hair, bridle, saddle, rider, hands and camera. Preserve ghost ownership, reduced motion, always-updating bones and the authoritative Core race root. Review actual moving Unity/player/stable captures before replacing runtime content. A new native art build must get a new build identity; preserve the verified 0.5/build 5 artifacts.

## Rebuild the face and coat

After the walk stage above, run from the repository root:

```bash
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/author_surface.py -- /tmp/barrel-horse-study/walk/HeroHorse-WalkStudy.blend /tmp/barrel-horse-study/surface
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/verify_surface.py -- /tmp/barrel-horse-study/surface
cp /tmp/barrel-horse-study/walk/walk-study.json /tmp/barrel-horse-study/surface/walk-study.json
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/verify_fbx.py -- /tmp/barrel-horse-study/surface HeroHorse-SurfaceStudy
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/render_surface.py -- /tmp/barrel-horse-study/surface
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/render_surface.py -- /tmp/barrel-horse-study/surface --motion
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/encode_surface.py -- /tmp/barrel-horse-study/surface
```

The still renderer uses Cycles CPU; the moving material review uses Cycles CPU at eight samples. Rendering is offline, and its frame rate is not a device benchmark. Neither renderer configures the Unity game. Preserve `CoatColor` and `EyeColor` as linear data during FBX import; `CoatColor.a` is a bare-surface mask, never opacity. The `.blend` is authoritative for the procedural materials. No downloaded maps or bitmap source is required.

## Rebuild the fitted groom

The latest `Candidate/HeroHorse-GroomStudy.blend` / `.fbx` adds a fitted mane, forelock and tail with five hair-only helper bones. Body/eyes/base walk remain identical to the surface candidate. The complete horse candidate is **48,504 triangles / four slots / three renderers / 39 bones**, before tack or rider. A copy is now in the isolated Unity development benchmark; the racing/stable/ghost horse is unchanged. See [groom checkpoint](../../Docs/Art/Reins-Horse-Groom-Study.md) for the actual review and limits.

Run after the face/coat stage, from the repository root:

```bash
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/author_groom.py -- /tmp/barrel-horse-study/surface/HeroHorse-SurfaceStudy.blend /tmp/barrel-horse-study/groom .
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/verify_groom.py -- /tmp/barrel-horse-study/groom
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/verify_groom_clearance.py -- /tmp/barrel-horse-study/groom
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/verify_rig_extension.py -- /tmp/barrel-horse-study/surface/HeroHorse-SurfaceStudy.blend /tmp/barrel-horse-study/groom/HeroHorse-GroomStudy.blend /tmp/barrel-horse-study/groom/body-preservation.json
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/compare_surface.py -- /tmp/barrel-horse-study/surface/HeroHorse-SurfaceStudy.blend /tmp/barrel-horse-study/groom/HeroHorse-GroomStudy.blend /tmp/barrel-horse-study/groom/surface-preservation.json
cp /tmp/barrel-horse-study/walk/walk-study.json /tmp/barrel-horse-study/groom/walk-study.json
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/verify_fbx.py -- /tmp/barrel-horse-study/groom HeroHorse-GroomStudy
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/render_groom.py -- /tmp/barrel-horse-study/groom
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/render_groom.py -- /tmp/barrel-horse-study/groom --motion
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/encode_groom.py -- /tmp/barrel-horse-study/groom
```

No downloaded code, add-on, new bitmap or paid asset is required. The `.blend` packs the unchanged atlas bytes. FBX uses image basenames, and does not transfer the Blender hair/coat shader graphs; bind the existing repo PNGs explicitly when making the Unity materials. UV-V runs from root to tip; the natural/separated layers use genuine alpha cutout at 0.36. The rig gains five explicitly named `Groom*` bones; no body or eye vertex uses them. Preserve the non-deforming Root and the measured original animation rather than renaming the rig to match legacy names.

The isolated [Unity benchmark](../../Docs/Art/Reins-Horse-Unity-Benchmark.md) now has explicit renderer/bone-role bindings, opaque coat/eye and cutout hair materials, a fitted saddle and inherited camera movement. Review it before replacing player/stable/ghost content. Fit bridle/reins/rider next; the animated Unity-to-source comparison still exceeds its retained 0.2 mm target. Remaining gaits, root shading, natural hair motion, body deformation, LODs and phone budgets remain open.

## Candidate rider and tack

The [new fitting checkpoint](../../Docs/Art/Reins-Horse-Rider-Fit.md) adds the existing credited rider/gloves, fitted bridle/reins, actual boot contacts and working first-person view switching to the isolated Unity scene. It does not replace runtime. Full candidate cost is 92,402 triangles/25 slots; natural remaining gaits/actions, visual detail, LODs, source-motion precision and device qualification remain open. Use `HeroHorseBenchmarkBuilder.CaptureAttachments` with a new scratch output folder for moving/neutral showroom review; never save its temporary substitution over MyStable.

## Candidate locomotion

Read [the current gait checkpoint](../../Docs/Art/Reins-Horse-Locomotion-Study.md). `Candidate/HeroHorse-Locomotion.blend` preserves the GroomStudy and original Walk while adding four original actions. `author_locomotion.py` and `verify_locomotion.py` accept the preserved GroomStudy file and a fresh output folder. Export files carry a tiny non-rendered binding mesh so neutral matrices and Unity hierarchy remain stable; never instantiate that carrier as character art. The corrected Unity capture refreshes skinning per explicit render; earlier movies are historical and cannot establish frame-by-frame deformation. Current speed blending/flat-floor correction are development-only, with no player/stable/ghost replacement.

## Unity coat and shared western hat

The [character finish checkpoint](../../Docs/Art/Reins-Character-Finish-Checkpoint.md) adds a Unity-only tangent mesh and opaque directional coat shader. Offline body, rig, gait, color and atlas sources remain unchanged. The hat is original project geometry generated by `WesternHatBuilder`, fitted to the preserved credited CC0 rider source. No new external assets or reference-image pixels are included. Its saved racing binding does not adopt the candidate horse or add wardrobe ownership.
