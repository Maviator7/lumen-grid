#!/bin/zsh
set -eu
MICHI_PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
MICHI_UNITY_APP="${UNITY_EDITOR_APP:-/Applications/Unity/Hub/Editor/6000.3.27f2/Unity.app}"
MICHI_MONO="$MICHI_UNITY_APP/Contents/Resources/Scripting/MonoBleedingEdge"
mkdir -p "$MICHI_PROJECT_DIR/Temp/PowerInspectionValidation"
"$MICHI_MONO/bin/mono" "$MICHI_MONO/lib/mono/4.5/csc.exe" -nologo -define:POWER_INSPECTION_STANDALONE -out:"$MICHI_PROJECT_DIR/Temp/PowerInspectionValidation/checks.exe" "$MICHI_PROJECT_DIR"/Assets/Michi/Scripts/Core/*.cs "$MICHI_PROJECT_DIR/Assets/Michi/Editor/PowerInspectionChecks.cs"
"$MICHI_MONO/bin/mono" "$MICHI_PROJECT_DIR/Temp/PowerInspectionValidation/checks.exe"
