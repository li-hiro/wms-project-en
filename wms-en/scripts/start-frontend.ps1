# Start the WMS frontend (Vue 2 dev server)
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$frontend = Join-Path $root 'frontend'

Write-Host '[WMS] Starting frontend service...' -ForegroundColor Cyan
Push-Location $frontend
try {
    if (-not (Test-Path 'node_modules')) {
        Write-Host '[WMS] node_modules not found, installing dependencies...' -ForegroundColor Yellow
        npm install
    }
    npm run serve
}
finally {
    Pop-Location
}
