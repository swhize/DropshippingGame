#!/bin/bash
# Einmalig: Unity-6.3-DLLs (aus dem GameCI-Docker-Image), Input System 1.20 und dotnet holen.
set -e
W=${CC_WORK:-/tmp/claude-0}
command -v dotnet >/dev/null || (apt-get install -y -q dotnet-sdk-8.0 || (apt-get update -q && apt-get install -y -q dotnet-sdk-8.0))
if [ ! -d $W/u63/opt/unity/Editor/Data/Managed ]; then
  mkdir -p $W/u63 && cd $W/u63
  TOKEN=$(curl -s "https://auth.docker.io/token?service=registry.docker.io&scope=repository:unityci/editor:pull" | python3 -c "import sys,json;print(json.load(sys.stdin)['token'])")
  # Layer mit dem Editor aus unityci/editor:ubuntu-6000.3.25f1-base-3 (amd64)
  curl -sL -H "Authorization: Bearer $TOKEN" https://registry-1.docker.io/v2/unityci/editor/blobs/sha256:0766848213c1abdcc8677bc092b3e765ef9f8afc4e69e0fc2dabdd8c21bc33e8 \
    | tar -xz --wildcards '*Editor/Data/Managed/*.dll' '*Editor/Data/NetStandard/*' '*Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/*'
fi
if [ ! -d $W/isys ]; then
  cd $W && git clone -q --depth 1 --filter=blob:none --sparse -b 1.20.0 https://github.com/Unity-Technologies/InputSystem isys
  cd isys && git sparse-checkout set Packages/com.unity.inputsystem/InputSystem
fi
echo setup ok
