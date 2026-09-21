# Fitted horse and walk study

This editable candidate advances S10/S12 but is **not integrated or visually accepted**. Current race/MyStable scenes and both verified 0.5 mobile artifacts still use the existing CC0 horse.

- `Source/StaticHorse39k.fbx`: pinned reduced b2przemo body, 19,502 vertices / 39,000 triangles, existing UVs, no supplied maps.
- `Candidate/HeroHorse-RigStudy.fbx`: 34 bones (one non-deforming root), at most four influences, no locomotion.
- `Candidate/HeroHorse-WalkStudy.blend` and `.fbx`: editable 1.067-second walk, independently controlled pasterns/hooves. Object root stays fixed; the visual skeletal root compresses by 114–126 mm. This is not Core/root motion.
- [Attribution](ATTRIBUTION.md) and [provenance](PROVENANCE.json) travel with every derivative.

[Checkpoint and limits](../../Docs/Art/Reins-Horse-Rig-Study.md) · [motion preview](../../Evidence/HorseRigStudy-Walk.mp4) · [roundtrip results](../../Evidence/HorseRigStudy-Roundtrip.json).

## Rebuild and inspect

Use Blender 4.5.13 LTS with automatic script execution disabled. Run from the repository root; choose a scratch output directory outside Unity Assets. No add-ons, downloaded Python packages or network access are needed. The tools use Blender's bundled Python, NumPy, mathutils and FBX exporter.

```bash
BARREL_BLENDER="/path/to/Blender.app/Contents/MacOS/Blender"
"$BARREL_BLENDER" --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Art/horse_study/fit_rig.py -- /tmp/barrel-horse-study/rig
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

`verify_fbx.py` explicitly disables Blender's default one-frame animation import offset, then compares geometry at the same time. It checks hierarchy, neutral vertex mapping, skin weights, UV corners/normals, 257 animated samples, loop closure, scale, sole orientation and reference-speed contact travel. A passing result does not accept biomechanics, every self-intersection, Unity conversion or phone cost. File hashes can change with FBX exporter metadata; validate semantic measurements as well as pinning distributed binaries.

The fixed 32-frame study is intentional. If changing duration or gait, update the renderer, encoder and independent check's expected cycle/phase contract together. The macOS `verify_video.swift` helper separately decodes six movie frames with AVFoundation; it is not required to generate the 3D asset.

## Integration gate

First improve shoulder/groin deformation and the crouched walk/weight transfer; some diagnostic edges still stretch about threefold. Then author eye/coat treatment, hoof roll-over, remaining gait/turn/brake motions and bodycam views. The 39k body is already 9,436 triangles above the current 29,564 body before any other character parts; create appropriate topology/LODs and profile the combined character.

Do not rename the 34-bone hierarchy to imitate the old 19-bone rig. Add an explicit presentation bone-role binding and deliberately refit `ReinsHorseSurface`, hair, bridle, saddle, rider, hands and camera. Preserve ghost ownership, reduced motion, always-updating bones and the authoritative Core race root. Review actual moving Unity/player/stable captures before replacing runtime content. A new native art build must get a new build identity; preserve the verified 0.5/build 5 artifacts.
