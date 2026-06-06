# OpenClaw 企业级 PaaS 平台架构设计

## 1. 架构概述

### 1.1 设计目标
- 统一托管 AIKnowledgeBase、AssetManagementSystem 及未来所有业务系统
- 提供标准化部署、监控、运维能力
- 支持水平扩展（未来可迁移到 Kubernetes）
- 单节点可运行，最小资源占用

### 1.2 核心原则
- **渐进式增强**：从 Docker Compose 开始，未来迁移到 K8s
- **服务自治**：每个服务独立部署、独立伸缩
- **统一入口**：Nginx 作为 API Gateway，统一认证和路由
- **事件驱动**：Redis Stream 作为轻量级消息总线

---

## 2. 技术栈选型

| 层级 | 组件 | 技术选型 | 说明 |
|------|------|---------|------|
| **接入层** | API Gateway | Nginx + Lua | 已部署，扩展路由规则 |
| **网关层** | 负载均衡 | Nginx upstream | 反向代理到各服务 |
| **服务层** | 知识库服务 | .NET 8 API | 现有系统，容器化 |
| | 资产服务 | .NET 8 API | 新增 Web API 版本 |
| | 认证服务 | .NET 8 Identity | 统一 LDAP/AD 认证 |
| **消息层** | 消息队列 | Redis Stream | 轻量级，无需额外部署 |
| **数据层** | 主数据库 | PostgreSQL 15 | 统一数据库 |
| | 缓存 | Redis 7 | 会话、缓存、消息队列 |
| | 对象存储 | MinIO | 文件、图片存储 |
| | 搜索 | MeiliSearch | 全文检索（轻量替代 ES）|
| **运维层** | 监控 | Prometheus + Grafana | 指标采集和可视化 |
| | 日志 | Loki + Promtail | 日志聚合和查询 |
| | 告警 | Alertmanager | 告警路由和通知 |
| | 追踪 | Jaeger | 分布式链路追踪 |
| **平台层** | 容器编排 | Docker Compose | 当前方案 |
| | 镜像仓库 | Harbor / 阿里云 ACR | 镜像管理 |
| | CI/CD | GitHub Actions | 自动化构建和部署 |
| | GitOps | ArgoCD (未来) | 声明式部署 |

---

## 3. 服务架构图

```
┌─────────────────────────────────────────────────────────┐
│                      用户层                               │
│  (浏览器 / 移动端 / 桌面端 / 第三方系统)                    │
└─────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────┐
│                    Nginx API Gateway                     │
│  ┌──────────────┬──────────────┬──────────────┐         │
│  │  /api/v1/kb  │  /api/v1/am  │  /api/v1/auth│         │
│  │  知识库路由   │  资产路由     │  认证路由     │         │
│  └──────────────┴──────────────┴──────────────┘         │
│  • 统一认证检查 (JWT/LDAP)                                 │
│  • 速率限制                                                │
│  • 请求日志                                                │
│  • 负载均衡                                                │
└─────────────────────────────────────────────────────────┘
                           │
           ┌───────────────┼───────────────┐
           ▼               ▼               ▼
┌────────────────┐ ┌────────────────┐ ┌────────────────┐
│  AIKnowledgeBase │ │  AssetManagement│ │   AuthService   │
│     Service      │ │     Service      │ │    Service      │
│                  │ │                  │ │                 │
│  • 文档管理       │ │  • 资产CRUD       │ │  • LDAP/AD认证   │
│  • 智能搜索       │ │  • 生命周期       │ │  • JWT签发       │
│  • 知识图谱       │ │  • 报表导出       │ │  • 权限管理       │
│  • 分类管理       │ │  • 流程审批       │ │  • 用户管理       │
└────────────────┘ └────────────────┘ └────────────────┘
           │               │               │
           └───────────────┼───────────────┘
                           ▼
┌─────────────────────────────────────────────────────────┐
│                    基础设施层                             │
│  ┌─────────────────────────────────────────────────┐   │
│  │  PostgreSQL 15 (主数据库)                         │   │
│  │  • 知识库数据                                      │   │
│  │  • 资产数据                                        │   │
│  │  • 用户数据                                        │   │
│  │  • 审计日志                                        │   │
│  └─────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────┐   │
│  │  Redis 7 (缓存 + 消息队列)                         │   │
│  │  • Session 缓存                                    │   │
│  │  • API 限流计数器                                   │   │
│  │  • 事件总线 (Stream)                               │   │
│  │  • 分布式锁                                        │   │
│  └─────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────┐   │
│  │  MinIO (对象存储)                                  │   │
│  │  • 文档附件                                        │   │
│  │  • 图片资源                                        │   │
│  │  • 备份文件                                        │   │
│  └─────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────┐   │
│  │  MeiliSearch (全文搜索)                            │   │
│  │  • 文档内容索引                                     │   │
│  │  • 资产信息检索                                     │   │
│  └─────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────┐
│                    可观测性层                             │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐  │
│  │ Prometheus   │ │    Loki      │ │   Jaeger     │  │
│  │  (指标采集)   │ │  (日志聚合)    │ │  (链路追踪)   │  │
│  └──────────────┘ └──────────────┘ └──────────────┘  │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐  │
│  │   Grafana    │ │ Alertmanager │ │   Promtail   │  │
│  │  (可视化)     │ │  (告警管理)    │ │  (日志采集)   │  │
│  └──────────────┘ └──────────────┘ └──────────────┘  │
└─────────────────────────────────────────────────────────┘
```

