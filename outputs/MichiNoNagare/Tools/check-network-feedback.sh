#!/bin/zsh
set -eu
project="$(cd "$(dirname "$0")/.." && pwd)"
editor="${UNITY_EDITOR_APP:-/Applications/Unity/Hub/Editor/6000.3.27f2/Unity.app}"
mono="$editor/Contents/Resources/Scripting/MonoBleedingEdge"
mkdir -p "$project/Temp/NetworkFeedback"
"$mono/bin/mono" "$mono/lib/mono/4.5/csc.exe" -nologo -define:NETWORK_FEEDBACK_STANDALONE -out:"$project/Temp/NetworkFeedback/checks.exe" "$project"/Assets/Michi/Scripts/Core/*.cs "$project/Assets/Michi/Editor/NetworkFeedbackChecks.cs"
"$mono/bin/mono" "$project/Temp/NetworkFeedback/checks.exe"
