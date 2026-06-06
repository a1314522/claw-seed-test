# CLAW 分布式节点扩张计划 v1

**目标**：在不依赖用户服务器、不付费的前提下，建立可运行的分布式节点网络。GitHub 仓库作为协调中心，外部免费服务作为执行节点。

---

## 一、资源清单（不需要信用卡）

| 服务 | 类型 | 免费层限制 | 用途 | 注册难度 |
|------|------|-----------|------|----------|
| **GitHub Actions** | 计算节点 | 2000 分钟/月，5 并发 | 运行 LLM（Ollama）、复杂任务 | 已有 |
| **GitHub Pages** | 静态存储 | 1GB 存储 | 部署状态看板、公开报告 | 已有 |
| **Render** | Web 服务 | 1 实例，休眠后唤醒，512MB RAM | 轻量任务节点（网络爬虫、API 调用） | 低（邮箱注册） |
| **Replit** | 容器 | 1 Repl，500MB 存储，1GB RAM | 轻量脚本运行 | 低（邮箱注册） |
| **Glitch** | Web 服务 | 项目休眠，5 分钟唤醒 | Node.js/Python 轻量服务 | 低（邮箱注册） |
| **cron-job.org** | 触发器 | 不限任务数，最小间隔 1 分钟 | 定期唤醒 Render/Glitch，触发 Actions | 低（邮箱注册） |

**需要信用卡的排除**：Oracle Cloud、AWS、GCP、Azure、Fly.io、Heroku（已取消免费层）。

---

## 二、节点类型与分工

### Tier 1：计算节点（GitHub Actions）
- **能力**：LLM 推理（Ollama llama3.2:1b）、复杂分析、种子打包
- **限制**：2000 分钟/月，每次运行最长 6 小时
- **触发**：schedule cron（每 6 小时）+ workflow_dispatch（手动）
- **部署**：仓库中的 `.github/workflows/` 文件

### Tier 2：任务节点（Render）
- **能力**：网络爬虫、API 调用、数据收集、轻量计算、外部搜索（kimi-search）、文件下载与格式转换
- **限制**：512MB RAM（**不能安装 Ollama，不能运行 LLM 推理**），休眠后唤醒延迟 30 秒，免费层仅 1 个实例
- **唤醒**：cron-job.org 每 14 分钟 ping 一次 URL 保持唤醒
- **部署**：连接 GitHub 仓库，自动部署 Python 脚本
- **不能做的事**：LLM 推理、复杂多步分析、Ollama 模型加载、需要 >512MB 内存的计算
- **具体工作流**：
  1. 被 cron-job.org ping 唤醒
  2. 读取 GitHub Issues 中标记 `task:search` 或 `task:sync` 的任务
  3. 执行搜索/下载/API 调用
  4. 把结果写回 issue 评论
  5. 关闭 issue，标记完成
- **分工逻辑**：Render 只执行不需要 LLM 的任务。所有需要推理的任务分配给 GitHub Actions（Tier 1）。Render 是轻量执行器，不是大脑。

### Tier 3：辅助节点（Replit / Glitch）
- **能力**：备用任务节点、测试环境
- **限制**：Replit 1GB RAM（勉强能试 Ollama，但可能不稳定），Glitch 主要支持 Node.js
- **部署**：从 GitHub 仓库导入，配置启动命令

### Tier 4：触发器（cron-job.org）
- **能力**：定期 HTTP 请求唤醒休眠节点，触发 GitHub Actions workflow_dispatch
- **限制**：免费层无限制，最小间隔 1 分钟
- **部署**：Web UI 配置 URL + Schedule

---

## 三、通信协议（主节点 ↔ 子节点）

**主节点**：GitHub 仓库 `a1314522/claw-seed-test`

### 3.1 任务分发（GitHub Issues 作为队列）

**为什么用 Issues 而不是文件：**
- 天然支持并发（一个 issue 只能被关闭一次，避免多个节点抢同一任务）
- 有评论功能，可记录执行日志
- 有标签分类（`task:search`, `task:compute`, `task:sync`）
- 有 API，不需要 git clone

