#!/bin/zsh
set -eu
MICHI_PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
MICHI_UNITY_APP="${UNITY_EDITOR_APP:-/Applications/Unity/Hub/Editor/6000.3.27f2/Unity.app}"
MICHI_MONO="$MICHI_UNITY_APP/Contents/Resources/Scripting/MonoBleedingEdge"
mkdir -p "$MICHI_PROJECT_DIR/Temp/CoreValidation"
python3 "$MICHI_PROJECT_DIR/Tools/check-input-safety.py"
"$MICHI_MONO/bin/mono" "$MICHI_MONO/lib/mono/4.5/csc.exe" -nologo -define:STANDALONE_CHECKS -out:"$MICHI_PROJECT_DIR/Temp/CoreValidation/checks.exe" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/RoadSimulation.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Editor/CoreChecks.cs"
"$MICHI_MONO/bin/mono" "$MICHI_PROJECT_DIR/Temp/CoreValidation/checks.exe"

"$MICHI_MONO/bin/mono" "$MICHI_MONO/lib/mono/4.5/csc.exe" -nologo -define:NETWORK_STANDALONE -out:"$MICHI_PROJECT_DIR/Temp/CoreValidation/network-checks.exe" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/RoadSimulation.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/NetworkSimulation.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CommunicationPeaks.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CommunicationReference.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/BatteryStorage.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/BatteryPowerCache.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/PowerInspection.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CardinalExpansion.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/DistrictRules.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/ContinuousCity.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/DataSupply.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CityMap.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Editor/NetworkChecks.cs"
"$MICHI_MONO/bin/mono" "$MICHI_PROJECT_DIR/Temp/CoreValidation/network-checks.exe"

"$MICHI_MONO/bin/mono" "$MICHI_MONO/lib/mono/4.5/csc.exe" -nologo -define:POINTER_STANDALONE -out:"$MICHI_PROJECT_DIR/Temp/CoreValidation/pointer-checks.exe" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/PointerGesture.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Editor/PointerChecks.cs"
"$MICHI_MONO/bin/mono" "$MICHI_PROJECT_DIR/Temp/CoreValidation/pointer-checks.exe"

"$MICHI_MONO/bin/mono" "$MICHI_MONO/lib/mono/4.5/csc.exe" -nologo -define:TUTORIAL_STANDALONE -out:"$MICHI_PROJECT_DIR/Temp/CoreValidation/tutorial-checks.exe" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/RoadSimulation.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/NetworkSimulation.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CommunicationPeaks.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CommunicationReference.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/BatteryStorage.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/BatteryPowerCache.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/PowerInspection.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CardinalExpansion.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/DistrictRules.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/ContinuousCity.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/DataSupply.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CityMap.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/NetworkTutorial.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Editor/TutorialChecks.cs"
"$MICHI_MONO/bin/mono" "$MICHI_PROJECT_DIR/Temp/CoreValidation/tutorial-checks.exe"

"$MICHI_MONO/bin/mono" "$MICHI_MONO/lib/mono/4.5/csc.exe" -nologo -define:CITY_STANDALONE -out:"$MICHI_PROJECT_DIR/Temp/CoreValidation/city-checks.exe" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/RoadSimulation.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/NetworkSimulation.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CommunicationPeaks.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CommunicationReference.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/BatteryStorage.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/BatteryPowerCache.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/PowerInspection.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CardinalExpansion.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/DistrictRules.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/ContinuousCity.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/DataSupply.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CityMap.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Editor/SeededCityChecks.cs"
"$MICHI_MONO/bin/mono" "$MICHI_PROJECT_DIR/Temp/CoreValidation/city-checks.exe"

