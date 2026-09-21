# Western rider source and modifications

The rider uses **CC0** MakeHuman assets, not the user's reference-image pixels. Its body/rig, skin, eyes, ponytail and button-up shirt/jeans come from MakeHuman system assets. The brown ankle boots are **Margaret Toigo / MRT**, `toigo_ankle_boots_male`, from Shoes 01. Their own MHCLO headers explicitly identify CC0; the publisher's checked pack tables confirm the same license. The copyright notices on system clothes/materials name Data Collection AB, Joel Palmius and Jonas Hauquier. Credits are retained even where CC0 does not require them.

Primary publisher records:

- [MPFB v2.0.17 and source/asset license distinction](https://github.com/makehumancommunity/mpfb2/tree/v2.0.17)
- [MakeHuman system asset list and licenses](https://static.makehumancommunity.org/assets/assetpacks/makehuman_system_assets.html)
- [Shoes 01 asset list, author and licenses](https://static.makehumancommunity.org/assets/assetpacks/shoes01.html)
- [MakeHuman licensing explanation](https://static.makehumancommunity.org/about/license.html)

`Source-Provenance.json` pins all three downloaded archives by SHA-256, the selected authoring parameters, mesh/bone inspection and every exported image/FBX. `CC0-1.0.txt` contains the upstream asset terms. The GPLv3 MPFB program remains an external authoring dependency; its program files are not included in the game. No paid asset or account service was used.

## Reproduction

Use the pinned Blender 4.5.13 Intel build with `--background --factory-startup --disable-autoexec --python-exit-code 1`. Run `Tools/Art/export_western_rider.py`, followed by `--` and four directory arguments: the extracted MPFB source root, extracted system pack, extracted Shoes 01 pack, and a new output directory. The script keeps this authoring session's MPFB preferences/cache in that output directory; it does not install a global add-on. Copy its `Rider-candidate.fbx` to `WesternRider.fbx`; the seven selected source texture basenames are recorded in the manifest.

Original project work adds a felt cowboy hat, sets an adult rider shape, bakes its active body morphs, removes hidden body/helper geometry and exports the seated-rig inputs. Clothing is fitted to the body by MPFB. The body uses its supplied game-engine skeleton (53 authoring bones). The original hat is weighted to the head. Existing project gloves remain equipped at the wrists; no duplicate ungloved hands are exported. These are authoring and integration changes, not a claim of finished character quality.

Unity must import the FBX as **Generic**, retaining actual skinned surfaces. `None` produces static renderers in the tested import and cannot follow bone posing. Import animation is disabled: the existing horse gait and the project's original rider limb solver own presentation. Explicit URP materials reference project textures, so operation does not depend on the authoring machine's FBX texture paths. Normal maps are imported as normal maps, texture size is limited to 2K at import, and rider renderers explicitly use four bone weights. A source clothing normal map is 4K; the Unity runtime import limit is 2K.

The initial export contained live body morph targets, which were baked before final mesh trimming/export. The first static import and first wrong-unit deformation measurement are rejected diagnostic iterations, not accepted evidence. Current verification and remaining character/device limits belong in the rider checkpoint. Do not change race simulation, collision bounds, timing, controls, saves, rewards or replay version to accommodate this art.
