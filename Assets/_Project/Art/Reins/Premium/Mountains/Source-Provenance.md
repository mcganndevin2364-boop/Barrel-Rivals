# Western mountain source

September 20, 2026. Current mountain geometry derives from credited USGS 3DEP elevation samples, with original game scaling, venue apron, topology, pine geometry and material code. See [elevation provenance](../../../../../../Docs/Art/Elevation-Source.md). No copied user-reference pixels or sponsor artwork is included. The texture acquisition below remains valid; its cylindrical/layered mapping descriptions record earlier candidates, superseded by the current world-mapped terrain material.

The two new 1024 × 1024 photographic material files come from [Poly Haven's Rock Face](https://polyhaven.com/a/rock_face), photographed by **Greg Zaal** and processed by **Dario Barresi**. The asset listing identifies the license as CC0. [Poly Haven's asset license](https://polyhaven.com/license) explicitly allows commercial use and redistribution; see also the [CC0 1.0 dedication](https://creativecommons.org/publicdomain/zero/1.0/). No purchase, account or paid service is involved.

Original downloaded bytes are preserved:

| Local source file | Direct original download | Bytes | SHA-256 |
|---|---|---:|---|
| `Textures/RockFace_Albedo_1K.jpg` | [Diffuse JPG, 1K](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/rock_face/rock_face_diff_1k.jpg) | 732,721 | `cce4b50517161264bdef196f5e247e328ca3739083cd6044ad3cc54d88cb82e2` |
| `Textures/RockFace_NormalGL_1K.jpg` | [OpenGL normal JPG, 1K](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/rock_face/rock_face_nor_gl_1k.jpg) | 1,139,857 | `e2682e1286c8b6aca7e01ba7b8fe1726cb6606042b9ee70d097aa67470d410ef` |

The listing describes a 2.4 m scan. For these distant original landforms, the final candidate uses cylindrical mapping around each range and actual-height vertical mapping, with approximate 35/50/70 m broad feature scales on the foothill/middle/far layers. These deliberately enlarged features are art direction, not a claim of terrain photogrammetry. Integer texture wraps and periodic offsets keep the circular join continuous and prevent aligned rows. The earlier 8 m floor projection produced visible diagonal repetition in actual Unity captures and was replaced. The normal strength is restrained and reduced on the far skyline; material tints compensate for the scan's orange color under the existing warm sun. Unity imports albedo as sRGB and normals as normal maps, with 1K limits, mipmaps, trilinear filtering, 2× anisotropy and compressed runtime textures. Source JPEGs are unchanged.

The shallow valley apron reuses the previously verified CC0 `ArenaSoil_Albedo_2K.png`; its authors/license/source hash remain in [the existing premium materials provenance](../../../../../../Docs/Art/Reins-Premium-Materials-Provenance.md). No wood, sponsor image or other unrelated texture substitutes for rock.

The second terrain candidate lowers peak amplitudes to roughly 52% of the first candidate, removes periodic radial fluting and triangle-by-triangle color selection, and uses four continuous landforms with local two-dimensional relief. Four opaque terrain materials plus two opaque pine materials retain six material batches. It is an art candidate pending the next actual Unity render and device profiling, not reference-quality or mobile-performance acceptance.
