# 启动 WMS 前端（Vue 2 开发服务器）
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$frontend = Join-Path $root 'frontend'

Write-Host '[WMS] 正在启动前端服务...' -ForegroundColor Cyan
Push-Location $frontend
try {
    if (-not (Test-Path 'node_modules')) {
        Write-Host '[WMS] 未检测到 node_modules，正在安装依赖...' -ForegroundColor Yellow
        npm install
    }
    npm run serve
}
finally {
    Pop-Location
}
