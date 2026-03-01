#!/bin/bash

echo "╔════════════════════════════════════════════════════════╗"
echo "║           📊 VideoCrawler 项目状态检查                ║"
echo "╚════════════════════════════════════════════════════════╝"
echo ""

cd "$(dirname "$0")"

# 检查项目结构
echo "📁 检查项目结构..."
echo ""

check_dir() {
    if [ -d "$1" ]; then
        echo "  ✅ $1"
    else
        echo "  ❌ $1 (缺失)"
    fi
}

check_file() {
    if [ -f "$1" ]; then
        echo "  ✅ $1"
    else
        echo "  ❌ $1 (缺失)"
    fi
}

echo "后端项目:"
check_dir "src/VideoCrawler.Domain"
check_dir "src/VideoCrawler.Application"
check_dir "src/VideoCrawler.Infrastructure"
check_dir "src/VideoCrawler.Api"

echo ""
echo "前端项目:"
check_dir "src/VideoCrawler.Web"
check_file "src/VideoCrawler.Web/package.json"
check_file "src/VideoCrawler.Web/src/main.ts"

echo ""
echo "测试项目:"
check_dir "tests/VideoCrawler.Test"
check_dir "tests/VideoCrawler.Domain.Tests"
check_dir "tests/VideoCrawler.Infrastructure.Tests"

echo ""
echo "配置文件:"
check_file "VideoCrawler.sln"
check_file "src/VideoCrawler.Api/appsettings.json"
check_file "src/VideoCrawler.Api/Program.cs"

echo ""
echo "启动脚本:"
check_file "start-api.sh"
check_file "start-web.sh"
check_file "deploy-and-test.sh"

echo ""
echo "═══════════════════════════════════════════════════════"
echo ""

# 统计代码量
echo "📊 代码统计:"
echo ""

count_lines() {
    find $1 -name "*.cs" -o -name "*.ts" -o -name "*.vue" 2>/dev/null | xargs wc -l 2>/dev/null | tail -1 | awk '{print $1}'
}

domain_lines=$(count_lines "src/VideoCrawler.Domain")
app_lines=$(count_lines "src/VideoCrawler.Application")
infra_lines=$(count_lines "src/VideoCrawler.Infrastructure")
api_lines=$(count_lines "src/VideoCrawler.Api")
web_lines=$(count_lines "src/VideoCrawler.Web")
test_lines=$(count_lines "tests")

total_lines=$((domain_lines + app_lines + infra_lines + api_lines + web_lines + test_lines))

echo "  Domain 层：     $domain_lines 行"
echo "  Application 层：$app_lines 行"
echo "  Infrastructure: $infra_lines 行"
echo "  API 层：       $api_lines 行"
echo "  Web 前端：     $web_lines 行"
echo "  Tests 测试：   $test_lines 行"
echo "  ─────────────────────────────"
echo "  总计：        $total_lines 行"

echo ""
echo "═══════════════════════════════════════════════════════"
echo ""

# 检查 Git 状态
echo "🔀 Git 状态:"
echo ""

if [ -d ".git" ]; then
    branch=$(git branch --show-current 2>/dev/null || echo "unknown")
    commits=$(git rev-list --count HEAD 2>/dev/null || echo "unknown")
    last_commit=$(git log -1 --format="%s" 2>/dev/null || echo "unknown")
    
    echo "  分支：$branch"
    echo "  提交数：$commits"
    echo "  最后提交：$last_commit"
    
    # 检查是否有未提交的更改
    changes=$(git status --porcelain 2>/dev/null | wc -l | tr -d ' ')
    if [ "$changes" -gt 0 ]; then
        echo "  ⚠️  有 $changes 个未提交的更改"
    else
        echo "  ✅ 工作区干净"
    fi
else
    echo "  ⚠️  不是 Git 仓库"
fi

echo ""
echo "═══════════════════════════════════════════════════════"
echo ""

# 功能清单
echo "✅ 功能清单:"
echo ""

features=(
    "DDD 架构（Domain/Application/Infrastructure）"
    "HuaduZY 专用解析器（40 条/页）"
    "分页爬取支持（最多 10 页）"
    "M3U8 地址提取"
    "SQLite 数据库持久化"
    "Repository 模式"
    "RESTful API"
    "Swagger 文档"
    "Vue 3 前端"
    "Element Plus UI"
    "Pinia 状态管理"
    "Polly 重试机制"
    "集成测试"
)

for feature in "${features[@]}"; do
    echo "  ✅ $feature"
done

echo ""
echo "═══════════════════════════════════════════════════════"
echo ""
echo "🚀 快速启动:"
echo ""
echo "  1. 运行完整测试:"
echo "     ./deploy-and-test.sh"
echo ""
echo "  2. 启动后端:"
echo "     ./start-api.sh"
echo ""
echo "  3. 启动前端（新终端）:"
echo "     ./start-web.sh"
echo ""
echo "═══════════════════════════════════════════════════════"