"$MICHI_MONO/bin/mono" "$MICHI_MONO/lib/mono/4.5/csc.exe" -nologo -define:RESET_STANDALONE -out:"$MICHI_PROJECT_DIR/Temp/CoreValidation/reset-checks.exe" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/RoadSimulation.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/NetworkSimulation.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CommunicationPeaks.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CommunicationReference.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/BatteryStorage.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/BatteryPowerCache.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/PowerInspection.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CardinalExpansion.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/DistrictRules.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/ContinuousCity.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/DataSupply.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CityMap.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/ResetConfirmation.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Editor/ResetChecks.cs"
"$MICHI_MONO/bin/mono" "$MICHI_PROJECT_DIR/Temp/CoreValidation/reset-checks.exe"

"$MICHI_MONO/bin/mono" "$MICHI_MONO/lib/mono/4.5/csc.exe" -nologo -define:CONTINUOUS_STANDALONE -out:"$MICHI_PROJECT_DIR/Temp/CoreValidation/continuous-checks.exe" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/RoadSimulation.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/NetworkSimulation.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CommunicationPeaks.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CommunicationReference.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/BatteryStorage.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/BatteryPowerCache.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/PowerInspection.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CardinalExpansion.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/DistrictRules.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/ContinuousCity.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/DataSupply.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CityMap.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Editor/ContinuousCityChecks.cs"
"$MICHI_MONO/bin/mono" "$MICHI_PROJECT_DIR/Temp/CoreValidation/continuous-checks.exe"

"$MICHI_MONO/bin/mono" "$MICHI_MONO/lib/mono/4.5/csc.exe" -nologo -define:DATA_BALANCE_STANDALONE -out:"$MICHI_PROJECT_DIR/Temp/CoreValidation/data-balance-checks.exe" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/RoadSimulation.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/NetworkSimulation.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CommunicationPeaks.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CommunicationReference.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/BatteryStorage.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/BatteryPowerCache.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/PowerInspection.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CardinalExpansion.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/DistrictRules.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/ContinuousCity.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/DataSupply.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/CityMap.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Editor/DataBalanceChecks.cs"
"$MICHI_MONO/bin/mono" "$MICHI_PROJECT_DIR/Temp/CoreValidation/data-balance-checks.exe"

"$MICHI_MONO/bin/mono" "$MICHI_MONO/lib/mono/4.5/csc.exe" -nologo -define:SAVE_STANDALONE -out:"$MICHI_PROJECT_DIR/Temp/CoreValidation/save-checks.exe" "$MICHI_PROJECT_DIR"/Assets/Michi/Scripts/Core/*.cs "$MICHI_PROJECT_DIR/Assets/Michi/Editor/SaveChecks.cs"
"$MICHI_MONO/bin/mono" "$MICHI_PROJECT_DIR/Temp/CoreValidation/save-checks.exe"

"$MICHI_MONO/bin/mono" "$MICHI_MONO/lib/mono/4.5/csc.exe" -nologo -define:ORBIT_STANDALONE -out:"$MICHI_PROJECT_DIR/Temp/CoreValidation/orbit-checks.exe" "$MICHI_PROJECT_DIR/Assets/Michi/Scripts/Core/OrbitRotation.cs" "$MICHI_PROJECT_DIR/Assets/Michi/Editor/OrbitChecks.cs"
"$MICHI_MONO/bin/mono" "$MICHI_PROJECT_DIR/Temp/CoreValidation/orbit-checks.exe"

"$MICHI_PROJECT_DIR/Tools/check-strategy.sh"

"$MICHI_PROJECT_DIR/Tools/check-power-inspection.sh"

"$MICHI_PROJECT_DIR/Tools/check-expansion-dialog.sh"

node "$MICHI_PROJECT_DIR/Tools/check-web-save.cjs"
python3 "$MICHI_PROJECT_DIR/Tools/check-server-save.py"

"$MICHI_PROJECT_DIR/Tools/check-cardinal-expansion.sh"

"$MICHI_PROJECT_DIR/Tools/check-battery.sh"

"$MICHI_PROJECT_DIR/Tools/check-communication.sh"
