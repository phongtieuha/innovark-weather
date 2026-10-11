#!/usr/bin/env bash
# Runs the .NET tests (dotnet test on the solution): unit, integration, and the web app's. Offline:
# explicit tests (e.g. the live Open-Meteo test) are skipped; test-all-with-live.sh runs them. Extra
# arguments are passed to dotnet test, e.g. ./scripts/test-dotnet.sh -c Release
set -euo pipefail

cd "$(dirname "$0")/.."

dotnet test "$@"
