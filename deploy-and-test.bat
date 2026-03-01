@echo off
chcp 65001 >nul

echo ╔════════════════════════════════════════════════════════╗
echo ║        VideoCrawler 完整部署和验证脚本 (Windows)       ║
echo ╚════════════════════════════════════════════════════════╝
echo.

echo 📋 检查运行环境...
echo.

REM 检查 .NET
where dotnet >nul 2>&1
if %errorlevel% equ 0 (
    for /f "tokens=*" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
    echo ✅ .NET SDK: %DOTNET_VERSION%
) else (
    echo ❌ .NET SDK 未安装
    echo    请安装 .NET 10 SDK: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

REM 检查 Node.js
where node >nul 2>&1
if %errorlevel% equ 0 (
    for /f "tokens=*" %%i in ('node --version') do set NODE_VERSION=%%i
    echo ✅ Node.js: %NODE_VERSION%
) else (
    echo ⚠️  Node.js 未安装（前端可选）
)

echo.
echo ═══════════════════════════════════════════════════════
echo.

REM 1. 恢复 NuGet 包
echo 📦 步骤 1: 恢复 NuGet 包...
dotnet restore src\VideoCrawler.Api\VideoCrawler.Api.csproj
if %errorlevel% neq 0 (
    echo ❌ NuGet 包恢复失败
    pause
    exit /b 1
)
echo ✅ NuGet 包恢复完成
echo.

REM 2. 构建后端
echo 🔨 步骤 2: 构建后端项目...
dotnet build src\VideoCrawler.Api\VideoCrawler.Api.csproj --configuration Release --no-restore
if %errorlevel% neq 0 (
    echo ❌ 后端构建失败
    pause
    exit /b 1
)
echo ✅ 后端构建成功
echo.

REM 3. 构建测试项目
echo 🔨 步骤 3: 构建测试项目...
dotnet build tests\VideoCrawler.Test\VideoCrawler.Test.csproj --configuration Release --no-restore
if %errorlevel% neq 0 (
    echo ❌ 测试项目构建失败
    pause
    exit /b 1
)
echo ✅ 测试项目构建成功
echo.

REM 4. 运行集成测试
echo 🧪 步骤 4: 运行集成测试...
echo.
dotnet run --project tests\VideoCrawler.Test\VideoCrawler.Test.csproj --configuration Release --no-build

set TEST_RESULT=%errorlevel%

echo.
echo ═══════════════════════════════════════════════════════
echo.

if %TEST_RESULT% equ 0 (
    echo ✅ 所有测试通过！
) else (
    echo ❌ 部分测试失败
    pause
    exit /b 1
)

echo.
echo ═══════════════════════════════════════════════════════
echo.
echo 🎉 部署和验证完成！
echo.
echo 📌 下一步操作：
echo.
echo    1. 启动后端 API:
echo.      start-api.bat
echo.      或：dotnet run --project src\VideoCrawler.Api
echo.
echo    2. 启动前端（新终端）:
echo.      start-web.bat
echo.      或：cd src\VideoCrawler.Web ^&^& npm run dev
echo.
echo    3. 访问应用:
echo.      - Swagger API: http://localhost:5000
echo.      - 前端页面：http://localhost:5173
echo.
echo ═══════════════════════════════════════════════════════

pause
