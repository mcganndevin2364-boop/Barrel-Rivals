from pathlib import Path
import hashlib
import json
import os
import struct
import subprocess
import zipfile

project = Path(__file__).resolve().parents[1]
android = Path(os.environ.get('BARREL_ANDROID_TOOLCHAIN', '/Applications/Unity/Hub/Editor/6000.6.0f1/PlaybackEngines/AndroidPlayer'))
build_tools = android / 'SDK/build-tools/36.0.0'
apk = project / 'Builds/Android/BarrelRivals-Foundation.apk'
assert apk.is_file(), 'The APK has not been produced.'
env = dict(os.environ)
env['JAVA_HOME'] = str(android / 'OpenJDK')

def run(args):
    result = subprocess.run([str(x) for x in args], capture_output=True, text=True, env=env)
    if result.returncode:
        raise RuntimeError(f'Artifact check failed: {args[0]}\n{result.stdout}\n{result.stderr}')
    return result.stdout.strip()

badging = run([build_tools/'aapt', 'dump', 'badging', apk])
metadata = [line for line in badging.splitlines() if line.startswith(('package:', 'sdkVersion:', 'targetSdkVersion:', 'native-code:'))]
assert any("name='com.barrelrivals.foundation'" in line for line in metadata)
assert "sdkVersion:'26'" in metadata
assert "targetSdkVersion:'36'" in metadata
assert "native-code: 'arm64-v8a'" in metadata
signing = run([build_tools/'apksigner', 'verify', '--verbose', apk])
run([build_tools/'zipalign', '-c', '-P', '16', '4', apk])

libraries = []
with zipfile.ZipFile(apk) as archive:
    for name in archive.namelist():
        if not name.startswith('lib/') or not name.endswith('.so'):
            continue
        data = archive.read(name)
        assert data[:6] == b'\x7fELF\x02\x01', f'Expected little-endian ELF64: {name}'
        offset = struct.unpack_from('<Q', data, 32)[0]
        size, count = struct.unpack_from('<HH', data, 54)
        alignments = []
        for i in range(count):
            header = offset + i * size
            if struct.unpack_from('<I', data, header)[0] == 1: # PT_LOAD
                alignment = struct.unpack_from('<Q', data, header + 48)[0]
                assert alignment >= 16384, f'Native load segment below 16 KiB: {name}'
                alignments.append(alignment)
        assert alignments, f'No load segments in {name}'
        libraries.append({'name': name, 'loadSegmentAlignments': alignments})
assert libraries, 'Expected native IL2CPP libraries.'
record = {
    'artifact': 'Builds/Android/BarrelRivals-Foundation.apk',
    'bytes': apk.stat().st_size,
    'sha256': hashlib.sha256(apk.read_bytes()).hexdigest(),
    'metadata': metadata,
    'signingVerification': signing.splitlines(),
    'apk16KiBAlignmentCheck': 'passed',
    'nativeLibraries': libraries,
    'deviceInstalledOrRun': False,
    'distribution': 'local development APK; debug signing; not store-ready'
}
(project/'Evidence/Android-Artifact.json').write_text(json.dumps(record, indent=2)+'\n')
print(json.dumps(record, indent=2))
