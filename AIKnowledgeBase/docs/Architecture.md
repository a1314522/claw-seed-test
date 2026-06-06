# AI 知识库 - 架构文档

## 项目概述

AI 知识库是一个基于 .NET 8 + Blazor WebAssembly 的企业级文档检索与问答系统，支持多格式文档解析、全文检索、分类管理、RBAC 权限控制和 LLM 智能问答。

---

## 技术栈

| 层级 | 技术 |
|------|------|
| 后端 | ASP.NET Core Web API 8.0 |
| 前端 | Blazor WebAssembly 8.0 |
| 数据库 | SQLite + EF Core 8.0 |
| 认证 | JWT + BCrypt |
| 文档解析 | DocumentFormat.OpenXml (Word), PdfPig (PDF), ClosedXML (Excel) |
| 检索 | 内存倒排索引 (TF-IDF + Jaccard) |
| LLM | 预留 Ollama 接口，默认 Mock 模式 |
| 测试 | xUnit + EF Core InMemory |

---

## 项目结构

```
AIKnowledgeBase/
├── AIKnowledgeBase.sln
├── src/
│   ├── AIKnowledgeBase.Core/           # 领域层
│   │   ├── Entities/                   # 实体模型
│   │   ├── DTOs/                       # 数据传输对象
│   │   ├── Enums/                      # 枚举
│   │   └── Interfaces/                 # 接口定义
│   ├── AIKnowledgeBase.Infrastructure/  # 基础设施层
│   │   ├── Data/                       # DbContext + 迁移
│   │   ├── Repositories/               # 通用仓储
│   │   ├── Services/                   # 业务服务
│   │   └── Identity/                   # JWT + 密码哈希 + 权限
│   ├── AIKnowledgeBase.API/            # API 层
│   │   ├── Controllers/                # RESTful API
│   │   ├── Middleware/                 # 异常处理中间件
│   │   └── Program.cs                  # 应用配置
│   └── AIKnowledgeBase.Web/            # Blazor 前端
│       ├── Pages/                        # 页面
│       ├── Shared/                       # 布局组件
│       ├── Services/                     # HTTP 客户端服务
│       └── wwwroot/                      # 静态资源
└── tests/
    └── AIKnowledgeBase.Tests/          # 单元测试
```

---

## 分层架构

### 1. Core（领域层）
- **职责**：定义实体、DTO、枚举、接口
- **原则**：不依赖任何外部框架，纯 C# 代码
- **关键设计**：
  - `BaseEntity` 提供通用字段（Id, CreatedAt, UpdatedAt）
  - `PermissionType` 枚举定义所有权限类型
  - `IRepository<T>` 通用仓储接口，支持 CRUD + 查询

### 2. Infrastructure（基础设施层）
- **职责**：数据库访问、文件解析、检索引擎、认证实现
- **依赖**：引用 Core 层
- **关键设计**：
  - `AppDbContext`：EF Core + SQLite，包含种子数据
  - `Repository<T>`：通用仓储实现
  - `SearchEngineService`：内存倒排索引，TF-IDF + Jaccard 混合评分
  - `AuthService`：JWT 生成 + 密码验证 + 权限提取
  - `DocumentParserService`：多格式文档解析（Word, PDF, Excel, TXT, Markdown）

### 3. API（应用层）
- **职责**：RESTful API 控制器、请求验证、响应封装
- **依赖**：引用 Core + Infrastructure
- **关键设计**：
  - 所有控制器返回 `ApiResponse<T>` 统一响应格式
  - JWT 认证 + 自定义 Authorization Policy
  - `ExceptionMiddleware` 全局异常处理
  - Swagger/OpenAPI 自动生成文档

### 4. Web（表示层）
- **职责**：Blazor WebAssembly 单页应用
- **依赖**：引用 Core（仅 DTO 类型）
- **关键设计**：
  - `AuthService` 管理 JWT Token 和用户状态
  - `HttpClient` 拦截器自动附加 Bearer Token
  - 导航菜单根据用户权限动态显示
  - 响应式布局适配桌面和移动端

---

## 数据库设计

### 实体关系图

```
Users (1) --- (N) UserRoles (N) --- (1) Roles (1) --- (N) RolePermissions (N) --- (1) Permissions

Users (1) --- (N) SearchHistory
Users (1) --- (N) UserCategoryAccess (N) --- (1) Categories (1) --- (N) Documents (1) --- (N) DocumentChunks
```

### 表说明

