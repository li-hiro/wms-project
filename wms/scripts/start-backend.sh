#!/usr/bin/env bash
# 启动 WMS 后端（.NET 8 WebAPI）
set -euo pipefail

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root_dir/backend"

echo "[WMS] 正在启动后端服务..."
dotnet restore
dotnet run --project src/Wms.WebApi/Wms.WebApi.csproj