---

## 4. 服务详细设计

### 4.1 AIKnowledgeBase Service (知识库服务)

**职责**：
- 文档的 CRUD 操作
- 全文检索（基于 MeiliSearch）
- 分类管理
- 权限控制
- 知识图谱（未来扩展）

**API 设计**：
```
GET    /api/v1/kb/documents          # 获取文档列表
POST   /api/v1/kb/documents          # 创建文档
GET    /api/v1/kb/documents/{id}     # 获取文档详情
PUT    /api/v1/kb/documents/{id}     # 更新文档
DELETE /api/v1/kb/documents/{id}     # 删除文档
POST   /api/v1/kb/search             # 全文搜索
GET    /api/v1/kb/categories         # 获取分类列表
POST   /api/v1/kb/categories         # 创建分类
```

**数据库表结构**：
```sql
-- 文档表
CREATE TABLE kb_documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(255) NOT NULL,
    content TEXT,
    category_id UUID REFERENCES kb_categories(id),
    tags VARCHAR[],
    created_by UUID REFERENCES sys_users(id),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    is_deleted BOOLEAN DEFAULT FALSE
);

-- 分类表
CREATE TABLE kb_categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    parent_id UUID REFERENCES kb_categories(id),
    sort_order INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT NOW()
);
```

### 4.2 AssetManagement Service (资产服务)

**职责**：
- 资产台账管理
- 资产生命周期跟踪（采购→入库→使用→报废）
- 耗材管理
- 组织架构同步（从金蝶/AD）
- 报表和导出

**API 设计**：
```
GET    /api/v1/am/assets              # 资产列表
POST   /api/v1/am/assets              # 新增资产
GET    /api/v1/am/assets/{id}         # 资产详情
PUT    /api/v1/am/assets/{id}         # 更新资产
DELETE /api/v1/am/assets/{id}         # 删除资产
POST   /api/v1/am/assets/{id}/transfer # 资产转移
POST   /api/v1/am/assets/{id}/scrap   # 资产报废
GET    /api/v1/am/consumables         # 耗材列表
POST   /api/v1/am/consumables/usage   # 领用记录
GET    /api/v1/am/reports/summary     # 资产汇总报表
GET    /api/v1/am/reports/department   # 部门资产报表
POST   /api/v1/am/sync/kingdee        # 同步金蝶数据
POST   /api/v1/am/sync/ad             # 同步AD组织架构
```

