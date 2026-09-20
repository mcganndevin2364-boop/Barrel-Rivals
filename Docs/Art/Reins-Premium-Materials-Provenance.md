# Reins photographic material sources — premium direction

Acquired September 19, 2026. These are real surface and lighting assets for the existing moving Unity scene, not a generated gameplay screenshot. No paid asset, subscription or external service was purchased. Acquisition alone does not establish that the game matches the user's reference or passes mobile frame-time/texture budgets.

## Rights and source verification

The asset pages and [Poly Haven asset license](https://polyhaven.com/license) identify these assets as **CC0**. Poly Haven explicitly allows commercial use, modification and redistribution; attribution is appreciated but is not required. The [CC0 1.0 deed](https://creativecommons.org/publicdomain/zero/1.0/) and [legal code](https://creativecommons.org/publicdomain/zero/1.0/legalcode) define the dedication. We preserve creator names and links for provenance. Poly Haven's logo, website text and example/preview renders are separate site content; none of those are included as game textures.

The public [Poly Haven API](https://polyhaven.com/our-api) supplied direct source file URLs, sizes and MD5 checksums. Each download was verified against its exact advertised size and MD5. SHA-256 was computed locally for the files below. The bytes were not edited, resized, recolored or repacked during acquisition. Source PNG normal maps remain lossless; JPG files are the provider's published compressed source variants. MD5 here confirms consistency with provider metadata, while SHA-256 records the acquired bytes.

## Selected materials

| Project prefix | Official source | Creators | Source tile / role |
|---|---|---|---|
| ArenaSoil | [Brown Mud Dry](https://polyhaven.com/a/brown_mud_dry) | Rob Tuytel | 1.3 × 1.3 m dry granular soil, selected as the close-range dirt base. Contains small clods/pebbles; no oversized rock geometry or wet highlights should be implied. |
| WeatheredWood | [Weathered Brown Planks](https://polyhaven.com/a/weathered_brown_planks) | Dimitrios Savva (photography), Rico Cilliers (processing) | 1.8 × 1.8 m weathered boards, appropriate for wooden building surfaces. Align grain/plank direction in UVs. |
| CorrugatedIron | [Corrugated Iron](https://polyhaven.com/a/corrugated_iron) | Dimitrios Savva (photography), Jenelle van Heerden (processing) | Approximately 1.12 × 1.12 m galvanized roof sheet. Use on roofs/sheet panels; corrugation must not be mapped as rail-tube geometry. |
| PaintedSteel | [Blue Metal Plate](https://polyhaven.com/a/blue_metal_plate) | Rob Tuytel | 2.5 × 2.5 m worn blue painted plate, appropriate for painted gates or panel details. Paint is dielectric; do not set the entire surface to metallic 1. |
| Leather | [Brown Leather](https://polyhaven.com/a/brown_leather) | Rob Tuytel | 0.4 × 0.4 m fine-grained matte brown leather, for tack and glove materials. Keep crease scale small; not a finished glove mesh or saddle design. |
| DuskSky | [Qwantani Dusk 2 (Pure Sky)](https://polyhaven.com/a/qwantani_dusk_2_puresky) | Greg Zaal (photography), Jarod Guest (processing) | 2048 × 1024 equirectangular RGBE Radiance HDR; natural dusk clouds and warm horizon. Sky-only version does not supply mountain geometry. |

These are photographic surface/HDR sources. The provider does not document a per-file capture method for every listed surface; this record does not claim independently verified photogrammetry reconstruction for those files. Dirt, wood and leather albedo were visually inspected during acquisition. The complete rendered material response, tiling and HDR orientation must be judged in Unity.

## Original acquired files

All files are under `Assets/_Project/Art/Reins/Premium/Textures`. Total source download: **46,844,098 bytes (44.67 MiB)** across 17 files. GPU residency after import is different and must be profiled. Ground albedo/normal and wood albedo are 2048². All other surface maps are 1024². The sky is 2048 × 1024.

| File | Semantic | Bytes | SHA-256 | Direct source |
|---|---|---:|---|---|
| `ArenaSoil_Albedo_2K.png` | Diffuse | 9,827,093 | `0d20a4256657755ea98960787ad54830e5dd1ba70a5037d12a30f354e08c0463` | [download](https://dl.polyhaven.org/file/ph-assets/Textures/png/2k/brown_mud_dry/brown_mud_dry_diff_2k.png) |
| `ArenaSoil_NormalGL_2K.png` | nor_gl | 9,578,253 | `3ad892a618f546cc572eecd1ee28103f8c3a3a2d9d8b9b5667ec32fecdfee0b6` | [download](https://dl.polyhaven.org/file/ph-assets/Textures/png/2k/brown_mud_dry/brown_mud_dry_nor_gl_2k.png) |
| `ArenaSoil_Roughness_1K.jpg` | Rough | 390,385 | `31fb99629f533924b1150a7dac8d9432de85450bb45cda5033e3eb723f8dda04` | [download](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/brown_mud_dry/brown_mud_dry_rough_1k.jpg) |
| `WeatheredWood_Albedo_2K.jpg` | Diffuse | 911,358 | `070ebc4c56a6729ca73a791f7cd3bab7670eb03bfa9ac0218864a554228bcc30` | [download](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/weathered_brown_planks/weathered_brown_planks_diff_2k.jpg) |
| `WeatheredWood_NormalGL_1K.png` | nor_gl | 6,305,092 | `17e74f7e4742f6b7f385b1ca57637fd448247bb1dcfcee3dbd58f4de8d32b1b2` | [download](https://dl.polyhaven.org/file/ph-assets/Textures/png/1k/weathered_brown_planks/weathered_brown_planks_nor_gl_1k.png) |
| `WeatheredWood_Roughness_1K.jpg` | Rough | 234,672 | `b80c45969d8299ca5f21de969f0269138a5e3e78e51fc911af809683d99c5bc9` | [download](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/weathered_brown_planks/weathered_brown_planks_rough_1k.jpg) |
| `CorrugatedIron_Albedo_1K.jpg` | Diffuse | 719,557 | `7f3649dd0d056a55a910146f09b1d5298370696a90f511a255fb4c5715bb4b58` | [download](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/corrugated_iron/corrugated_iron_diff_1k.jpg) |
| `CorrugatedIron_NormalGL_1K.png` | nor_gl | 5,519,019 | `41a3a63490c19a4ce234d49222c55270dad32c7283825a3de7fa43400a4df5a0` | [download](https://dl.polyhaven.org/file/ph-assets/Textures/png/1k/corrugated_iron/corrugated_iron_nor_gl_1k.png) |
| `CorrugatedIron_Roughness_1K.jpg` | Rough | 455,677 | `1addcfc0210fc101f9f12c8e4dbd7a7d2642a5ef76699a8a485f00b5476af357` | [download](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/corrugated_iron/corrugated_iron_rough_1k.jpg) |
| `CorrugatedIron_Metallic_1K.jpg` | Metal | 251,113 | `9891bffb56bc778aa71568e7d050f4cd0c4b7a90e31b8e57340dec50255f6f70` | [download](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/corrugated_iron/corrugated_iron_metal_1k.jpg) |
| `PaintedSteel_Albedo_1K.jpg` | Diffuse | 395,923 | `a0162bffce47d4a35613a12af22571b28c18412dc5805cbb69eac343554ef750` | [download](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/blue_metal_plate/blue_metal_plate_diff_1k.jpg) |
| `PaintedSteel_NormalGL_1K.png` | nor_gl | 4,125,323 | `9f03852e15eadef3b0e6610ed6f040f2e8f0fd818ca31b76c94711e43b4c4e7a` | [download](https://dl.polyhaven.org/file/ph-assets/Textures/png/1k/blue_metal_plate/blue_metal_plate_nor_gl_1k.png) |
| `PaintedSteel_Roughness_1K.jpg` | Rough | 402,450 | `37168cf57144db28dd0743dab47fbebc09147fac919d5e2b5610b069c0b13a46` | [download](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/blue_metal_plate/blue_metal_plate_rough_1k.jpg) |
| `DuskSky_2K.hdr` | hdri | 4,612,798 | `440d1b5d29c79bea0985a13777642df6829e80399602a9d5f39bc20203ae6d1b` | [download](https://dl.polyhaven.org/file/ph-assets/HDRIs/hdr/2k/qwantani_dusk_2_puresky_2k.hdr) |
| `Leather_Albedo_1K.jpg` | Diffuse | 503,348 | `7e9e1d6566d1fd91d4ceb4966c4820361e85d53a7a828b6bab3b2a093a876cd1` | [download](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/brown_leather/brown_leather_albedo_1k.jpg) |
| `Leather_NormalGL_1K.png` | nor_gl | 1,925,895 | `6354daff40330a2fcd6c1061912876ddc79889c960975adba336b013db5edcd5` | [download](https://dl.polyhaven.org/file/ph-assets/Textures/png/1k/brown_leather/brown_leather_nor_gl_1k.png) |
| `Leather_Roughness_1K.jpg` | Rough | 686,142 | `f9739ef2da2e3d17b9ec253f5042f61b10dc10f3ae28f6cf6491650d3d1b0ca3` | [download](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/brown_leather/brown_leather_rough_1k.jpg) |

## Unity 6 / URP import and material contract

These settings are integration recommendations, not a claim that importer/material state has already been applied:

- **Albedo:** Default texture type, sRGB enabled, mipmaps enabled, Repeat wrapping, trilinear filtering, Read/Write disabled after any editor-only processing. Start with white material tint, then grade the scene deliberately rather than baking amber light into all textures. Maximum size matches the supplied 1024/2048 source.
- **NormalGL:** Texture Type Normal Map, `convertToNormalmap = false`, Flip Green Channel disabled. These are the official `nor_gl` **OpenGL / +Y tangent-space** maps. Normal data must not be sampled as sRGB color. Their authored RGB channels encode tangent X/Y/Z, with neutral normal near (0.5, 0.5, 1). Enable `_NORMALMAP` when wiring a URP Lit material. Start with normal strength 0.5–1 and inspect grazing highlights; excessive strength makes fine soil look like rock.
- **Roughness / Metallic:** Default texture type with sRGB **disabled**; maps are linear data. Roughness is 0 = smooth, 1 = rough. Metallic is 0 = dielectric, 1 = bare metal. The downloaded single-map values are in the red channel (grayscale RGB equivalents are acceptable). Soil, wood and leather use metallic 0. For the corrugated sheet, use its supplied metallic texture so oxidized areas need not behave like bare metal. PaintedSteel has no downloaded metal mask; a dielectric painted surface is the conservative starting point.
- **URP Lit packing:** A roughness texture cannot be assigned directly to `_MetallicGlossMap`. URP's metallic workflow reads **R = metallic** and **A = smoothness**, with smoothness derived from `1 − roughness`. Pack those values into a separate *derived* RGBA texture, document the transform, leave originals unchanged, import the derived map as linear, set `_SmoothnessTextureChannel = 0`, enable `_METALLICSPECGLOSSMAP`, and use `_Smoothness = 1` for an unscaled source result. Green/blue are unused for this map. If adding occlusion separately, URP Lit samples `_OcclusionMap.g`; this asset set did not download separate AO maps. No source ARM texture is being misidentified as a Unity mask.
- **Sky:** The `.hdr` stores scene-linear high dynamic range color in RGBE, with a 2:1 equirectangular projection. Use a panoramic skybox or correctly converted cubemap, not an sRGB flat backdrop. Start with a neutral tint/exposure and tune the directional light to the apparent sunset direction. Use appropriate imported HDR storage (and compatible mobile compression where supported), no Read/Write, no mip requirement for a simple sky display. Reflection filtering should be evaluated separately. The sky does not itself supply Unity shadow-casting sunlight or geometric mountains.
- **Filtering and mobile budget:** Start dirt anisotropy at 8, other surfaces at 4. Use mipmaps; compare motion shimmer and seams on a physical phone. Select ASTC/platform import quality only after checking visual artifacts and device support. Source file size is not GPU memory cost, draw count or frame-time evidence.
- **World scale:** Honor the recorded material tile width when mapping UVs. Repeated one-metre-scale patterns over an entire arena need macro variation and genuine track/decal geometry, not enlarged stones. In a 60 m span the source soil repeats roughly 46 times at its measured width; any art-directed change should be judged against horse/barrel scale.

The channel recommendation was checked against the installed URP package `Library/PackageCache/com.unity.render-pipelines.universal@8457e85b8184/Shaders/LitInput.hlsl` (`SampleMetallicSpecGloss`, `SampleOcclusion`) and [Unity's Lit material reference](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/lit-shader.html). Runtime rendering still needs verification after material assignment.

## Scope and acceptance

Acquisition changed only the new texture asset directory and this provenance record. No scene, script, shader, material or original asset was modified by this acquisition task, and no Unity Editor was launched for it. The integrating graphics change must add valid Unity metadata, persist material references, and capture real gameplay at the gate, all three barrels and finish. It must also check texture repetition, normal orientation, HDR exposure, silhouettes, draw calls and physical-device performance before describing this as premium-quality acceptance.
