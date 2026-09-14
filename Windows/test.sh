#!/bin/zsh
set -euo pipefail
PROJECT_DIR="${0:A:h}"
if [[ -n "${DOTNET_ROOT:-}" && -x "$DOTNET_ROOT/dotnet" ]]; then
  DOTNET_BIN="$DOTNET_ROOT/dotnet"
elif command -v dotnet >/dev/null 2>&1; then
  DOTNET_BIN="$(command -v dotnet)"
elif [[ -x "$PROJECT_DIR/work/tools/dotnet/dotnet" ]]; then
  export DOTNET_ROOT="$PROJECT_DIR/work/tools/dotnet"
  DOTNET_BIN="$DOTNET_ROOT/dotnet"
else
  print -u2 "Install the .NET 10 SDK or set DOTNET_ROOT to an existing SDK directory."
  exit 1
fi
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-$PROJECT_DIR/work/dotnet-home}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-$PROJECT_DIR/work/nuget-packages}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
"$DOTNET_BIN" run --project "$PROJECT_DIR/tests/CoreTests.csproj" -c Release
