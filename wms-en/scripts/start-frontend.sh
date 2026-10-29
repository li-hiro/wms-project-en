#!/usr/bin/env bash
# Start the WMS frontend (Vue 2 dev server)
set -euo pipefail

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root_dir/frontend"

echo "[WMS] Starting frontend service..."
if [ ! -d node_modules ]; then
  echo "[WMS] node_modules not found, installing dependencies..."
  npm install
fi
npm run serve
