#!/usr/bin/env bash
# Compile the existing Unity export without signing, provisioning or installing it.
set -euo pipefail
umask 077

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
EXPORT_DIR="$PROJECT_DIR/Builds/iOS/BarrelRivals-Practice"
XCODE_PROJECT="$EXPORT_DIR/Unity-iPhone.xcodeproj"
XCODE_DEVELOPER_DIR="${DEVELOPER_DIR:-/Applications/Xcode.app/Contents/Developer}"
XCODEBUILD="$XCODE_DEVELOPER_DIR/usr/bin/xcodebuild"
NATIVE_ROOT="${BARREL_IOS_NATIVE_ROOT:-$PROJECT_DIR/Builds/iOS/Native}"
LOG_PATH="$PROJECT_DIR/Logs/m1/ios-native.log"

if [[ $# -ne 0 ]]; then
  echo 'Usage: Tools/build-ios-native.sh (optional DEVELOPER_DIR and BARREL_IOS_NATIVE_ROOT)' >&2
  exit 2
fi
if [[ ! -x "$XCODEBUILD" ]]; then
  echo "Full Xcode not found: $XCODEBUILD" >&2
  exit 2
fi
if [[ ! -f "$XCODE_PROJECT/project.pbxproj" ]]; then
  echo 'Missing iOS export. Run Tools/run-m1.sh ios first.' >&2
  exit 2
fi

# Resolve overrides before creating folders; build products must stay outside source.
NATIVE_ROOT="$(python3 - "$NATIVE_ROOT" "$PROJECT_DIR" "$EXPORT_DIR" <<'PY'
import sys
from pathlib import Path
root = Path(sys.argv[1]).expanduser().resolve()
project = Path(sys.argv[2]).resolve()
export = Path(sys.argv[3]).resolve()
protected = [export, project / 'Assets', project / 'Packages', project / 'ProjectSettings']
if root == project or any(root == path or path in root.parents for path in protected):
    raise SystemExit('BARREL_IOS_NATIVE_ROOT must be outside source and the Unity export.')
print(root)
PY
)"

mkdir -p "$PROJECT_DIR/Logs/m1" "$NATIVE_ROOT"
printf 'Checking Xcode setup. Log: %s\n' "$LOG_PATH"
if DEVELOPER_DIR="$XCODE_DEVELOPER_DIR" "$XCODEBUILD" -checkFirstLaunchStatus >"$LOG_PATH" 2>&1; then
  :
else
  BUILD_STATUS=$?
  printf 'Xcode setup is incomplete (exit %s). Complete setup in Xcode, then retry. Log: %s\n' "$BUILD_STATUS" "$LOG_PATH" >&2
  exit "$BUILD_STATUS"
fi

printf 'Compiling unsigned iPhone app. Output: %s\n' "$NATIVE_ROOT"
# Target selection reaches the device compiler on Xcode 15.2 without requiring
# the generic iOS destination to pass its separate platform-selection check.
if DEVELOPER_DIR="$XCODE_DEVELOPER_DIR" "$XCODEBUILD" \
  -project "$XCODE_PROJECT" \
  -target Unity-iPhone \
  -configuration ReleaseForRunning \
  -sdk iphoneos \
  -jobs 2 \
  -hideShellScriptEnvironment \
  CODE_SIGNING_ALLOWED=NO \
  CODE_SIGNING_REQUIRED=NO \
  CODE_SIGN_IDENTITY= \
  "SYMROOT=$NATIVE_ROOT/Products" \
  "OBJROOT=$NATIVE_ROOT/Intermediates" \
  build >>"$LOG_PATH" 2>&1; then
  printf 'Unsigned native build succeeded. Signing and phone installation remain unfinished. Log: %s\n' "$LOG_PATH"
else
  BUILD_STATUS=$?
  printf 'Unsigned native build failed (exit %s). Log: %s\n' "$BUILD_STATUS" "$LOG_PATH" >&2
  exit "$BUILD_STATUS"
fi
