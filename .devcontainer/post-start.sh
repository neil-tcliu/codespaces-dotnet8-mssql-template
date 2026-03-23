#!/usr/bin/env bash

set -euo pipefail

workspace_folder="/workspaces/codespaces-blank"
sql_root="$workspace_folder/src/features/mssql/.sqlserver"

for dir in "$sql_root" "$sql_root/data" "$sql_root/log" "$sql_root/secrets"; do
  mkdir -p "$dir"
  chmod 0777 "$dir" || true
done

if ! dotnet --list-runtimes | grep -q '^Microsoft.NETCore.App 8\.'; then
  echo "WARNING: .NET 8 runtime was not found in the current environment."
  echo "This repository expects commands to run inside the dev container defined in .devcontainer/devcontainer.json."
  echo "If you copied this repository into an existing Codespace or container, run 'Dev Containers: Rebuild and Reopen in Container'."
fi

if ! getent hosts sqlserver >/dev/null 2>&1; then
  echo "WARNING: Hostname 'sqlserver' is not resolvable from the current shell."
  echo "The SQL integration tests require the workspace container and sqlserver sidecar to share the compose network."
fi