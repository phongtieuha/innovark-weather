#!/usr/bin/env bash
# Runs the offline tests with code coverage, and fails if coverage drops below the minimums below:
# 1. The .NET tests, with Microsoft.Testing.Extensions.CodeCoverage (settings in coverage.config). The
#    three projects' Cobertura files are merged by ReportGenerator into coverage/ (index.html,
#    Summary.txt, Summary.json, and Cobertura.xml for other tools).
# 2. The UI's tests (tests/Innovark.Weather.UI), with their own 80% minimums (each vitest.config.ts);
#    their reports are in each package's coverage/ folder.
# Explicit tests (e.g. the live Open-Meteo test) are skipped. Extra arguments are passed to dotnet test.
set -euo pipefail

# A ratchet, just under the coverage when these were set: raise them as coverage grows, never lower
# them to make a change pass.
MIN_LINE_COVERAGE=95
MIN_BRANCH_COVERAGE=85

cd "$(dirname "$0")/.."

rm -rf TestResults coverage
dotnet test --coverage --coverage-output-format cobertura \
  --coverage-settings "$PWD/coverage.config" --results-directory "$PWD/TestResults" "$@"

dotnet tool restore > /dev/null
dotnet reportgenerator -reports:"TestResults/*.cobertura.xml" -targetdir:coverage \
  -reporttypes:"Html;TextSummary;JsonSummary;Cobertura" -verbosity:Warning
sed -n '/^Summary/,/^$/p' coverage/Summary.txt

# The summary's percentage for a key, e.g. "linecoverage": 96.3.
coverage_of() {
  sed -n "s/.*\"$1\": *\([0-9.]*\).*/\1/p" coverage/Summary.json | head -n 1
}

failed=0
check() {
  local name=$1 actual=$2 minimum=$3
  if awk -v a="$actual" -v m="$minimum" 'BEGIN { exit !(a < m) }'; then
    echo ".NET $name coverage is $actual%, below the minimum of $minimum%." >&2
    failed=1
  fi
}
check line "$(coverage_of linecoverage)" "$MIN_LINE_COVERAGE"
check branch "$(coverage_of branchcoverage)" "$MIN_BRANCH_COVERAGE"
if [[ $failed -ne 0 ]]; then
  echo "See coverage/index.html for the uncovered lines." >&2
  exit 1
fi
echo ".NET coverage meets the minimums (lines $MIN_LINE_COVERAGE%, branches $MIN_BRANCH_COVERAGE%). Report: coverage/index.html"

[[ -d src/Innovark.Weather.UI/node_modules ]] || (cd src/Innovark.Weather.UI && bun install)
for package in tests/Innovark.Weather.UI/Innovark.Weather.Components.Tests tests/Innovark.Weather.UI/Innovark.Weather.Web.Tests; do
  (cd "$package" && bun run test:coverage)
done
