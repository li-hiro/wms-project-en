# Start the WMS backend and frontend (each in its own window)
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

Write-Host '[WMS] Starting backend (http://localhost:5080)...' -ForegroundColor Cyan
Start-Process -FilePath 'powershell' -ArgumentList @(
    '-NoExit',
    '-ExecutionPolicy', 'Bypass',
    '-File', (Join-Path $root 'scripts\start-backend.ps1')
)

Start-Sleep -Seconds 3

Write-Host '[WMS] Starting frontend (http://localhost:8080)...' -ForegroundColor Cyan
Start-Process -FilePath 'powershell' -ArgumentList @(
    '-NoExit',
    '-ExecutionPolicy', 'Bypass',
    '-File', (Join-Path $root 'scripts\start-frontend.ps1')
)

Write-Host ''
Write-Host 'Backend Swagger : http://localhost:5080/swagger' -ForegroundColor Green
Write-Host 'Frontend URL    : http://localhost:8080' -ForegroundColor Green
Write-Host 'Demo account    : admin / admin123' -ForegroundColor Green
