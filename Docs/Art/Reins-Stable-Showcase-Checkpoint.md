# MyStable animated tack and subject framing

This increment changes the saved MyStable scene, not only the isolated replacement-horse study. Copper now fills the central showroom area, with warmer fill light and the view hint/reset placed beside the main action buttons. The existing horse, tack, twenty local styles and five equipment slots remain connected to the saved loadout.

## Visible and runtime changes

The camera is fitted to actual visible skinned and static vertices. It retains the three-quarter view, keeping ears and hooves between the header, roster, traits and action buttons. The first bounds-corner attempt was visibly too distant and showed a side-wall post; it was replaced with the actual vertex fit before publication. Landscape and tablet captures and projected body limits check the saved result. The fill light becomes warmer and stronger; the scene retains its existing one shadowed directional light and two unshadowed point lights. This is not a new baked-lighting or measured mobile-performance result.

The stowed reins were static meshes fitted only to the neutral horse. `StableReinDrapeBuilder` now binds each of 41 surface-fitted center points to a normalized four-bone blend sampled from the nearby neutral body. Each point retains its own local offset in those bones. The endpoints attach explicitly below the saddle-horn cap and at the moving bit. At runtime `StableReinDrape` evaluates those bindings after animation and writes the existing twelve-sided cord topology at a 9 mm radius. This is controlled skinning for the existing showroom idle, not a physics rope or continuous collision solver.

Each rein owns a private dynamic mesh and reuses its vertex/normal/tangent and transform buffers. Tab changes reuse the instance; destruction releases it without altering persistent assets or another horse. No runtime colliders, body deformation, race-root movement or gameplay rules are added. Existing three-slot rein material bindings preserve the saved cosmetic dye. Rebuilt real thumbnails reflect the narrower, lower horn attachment.

The full horse is hidden during the tack collection so the closer view cannot show through gaps between panels. The collection uses its real rendered item thumbnails. Preview/equip/cancel and saved appearance still resolve through the same catalog IDs, and returning to MyStable restores the equipped look. Rider Gear still shows the interactive glove inspection. This does not add hats, shirts, levels, training, purchases or trusted ownership.

## Evidence and limits

The new focused checks cover 48 samples of the saved Idle clip, both horn/bit endpoints, body cross-sections, unchanged root transforms, independent mesh ownership, disable/enable reuse and destruction. The existing stable flow checks cover all five cosmetic slots, preview cancellation, explicit equip, restart, race appearance/ghost isolation, active-race navigation restrictions and actual UI reachability. The existing saved-saddle regression checks torso attachment through the imported gait. **Five focused Play Mode tests and one focused Edit Mode test pass, zero skipped.** Across 48 idle poses / 1,824 side cross-sections, minimum centerline clearance is 27.933 mm for the 9 mm-radius cord, with endpoint error below 0.001 mm. Rein vertices move 39.015 mm through the cycle. These measurements do not establish continuous collision or natural-motion acceptance. The active showroom character contains 51,500 triangles / 13 renderers / 18 slots; no LOD or phone budget is accepted. All **675 historical evidence files remain unchanged**.

Exact counts/results are in [the checkpoint record](../../Evidence/StableShowcase-Checkpoint.json).

The moving preview uses 120 actual Unity frames at 960×540 with the real ungraded UI over the graded world. Skin matrices refresh for each explicit render. The Idle clip is sampled at 30 Hz for four seconds and encoded at 30 fps; the clip's two-second cycle repeats twice. There is no audio. This controlled review is not phone footage, measured game FPS or proof of natural animation. Static images include the saved stable, tablet layout, saddle collection and glove inspection.

The saved scene still uses the existing production horse. The newer coat/horse candidate remains separate; [the character finish checkpoint](Reins-Character-Finish-Checkpoint.md) retains that scope. The close stable view makes the remaining coarse mane, stiff facial expression, simple saddle surfaces and anatomy more obvious. Matching the reference still needs substantial modeling, grooming, material, animation and lighting work. The wider progression, multiplayer and release requirements remain open.

Native artifacts were not rebuilt or installed. Both verified 0.5 artifacts remain uninstalled/unqualified; the last verified installed iPhone remains 0.4.0/build 4. Free/original asset policy and all eight categories/53 sections are unchanged. Earlier evidence files are preserved byte for byte under their historical names; current images use `StableShowcase-*`.

## Reproduce

Use one Unity 6000.6.0f1 Editor per project. `StableBuilder.Generate` rebuilds the stable from the saved Reins horse, binds the animated drape, renders item thumbnails and saves/reopens the showroom. Preserve historical evidence before running `StableFlowTests`, which writes earlier capture filenames. Run `StableShowcaseTests` with `BARREL_STABLE_SHOWCASE_OUTPUT` set to a fresh private directory; run `StableTackBindingTests` in Edit Mode. Encode with `Tools/Art/horse_study/encode_stable_showcase.py` and verify with the existing `verify_video.swift`.

Next work should improve actual character/tack materials, grooming and natural action in the shared game presentation, followed by replacement integration, measured LOD budgets and new native/device qualification. Do not substitute repeated isolated stills or narrow source tests for that remaining work.
