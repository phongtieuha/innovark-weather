#!/usr/bin/env bash
# Runs the web app locally in Development on http://localhost:5048: the Vite dev server
# (http://localhost:5173, React Fast Refresh) serves the client code, dotnet watch serves the Razor
# page and its endpoint, and the API it calls runs on http://localhost:5122 (as run-api.sh).
# Needs bun. Installs the client packages on first run.
# Extra arguments are passed to the web app's dotnet watch run.
set -euo pipefail

cd "$(dirname "$0")/.."

[[ -d src/Innovark.Weather.UI/node_modules ]] || (cd src/Innovark.Weather.UI && bun install)

# On exit (Ctrl+C, kill, or dotnet watch ending), stop everything this script started, including
# the Vite dev server that bun spawns as a grandchild.
trap 'trap - EXIT; kill 0 2>/dev/null' EXIT

(cd src/Innovark.Weather.UI/Innovark.Weather.Web && bun run dev) &

export DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1

dotnet watch run --project src/Innovark.Weather.Api --launch-profile http --non-interactive &

dotnet watch run --project src/Innovark.Weather.UI/Innovark.Weather.Web --launch-profile http "$@"