| 表 | 说明 | 关键字段 |
|----|------|----------|
| Users | 用户表 | Username, PasswordHash, IsAdmin, IsActive |
| Roles | 角色表 | Name, Description |
| Permissions | 权限表 | Name, Type, Description |
| RolePermissions | 角色权限关联 | RoleId, PermissionId |
| UserRoles | 用户角色关联 | UserId, RoleId |
| Categories | 分类表 | Name, Description, IsPublic |
| Documents | 文档表 | FileName, OriginalName, DocType, CategoryId, Status |
| DocumentChunks | 文档分块 | DocumentId, ChunkIndex, Text, Embedding |
| SearchHistory | 搜索历史 | UserId, Question, Answer, Sources |
| UserCategoryAccess | 用户分类访问权限 | UserId, CategoryId, CanRead, CanWrite, CanDelete |

---

## 权限模型（RBAC）

### 权限类型

| 权限 | 说明 | 默认角色 |
|------|------|----------|
| UserView | 查看用户列表 | 超级管理员 |
| UserCreate | 创建用户 | 超级管理员 |
| UserEdit | 编辑用户 | 超级管理员 |
| UserDelete | 删除用户 | 超级管理员 |
| RoleManage | 管理角色和权限 | 超级管理员 |
| CategoryView | 查看分类 | 所有角色 |
| CategoryCreate | 创建分类 | 编辑者, 管理员 |
| CategoryEdit | 编辑分类 | 编辑者, 管理员 |
| CategoryDelete | 删除分类 | 编辑者, 管理员 |
| DocumentView | 查看文档 | 所有角色 |
| DocumentUpload | 上传文档 | 编辑者, 管理员 |
| DocumentDelete | 删除文档 | 编辑者, 管理员 |
| DocumentManage | 管理所有文档 | 管理员 |
| SearchAll | 搜索所有分类 | 所有角色 |
| HistoryView | 查看搜索历史 | 所有角色 |
| HistoryClear | 清空搜索历史 | 所有角色 |
| SystemManage | 系统管理 | 超级管理员 |

### 预设角色

| 角色 | 权限 |
|------|------|
| 超级管理员 | 全部权限 |
| 编辑者 | CategoryCreate, CategoryEdit, CategoryDelete, DocumentUpload, DocumentDelete, DocumentManage |
| 普通用户 | CategoryView, DocumentView, SearchAll, HistoryView, HistoryClear |

---

## 检索引擎设计

### 混合评分算法

```
Score = TF-IDF * 0.7 + Jaccard * 10.0
```

- **TF-IDF**：词频-逆文档频率，衡量查询词在文档中的重要性
- **Jaccard**：集合相似度，衡量查询词与文档内容的重叠度
- **权重**：TF-IDF 侧重语义重要性，Jaccard 侧重词匹配精确度

### 中文分词

- 提取英文单词（3字符以上）
- 提取中文单字
- 提取中文 2-3 字短语（重叠滑动窗口）
- 停用词过滤

### 分类过滤

支持按 `categoryId` 限定搜索范围，实现分类级别的检索隔离。

---

## LLM 集成预留

### 接口设计

```csharp
public interface ILLMService
{
    Task<string> GenerateAnswerAsync(string question, List<string> contexts);
    bool IsAvailable { get; }
}
```

### 实现

| 实现 | 说明 | 配置 |
|------|------|------|
| `MockLLMService` | 测试模式，返回模拟回答 | 默认启用 |
| `OllamaLLMService` | 接入 Ollama 本地大模型 | 配置 `Ollama:Enabled=true` |

### 扩展方向

- 接入 OpenAI / Azure OpenAI API
- 接入国内大模型（通义千问、文心一言、Kimi）
- 接入本地部署模型（Ollama、LM Studio）

---

## API 设计

### 认证

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | /api/auth/login | 登录 |
| GET | /api/auth/me | 获取当前用户 |

### 用户管理

| 方法 | 路径 | 权限 |
|------|------|------|
| GET | /api/users | UserView |
| GET | /api/users/{id} | UserView |
| POST | /api/users | UserCreate |
| PUT | /api/users/{id} | UserEdit |
| DELETE | /api/users/{id} | UserDelete |

### 角色管理

| 方法 | 路径 | 权限 |
|------|------|------|
| GET | /api/roles | RoleManage |
| POST | /api/roles | RoleManage |
| DELETE | /api/roles/{id} | RoleManage |

### 分类管理

| 方法 | 路径 | 权限 |
|------|------|------|
| GET | /api/categories | CategoryView |
| POST | /api/categories | CategoryCreate |
| PUT | /api/categories/{id} | CategoryEdit |
| DELETE | /api/categories/{id} | CategoryDelete |

