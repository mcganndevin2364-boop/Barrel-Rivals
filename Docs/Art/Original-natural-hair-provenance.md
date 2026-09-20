# Original natural horse-hair atlas — generation provenance

September 20, 2026. Original generated texture candidate for the existing Reins horse's mane, forelock and tail cards. This record identifies the source image and intended import; it does not establish finished character art or device acceptance.

## Source and identity

- Asset: `Assets/_Project/Art/Reins/Hair/Original natural strand atlas.png`.
- Method: built-in `image_gen` generation from the original text prompt below. No API/CLI fallback, downloaded hair pack, paid purchase or reference-image pixels were used.
- Native output: **1,254 × 1,254 RGBA PNG**, **2,367,593 bytes**.
- SHA-256: **`76d95b131e996f01fac36ea5a8b6df0dd0ac04184944116e29c3716650158a21`**.
- The prompt requested 2,048 × 2,048; the tool returned 1,254 × 1,254. The native generated PNG was copied unchanged into the project, preserving transparent alpha. No Python image editing, external resizing, matte removal or retouching was performed.
- Generation record: `work/art-fit/hair-generation.json`, including the original tool-output location and exact prompt. That temporary work path is not a runtime dependency.
- No third-party artist attribution is associated with this generated image. This records provenance, not exclusive copyright or a legal-rights certification.

## Inspection and intended use

The inspected source shows eight dark brown vertical locks, each with fine strands and warmer brown variation. Roots are dense and tips are uneven, with transparent gutters and strand edges. No horse body, lettering, sponsor marks or scenery are visible. The locks remain recognizably repeated forms; this is a hair-card texture, not simulated individual hairs or a finished groom. Some tips approach the bottom edge, and faint edge pixels require alpha/filtering review in the actual material.

The source file keeps its native dimensions. `ReinsHairBuilder` sets the Unity importer maximum to **1,024**, NPOT scaling to None, clamp wrapping, mipmaps, preserved alpha coverage and a 0.36 alpha-test reference. Current source requests uncompressed texture data and anisotropy 4. Actual imported dimensions, memory residency and mobile compression choices must be verified separately; the import maximum is not a claim that the PNG on disk was resized.

The URP material uses two-sided alpha clipping, low smoothness, zero metallic and disabled environment reflections. UVs select the eight columns with internal padding and run from the dense root toward the tip. These settings are a candidate for improved strand readability; shimmering, block edges, overdraw, ghost opacity and close-up repetition still require Unity and device inspection. No normal, roughness, height or flow map is supplied by this image.

This atlas replaces the active material's earlier original procedural strand image. The earlier image and its provenance remain historical. Current card geometry, source fitting and pending checks are described in [the art-fit checkpoint](Reins-Art-Fit-Checkpoint.md). No new Unity test result, native build or phone acceptance is asserted by this provenance record.

## Complete generation prompt

```text
Use case: photorealistic-natural. Asset type: production 3D game hair-card RGBA texture atlas for a dark bay horse, not a scene or screenshot. Create a square 2048x2048 atlas with exactly eight separate narrow vertical horse mane and tail hair bundles in eight equal-width columns. Each bundle begins with dense dark brown-black roots near the top, follows almost straight natural long individual hairs down the column, and ends in uneven sparse wispy tapered tips near the bottom. Mix dense central locks and fine flyaway strands with subtle warm brown highlight variation, photoreal natural horse hair, flat neutral diffuse light, no baked dramatic lighting or shadow. Every bundle fits within its own column with clear transparent gutters. All eight roots begin at the same height and tips end at varied heights near the bottom. True transparent alpha background, transparent between individual strands, no white matte, no visible checkerboard, no letters, no numbers, no labels, no head, no horse body, no braids, no knots, no objects. Flat orthographic texture sheet, no perspective. Color detail must remain visible but very dark brown, not orange.
```
