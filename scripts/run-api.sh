#!/usr/bin/env bash
# Runs the API locally in Development on http://localhost:5122
# (Scalar UI at /scalar, OpenAPI document at /openapi/v1.json, health check at /health).
# Extra arguments are passed to dotnet run, e.g. ./scripts/run-api.sh --no-build
set -euo pipefail

cd "$(dirname "$0")/.."

dotnet run --project src/Innovark.Weather.Api --launch-profile http "$@"
