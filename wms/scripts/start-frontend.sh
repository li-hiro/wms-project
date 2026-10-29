#!/usr/bin/env bash
# 启动 WMS 前端（Vue 2 开发服务器）
set -euo pipefail

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root_dir/frontend"

echo "[WMS] 正在启动前端服务..."
if [ ! -d node_modules ]; then
  echo "[WMS] 未检测到 node_modules，正在安装依赖..."
  npm install
fi
npm run serve