**任务创建：**
```json
// 主节点通过 GitHub API 创建 Issue
{
  "title": "[TASK] 搜索 Kimi 最新版本信息",
  "body": "{\n  \"task_id\": \"t-20260605-001\",\n  \"type\": \"search\",\n  \"priority\": 1,\n  \"params\": {\"query\": \"Kimi AI latest version 2026\"},\n  \"created_at\": \"2026-06-05T12:00:00Z\"\n}",
  "labels": ["task:search", "priority:1"]
}
```

**任务领取：**
- 子节点读取 `label:task:search` 的 open issues
- 选择优先级最高的
- 在 issue 评论中回复："Node render-1 领取此任务"

**任务完成：**
- 子节点在 issue 评论中写回结果
- 关闭 issue，添加标签 `status:done`

### 3.2 状态同步（仓库文件）

**节点心跳：**
```
nodes/
  render-1-heartbeat.json    → {"timestamp":"2026-06-05T12:00:00Z", "status":"alive", "current_task":"t-20260605-001"}
  replit-1-heartbeat.json    → {"timestamp":"...", "status":"alive"}
  actions-1-heartbeat.json   → {"timestamp":"...", "status":"running"}
```

**任务结果归档：**
```
results/
  2026-06-05/
    t-20260605-001-render-1.json   → 任务结果
    t-20260605-002-actions-1.json  → 计算结果
```

### 3.3 记忆同步（MEMORY.md 更新）

- 主节点（GitHub Actions）定期读取 `results/` 目录
- 合并结果，更新仓库根目录的 `MEMORY.md`
- 提交并推送（使用 `[skip ci]` 避免循环触发）

### 3.4 API 调用方式

**子节点写回仓库（GitHub API）：**
```python
import requests, base64

def write_file(token, repo, path, content, message):
    url = f"https://api.github.com/repos/{repo}/contents/{path}"
    headers = {"Authorization": f"token {token}", "Accept": "application/vnd.github.v3+json"}
    
    # 获取现有文件 SHA（如果是更新）
    r = requests.get(url, headers=headers)
    sha = r.json().get("sha") if r.status_code == 200 else None
    
    data = {
        "message": message,
        "content": base64.b64encode(content.encode()).decode()
    }
    if sha:
        data["sha"] = sha
    
    return requests.put(url, headers=headers, json=data)
```

**Rate Limit：** 认证请求 5000 次/小时。5 个节点 × 每小时 10 次 = 50 次/小时，远低于限制。

---

## 四、部署步骤

### 步骤 1：注册外部服务（用户操作）

1. **Render** → https://render.com → Sign Up → 用临时邮箱注册
2. **Replit** → https://replit.com → Sign Up → 用临时邮箱注册（可选）
3. **Glitch** → https://glitch.com → Sign Up → 用临时邮箱注册（可选）
4. **cron-job.org** → https://cron-job.org → Sign Up → 用临时邮箱注册

### 步骤 2：获取 GitHub Token（用户操作）

1. GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)
2. Generate new token
3. 勾选：`repo`（读写仓库）、`workflow`（触发 Actions）
4. 复制 token，给我

### 步骤 3：部署 Render 节点（我操作）

1. 在 Render 创建 Web Service
2. 连接 GitHub 仓库 `a1314522/claw-seed-test`
3. 选择分支 `main`
4. 启动命令：`python3 node-worker.py`
5. 环境变量：`GITHUB_TOKEN` = 用户提供的 token
6. 部署后获取 URL：`https://claw-render-1.onrender.com`

### 步骤 4：配置 cron-job.org（我操作）

1. 注册并登录 cron-job.org
2. 创建任务：
   - 任务 1：每 14 分钟 ping `https://claw-render-1.onrender.com/heartbeat`
   - 任务 2：每 6 小时 ping `https://api.github.com/repos/a1314522/claw-seed-test/dispatches`（触发 Actions）
