# Original separated hair atlas

This original game texture was generated with the built-in image-generation tool and used unchanged as RGBA data. No paid asset pack, external API or reference-image pixels were used.

- Asset: `Assets/_Project/Art/Reins/Hair/Original separated strand atlas.png`.
- Source output: `exec-eeee2aed-c8ac-4c5f-b320-5322ac3c3134.png`.
- SHA-256: `7e9ffd61284340429ca8fae65ab6cde07ac515953a4f921fd2fd7efa69e1f236`.
- Native image: 1254 × 1254, RGBA. Unity imports at maximum 1024, with alpha clipping 0.36 and coverage-preserving mipmaps.
- Eight vertical UV columns, roots at the top. The previous natural atlas supplies the short dense undercoat; this separated atlas supplies the longer outer locks. Both are clipped and share one skinned mesh with two material slots.
- The first candidate remained too opaque and was rejected. A targeted edit of that candidate supplied genuine through-gaps: mean central pass coverage at the 0.36 cutoff was 34.0%, versus 97.1% for the preceding shipped atlas. These are source-image analytical measurements, not GPU overdraw measurements.
- The generated RGB was lighter than the preceding atlas. A material multiplier of (0.24, 0.25, 0.28) restores a dark brown appearance without changing source alpha.
- Actual race/stable/moving Unity captures, and the remaining visual limitations, belong to [the groom checkpoint](Reins-Hair-Groom-Checkpoint.md).

## Final edit prompt (exact)

```text
Edit this game hair atlas. Keep the square format, exactly eight equal-width vertical UV columns, dark neutral brown fiber color and roots at the top. Change the ALPHA COVERAGE radically: REMOVE about 80 percent of the fibers everywhere. Each column must contain ONLY 12 to 18 separated very thin long wavy individual filaments, each just 1 to 3 pixels thick, with wide completely TRANSPARENT gaps between them extending from root to tip. No filled lock silhouette or solid backing whatsoever. Imagine a sparse comb with long separated teeth, not a wig or dense pelt. Maintain subtle fine hair color variation. Transparent space must greatly outnumber visible hair pixels in the middle of every column. Bottom tips should end individually at different lengths. Genuine transparent RGBA, no black/white/checkerboard background. No text, labels, objects or shadows. The attached dense texture is only for column layout and color; remove most of its solid coverage.
```

## Initial generation prompt (exact, rejected candidate)

```text
Create an original production game texture atlas of dark bay horse mane and tail HAIR STRANDS on a genuinely transparent RGBA background. Square image. Exactly EIGHT separate equally spaced vertical columns, each confined to its own one-eighth-width UV cell with a transparent gutter; hair roots near the top and naturally tapered individual strand tips near the bottom. This is a sparse HAIR-CARD texture, not eight opaque fur strips. In each column show only roughly 30–45 very fine individually separated long filaments, with substantial clearly transparent gaps THROUGH the entire lock. Target approximately 30–40 percent visible hair coverage per cell. A little more density only in the top 5 percent near the roots; progressively fewer strands toward the tips. Each lock has subtle different flowing waviness and gently separated subclumps, but the strands remain mostly vertical and parallel. Long smooth horse hair, deep neutral espresso brown and almost black with fine muted brown variation, uniform diffuse illumination, no painted bright highlights, no shadow underneath. Show individual glossy fibers without a solid backing, without fuzz, without a pelt or feather shape. Transparent background must continue between the individual strands, not just around the outside of a filled silhouette. No horse, skin, rider, objects, text, border, contact shadow, checkerboard, gray/white/black background or watermark. Orthographic flat texture, not a perspective scene. Keep top roots and lowest fine tips fully inside the image with a small transparent margin. High resolution suitable for a 2K Unity hair texture.
```
