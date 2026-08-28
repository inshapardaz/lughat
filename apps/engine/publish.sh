#!/usr/bin/env bash
# Publishes the sidecar as a self-contained single-file executable for every platform Lughat
# ships on. Deliberately does NOT pass -p:PublishTrimmed=true — see the comment on
# EngineInfo/spawnEngine in apps/shell/src/engine-process.ts for why: trimming breaks
# Dapper's dynamic IL-emit deserializer (both parameter binding and record materialization),
# a known, unresolved limitation of Dapper rather than something fixable with a few
# annotations. Untrimmed self-contained is ~104MB on win-x64; trimmed was ~23MB but silently
# 500'd on most endpoints at runtime, which is a worse trade than the extra disk space.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

RIDS=("win-x64" "osx-x64" "osx-arm64" "linux-x64")
OUT_ROOT="publish"

for rid in "${RIDS[@]}"; do
  echo "Publishing Lughat.Engine.Api for $rid..."
  dotnet publish Lughat.Engine.Api/Lughat.Engine.Api.csproj \
    -c Release \
    -r "$rid" \
    --self-contained \
    -p:PublishSingleFile=true \
    -o "$OUT_ROOT/$rid"
done
