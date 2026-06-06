# AI Knowledge Base

企业级 AI 知识库系统，支持文档上传、智能检索（RAG）、权限管理。

## 快速开始

```bash
# 还原依赖
dotnet restore

# 运行 API（端口 5000）
cd src/AIKnowledgeBase.API
dotnet run

# 运行 Web 前端（需要单独终端）
cd src/AIKnowledgeBase.Web
dotnet run
```

API 默认地址: `http://localhost:5000`  
Swagger 文档: `http://localhost:5000/swagger`

## 默认账户
- 用户名: `admin`
- 密码: `admin123`

## 项目结构

| 项目 | 说明 |
|------|------|
| `AIKnowledgeBase.Core` | 实体、DTO、接口 |
| `AIKnowledgeBase.Infrastructure` | 数据访问、身份认证、服务实现 |
| `AIKnowledgeBase.API` | RESTful API |
| `AIKnowledgeBase.Web` | Blazor WebAssembly 前端 |
| `AIKnowledgeBase.Tests` | 单元测试和集成测试 |

## 技术栈

- .NET 8 + ASP.NET Core
- EF Core + SQLite
- JWT Bearer 认证
- TF-IDF + Jaccard 检索
- Ollama / Mock LLM
- Blazor WASM + Bootstrap 5

## 配置

编辑 `src/AIKnowledgeBase.API/appsettings.json`:

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| `Jwt:Secret` | JWT 签名密钥 | 自动生成 |
| `Ollama:Enabled` | 启用 Ollama | `false` |
| `Ollama:BaseUrl` | Ollama 地址 | `http://localhost:11434` |
| `Ollama:Model` | 使用模型 | `qwen2.5` |

## 部署

### 单服务部署（API + 前端一体）
```bash
cd src/AIKnowledgeBase.API
dotnet publish -c Release -o ./publish
```

### 前端单独部署
```bash
cd src/AIKnowledgeBase.Web
dotnet publish -c Release -o ./publish
# 将 publish/wwwroot 部署到静态服务器
```

## 测试

```bash
dotnet test
```

## 文档

详见 `docs/Architecture.md`
