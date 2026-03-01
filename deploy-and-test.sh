#!/bin/bash

echo "╔════════════════════════════════════════════════════════╗"
echo "║        🎬 VideoCrawler 完整部署和验证脚本              ║"
echo "╚════════════════════════════════════════════════════════╝"
echo ""

# 检查环境
echo "📋 检查运行环境..."
echo ""

# 检查 .NET
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    echo "✅ .NET SDK: $DOTNET_VERSION"
else
    echo "❌ .NET SDK 未安装"
    echo "   请安装 .NET 10 SDK: https://dotnet.microsoft.com/download"
    exit 1
fi

# 检查 Node.js
if command -v node &> /dev/null; then
    NODE_VERSION=$(node --version)
    echo "✅ Node.js: $NODE_VERSION"
else
    echo "⚠️  Node.js 未安装（前端可选）"
fi

echo ""
echo "═══════════════════════════════════════════════════════"
echo ""

# 1. 恢复 NuGet 包
echo "📦 步骤 1: 恢复 NuGet 包..."
dotnet restore src/VideoCrawler.Api/VideoCrawler.Api.csproj
if [ $? -ne 0 ]; then
    echo "❌ NuGet 包恢复失败"
    exit 1
fi
echo "✅ NuGet 包恢复完成"
echo ""

# 2. 构建后端
echo "🔨 步骤 2: 构建后端项目..."
dotnet build src/VideoCrawler.Api/VideoCrawler.Api.csproj --configuration Release --no-restore
if [ $? -ne 0 ]; then
    echo "❌ 后端构建失败"
    exit 1
fi
echo "✅ 后端构建成功"
echo ""

# 3. 构建测试项目
echo "🔨 步骤 3: 构建测试项目..."
dotnet build tests/VideoCrawler.Test/VideoCrawler.Test.csproj --configuration Release --no-restore
if [ $? -ne 0 ]; then
    echo "❌ 测试项目构建失败"
    exit 1
fi
echo "✅ 测试项目构建成功"
echo ""

# 4. 运行集成测试
echo "🧪 步骤 4: 运行集成测试..."
echo ""
dotnet run --project tests/VideoCrawler.Test/VideoCrawler.Test.csproj --configuration Release --no-build

TEST_RESULT=$?

echo ""
echo "═══════════════════════════════════════════════════════"
echo ""

if [ $TEST_RESULT -eq 0 ]; then
    echo "✅ 所有测试通过！"
else
    echo "❌ 部分测试失败"
    exit 1
fi

# 5. 构建前端（如果有 Node.js）
if command -v npm &> /dev/null; then
    echo ""
    echo "🔨 步骤 5: 构建前端项目..."
    cd src/VideoCrawler.Web
    
    if [ ! -d "node_modules" ]; then
        echo "📦 安装前端依赖..."
        npm install
    fi
    
    echo "🔨 构建前端..."
    npm run build
    
    if [ $? -eq 0 ]; then
        echo "✅ 前端构建成功"
    else
        echo "⚠️  前端构建失败（不影响后端功能）"
    fi
    
    cd ../../
else
    echo ""
    echo "⚠️  跳过前端构建（Node.js 未安装）"
fi

echo ""
echo "═══════════════════════════════════════════════════════"
echo ""
echo "🎉 部署和验证完成！"
echo ""
echo "📌 下一步操作："
echo ""
echo "   1. 启动后端 API:"
echo "      ./start-api.sh"
echo "      或：dotnet run --project src/VideoCrawler.Api"
echo ""
echo "   2. 启动前端（新终端）:"
echo "      ./start-web.sh"
echo "      或：cd src/VideoCrawler.Web && npm run dev"
echo ""
echo "   3. 访问应用:"
echo "      - Swagger API: http://localhost:5000"
echo "      - 前端页面：http://localhost:5173"
echo ""
echo "═══════════════════════════════════════════════════════"
