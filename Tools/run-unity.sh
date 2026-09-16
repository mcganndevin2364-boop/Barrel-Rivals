#!/usr/bin/env bash
set -euo pipefail
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
EDITOR="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity}"
ACTION="${1:-validate}"
if [[ ! -x "$EDITOR" ]]; then
  echo 'Set UNITY_EDITOR to the Unity 6000.6.0f1 executable.' >&2
  exit 1
fi
mkdir -p "$PROJECT_DIR/Logs/m0" "$PROJECT_DIR/Evidence"
ARGS=(-batchmode -projectPath "$PROJECT_DIR" -logFile "$PROJECT_DIR/Logs/m0/$ACTION.log")
case "$ACTION" in
  generate) ARGS+=(-nographics -quit -executeMethod BarrelRivals.Editor.FoundationBuilder.Generate) ;;
  validate) ARGS+=(-nographics -quit -executeMethod BarrelRivals.Editor.FoundationBuilder.Validate) ;;
  render) ARGS+=(-quit -executeMethod BarrelRivals.Editor.FoundationBuilder.RenderOverview) ;;
  editmode) ARGS+=(-nographics -runTests -testPlatform EditMode -testResults "$PROJECT_DIR/Evidence/EditMode.xml") ;;
  playmode) ARGS+=(-runTests -testPlatform PlayMode -testResults "$PROJECT_DIR/Evidence/PlayMode.xml") ;;
  android) ARGS+=(-nographics -quit -buildTarget Android -executeMethod BarrelRivals.Editor.FoundationBuilder.BuildAndroid) ;;
  *) echo 'Usage: Tools/run-unity.sh {generate|validate|render|editmode|playmode|android}' >&2; exit 2 ;;
esac
cd "$PROJECT_DIR"
"$EDITOR" "${ARGS[@]}"
if [[ "$ACTION" == editmode || "$ACTION" == playmode ]]; then
  python3 - "$PROJECT_DIR/Evidence" "$ACTION" <<'PY'
import sys, pathlib, xml.etree.ElementTree as ET
path = pathlib.Path(sys.argv[1]) / ('EditMode.xml' if sys.argv[2] == 'editmode' else 'PlayMode.xml')
root = ET.parse(path).getroot()
print(root.attrib)
if root.get('result') != 'Passed' or int(root.get('total', '0')) == 0:
    raise SystemExit('Unity tests did not produce a nonempty passing report.')
PY
fi
