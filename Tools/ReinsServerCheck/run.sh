#!/bin/bash
set -euo pipefail
reins_tool_dir="$(cd "$(dirname "$0")" && pwd)"
reins_repo_root="$(cd "$reins_tool_dir/../.." && pwd)"
reins_task_root="$(cd "$reins_repo_root/../.." && pwd)"
reins_dotnet="${BARREL_DOTNET:-/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet}"
export DOTNET_CLI_HOME="${BARREL_DOTNET_CLI_HOME:-$reins_task_root/work/m0/dotnet-home}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 DOTNET_MULTILEVEL_LOOKUP=0
mkdir -p "$DOTNET_CLI_HOME"
cd "$reins_repo_root"
case "${1:-build}" in
  build)
    "$reins_dotnet" restore "$reins_tool_dir/ReinsServerCheck.csproj" --configfile "$reins_tool_dir/NuGet.Config" --disable-parallel -p:NuGetAudit=false
    "$reins_dotnet" build "$reins_tool_dir/ReinsServerCheck.csproj" --no-restore -c Release -m:1 -p:UseSharedCompilation=false
    ;;
  serve)
    exec "$reins_dotnet" "$reins_tool_dir/bin/Release/net8.0/ReinsServerCheck.dll"
    ;;
  smoke)
    exec python3 "$reins_tool_dir/smoke.py"
    ;;
  fingerprint)
    exec python3 "$reins_tool_dir/fingerprint.py" --write
    ;;
  generate-fixture)
    exec "$reins_dotnet" "$reins_tool_dir/bin/Release/net8.0/ReinsServerCheck.dll" --generate-fixture
    ;;
  *)
    echo "Usage: bash Tools/ReinsServerCheck/run.sh build|serve|smoke|fingerprint|generate-fixture" >&2
    exit 2
    ;;
esac
