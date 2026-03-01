#!/bin/bash

echo "========================================"
echo "🎬 VideoCrawler 启动脚本"
echo "========================================"

# 进入项目目录
cd "$(dirname "$0")"

# 检查 .NET 版本
echo ""
echo "📦 检查 .NET 版本..."
dotnet --version

# 恢复 NuGet 包
echo ""
echo "📦 恢复 NuGet 包..."
dotnet restore src/VideoCrawler.Api/VideoCrawler.Api.csproj

# 构建项目
echo ""
echo "🔨 构建项目..."
dotnet build src/VideoCrawler.Api/VideoCrawler.Api.csproj --configuration Release

# 运行 API
echo ""
echo "🚀 启动 VideoCrawler API..."
dotnet run --project src/VideoCrawler.Api/VideoCrawler.Api.csproj --configuration Release