**数据库表结构**：
```sql
-- 资产表
CREATE TABLE am_assets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    asset_code VARCHAR(50) UNIQUE NOT NULL,  -- 资产编号
    asset_name VARCHAR(255) NOT NULL,
    asset_type VARCHAR(50),  -- 固定资产/耗材/低值易耗品
    category_id UUID REFERENCES am_categories(id),
    department_id UUID REFERENCES sys_departments(id),
    user_id UUID REFERENCES sys_users(id),  -- 使用人
    purchase_date DATE,
    purchase_price DECIMAL(15,2),
    vendor VARCHAR(255),
    warranty_period INT,  -- 保修期（月）
    status VARCHAR(20) DEFAULT 'in_use',  -- in_use/maintenance/scrap/transfer
    location VARCHAR(255),
    specs JSONB,  -- 规格参数（JSON格式）
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- 资产流水（生命周期记录）
CREATE TABLE am_asset_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    asset_id UUID REFERENCES am_assets(id),
    action_type VARCHAR(50),  -- purchase/receive/assign/return/repair/scrap
    from_user_id UUID REFERENCES sys_users(id),
    to_user_id UUID REFERENCES sys_users(id),
    from_department_id UUID REFERENCES sys_departments(id),
    to_department_id UUID REFERENCES sys_departments(id),
    action_date TIMESTAMP DEFAULT NOW(),
    remark TEXT,
    operated_by UUID REFERENCES sys_users(id)
);

-- 耗材领用记录
CREATE TABLE am_consumable_usage (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    consumable_id UUID REFERENCES am_assets(id),
    user_id UUID REFERENCES sys_users(id),
    department_id UUID REFERENCES sys_departments(id),
    quantity INT NOT NULL,
    usage_date TIMESTAMP DEFAULT NOW(),
    purpose TEXT,
    approved_by UUID REFERENCES sys_users(id)
);
```

### 4.3 AuthService (统一认证服务)

**职责**：
- LDAP/Active Directory 认证
- JWT Token 签发和验证
- 用户权限管理 (RBAC)
- 组织架构同步
- 单点登录 (SSO)

**API 设计**：
```
POST /api/v1/auth/login           # 用户名密码登录
POST /api/v1/auth/ldap-login      # LDAP 认证
POST /api/v1/auth/refresh         # 刷新 Token
POST /api/v1/auth/logout          # 退出登录
GET  /api/v1/auth/me              # 获取当前用户信息
GET  /api/v1/auth/users           # 用户列表（管理员）
POST /api/v1/auth/users           # 创建用户
GET  /api/v1/auth/roles           # 角色列表
POST /api/v1/auth/roles           # 创建角色
GET  /api/v1/auth/permissions     # 权限列表
POST /api/v1/auth/sync-ldap       # 同步 LDAP 用户
```

**数据库表结构**：
```sql
-- 用户表（统一用户）
CREATE TABLE sys_users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(100),
    phone VARCHAR(20),
    display_name VARCHAR(100),
    department_id UUID REFERENCES sys_departments(id),
    is_active BOOLEAN DEFAULT TRUE,
    is_admin BOOLEAN DEFAULT FALSE,
    ldap_dn VARCHAR(500),  -- LDAP  distinguished name
    created_at TIMESTAMP DEFAULT NOW(),
    last_login_at TIMESTAMP
);

-- 部门表（组织架构）
CREATE TABLE sys_departments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    dept_code VARCHAR(50) UNIQUE NOT NULL,
    dept_name VARCHAR(100) NOT NULL,
    parent_id UUID REFERENCES sys_departments(id),
    manager_id UUID REFERENCES sys_users(id),
    level INT DEFAULT 1,  -- 层级
    source VARCHAR(20) DEFAULT 'manual',  -- manual/kingdee/ad
    created_at TIMESTAMP DEFAULT NOW()
);

-- 角色表
CREATE TABLE sys_roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    role_code VARCHAR(50) UNIQUE NOT NULL,
    role_name VARCHAR(100) NOT NULL,
    description TEXT,
    created_at TIMESTAMP DEFAULT NOW()
);

-- 用户角色关联
CREATE TABLE sys_user_roles (
    user_id UUID REFERENCES sys_users(id) ON DELETE CASCADE,
    role_id UUID REFERENCES sys_roles(id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, role_id)
);

-- 权限表
CREATE TABLE sys_permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    resource VARCHAR(50) NOT NULL,  -- 资源名称：kb_document, am_asset
    action VARCHAR(50) NOT NULL,    -- 操作：create, read, update, delete
    description TEXT,
    UNIQUE(resource, action)
);

-- 角色权限关联
CREATE TABLE sys_role_permissions (
    role_id UUID REFERENCES sys_roles(id) ON DELETE CASCADE,
    permission_id UUID REFERENCES sys_permissions(id) ON DELETE CASCADE,
    PRIMARY KEY (role_id, permission_id)
);
```

