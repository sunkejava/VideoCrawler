@echo off
echo ========================================
echo VideoCrawler 启动脚本 (Windows)
echo ========================================

cd /d "%~dp0"

echo.
echo 检查 .NET 版本...
dotnet --version

echo.
echo 恢复 NuGet 包...
dotnet restore src\VideoCrawler.Api\VideoCrawler.Api.csproj

echo.
echo 构建项目...
dotnet build src\VideoCrawler.Api\VideoCrawler.Api.csproj --configuration Release

echo.
echo 启动 VideoCrawler API...
dotnet run --project src\VideoCrawler.Api\VideoCrawler.Api.csproj --configuration Release

pause
