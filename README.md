# VideoCrawler - 视频爬取系统

基于 .NET 10 + Vue 3 的 DDD 架构视频爬取系统，支持多智能体任务分配和自动化缓存。

## 🏗️ 技术栈

### 后端
- **.NET 10** - 最新 LTS 版本
- **Entity Framework Core** - ORM 框架
- **SQLite** - 轻量级数据库
- **MediatR** - CQRS 模式支持
- **FluentValidation** - 验证框架

### 前端
- **Vue 3** - 渐进式框架
- **TypeScript** - 类型安全
- **Vite** - 快速构建工具
- **Element Plus** - UI 组件库
- **Pinia** - 状态管理
- **Vue Router** - 路由管理

### 架构模式
- **DDD (领域驱动设计)** - 清晰的业务边界
- **Repository Pattern** - 数据访问抽象
- **CQRS** - 命令查询职责分离

## 📁 项目结构

```
VideoCrawler/
├── src/
│   ├── VideoCrawler.Domain/          # 领域层 - 实体、值对象、接口
│   ├── VideoCrawler.Application/     # 应用层 - DTOs、服务、处理器
│   ├── VideoCrawler.Infrastructure/  # 基础设施层 - 仓储实现、外部服务
│   ├── VideoCrawler.Api/             # API 层 - 控制器、中间件
│   └── VideoCrawler.Web/             # 前端 - Vue 3 应用
├── tests/
│   ├── VideoCrawler.Domain.Tests/
│   ├── VideoCrawler.Application.Tests/
│   ├── VideoCrawler.Infrastructure.Tests/
│   └── VideoCrawler.Api.Tests/
└── VideoCrawler.sln
```

## 🚀 快速开始

### 1. 后端启动

```bash
cd src/VideoCrawler.Api
dotnet restore
dotnet run
```

API 将运行在 `http://localhost:5000`

### 2. 前端启动

```bash
cd src/VideoCrawler.Web
npm install
npm run dev
```

前端将运行在 `http://localhost:5173`

### 3. 运行测试

```bash
dotnet test
```

## 📖 API 文档

启动后端后访问：`http://localhost:5000/swagger`

### 主要接口

#### 视频管理
- `GET /api/videos` - 获取视频列表
- `GET /api/videos/{id}` - 获取视频详情
- `GET /api/videos/category/{category}` - 按分类获取
- `GET /api/videos/search?keyword=xxx` - 搜索视频
- `GET /api/videos/cached` - 获取已缓存视频

#### 爬取任务
- `POST /api/crawlerTasks` - 创建爬取任务
- `GET /api/crawlerTasks` - 获取任务列表
- `GET /api/crawlerTasks/{id}` - 获取任务详情
- `POST /api/crawlerTasks/{id}/start` - 启动任务
- `POST /api/crawlerTasks/{id}/cancel` - 取消任务
- `POST /api/crawlerTasks/{id}/retry` - 重试任务

#### 工作节点
- `GET /api/crawlerTasks/workers` - 获取可用工作节点

## 🎯 核心功能

### 1. 智能爬取
- 支持增量/全量爬取模式
- 自动检测已缓存数据，避免重复爬取
- 多选择器备选方案，应对页面结构变化

### 2. 视频缓存
- 自动缓存 M3U8 视频流
- 自动下载封面图片
- 可配置缓存过期策略

### 3. 多智能体任务分配
- 工作节点自动注册/注销
- 任务智能分配给空闲节点
- 节点状态实时监控

### 4. 异常处理
- 网络超时自动重试（指数退避）
- 元素定位失败自动切换选择器
- API 故障自动切换备用源

## ⚙️ 配置说明

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=vodcrawler.db"
  },
  "Cache": {
    "Path": "./cache",
    "DefaultExpiration": "30",
    "MaxCacheSizeBytes": "10737418240"
  }
}
```

## 📝 开发计划

- [x] DDD 架构搭建
- [x] 领域模型设计
- [x] 基础仓储实现
- [x] API 控制器
- [x] 前端页面
- [x] 网站解析器（HtmlAgilityPack）- 支持苹果 CMS 和 JSON-LD
- [x] HTTP 爬虫服务（Polly 重试）
- [x] 网站结构分析工具
- [ ] M3U8 完整下载器（TS 分片合并）
- [ ] 工作节点分布式支持
- [ ] 定时任务调度
- [ ] 用户认证授权

## 🛠️ 调试工具

### 分析网站结构
```bash
GET /api/debug/analyze?url=https://b.huaduzy.cc/vodshow/tangxinVlog-----------.html
```

### 测试爬取列表
```bash
GET /api/debug/test-list?url=https://b.huaduzy.cc/vodshow/tangxinVlog-----------.html&maxCount=10
```

### 测试爬取详情
```bash
GET /api/debug/test-detail?url=https://b.huaduzy.cc/voddetail/123.html
```

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

MIT License
