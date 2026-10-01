#!/usr/bin/env bash
# Regenerates the generated API clients after an endpoint changes:
# 1. Builds the API, which writes src/Innovark.Weather.Api/openapi.json, and regenerates the web
#    app's Kiota client for it (src/Innovark.Weather.UI/Innovark.Weather.Web/WeatherApi) with the
#    settings in its kiota-lock.json. Kiota is pinned in .config/dotnet-tools.json.
# 2. Builds the web app, which writes its openapi.json (next to its .csproj), and regenerates the
#    page's Orval client for it (Innovark.Weather.Web/client/api/generated, see orval.config.ts).
set -euo pipefail

cd "$(dirname "$0")/.."

dotnet tool restore
dotnet build src/Innovark.Weather.Api
dotnet kiota update -o src/Innovark.Weather.UI/Innovark.Weather.Web/WeatherApi
rm -f src/Innovark.Weather.UI/Innovark.Weather.Web/WeatherApi/.kiota.log

dotnet build src/Innovark.Weather.UI/Innovark.Weather.Web
[[ -d src/Innovark.Weather.UI/node_modules ]] || (cd src/Innovark.Weather.UI && bun install)
(cd src/Innovark.Weather.UI/Innovark.Weather.Web && bun run generate:api)
