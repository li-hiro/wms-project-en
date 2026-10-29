#!/usr/bin/env bash
# Start the WMS backend (.NET 8 WebAPI)
set -euo pipefail

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root_dir/backend"

echo "[WMS] Starting backend service..."
dotnet restore
dotnet run --project src/Wms.WebApi/Wms.WebApi.csproj
