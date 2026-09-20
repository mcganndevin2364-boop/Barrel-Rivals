#!/usr/bin/env python3
"""Convert the recorded 513x513 F32 USGS elevation raster to editor-only BRH1 samples.

Requires Pillow. No resampling, image synthesis, normalization or geographical
reprojection occurs here. The server request/extent remain in the acquisition JSON.
"""
import argparse
import hashlib
import math
from pathlib import Path
import struct
from PIL import Image

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('source', type=Path)
parser.add_argument('output', type=Path)
args = parser.parse_args()
with Image.open(args.source) as image:
    if image.mode != 'F' or image.size != (513, 513):
        raise SystemExit('Expected the recorded 513x513 single-band float elevation raster.')
    samples = tuple(image.get_flattened_data() if hasattr(image, "get_flattened_data") else image.getdata())
if not all(math.isfinite(value) and 0 <= value <= 6000 for value in samples):
    raise SystemExit('Missing or invalid elevation sample; refusing to build a terrain source.')
data = b'BRH1' + struct.pack('<II', 513, 513) + struct.pack('<' + str(len(samples)) + 'f', *samples)
if args.output.exists() and args.output.read_bytes() != data:
    raise SystemExit('Output already contains different samples; preserve it before replacing deliberately.')
args.output.parent.mkdir(parents=True, exist_ok=True)
args.output.write_bytes(data)
print(f'{len(data)} bytes; SHA-256 {hashlib.sha256(data).hexdigest()}')
