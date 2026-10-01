#!/usr/bin/env bash
# Copilot postToolUse hook: after Copilot edits or creates a file, format the C# files it changed with
# the repo's .editorconfig rules (dotnet format whitespace, the same formatter VS Code uses for C#).
set -uo pipefail

input=$(cat)

# Only file-changing tools (Copilot's "edit"/"create", plus other names agents use for writes).
tool=$(jq -r '.toolName // empty' <<<"$input" 2>/dev/null | tr '[:upper:]' '[:lower:]')
case "$tool" in
  *edit*|*create*|*write*|*replace*|*patch*) ;;
  *) exit 0 ;;
esac

cd "$(git rev-parse --show-toplevel 2>/dev/null)" || exit 0

# toolArgs may be an object or a JSON string, and the path key isn't documented, so try common names.
path=$(jq -r '(.toolArgs | if type == "string" then (fromjson? // {}) else . end)
              | (.path // .filePath // .file_path // empty)' <<<"$input" 2>/dev/null)

files=()
if [[ "$path" == *.cs ]]; then
  files+=("$path")
else
  # No usable path: format every changed or new C# file instead.
  while IFS= read -r f; do
    files+=("$f")
  done < <(git status --porcelain --untracked-files=all -- '*.cs' | sed -E 's/^.. //; s/.* -> //')
fi

[[ ${#files[@]} -gt 0 ]] || exit 0
dotnet format whitespace --folder --include "${files[@]}" >/dev/null 2>&1 || true

# dotnet format leaves blank lines between using directives in place, although .editorconfig sets
# dotnet_separate_import_directive_groups = false, so join them here. Only unindented lines match, so
# `using var` statements inside methods are left alone.
perl -0pi -e 's/^((?:global )?using [^;\n(]+;\n)\n+(?=(?:global )?using [^;\n(]+;)/$1/mg' "${files[@]}" 2>/dev/null || true
exit 0
