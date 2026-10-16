#!/usr/bin/env bash
# Runs all offline tests: the .NET tests (test-dotnet.sh), then the UI's (test-ui.sh). Explicit tests
# (e.g. the live Open-Meteo test) are skipped. Extra arguments are passed to dotnet test, e.g.
# ./scripts/test-all.sh -c Release
set -euo pipefail

cd "$(dirname "$0")/.."

./scripts/test-dotnet.sh "$@"
./scripts/test-ui.sh