---

## 5. 事件总线设计

使用 Redis Stream 作为轻量级事件总线，实现服务间异步通信。

### 5.1 事件类型

```
kb.document.created       # 文档创建
kb.document.updated       # 文档更新
kb.document.deleted       # 文档删除
am.asset.created          # 资产创建
am.asset.transferred      # 资产转移
am.asset.scrapped         # 资产报废
am.asset.low-stock        # 耗材库存不足
sys.user.created          # 用户创建
sys.user.ldap-synced      # LDAP 同步完成
sys.dept.changed          # 部门变更
```

### 5.2 事件格式

```json
{
  "event_id": "uuid",
  "event_type": "am.asset.created",
  "timestamp": "2024-01-01T00:00:00Z",
  "source": "asset-service",
  "payload": {
    "asset_id": "uuid",
    "asset_code": "ZC-2024-001",
    "asset_name": "ThinkPad X1",
    "department_id": "uuid",
    "user_id": "uuid"
  }
}
```

---

## 6. 部署架构

### 6.1 Docker Compose 编排

```yaml
version: '3.8'

services:
  # 数据库层
  postgres:
    image: postgres:15-alpine
    environment:
      POSTGRES_USER: openclaw
      POSTGRES_PASSWORD: ${DB_PASSWORD}
      POSTGRES_DB: openclaw_paas
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U openclaw"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    volumes:
      - redis_data:/data
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  minio:
    image: minio/minio:latest
    command: server /data --console-address ":9001"
    environment:
      MINIO_ROOT_USER: openclaw
      MINIO_ROOT_PASSWORD: ${MINIO_PASSWORD}
    volumes:
      - minio_data:/data
    ports:
      - "9000:9000"
      - "9001:9001"

  meilisearch:
    image: getmeili/meilisearch:v1.6
    environment:
      MEILI_MASTER_KEY: ${MEILI_KEY}
    volumes:
      - meili_data:/meili_data
    ports:
      - "7700:7700"

  # 服务层
  auth-service:
    image: openclaw/auth-service:latest
    environment:
      DB_CONNECTION: Host=postgres;Database=openclaw_paas;Username=openclaw;Password=${DB_PASSWORD}
      REDIS_CONNECTION: redis:6379
      JWT_SECRET: ${JWT_SECRET}
      LDAP_SERVER: ${LDAP_SERVER}
      LDAP_BIND_DN: ${LDAP_BIND_DN}
      LDAP_PASSWORD: ${LDAP_PASSWORD}
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
    ports:
      - "5001:80"
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health"]
      interval: 30s

  kb-service:
    image: openclaw/kb-service:latest
    environment:
      DB_CONNECTION: Host=postgres;Database=openclaw_paas;Username=openclaw;Password=${DB_PASSWORD}
      REDIS_CONNECTION: redis:6379
      MEILI_HOST: http://meilisearch:7700
      MEILI_KEY: ${MEILI_KEY}
      AUTH_SERVICE_URL: http://auth-service:80
    depends_on:
      - postgres
      - redis
      - meilisearch
    ports:
      - "5002:80"

  am-service:
    image: openclaw/am-service:latest
    environment:
      DB_CONNECTION: Host=postgres;Database=openclaw_paas;Username=openclaw;Password=${DB_PASSWORD}
      REDIS_CONNECTION: redis:6379
      MINIO_ENDPOINT: minio:9000
      MINIO_ACCESS_KEY: openclaw
      MINIO_SECRET_KEY: ${MINIO_PASSWORD}
      AUTH_SERVICE_URL: http://auth-service:80
    depends_on:
      - postgres
      - redis
      - minio
    ports:
      - "5003:80"

  # 接入层
  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./frontend:/usr/share/nginx/html
    depends_on:
      - auth-service
      - kb-service
      - am-service

  # 监控层
  prometheus:
    image: prom/prometheus:latest
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus_data:/prometheus
    ports:
      - "9090:9090"

  grafana:
    image: grafana/grafana:latest
    environment:
      GF_SECURITY_ADMIN_PASSWORD: ${GRAFANA_PASSWORD}
    volumes:
      - grafana_data:/var/lib/grafana
      - ./grafana/dashboards:/etc/grafana/provisioning/dashboards
    ports:
      - "3000:3000"

  loki:
    image: grafana/loki:2.9.0
    ports:
      - "3100:3100"
    volumes:
      - ./loki-config.yml:/etc/loki/local-config.yaml
    command: -config.file=/etc/loki/local-config.yaml

  promtail:
    image: grafana/promtail:2.9.0
    volumes:
      - /var/log:/var/log:ro
      - ./promtail-config.yml:/etc/promtail/config.yml
    command: -config.file=/etc/promtail/config.yml

  jaeger:
    image: jaegertracing/all-in-one:1.50
    ports:
      - "16686:16686"
      - "4317:4317"
    environment:
      COLLECTOR_OTLP_ENABLED: true

volumes:
  postgres_data:
  redis_data:
  minio_data:
  meili_data:
  prometheus_data:
  grafana_data:
```

