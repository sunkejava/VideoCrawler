#!/bin/bash

echo "========================================"
echo "🎨 VideoCrawler Web 前端启动脚本"
echo "========================================"

# 进入前端目录
cd "$(dirname "$0")/src/VideoCrawler.Web"

# 检查 Node.js
echo ""
echo "📦 检查 Node.js 版本..."
node --version
npm --version

# 检查 node_modules
if [ ! -d "node_modules" ]; then
    echo ""
    echo "📦 首次运行，安装依赖..."
    npm install
fi

# 启动开发服务器
echo ""
echo "🚀 启动前端开发服务器..."
echo "访问地址：http://localhost:5173"
echo ""

npm run dev
