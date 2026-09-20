# Reins arena dirt generation

Generated for the user-approved realistic arcade rodeo presentation on September 19, 2026 (local date; generator metadata may use September 20 UTC).

## Asset and provenance

- Asset: `Assets/_Project/Art/Reins/Textures/ArenaDirtAlbedo.png`.
- Method: built-in `image_gen.imagegen` tool, one generation. No API/CLI fallback, external asset download, or paid asset purchase.
- Input: original text prompt guided by the user's attached gameplay reference. The reference image was not passed to the generator; no reference pixels, sponsor logos, or image UI were copied.
- Authoring status: original AI-generated base-color candidate selected for arena integration; final runtime material/device quality is not yet approved.
- Native output: 1254 × 1254 RGB PNG, 3,548,927 bytes.
- SHA-256: `19ae14dab9d90a2cf148e29d1e2cdd3eedd706656363586243fd536a95ab1a42`.
- The prompt requested 2048 × 2048 if supported; the built-in tool returned 1254 × 1254. The native PNG was copied byte-for-byte into the project. It was not cropped, resized, retouched, or stripped of generator metadata.
- No third-party asset attribution is associated with this generated image. This document records provenance; it does not represent an exclusive copyright or legal-rights certification.
- Import the source as sRGB base color with repeat wrapping, mipmaps and mobile texture compression. Use Unity importer size/NPOT settings for runtime texture sizing; retain the native source file.

## Inspection and limits

Visual inspection found a warm neutral brown, top-down granular dirt surface with subtle irregular ruts, sparse small clumps, and no logos, lettering, HUD, horse, horizon, grass, or focal object. No obvious large cast shadows or vignette are present. Some microrelief shading is present as is common in generated material images; this is not a measured photogrammetric albedo.

The generator was asked for both-axis seamless tiling. A read-only pixel check found mean opposite-edge absolute RGB differences of 15.706 horizontally and 16.472 vertically (8-bit channel units), compared with internal adjacent-pixel means of 12.145 and 14.383. These measurements suggest no extreme edge color jump; they **do not establish true seamlessness**. Inspect repeat seams, perceived scale, grazing-angle aliasing and visible repetition on the arena ground in Unity and on phone before calling the material production-ready.

This image is only a base-color map. It supplies no verified normal, height, roughness, or displacement map. Keep broad ground shape, local hoof depressions, surface rules and collision separate from this color texture. Use a moderate repeat scale (initial intent: approximately four metres per tile) and preserve readable barrels and hoof effects.

No Unity import, scene regeneration, build, performance test, or physical-device review was performed for this asset by the generation task.

## Complete generation prompt

```text
Use case: photorealistic-natural
Asset type: Production-bound seamless PBR base color / albedo texture for a Unity mobile rodeo arena ground, a single square image, 2048 by 2048 pixels if supported.
Primary request: Original realistic well-used rodeo arena dirt, a four-metre-square patch of dry compacted tan-brown clay dirt with fine granular soil, very small irregular crumbs and subtle overlapping shallow horse hoof impressions.
Composition: Perfect orthographic overhead top-down view of the ground plane, fills the entire square edge to edge. Seamless tileable on both axes with matched opposite edges. Uniform spatial detail and scale throughout. No focal subject, no border.
Materials and palette: Natural warm neutral brown soil with gently varied muted tan patches, not yellow, not orange, not gray. Fine scattered soil grains and small clumps; hoof impressions are restrained, soft-edged and randomly overlapped. Avoid obvious repeated imprints and deep trench or furrow lines. Natural microvariation, but low-frequency value and hue variation must remain subtle for tiling over a large arena.
Lighting: Albedo/basecolor map only, evenly diffusely lit, remove directional illumination and cast shadows, no sunlit or shaded side, no ambient occlusion dark pools, no specular highlights, no vignette, no perspective shading. Terrain form is suggested only gently through color and surface texture, not strong baked lighting.
Constraints: Only dirt texture. No horse, rider, saddle, people, grass, plants, rocks larger than fine gravel, sky, horizon, fencing, barrel, text, HUD, logo, brand, watermark, painted marks, perspective, depth of field, motion blur, deep footprints, glossy wet patches or dramatic shadows. This is a neutral game material map, not a scene render.
```