### 6.2 Nginx 路由配置

```nginx
upstream auth_service {
    server auth-service:80;
}

upstream kb_service {
    server kb-service:80;
}

upstream am_service {
    server am-service:80;
}

server {
    listen 80;
    server_name _;

    # 前端应用
    location / {
        root /usr/share/nginx/html;
        try_files $uri $uri/ /index.html;
    }

    # 认证服务
    location /api/v1/auth/ {
        proxy_pass http://auth_service/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }

    # 知识库服务
    location /api/v1/kb/ {
        auth_request /api/v1/auth/verify;
        proxy_pass http://kb_service/;
        proxy_set_header Host $host;
        proxy_set_header X-User-Id $auth_user_id;
    }

    # 资产服务
    location /api/v1/am/ {
        auth_request /api/v1/auth/verify;
        proxy_pass http://am_service/;
        proxy_set_header Host $host;
        proxy_set_header X-User-Id $auth_user_id;
    }

    # 内部认证验证
    location = /api/v1/auth/verify {
        internal;
        proxy_pass http://auth_service/api/v1/auth/verify;
        proxy_pass_request_body off;
        proxy_set_header Content-Length "";
        proxy_set_header X-Original-URI $request_uri;
    }

    # 监控入口
    location /monitoring/ {
        proxy_pass http://grafana:3000/;
    }
}
```

---

## 7. 实施路线图

### 阶段 1：基础设施部署（Week 1）
- [ ] 部署 PostgreSQL + Redis + MinIO + MeiliSearch
- [ ] 配置 Nginx 路由规则
- [ ] 部署监控栈（Prometheus + Grafana + Loki + Jaeger）
- [ ] 设置告警规则（Alertmanager）

### 阶段 2：认证服务（Week 2）
- [ ] 开发 AuthService（.NET 8）
- [ ] 实现 JWT 认证
- [ ] 实现 LDAP/AD 集成
- [ ] RBAC 权限系统
- [ ] 用户管理界面

### 阶段 3：知识库迁移（Week 3）
- [ ] 迁移 SQLite 到 PostgreSQL
- [ ] 集成 MeiliSearch 全文检索
- [ ] 接入事件总线
- [ ] 容器化部署
- [ ] 性能测试

### 阶段 4：资产服务（Week 4-5）
- [ ] 开发 AssetManagement API
- [ ] 实现资产生命周期管理
- [ ] 金蝶/AD 数据同步
- [ ] 报表和导出功能
- [ ] 前端管理界面

### 阶段 5：统一平台（Week 6）
- [ ] 开发统一控制台前端
- [ ] 集成所有服务 API
- [ ] 实现 SSO 单点登录
- [ ] 统一日志和监控
- [ ] 生产环境调优

