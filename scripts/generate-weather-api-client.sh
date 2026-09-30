#!/usr/bin/env bash
# Regenerates the web app's Kiota client for Innovark.Weather.Api after the API changes:
# builds the API, which writes docs/openapi/Innovark.Weather.Api.json, then regenerates
# src/Innovark.Weather.UI/Innovark.Weather.Web/WeatherApi from it with the settings in its
# kiota-lock.json. Kiota is pinned in .config/dotnet-tools.json.
set -euo pipefail

cd "$(dirname "$0")/.."

dotnet tool restore
dotnet build src/Innovark.Weather.Api
dotnet kiota update -o src/Innovark.Weather.UI/Innovark.Weather.Web/WeatherApi
rm -f src/Innovark.Weather.UI/Innovark.Weather.Web/WeatherApi/.kiota.log
