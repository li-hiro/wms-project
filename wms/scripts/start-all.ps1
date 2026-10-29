# 一键启动 WMS 前后端（分别打开独立窗口）
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

Write-Host '[WMS] 启动后端（http://localhost:5080）...' -ForegroundColor Cyan
Start-Process -FilePath 'powershell' -ArgumentList @(
    '-NoExit',
    '-ExecutionPolicy', 'Bypass',
    '-File', (Join-Path $root 'scripts\start-backend.ps1')
)

Start-Sleep -Seconds 3

Write-Host '[WMS] 启动前端（http://localhost:8080）...' -ForegroundColor Cyan
Start-Process -FilePath 'powershell' -ArgumentList @(
    '-NoExit',
    '-ExecutionPolicy', 'Bypass',
    '-File', (Join-Path $root 'scripts\start-frontend.ps1')
)

Write-Host ''
Write-Host '后端 Swagger: http://localhost:5080/swagger' -ForegroundColor Green
Write-Host '前端地址   : http://localhost:8080' -ForegroundColor Green
Write-Host '演示账号   : admin / admin123' -ForegroundColor Green
