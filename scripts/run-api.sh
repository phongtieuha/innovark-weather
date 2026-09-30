#!/usr/bin/env bash
# Runs the API locally in Development on http://localhost:5122 with dotnet watch: code changes are
# hot-reloaded, or the app restarts when a change can't be applied while running.
# (Scalar UI at /scalar, OpenAPI document at /openapi/v1.json, health check at /health.)
# Extra arguments are passed to dotnet watch run, e.g. ./scripts/run-api.sh --no-hot-reload
set -euo pipefail

cd "$(dirname "$0")/.."

# Restart automatically after edits Hot Reload can't apply (e.g. a new constructor parameter),
# instead of asking in the terminal.
export DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1

dotnet watch run --project src/Innovark.Weather.Api --launch-profile http "$@"