---

## 8. 资源需求

### 8.1 最小部署（开发环境）

| 服务 | CPU | 内存 | 存储 |
|------|-----|------|------|
| PostgreSQL | 0.5 | 512MB | 10GB |
| Redis | 0.25 | 256MB | 1GB |
| MinIO | 0.25 | 256MB | 10GB |
| MeiliSearch | 0.5 | 512MB | 5GB |
| AuthService | 0.25 | 256MB | - |
| KB Service | 0.5 | 512MB | - |
| AM Service | 0.5 | 512MB | - |
| Nginx | 0.1 | 128MB | - |
| Prometheus | 0.25 | 256MB | 5GB |
| Grafana | 0.25 | 256MB | 1GB |
| Loki | 0.25 | 256MB | 5GB |
| **总计** | **~3.4** | **~4.2GB** | **~37GB** |

当前阿里云节点（4核8G）可以运行，但生产环境建议升级到 8核16G。

### 8.2 生产环境建议
- 应用服务器：4核8G × 2（负载均衡）
- 数据库服务器：4核8G（PostgreSQL + Redis）
- 存储服务器：2核4G（MinIO + MeiliSearch）
- 监控服务器：2核4G（Prometheus + Grafana + Loki）

---

## 9. 安全设计

### 9.1 认证与授权
- 统一 JWT Token 认证（RS256 非对称加密）
- LDAP/AD 集成，支持企业组织架构
- RBAC 权限模型（用户-角色-权限）
- API 级别权限控制
- 敏感操作二次确认

### 9.2 数据安全
- 数据库连接 SSL/TLS
- 敏感字段加密存储（AES-256）
- 定期备份策略（每日全量 + 实时增量）
- 审计日志记录所有操作

### 9.3 网络安全
- Nginx 反向代理，隐藏内部服务
- 速率限制（Rate Limiting）
- IP 白名单支持
- WAF 规则防护（SQL 注入、XSS）
- 定期安全扫描

---

## 10. 运维与监控

### 10.1 监控指标
- **系统层**：CPU、内存、磁盘、网络
- **应用层**：QPS、响应时间、错误率、并发数
- **业务层**：活跃用户数、操作频率、数据增长
- **数据库**：连接数、慢查询、缓存命中率

### 10.2 告警规则
- CPU > 80% 持续 5 分钟
- 内存 > 85% 持续 5 分钟
- 磁盘 > 80%
- API 错误率 > 5%
- 服务不可用（心跳检测失败）
- 数据库连接数 > 80%

### 10.3 日志规范
- 统一 JSON 格式日志
- 包含 trace_id 实现全链路追踪
- 分级日志：DEBUG、INFO、WARN、ERROR、FATAL
- 日志保留策略：30 天热存储，90 天冷存储

---

## 11. 扩展性设计

### 11.1 水平扩展路径
当前 Docker Compose → 未来 Kubernetes：
- 服务无状态化设计（状态外置到 Redis/DB）
- 配置外部化（环境变量 + 配置中心）
- 支持多实例部署
- 健康检查接口标准化

### 11.2 未来功能扩展
- **消息通知**：钉钉/企业微信/邮件集成
- **工作流引擎**：资产审批流程
- **BI 分析**：资产使用率、成本分析
- **IoT 集成**：资产标签、位置追踪
- **AI 助手**：智能问答、自动分类

---

## 12. 总结

本架构设计采用**渐进式演进**策略：
- **当前**：Docker Compose 单节点部署，满足 300 用户规模
- **未来**：迁移到 Kubernetes，支持多节点集群

核心技术栈：
- **后端**：.NET 8 + PostgreSQL + Redis
- **前端**：React/Vue（SPA）或 Razor Pages
- **运维**：Prometheus + Grafana + Loki + Jaeger
- **部署**：Docker + GitHub Actions

预计总开发周期：**6 周**（2 周基础设施 + 4 周业务开发）

如需立即启动，我可以在**1 小时内**完成基础设施部署（PostgreSQL + Redis + MinIO + MeiliSearch + 监控栈）。
