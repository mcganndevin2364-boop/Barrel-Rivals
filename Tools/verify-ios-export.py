"""Inspect Unity's Xcode export without claiming a compiled or signed iPhone app."""
import hashlib
import argparse
import json
import plistlib
import subprocess
from pathlib import Path

project = Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--version',default='0.3.0')
parser.add_argument('--build',default='3')
parser.add_argument('--output',default='Evidence/M1-iOS-Export.json')
args=parser.parse_args()
export = project / 'Builds/iOS/BarrelRivals-Practice'
pbx = export / 'Unity-iPhone.xcodeproj/project.pbxproj'
info = export / 'Info.plist'
for path in (pbx, info, export / 'Data'):
    if not path.exists():
        raise SystemExit(f'Missing export component: {path.relative_to(project)}')

result = subprocess.run(
    ['/usr/bin/plutil', '-convert', 'json', '-o', '-', str(pbx)],
    capture_output=True, text=True, check=True)
objects = json.loads(result.stdout)['objects']
targets = [obj['name'] for obj in objects.values() if obj.get('isa') == 'PBXNativeTarget']
assert 'Unity-iPhone' in targets and 'UnityFramework' in targets, targets
configuration = []
for owner in objects.values():
    if owner.get('isa') not in ('PBXProject', 'PBXNativeTarget'):
        continue
    configurations = objects[owner['buildConfigurationList']]['buildConfigurations']
    for configuration_id in configurations:
        obj = objects[configuration_id]
        settings = obj.get('buildSettings', {})
        keep = {key: settings[key] for key in (
            'SDKROOT', 'ARCHS', 'IPHONEOS_DEPLOYMENT_TARGET', 'PRODUCT_BUNDLE_IDENTIFIER',
            'CODE_SIGN_STYLE', 'TARGETED_DEVICE_FAMILY') if key in settings}
        configuration.append({
            'owner': owner.get('name', 'Project'), 'name': obj.get('name'), 'settings': keep})

with info.open('rb') as handle:
    plist = plistlib.load(handle)
assert plist.get('CFBundleShortVersionString') == args.version, 'Unexpected app version'
assert str(plist.get('CFBundleVersion')) == args.build, 'Unexpected build number'
assert any(row['settings'].get('SDKROOT') == 'iphoneos' for row in configuration), 'Not a device export'
app_configurations = [row for row in configuration if row['owner'] == 'Unity-iPhone']
assert app_configurations, 'Missing app build configurations'
assert all(row['settings'].get('PRODUCT_BUNDLE_IDENTIFIER') == 'com.barrelrivals.foundation'
           for row in app_configurations), 'Unexpected development bundle identifier'
assert any(row['settings'].get('ARCHS') == 'arm64' for row in configuration), 'Missing ARM64 configuration'
il2cpp = export / 'Il2CppOutputProject'
assert il2cpp.is_dir(), 'Missing IL2CPP source project'
record = {
    'artifactType': 'unsigned Xcode source project',
    'path': str(export.relative_to(project)),
    'projectSha256': hashlib.sha256(pbx.read_bytes()).hexdigest(),
    'version': plist['CFBundleShortVersionString'],
    'buildNumber': str(plist['CFBundleVersion']),
    'targets': targets,
    'configurations': configuration,
    'il2cppProjectPresent': True,
    'unityExportVerified': True,
    'nativeCompilationVerified': False,
    'signedAppProduced': False,
    'iPhoneInstalledOrRun': False,
    'limitation': 'Requires Xcode native compilation, Apple signing and a physical-device run.'
}
destination = project / args.output
destination.write_text(json.dumps(record, indent=2) + '\n')
print(json.dumps(record, indent=2))
