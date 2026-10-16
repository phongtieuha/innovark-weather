#!/usr/bin/env bash
# Runs all tests, like test-all.sh, plus the live ones: the .NET tests including explicit ones (the
# live Open-Meteo tests), then the UI's (test-ui.sh). Needs network access. Extra arguments are
# passed to dotnet test.
set -euo pipefail

cd "$(dirname "$0")/.."

dotnet test "$@" -- --explicit on
./scripts/test-ui.sh
