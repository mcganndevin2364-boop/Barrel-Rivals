#!/usr/bin/env bash
set -euo pipefail
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
EDITOR="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity}"
ACTION="${1:-validate}"
mkdir -p "$PROJECT_DIR/Logs/reins" "$PROJECT_DIR/Evidence"
ARGS=(-batchmode -projectPath "$PROJECT_DIR" -logFile "$PROJECT_DIR/Logs/reins/$ACTION.log")
case "$ACTION" in
 generate) ARGS+=(-nographics -quit -executeMethod BarrelRivals.Editor.ReinsLabBuilder.Generate) ;;
 validate) ARGS+=(-nographics -quit -executeMethod BarrelRivals.Editor.ReinsLabBuilder.Validate) ;;
 editmode) ARGS+=(-nographics -runTests -testPlatform EditMode -testResults "$PROJECT_DIR/Evidence/Reins-EditMode.xml") ;;
 playmode) ARGS+=(-runTests -testPlatform PlayMode -testResults "$PROJECT_DIR/Evidence/Reins-PlayMode.xml") ;;
 android) ARGS+=(-nographics -quit -buildTarget Android -executeMethod BarrelRivals.Editor.ReinsLabBuilder.BuildAndroid) ;;
 ios) ARGS+=(-nographics -quit -buildTarget iOS -executeMethod BarrelRivals.Editor.ReinsLabBuilder.ExportIOS) ;;
 *) echo 'Usage: Tools/run-reins.sh {generate|validate|editmode|playmode|android|ios}' >&2; exit 2 ;;
esac
cd "$PROJECT_DIR"
"$EDITOR" "${ARGS[@]}"
if [[ "$ACTION" == editmode || "$ACTION" == playmode ]]; then
 python3 - "$PROJECT_DIR/Evidence" "$ACTION" <<'PY'
import sys,pathlib,xml.etree.ElementTree as ET
p=pathlib.Path(sys.argv[1])/('Reins-EditMode.xml' if sys.argv[2]=='editmode' else 'Reins-PlayMode.xml')
r=ET.parse(p).getroot()
print({k:r.get(k) for k in ('result','total','passed','failed','duration')})
if r.get('result')!='Passed' or int(r.get('total','0'))==0:raise SystemExit('No nonempty passing Unity result.')
PY
fi
