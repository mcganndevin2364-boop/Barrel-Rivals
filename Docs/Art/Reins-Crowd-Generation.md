# Reins crowd strip generation

Generated September 19, 2026 (local date; generation metadata may use September 20 UTC).

## Asset and provenance

- Asset: `Assets/_Project/Art/Reins/Textures/RodeoCrowdStrip.png`.
- Method: one built-in `image_gen.imagegen` generation. No API/CLI fallback, paid asset pack, downloaded photograph, or image-editing operation.
- Original text-described fictional adults; no real individual identity or provided reference pixels were passed into generation.
- Native output: 2079 × 756 RGBA PNG, 1,497,087 bytes.
- SHA-256: `9fe7e7a49402127dea591f5e66e5cf875f8abef2259d2e5fc863dcafbb62a01e`.
- The prompt requested a wide 1536 × 768 or 1536 × 1024 asset; the tool returned 2079 × 756. The original PNG was copied unchanged to the project, preserving metadata.
- Authoring status: distant spectator-card candidate selected for integration, not final in-game visual/device approval.
- No third-party asset attribution is associated with this generated strip. This is a provenance record, not an exclusive copyright or legal-rights certification.

## Inspection

The image contains one straight front-facing row of fourteen seated fictional adults in unbranded western casual clothing, with a mixture of cowboy hats and bare heads. No chairs, floor, arena background, fence, logo or lettering is visible. All subjects are within the frame. The generator included boots/lower legs rather than stopping at the requested knees.

Read-only pixel inspection confirmed **actual alpha**, not a black or checkerboard background:

| Alpha interval | Fraction of image |
|---|---:|
| 0 (transparent) | 57.940% |
| 1–16 | 2.358% |
| 17–63 | 0.427% |
| 64–127 | 0.390% |
| 128–229 | 0.737% |
| 230–254 | 37.997% |
| 255 | 0.151% |

The near-opaque foreground predominantly uses alpha 230–254. Use an alpha-clipped crowd material with an initial cutoff around 0.5 rather than requiring alpha exactly 255. Some faint low-alpha speckles extend into the padding; alpha clipping removes them. The substantial subject bounds at alpha >=128 are `(17,164)–(2057,599)` in top-left pixel coordinates. The whole nonzero-alpha bounds include faint noise down to the bottom edge. Material/mesh UV cropping with a little padding can reduce wasted overdraw without modifying the native source image.

## Runtime use and limits

- This is a **distant seated crowd impostor**, not a replacement for foreground 3D spectators or a stadium background.
- Use on cards aligned to actual stands, with geometry behind/below supplying seats and structure. Do not bake it across the arena horizon.
- All people face the camera front-on. Strong oblique angles expose the card limitation; use stands oriented toward the riding area and review from the entire course.
- Faces, hands, hats and silhouette edges may have generated imperfections. Minor color fringes can be visible close up; do not use for close-ups.
- Retain moderate row-to-row variation in placement/tint and avoid obvious repetition. Alpha-clipped cards should not cast full-size crowd shadows until verified.
- Import with sRGB, alpha from input, alpha-aware filtering, mipmaps, clamp wrapping and appropriate mobile compression/size. Preserve the source dimensions on disk; Unity import settings can control runtime size.
- No scene import, mesh setup, build, frame-time check or phone review was performed by this generation task. Final transparency, edge behavior and repeated-card readability need Unity/device inspection.

## Complete generation prompt

```text
Use case: photorealistic-natural
Asset type: Original transparent PNG distant spectator impostor strip for a real-time Unity mobile rodeo arena. Generate a wide image, approximately 1536 by 768 pixels or 1536 by 1024 if supported, with an actual transparent alpha background.
Primary request: A single straight horizontal row of fourteen fictional adult rodeo spectators sitting side by side, all facing the camera, naturally varied age, gender presentation, body size, facial features and skin tone. Natural realistic human proportions and faces, neutral friendly or attentive expressions. Western casual shirts, plain T-shirts, denim jackets and jeans, a few plain cowboy hats. Clothing palette varied muted cream, navy, burgundy and denim blue, with natural unbranded detail.
Composition: Orthographic/front-on group cutout, not a scene. All figures at the same depth, camera at seated chest height, no receding perspective line, no additional row behind or in front. Include every spectator completely from top of hair or hat through knees, with visible lap and bent legs; leave clear transparent padding around the entire group. Closely seated but distinguishable individual silhouettes; some small transparent gaps between people. Hands rest naturally in laps or on thighs. No waving or raised arms. Proportions consistent across the strip. No person cropped by the left, right or top edge.
Lighting: Even soft neutral diffuse light, realistic soft fabric and skin detail, no strong sun direction, no dramatic highlights, no cast shadow outside bodies.
Constraints: Genuine transparent background, not a checkerboard drawing. Only fourteen seated people. Do not show or draw seats, chairs, bench, floor, fence, building, sky, stage, arena, sign, text, logo, brand, watermark, extra spectators, wheelchairs, foreground props or backdrop. Do not add a continuous opaque rectangle behind the row. The output must function as an alpha-cutout crowd card seen from a distance.
```