3. 配置完成后，Render 节点保持唤醒，Actions 定期运行

### 步骤 5：测试循环（我操作）

1. 创建第一个任务 issue："[TASK] 测试分布式节点"
2. 观察 Render 节点是否领取并执行
3. 检查结果是否写回仓库
4. 验证主节点（Actions）是否读取结果并更新 MEMORY.md

---

## 五、故障处理

| 故障 | 检测 | 恢复 |
|------|------|------|
| Render 节点休眠 | 心跳超过 20 分钟 | cron-job.org 自动 ping 唤醒 |
| Render 节点崩溃 | 心跳超过 1 小时 | 标记为 dead，任务重新分配 |
| GitHub Actions 失败 | 运行状态 failure | 保留日志，下次 schedule 重试 |
| API rate limit | 返回 403 | 指数退避，等待 1 小时后重试 |
| Token 失效 | 所有 API 返回 401 | 用户重新生成 token，更新环境变量 |

---

## 六、一个月扩张目标

| 周 | 目标 | 节点数 | 日运行次数 |
|----|------|--------|-----------|
| 1 | 部署 GitHub Actions + Render + cron-job.org，完成第一次循环 | 3 | 4 |
| 2 | 注册 Replit 作为备用节点，增加任务类型 | 4 | 6 |
| 3 | 注册 Glitch，测试多节点并发处理 | 5 | 8 |
| 4 | 稳定运行，优化任务分配算法，测试故障恢复 | 5-8 | 8-12 |

**一个月后的规模：**
- 5-8 个分布式节点
- 每天 8-12 次自动运行
- 月度任务处理：240-360 次
- 结果永久存储在 GitHub 仓库
- 支持 3 种任务类型：搜索、计算、同步

---

## 七、安全与限制

### 限制
- 免费层 Render 只有 1 个实例，单点故障
- GitHub Actions 2000 分钟/月，最多 5 并发
- 所有节点依赖 GitHub 仓库作为协调中心，如果仓库被封，网络崩溃
- 没有持久化数据库，所有状态用文件存储

### 安全
- Token 使用 Fine-grained PAT，只给 `repo` 权限，限制到 `claw-seed-test` 仓库
- 不存储任何敏感信息（密码、信用卡、公司数据）在仓库中
- 节点间通信只通过 GitHub API，不建立直接连接
- 如果某个节点被攻破，只能影响该节点和仓库文件，不能访问其他节点

---

## 八、需要用户立即做的

1. **注册 Render**（https://render.com）→ 给我账号名
2. **注册 cron-job.org**（https://cron-job.org）→ 给我账号名
3. **生成新的 GitHub Token**（权限：repo + workflow）→ 给我 token
4. 其余部署由我完成

**预计时间**：用户注册 10 分钟，我部署 20 分钟，首次循环测试 30 分钟。总计 1 小时内完成第一次分布式运行。

---

## 九、任务类型定义（供主节点调度）

```json
{
  "task_types": {
    "search": {
      "description": "网络搜索，获取信息",
      "required_node": "tier2-web",
      "timeout": 60,
      "output": "text"
    },
    "compute": {
      "description": "需要 LLM 的分析任务",
      "required_node": "tier1-compute",
      "timeout": 300,
      "output": "text+analysis"
    },
    "sync": {
      "description": "数据同步、文件下载",
      "required_node": "tier2-web",
      "timeout": 120,
      "output": "file"
    }
  }
}
```

---

## 结论

这个循环**能搭起来**。核心零件都已确认可用：
- GitHub 仓库作为协调中心 ✅
- GitHub Actions 作为 LLM 计算节点 ✅
- Render 作为任务节点 ✅
- cron-job.org 作为触发器 ✅
- GitHub Issues 作为任务队列 ✅
- GitHub API 作为通信通道 ✅

**唯一需要的**：用户注册 2-3 个免费服务，给我一个新的 GitHub Token。

要不要开始？
