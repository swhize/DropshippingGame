#!/bin/bash
# Compile-Check gegen die echten Unity-6.3-DLLs. Aufruf: tools/compilecheck/build.sh [repo]
# Voraussetzung: setup.sh einmal ausgeführt. Ausgabe: CORE/RUNTIME/EDITOR OK oder Fehlerliste.
R=$(cd ${1:-$(dirname $0)/../..} && pwd)
W=${CC_WORK:-/tmp/claude-0}
U=$W/u63/opt/unity/Editor/Data
OUT=$W/uc/out; mkdir -p $OUT
SDK=$(ls -d /usr/lib/dotnet/sdk/*/ | head -1)
CSC="dotnet ${SDK}Roslyn/bincore/csc.dll"
NS="-r:$U/NetStandard/ref/2.1.0/netstandard.dll"
ENG=""; for f in $U/Managed/UnityEngine/UnityEngine*.dll; do ENG="$ENG -r:$f"; done
DEF="-define:UNITY_6000_3_OR_NEWER;UNITY_6000_0_OR_NEWER;UNITY_2023_3_OR_NEWER;UNITY_2023_2_OR_NEWER;UNITY_2023_1_OR_NEWER;UNITY_2022_3_OR_NEWER;UNITY_2022_2_OR_NEWER;UNITY_2022_1_OR_NEWER;UNITY_2021_3_OR_NEWER;UNITY_2021_2_OR_NEWER;UNITY_2021_1_OR_NEWER;UNITY_2020_3_OR_NEWER;UNITY_2020_2_OR_NEWER;UNITY_2020_1_OR_NEWER;UNITY_2019_4_OR_NEWER;UNITY_2019_3_OR_NEWER;UNITY_2019_2_OR_NEWER;UNITY_2019_1_OR_NEWER;UNITY_2018_4_OR_NEWER;UNITY_2018_3_OR_NEWER;ENABLE_INPUT_SYSTEM;UNITY_STANDALONE;UNITY_STANDALONE_LINUX;ENABLE_MONO;UNITY_INPUT_SYSTEM_ENABLE_UI;UNITY_INPUT_SYSTEM_ENABLE_PHYSICS"
NOWARN="-nowarn:1701,1702,0436,0618,0649,0169,0414,0067,0219,0162,0168"
common="-nologo -nostdlib -noconfig -target:library -langversion:9 $NOWARN -unsafe $NS $DEF"
UGUI=$U/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/UGUI
[ -f $OUT/UnityEngine.UI.dll ] || $CSC $common $ENG -out:$OUT/UnityEngine.UI.dll -recurse:"$UGUI/*.cs" >$OUT/ugui.log 2>&1 || { echo "UGUI FAIL"; grep -m10 error $OUT/ugui.log; }
IS=$W/isys/Packages/com.unity.inputsystem/InputSystem
if [ ! -f $OUT/Unity.InputSystem.dll ]; then
  files=$(find $IS -name "*.cs" -not -path "*/Editor/*" -not -path "*/Plugins/InputForUI/*" -not -path "*/Tests/*")
  $CSC $common $ENG -r:$OUT/UnityEngine.UI.dll -out:$OUT/Unity.InputSystem.dll $files >$OUT/is.log 2>&1 || { echo "INPUTSYSTEM FAIL"; grep -m10 error $OUT/is.log; }
fi
$CSC $common $ENG -out:$OUT/URPStub.dll $(dirname $0)/URPStub.cs > $OUT/urp.log 2>&1 || { echo "URPSTUB FAIL"; head $OUT/urp.log; }
S=$R/Assets/DropshippingGame/Scripts
$CSC $common $ENG -out:$OUT/Core.dll -recurse:"$S/Core/*.cs" > $OUT/core.log 2>&1 && echo "CORE OK" || { echo "CORE FAIL"; grep error $OUT/core.log | sed "s|$R/||" | head -40; }
$CSC $common $ENG -r:$OUT/Core.dll -r:$OUT/UnityEngine.UI.dll -r:$OUT/Unity.InputSystem.dll -r:$OUT/URPStub.dll -out:$OUT/Runtime.dll -recurse:"$S/Runtime/*.cs" > $OUT/rt.log 2>&1 && echo "RUNTIME OK" || { echo "RUNTIME FAIL"; grep error $OUT/rt.log | sed "s|$R/||" | head -80; }
ED=""; for f in $U/Managed/UnityEngine/*.dll; do ED="$ED -r:$f"; done
$CSC $common $ED -r:$OUT/Core.dll -r:$OUT/Runtime.dll -r:$OUT/UnityEngine.UI.dll -r:$OUT/URPStub.dll -out:$OUT/Editor.dll -recurse:"$S/Editor/*.cs" > $OUT/ed.log 2>&1 && echo "EDITOR OK" || { echo "EDITOR FAIL"; grep error $OUT/ed.log | sed "s|$R/||" | head -40; }
