#!/usr/bin/env bash
# Runs the web app locally in Development on http://localhost:5048: the Vite dev server
# (http://localhost:5173, React Fast Refresh) serves the client code, dotnet watch serves the Razor
# page and its endpoint, and the API it calls runs on http://localhost:5122 (without hot reload).
# Needs bun. Installs the client packages on first run.
# Extra arguments are passed to the web app's dotnet watch run.
set -euo pipefail

cd "$(dirname "$0")/.."

[[ -d src/Innovark.Weather.UI/node_modules ]] || (cd src/Innovark.Weather.UI && bun install)

pids=()

# Stops a process and everything it started, children first (bun starts Vite; dotnet run and
# dotnet watch start the app).
stop_tree() {
  local child
  for child in $(pgrep -P "$1" 2>/dev/null); do stop_tree "$child" "$2"; done
  kill "-$2" "$1" 2>/dev/null || true
}

# On exit (Ctrl+C, kill, closing the terminal, or the web app's dotnet watch ending), stop everything
# this script started: SIGTERM so the apps shut down cleanly, then SIGKILL for what's left, since
# dotnet watch treats SIGTERM as "stop the app" and keeps watching. Only this script's own processes: `kill 0` would also
# hit whatever started the script when it isn't run from an interactive terminal.
cleanup() {
  trap - EXIT INT TERM HUP
  local pid
  for pid in ${pids[@]+"${pids[@]}"}; do stop_tree "$pid" TERM; done
  sleep 2
  for pid in ${pids[@]+"${pids[@]}"}; do stop_tree "$pid" KILL; done
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
trap 'exit 129' HUP   # closing the terminal; untrapped, the script would die without cleaning up

export DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1
# No dotnet watch browser script: Vite already reloads the page's code, and the script's compile-error
# overlay (#dotnet-compile-error) can get stuck as an empty grey layer over the page after a restart.
export DOTNET_WATCH_SUPPRESS_BROWSER_REFRESH=1

(cd src/Innovark.Weather.UI/Innovark.Weather.Web && exec bun run dev) &
pids+=("$!")

# The API without hot reload (use run-api.sh for that).
dotnet run --project src/Innovark.Weather.Api --launch-profile http &
pids+=("$!")

dotnet watch run --project src/Innovark.Weather.UI/Innovark.Weather.Web --launch-profile http --non-interactive "$@" &
web=$!
pids+=("$web")

# Runs until the web app's dotnet watch ends or the script is stopped (wait returns on a signal).
wait "$web"
