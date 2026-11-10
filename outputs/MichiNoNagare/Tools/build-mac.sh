#!/bin/zsh
set -eu
MICHI_PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
MICHI_UNITY_APP="${UNITY_EDITOR_APP:-/Applications/Unity/Hub/Editor/6000.3.27f2/Unity.app}"
mkdir -p "$MICHI_PROJECT_DIR/Logs"
"$MICHI_UNITY_APP/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath "$MICHI_PROJECT_DIR" -executeMethod ProjectTools.BuildMac -logFile "$MICHI_PROJECT_DIR/Logs/build.log"
