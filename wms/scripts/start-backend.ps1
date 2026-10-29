# 启动 WMS 后端（.NET 8 WebAPI）
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$backend = Join-Path $root 'backend'

Write-Host '[WMS] 正在启动后端服务...' -ForegroundColor Cyan
Push-Location $backend
try {
    dotnet restore
    dotnet run --project src/Wms.WebApi/Wms.WebApi.csproj
}
finally {
    Pop-Location
}
