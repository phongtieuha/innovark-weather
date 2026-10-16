#!/usr/bin/env bash
# Runs the UI's tests (tests/Innovark.Weather.UI) with bun: the components' and the web page's Vitest
# tests, and a type-check of each, since Vitest runs tests without checking types. Offline. The web
# app's .NET tests in the same folder run with dotnet test. Installs the bun workspace on first run.
set -euo pipefail

cd "$(dirname "$0")/.."

[[ -d src/Innovark.Weather.UI/node_modules ]] || (cd src/Innovark.Weather.UI && bun install)

for package in tests/Innovark.Weather.UI/Innovark.Weather.Components.Tests tests/Innovark.Weather.UI/Innovark.Weather.Web.Tests; do
  (cd "$package" && bun run check-types && bun run test)
done
