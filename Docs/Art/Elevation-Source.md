# Eagle Valley elevation source

Credit: **U.S. Geological Survey, 3D Elevation Program (3DEP)**. Retrieved September 20, 2026 from the [official 3DEP elevation service](https://elevation.nationalmap.gov/arcgis/rest/services/3DEPElevation/ImageServer).

The [3DEP program page](https://www.usgs.gov/3d-elevation-program) states that its products are free of charge and without use restrictions. The [USGS copyright policy](https://www.usgs.gov/information-policies-and-instructions/copyrights-and-credits) identifies USGS-produced data as U.S. public domain and asks for credit. This acquisition contains numerical Colorado bare-earth elevations, not third-party photographs, road maps, agency logos or sponsor graphics. No account or purchase was used.

## Preserved acquisition

[Original GeoTIFF](SourceData/EagleValley-USGS-3DEP.tif) and [complete request, returned extent and hashes](SourceData/EagleValley-acquisition.json) are tracked. The original export is 1,639,746 bytes, SHA-256 `538201d0db19a813eaa56f0f00ad16a65ac3a025d199c5305b9de6f5032b0309`.

The request selected Eagle Valley, Colorado (`-107.00,39.57,-106.80,39.73` in EPSG:4326), a 513 × 513 output in EPSG:3857, Float32 TIFF, bilinear interpolation and the service's `None` raster function. The server adjusted the final extent for square pixels; the acquisition JSON records that exact returned extent. All 263,169 source samples are finite, from 1,891.6171 to 2,976.7856 metres; the center sample is 1,947.87085 metres. This is a sampled export of the provider's elevation service, not a new survey or a guaranteed native resolution claim.

The temporary server TIFF URL may expire or a later request may reflect updated upstream data. Reproduce this checkpoint from the preserved TIFF, not an unpinned new network request.

## Editor data and reproduction

`Assets/_Project/Art/Reins/Premium/Mountains/Elevation/EagleValley.bytes` contains a 12-byte header (`BRH1`, UInt32LE width, UInt32LE height) followed by north-first row-major Float32LE metre samples. It is 1,052,688 bytes, SHA-256 `b78ee20eba7c8b34ad31be5c389d41824c7a598854af96560f6ab74e4e27a198`. Numeric extraction does not resample, normalize, paint or synthesize an image.

With Python and Pillow installed, from the repository root:

```sh
python3 Tools/Art/convert-elevation.py Docs/Art/SourceData/EagleValley-USGS-3DEP.tif Assets/_Project/Art/Reins/Premium/Mountains/Elevation/EagleValley.bytes
```

The converter rejects incorrect mode/dimensions, missing/non-finite samples and an existing different output. Re-running it against the pinned source reproduced the exact committed byte hash. `ReinsMountainBuilder` validates the header, byte length, dimensions and values when loading the local samples; scene generation requires no network.

## Deliberate game adaptation

The north-first grid maps onto 1,820 × 1,820 game metres, with positive Z toward the source north. Bilinear samples are offset by the center elevation, lower values are clamped to the venue floor and vertical differences scaled by 0.17. A smooth clear apron ramps between radii 150–260 m; the outermost 120 m blends below the horizon. The sampled mesh retains natural valleys and ridge variation. This is a fictional scaled backdrop, not a geographically accurate Eagle Valley venue.

The existing CC0 Rock Face and ArenaSoil albedos provide surface detail. Their original provenance and hashes remain unchanged; the new original URP shader uses world-space triplanar rock mapping, planar soil, slope/height blending and broad vertex tint variation. Soil and rock saturation are reduced in the shader before applying restrained sage/mineral tints; the original image bytes are unchanged. These textures are not aerial imagery of Colorado. The older rock normal map remains as source history but is not sampled by the new distant shader. Elevation data is read by editor tooling only; the saved static mesh renders without runtime terrain generation, streaming, collisions or gameplay authority.
