# Start the WMS backend (.NET 8 WebAPI)
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$backend = Join-Path $root 'backend'

Write-Host '[WMS] Starting backend service...' -ForegroundColor Cyan
Push-Location $backend
try {
    dotnet restore
    dotnet run --project src/Wms.WebApi/Wms.WebApi.csproj
}
finally {
    Pop-Location
}
