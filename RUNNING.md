# 🚀 VideoCrawler 运行指南

## 📋 前置要求

### 必需
- **.NET 10 SDK** - https://dotnet.microsoft.com/download
- **Git** - https://git-scm.com/downloads

### 可选（前端开发）
- **Node.js 18+** - https://nodejs.org/
- **npm** - 随 Node.js 一起安装

---

## 🔧 快速开始

### 方式一：一键部署和测试（推荐）

**Linux/macOS:**
```bash
./deploy-and-test.sh
```

**Windows:**
```bash
deploy-and-test.bat
```

这个脚本会自动：
1. ✅ 检查运行环境
2. ✅ 恢复 NuGet 包
3. ✅ 构建后端项目
4. ✅ 构建测试项目
5. ✅ 运行集成测试
6. ✅ 构建前端项目（如果有 Node.js）

### 方式二：分步执行

#### 1. 检查项目状态
```bash
./check-status.sh
```

#### 2. 运行测试
```bash
dotnet run --project tests/VideoCrawler.Test
```

#### 3. 启动后端
```bash
./start-api.sh
# 或 Windows: start-api.bat
```

#### 4. 启动前端（新终端）
```bash
./start-web.sh
# 或 Windows: start-web.bat
```

---

## 📊 验证功能

### 1. 访问 Swagger API 文档
```
http://localhost:5000
```

### 2. 访问前端页面
```
http://localhost:5173
```

### 3. 测试 API 接口

**获取视频列表:**
```bash
curl http://localhost:5000/api/videos?page=1&pageSize=24
```

**创建爬取任务:**
```bash
curl -X POST http://localhost:5000/api/crawlerTasks \
  -H "Content-Type: application/json" \
  -d '{
    "targetUrl": "https://b.huaduzy.cc/vodshow/tangxinVlog-----------.html",
    "taskType": "Incremental"
  }'
```

**分析网站结构:**
```bash
curl "http://localhost:5000/api/debug/analyze?url=https://b.huaduzy.cc/vodshow/tangxinVlog-----------.html"
```

---

## 🧪 测试说明

### 集成测试包含：

1. **数据库初始化测试** - 验证 SQLite 数据库创建
2. **网站结构分析** - 分析目标网站 HTML 结构
3. **视频列表爬取** - 爬取第 1 页（40 条视频）
4. **视频详情爬取** - 爬取视频详细信息
5. **数据持久化** - 验证数据库读写

### 运行特定测试：
```bash
# 完整集成测试
dotnet run --project tests/VideoCrawler.Test

# 单元测试
dotnet test tests/VideoCrawler.Domain.Tests
dotnet test tests/VideoCrawler.Infrastructure.Tests
```

---

## 📁 项目结构

```
VideoCrawler/
├── src/
│   ├── VideoCrawler.Domain/          # 领域层（实体、接口）
│   ├── VideoCrawler.Application/     # 应用层（DTOs、服务）
│   ├── VideoCrawler.Infrastructure/  # 基础设施（仓储、爬虫）
│   ├── VideoCrawler.Api/             # API 层（控制器）
│   └── VideoCrawler.Web/             # 前端（Vue 3）
├── tests/
│   ├── VideoCrawler.Test/            # 集成测试
│   ├── VideoCrawler.Domain.Tests/    # Domain 单元测试
│   └── VideoCrawler.Infrastructure.Tests/  # 基础设施测试
├── start-api.sh / start-api.bat      # 启动后端
├── start-web.sh / start-web.bat      # 启动前端
├── deploy-and-test.sh                # 部署和测试
└── check-status.sh                   # 状态检查
```

---

## 🔍 故障排查

### 后端启动失败

**错误：端口被占用**
```bash
# 查看占用端口的进程
lsof -i :5000
# 或
netstat -ano | findstr :5000

# 杀死进程或修改端口
```

**错误：数据库锁定**
```bash
# 删除数据库文件
rm src/VideoCrawler.Api/vodcrawler.db
# 重新启动
```

### 前端启动失败

**错误：依赖未安装**
```bash
cd src/VideoCrawler.Web
npm install
```

**错误：端口被占用**
```bash
# 修改 vite.config.ts 中的端口
# 或使用其他端口
npm run dev -- --port 5174
```

### 爬取失败

**错误：网站无法访问**
- 检查网络连接
- 检查目标网站是否可访问
- 可能需要使用代理

**错误：解析失败**
- 查看日志输出
- 检查网站结构是否变化
- 更新解析器选择器

---

## 📝 常用命令

### 构建
```bash
# 构建所有项目
dotnet build

# 发布
dotnet publish -c Release -o ./publish
```

### 测试
```bash
# 运行所有测试
dotnet test

# 运行特定测试项目
dotnet test tests/VideoCrawler.Test
```

### 运行
```bash
# 运行后端
dotnet run --project src/VideoCrawler.Api

# 运行测试
dotnet run --project tests/VideoCrawler.Test
```

---

## 🎯 下一步

1. **运行完整测试** - 验证所有功能正常
   ```bash
   ./deploy-and-test.sh
   ```

2. **启动后端** - 开始提供 API 服务
   ```bash
   ./start-api.sh
   ```

3. **访问 Swagger** - 查看和测试 API
   ```
   http://localhost:5000
   ```

4. **创建爬取任务** - 开始爬取视频
   ```bash
   curl -X POST http://localhost:5000/api/crawlerTasks \
     -H "Content-Type: application/json" \
     -d '{"targetUrl":"https://b.huaduzy.cc/vodshow/tangxinVlog-----------.html"}'
   ```

---

## 📞 获取帮助

如果遇到问题：
1. 查看控制台错误信息
2. 检查日志文件
3. 运行 `./check-status.sh` 查看项目状态
4. 查看 GitHub Issues

---

**祝使用愉快！** 🎉
