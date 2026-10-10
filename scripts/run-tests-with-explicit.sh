#!/usr/bin/env bash
# Runs all tests, including explicit ones such as the live Open-Meteo test, then the UI's
# (run-ui-tests.sh). Needs network access. Extra arguments are passed to dotnet test.
set -euo pipefail

cd "$(dirname "$0")/.."

dotnet test "$@" -- --explicit on
./scripts/run-ui-tests.sh