### 文档管理

| 方法 | 路径 | 权限 |
|------|------|------|
| GET | /api/documents | DocumentView |
| POST | /api/documents/upload | DocumentUpload |
| DELETE | /api/documents/{id} | DocumentDelete |
| PUT | /api/documents/{id}/category | DocumentManage |

### 知识库

| 方法 | 路径 | 权限 |
|------|------|------|
| POST | /api/knowledge/search | DocumentView |
| POST | /api/knowledge/ask | DocumentView |

### 搜索历史

| 方法 | 路径 | 权限 |
|------|------|------|
| GET | /api/history | HistoryView |
| DELETE | /api/history | HistoryClear |

---

## 可扩展性设计

### 1. 向量搜索预留

当前使用 TF-IDF + Jaccard 内存索引，预留向量搜索接口：
- 在 `DocumentChunk` 中保留 `Embedding` 字段（JSON 序列化）
- 可接入 Milvus / Qdrant / Chroma 等向量数据库
- 替换 `SearchEngineService` 实现即可，API 接口不变

### 2. LLM 切换

通过 `IHostService` 接口和 DI 配置，支持无缝切换：
- Mock → Ollama → OpenAI → Azure → 其他
- 无需修改 API 或前端代码

### 3. 文档解析扩展

`IDocumentParser` 接口支持新增解析器：
- PPT（.pptx）解析
- 图片 OCR（.jpg, .png）
- 压缩包解析（.zip 内嵌文档）

### 4. 存储层扩展

当前使用 SQLite，可通过修改连接字符串切换：
- SQL Server（企业部署）
- PostgreSQL（开源方案）
- MySQL（国内常用）

### 5. 多租户预留

数据库设计中已预留租户字段扩展点，可在 `User` 和 `Document` 中添加 `TenantId` 实现多租户。

### 6. 外部系统集成 API

所有 API 返回统一 JSON 格式，支持：
- 企业微信/钉钉/飞书机器人接入
- 第三方系统 API 调用
- SSO 单点登录（预留 OAuth2.0 接口）

---

## 部署说明

### 开发环境

```bash
# 1. 还原 NuGet 包
dotnet restore

# 2. 数据库迁移
dotnet ef migrations add InitialCreate --project src/AIKnowledgeBase.API
dotnet ef database update --project src/AIKnowledgeBase.API

# 3. 启动 API
dotnet run --project src/AIKnowledgeBase.API

# 4. 启动 Web（新开终端）
dotnet run --project src/AIKnowledgeBase.Web
```

### 生产环境

1. 修改 `appsettings.json` 中的 JWT Secret（至少 32 字节）
2. 配置 Ollama 地址（如需接入真实 LLM）
3. 使用 `dotnet publish` 打包
4. 部署到 IIS / Docker / K8s

### Docker 部署（预留）

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY ./publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "AIKnowledgeBase.API.dll"]
```

---

## 安全设计

1. **密码安全**：BCrypt 哈希，自动加盐
2. **JWT 安全**：HS256 签名，60 分钟过期
3. **权限控制**：RBAC + Policy-based Authorization
4. **SQL 注入**：EF Core 参数化查询
5. **文件上传**：限制类型（txt, md, pdf, docx, xlsx），限制大小
6. **CORS**：开发环境允许跨域，生产环境可配置白名单

---

## 测试覆盖

| 测试类 | 测试内容 |
|--------|----------|
| AuthServiceTests | 登录验证、密码哈希、JWT 生成 |
| SearchEngineTests | 索引构建、分类过滤、混合评分 |
| DocumentParserTests | 文本解析、格式检测 |

运行测试：
```bash
dotnet test
```

---

## 前端页面

| 页面 | 路径 | 说明 |
|------|------|------|
| 首页 | / | 概览和导航 |
| 智能搜索 | /search | 问答界面 + 历史记录 |
| 文档管理 | /documents | 上传、查看、删除文档 |
| 分类管理 | /categories | 创建、编辑、删除分类 |
| 搜索历史 | /history | 查看和清空历史 |
| 用户管理 | /users | 创建、删除用户（管理员） |
| 角色管理 | /roles | 创建、删除角色（管理员） |
| 登录 | /login | 登录界面 |

---

## 总结

本项目采用 Clean Architecture 分层设计，确保：
- **高内聚**：每层职责单一，依赖关系清晰
- **低耦合**：通过接口隔离，便于替换实现
- **可测试**：每层可独立单元测试
- **可扩展**：预留 LLM、向量搜索、多租户等扩展点
- **可维护**：代码结构清晰，文档完整
