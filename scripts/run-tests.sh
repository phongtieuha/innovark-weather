#!/usr/bin/env bash
# Runs all offline tests. Explicit tests (e.g. the live Open-Meteo test) are skipped.
# Extra arguments are passed to dotnet test, e.g. ./scripts/run-tests.sh -c Release
set -euo pipefail

cd "$(dirname "$0")/.."

dotnet test "$@"
