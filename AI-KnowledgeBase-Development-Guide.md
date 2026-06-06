# AI 知识库完整开发文档

> 面向企业内网的智能知识库系统，支持本地文档检索、问答、权限管理。
> 适配域环境，无需外网依赖，可完全部署在内网服务器。

---

## 一、系统架构

```
┌─────────────────────────────────────────────────────────────────┐
│                        用户层 (User Layer)                        │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐        │
│  │ Web 前端 │  │ 企业微信 │  │  飞书   │  │  API   │        │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘        │
└───────┼────────────┼────────────┼────────────┼────────────┘
        │            │            │            │
┌───────┴────────────┴────────────┴────────────┴─────────────────┐
│                      API 网关层 (API Gateway)                     │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  FastAPI / Flask                                         │  │
│  │  - 认证鉴权 (JWT / LDAP)                                 │  │
│  │  - 请求限流                                              │  │
│  │  - 日志审计                                              │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
        │
┌───────┴─────────────────────────────────────────────────────────┐
│                    RAG 核心层 (RAG Core)                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │  Query解析   │  │  检索引擎    │  │  生成引擎    │          │
│  │  - 意图识别  │  │  - 向量检索  │  │  - 大模型    │          │
│  │  - 关键词提 │  │  - 混合搜索  │  │  - 提示工程  │          │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘          │
│         └──────────────────┼──────────────────┘                 │
│  ┌───────────────────────┴───────────────────────┐             │
│  │              Re-rank 重排序引擎               │             │
│  └───────────────────────────────────────────────┘             │
└─────────────────────────────────────────────────────────────────┘
        │
┌───────┴─────────────────────────────────────────────────────────┐
│                  数据层 (Data Layer)                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ 向量数据库     │  │ 文档存储     │  │ 元数据库     │      │
│  │  Milvus/      │  │  MinIO/      │  │  PostgreSQL/ │      │
│  │  Chroma       │  │  文件系统    │  │  SQLite      │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────────┘
        │
┌───────┴─────────────────────────────────────────────────────────┐
│                  文档处理层 (Document Processing)                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ 文档解析     │  │ 文本分块     │  │ 嵌入生成     │      │
│  │  - PDF/Word  │  │  - 语义分块  │  │  - 本地模型  │      │
│  │  - Excel/PPT │  │  - 滑动窗口  │  │  - 向量化    │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────────┘
```

---

## 二、技术选型

| 组件 | 推荐方案 | 备选方案 | 说明 |
|------|----------|----------|------|
| **后端框架** | FastAPI | Flask/Django | 异步高性能，自动 Swagger |
| **向量数据库** | Chroma | Milvus/PGVector | 轻量，内网部署方便 |
| **嵌入模型** | BGE-M3 (本地) | text2vec/m3e | 多语言，中文好，4GB |
| **大模型** | Ollama + qwen2.5 | vLLM/llama.cpp | 完全本地，无需外网 |
| **文档解析** | unstructured | python-docx/PyPDF2 | 复杂格式用前者 |
| **前端** | React + Ant Design | Vue3 + Element Plus | 企业级UI |
| **元数据库** | SQLite | PostgreSQL | 小团队用 SQLite 够了 |
| **文件存储** | 本地文件系统 | MinIO | 文档量<10GB直接用本地 |
| **任务队列** | 无需 | Celery/RQ | 300人规模同步处理即可 |

---

## 三、环境准备

### 3.1 服务器要求

| 规模 | CPU | 内存 | 存储 | 说明 |
|------|-----|------|------|------|
| 最小可用 | 4核 | 16GB | 50GB | 单模型，10文档 |
| 推荐配置 | 8核 | 32GB | 200GB | 多模型，1000文档 |
| 你的规模 | 16核 | 64GB+ | 500GB | 300人，支持并发 |

### 3.2 依赖安装

```bash
# Python 3.10+
python -m venv venv
source venv/bin/activate  # Windows: venv\Scripts\activate

pip install fastapi uvicorn[standard]
pip install chromadb sentence-transformers
pip install unstructured[all-docs]  # 文档解析
pip install ollama  # 本地大模型客户端
pip install langchain langchain-community
pip install python-multipart python-jose[cryptography]
pip install passlib[bcrypt]  # 密码哈希
pip install aiofiles  # 异步文件
```

### 3.3 本地模型部署（Ollama）

```bash
# 1. 安装 Ollama（Linux/macOS）
curl -fsSL https://ollama.com/install.sh | sh

# 2. 拉取模型（首次下载，约4-8GB）
ollama pull qwen2.5:7b          # 通用中文问答
ollama pull bge-m3              # 嵌入模型（如果ollama支持）

# 3. 验证
ollama run qwen2.5:7b
# 输入 "你好" 测试

# 4. 嵌入模型（HuggingFace下载）
# 会在代码中自动下载到 ~/.cache/huggingface
```

> Windows 用户：下载 https://ollama.com/download/windows 安装包

---

## 四、项目结构

```
ai-knowledge-base/
├── backend/                    # 后端服务
│   ├── main.py                # FastAPI 入口
│   ├── config.py              # 配置文件
│   ├── auth/                  # 认证模块
│   │   ├── __init__.py
│   │   ├── router.py          # 登录/注册路由
│   │   └── dependencies.py    # JWT校验依赖
│   ├── documents/             # 文档管理
│   │   ├── __init__.py
│   │   ├── router.py          # 上传/列表/删除
│   │   ├── models.py          # 数据模型
│   │   └── processor.py       # 文档解析+分块
│   ├── knowledge/             # 知识库核心
│   │   ├── __init__.py
│   │   ├── router.py          # 问答/检索
│   │   ├── retriever.py       # 向量检索逻辑
│   │   └── generator.py       # 大模型生成
│   ├── database/              # 数据库
│   │   ├── __init__.py
│   │   ├── chroma_client.py   # 向量库连接
│   │   └── sqlite_db.py       # 元数据库
│   └── utils/
│       ├── __init__.py
│       └── file_utils.py
│
├── frontend/                   # 前端（可选）
│   ├── public/
│   └── src/
│
├── models/                     # 本地模型缓存
├── data/                       # 数据目录
│   ├── documents/             # 原始文档
│   ├── chunks/                # 分块缓存
│   └── chroma_db/             # 向量数据库
│
├── requirements.txt
├── .env                        # 环境变量
└── README.md
```

---

## 五、核心代码实现

### 5.1 配置文件 `backend/config.py`

```python
import os
from pathlib import Path

BASE_DIR = Path(__file__).resolve().parent.parent

# 路径配置
DATA_DIR = BASE_DIR / "data"
DOC_DIR = DATA_DIR / "documents"
CHROMA_DIR = DATA_DIR / "chroma_db"

# 确保目录存在
DATA_DIR.mkdir(exist_ok=True)
DOC_DIR.mkdir(exist_ok=True)
CHROMA_DIR.mkdir(exist_ok=True)

# 模型配置
EMBEDDING_MODEL = os.getenv("EMBEDDING_MODEL", "BAAI/bge-m3")
LLM_MODEL = os.getenv("LLM_MODEL", "qwen2.5:7b")
LLM_BASE_URL = os.getenv("LLM_BASE_URL", "http://localhost:11434")  # Ollama默认端口

# 分块配置
CHUNK_SIZE = 512
CHUNK_OVERLAP = 50

# 检索配置
TOP_K = 5
RETRIEVAL_THRESHOLD = 0.7  # 相似度阈值

# 认证配置
SECRET_KEY = os.getenv("SECRET_KEY", "your-secret-key-change-this")
ALGORITHM = "HS256"
ACCESS_TOKEN_EXPIRE_MINUTES = 480  # 8小时

# 管理员账号（首次启动自动创建）
ADMIN_USERNAME = os.getenv("ADMIN_USERNAME", "admin")
ADMIN_PASSWORD = os.getenv("ADMIN_PASSWORD", "admin123")  # 生产环境修改！
```

### 5.2 主入口 `backend/main.py`

```python
from fastapi import FastAPI, Depends
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from contextlib import asynccontextmanager

from auth.router import router as auth_router
from documents.router import router as doc_router
from knowledge.router import router as kb_router
from database.sqlite_db import init_db
from config import DOC_DIR

@asynccontextmanager
async def lifespan(app: FastAPI):
    # 启动时初始化
    init_db()
    yield
    # 关闭时清理

app = FastAPI(
    title="AI知识库系统",
    description="企业内网智能知识检索问答系统",
    version="1.0.0",
    lifespan=lifespan
)

# CORS（前端跨域，生产环境限制域名）
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # 生产环境改为具体域名
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# 静态文件（上传的文档）
app.mount("/files", StaticFiles(directory=DOC_DIR), name="files")

# 路由注册
app.include_router(auth_router, prefix="/api/auth", tags=["认证"])
app.include_router(doc_router, prefix="/api/documents", tags=["文档管理"])
app.include_router(kb_router, prefix="/api/knowledge", tags=["知识库"])

@app.get("/api/health")
async def health_check():
    return {"status": "ok", "version": "1.0.0"}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("main:app", host="0.0.0.0", port=8000, reload=True)
```

### 5.3 认证模块

**`backend/auth/models.py`**
```python
from pydantic import BaseModel

class UserLogin(BaseModel):
    username: str
    password: str

class Token(BaseModel):
    access_token: str
    token_type: str = "bearer"

class UserInfo(BaseModel):
    username: str
    is_admin: bool = False
```

**`backend/auth/dependencies.py`**
```python
from fastapi import Depends, HTTPException, status
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
from jose import JWTError, jwt
from datetime import datetime, timedelta
from passlib.context import CryptContext

from config import SECRET_KEY, ALGORITHM, ACCESS_TOKEN_EXPIRE_MINUTES

pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")
security = HTTPBearer()

def verify_password(plain, hashed):
    return pwd_context.verify(plain, hashed)

def get_password_hash(password):
    return pwd_context.hash(password)

def create_access_token(data: dict):
    to_encode = data.copy()
    expire = datetime.utcnow() + timedelta(minutes=ACCESS_TOKEN_EXPIRE_MINUTES)
    to_encode.update({"exp": expire})
    return jwt.encode(to_encode, SECRET_KEY, algorithm=ALGORITHM)

async def get_current_user(credentials: HTTPAuthorizationCredentials = Depends(security)):
    token = credentials.credentials
    try:
        payload = jwt.decode(token, SECRET_KEY, algorithms=[ALGORITHM])
        username = payload.get("sub")
        if username is None:
            raise HTTPException(status_code=401, detail="Invalid token")
        return {"username": username, "is_admin": payload.get("is_admin", False)}
    except JWTError:
        raise HTTPException(status_code=401, detail="Invalid token")
```

**`backend/auth/router.py`**
```python
from fastapi import APIRouter, HTTPException
from database.sqlite_db import get_user, create_user
from auth.models import UserLogin, Token
from auth.dependencies import verify_password, create_access_token, get_password_hash
from config import ADMIN_USERNAME, ADMIN_PASSWORD

router = APIRouter()

# 首次启动自动创建管理员
@router.on_event("startup")
async def init_admin():
    user = get_user(ADMIN_USERNAME)
    if not user:
        create_user(ADMIN_USERNAME, get_password_hash(ADMIN_PASSWORD), is_admin=True)

@router.post("/login", response_model=Token)
async def login(data: UserLogin):
    user = get_user(data.username)
    if not user or not verify_password(data.password, user["password_hash"]):
        raise HTTPException(status_code=401, detail="用户名或密码错误")
    token = create_access_token({
        "sub": user["username"],
        "is_admin": user["is_admin"]
    })
    return {"access_token": token}
```

### 5.4 数据库层

**`backend/database/sqlite_db.py`**
```python
import sqlite3
from config import DATA_DIR

DB_PATH = DATA_DIR / "app.db"

def init_db():
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    c.execute('''
        CREATE TABLE IF NOT EXISTS users (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            username TEXT UNIQUE NOT NULL,
            password_hash TEXT NOT NULL,
            is_admin BOOLEAN DEFAULT 0,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    ''')
    c.execute('''
        CREATE TABLE IF NOT EXISTS documents (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            filename TEXT NOT NULL,
            original_name TEXT NOT NULL,
            file_size INTEGER,
            doc_type TEXT,  -- pdf/word/excel
            chunk_count INTEGER DEFAULT 0,
            status TEXT DEFAULT 'pending',  -- pending/processing/done/error
            uploaded_by TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    ''')
    conn.commit()
    conn.close()

def get_user(username: str) -> dict:
    conn = sqlite3.connect(DB_PATH)
    conn.row_factory = sqlite3.Row
    c = conn.cursor()
    c.execute("SELECT * FROM users WHERE username = ?", (username,))
    row = c.fetchone()
    conn.close()
    return dict(row) if row else None

def create_user(username, password_hash, is_admin=False):
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    c.execute(
        "INSERT INTO users (username, password_hash, is_admin) VALUES (?, ?, ?)",
        (username, password_hash, is_admin)
    )
    conn.commit()
    conn.close()

def add_document(filename, original_name, file_size, doc_type, uploaded_by):
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    c.execute(
        "INSERT INTO documents (filename, original_name, file_size, doc_type, uploaded_by) VALUES (?, ?, ?, ?, ?)",
        (filename, original_name, file_size, doc_type, uploaded_by)
    )
    doc_id = c.lastrowid
    conn.commit()
    conn.close()
    return doc_id

def list_documents() -> list:
    conn = sqlite3.connect(DB_PATH)
    conn.row_factory = sqlite3.Row
    c = conn.cursor()
    c.execute("SELECT * FROM documents ORDER BY created_at DESC")
    rows = c.fetchall()
    conn.close()
    return [dict(r) for r in rows]

def update_doc_status(doc_id, status, chunk_count=None):
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    if chunk_count is not None:
        c.execute("UPDATE documents SET status = ?, chunk_count = ? WHERE id = ?", (status, chunk_count, doc_id))
    else:
        c.execute("UPDATE documents SET status = ? WHERE id = ?", (status, doc_id))
    conn.commit()
    conn.close()
```

**`backend/database/chroma_client.py`**
```python
import chromadb
from chromadb.config import Settings
from sentence_transformers import SentenceTransformer
from config import CHROMA_DIR, EMBEDDING_MODEL

class ChromaManager:
    def __init__(self):
        self.client = chromadb.PersistentClient(
            path=str(CHROMA_DIR),
            settings=Settings(anonymized_telemetry=False)
        )
        self.collection = self.client.get_or_create_collection("knowledge_base")
        # 加载嵌入模型（首次下载约1-4GB）
        self.encoder = SentenceTransformer(EMBEDDING_MODEL, trust_remote_code=True)
    
    def add_chunks(self, chunks: list, doc_id: int, metadata: dict):
        """添加文档分块到向量库
        chunks: [(chunk_id, text), ...]
        """
        texts = [c[1] for c in chunks]
        ids = [f"doc{doc_id}_chunk{i}" for i in range(len(chunks))]
        metadatas = [{
            "doc_id": doc_id,
            "chunk_index": i,
            "source": metadata.get("filename", ""),
            **metadata
        } for i in range(len(chunks))]
        
        embeddings = self.encoder.encode(texts, normalize_embeddings=True).tolist()
        
        self.collection.add(
            embeddings=embeddings,
            documents=texts,
            metadatas=metadatas,
            ids=ids
        )
        return len(chunks)
    
    def query(self, question: str, top_k: int = 5, threshold: float = 0.7):
        """检索相关文本块"""
        embedding = self.encoder.encode([question], normalize_embeddings=True).tolist()
        results = self.collection.query(
            query_embeddings=embedding,
            n_results=top_k,
            include=["documents", "metadatas", "distances"]
        )
        
        # Chroma返回的是距离（越小越相似），转为相似度
        chunks = []
        for i in range(len(results["ids"][0])):
            distance = results["distances"][0][i]
            # 余弦距离转相似度: 1 - distance
            similarity = 1 - distance
            if similarity >= threshold:
                chunks.append({
                    "text": results["documents"][0][i],
                    "metadata": results["metadatas"][0][i],
                    "similarity": round(similarity, 3)
                })
        return chunks
    
    def delete_by_doc(self, doc_id: int):
        """删除文档的所有分块"""
        # Chroma 按 where 过滤删除
        self.collection.delete(where={"doc_id": doc_id})

chroma_mgr = ChromaManager()
```

### 5.5 文档处理模块

**`backend/documents/processor.py`**
```python
import os
import re
from pathlib import Path
from typing import List, Tuple

from config import CHUNK_SIZE, CHUNK_OVERLAP

def detect_file_type(filename: str) -> str:
    ext = Path(filename).suffix.lower()
    mapping = {
        '.pdf': 'pdf', '.docx': 'word', '.doc': 'word',
        '.xlsx': 'excel', '.xls': 'excel',
        '.pptx': 'ppt', '.ppt': 'ppt',
        '.txt': 'text', '.md': 'text', '.json': 'text'
    }
    return mapping.get(ext, 'unknown')

def extract_text(file_path: str, doc_type: str) -> str:
    """提取文档文本内容"""
    if doc_type == 'pdf':
        try:
            import fitz  # PyMuPDF
            doc = fitz.open(file_path)
            text = "\n".join(page.get_text() for page in doc)
            doc.close()
            return text
        except ImportError:
            # fallback: 使用 unstructured
            from unstructured.partition.pdf import partition_pdf
            elements = partition_pdf(file_path)
            return "\n".join(str(e) for e in elements)
    
    elif doc_type == 'word':
        from docx import Document
        doc = Document(file_path)
        return "\n".join(p.text for p in doc.paragraphs if p.text.strip())
    
    elif doc_type == 'excel':
        import pandas as pd
        df = pd.read_excel(file_path)
        return df.to_string()
    
    elif doc_type == 'text':
        with open(file_path, 'r', encoding='utf-8') as f:
            return f.read()
    
    else:
        return ""

def semantic_chunk(text: str, chunk_size: int = CHUNK_SIZE, overlap: int = CHUNK_OVERLAP) -> List[Tuple[int, str]]:
    """
    语义分块：按段落/句子边界切分，避免截断句子
    返回: [(chunk_index, chunk_text), ...]
    """
    # 先按段落分割
    paragraphs = re.split(r'\n\s*\n', text.strip())
    
    chunks = []
    current_chunk = []
    current_len = 0
    chunk_idx = 0
    
    for para in paragraphs:
        para = para.strip()
        if not para:
            continue
        
        para_len = len(para)
        
        # 如果当前段落本身就超过 chunk_size，按句子切分
        if para_len > chunk_size:
            sentences = re.split(r'(?<=[。！？.!?])\s+', para)
            for sent in sentences:
                if current_len + len(sent) > chunk_size and current_chunk:
                    chunks.append((chunk_idx, "\n".join(current_chunk)))
                    chunk_idx += 1
                    # 保留 overlap
                    overlap_text = "\n".join(current_chunk)
                    overlap_chars = overlap_text[-overlap:] if len(overlap_text) > overlap else overlap_text
                    current_chunk = [overlap_chars, sent]
                    current_len = len(overlap_chars) + len(sent)
                else:
                    current_chunk.append(sent)
                    current_len += len(sent)
        else:
            if current_len + para_len > chunk_size and current_chunk:
                chunks.append((chunk_idx, "\n".join(current_chunk)))
                chunk_idx += 1
                overlap_text = "\n".join(current_chunk)
                overlap_chars = overlap_text[-overlap:] if len(overlap_text) > overlap else overlap_text
                current_chunk = [overlap_chars, para]
                current_len = len(overlap_chars) + para_len
            else:
                current_chunk.append(para)
                current_len += para_len
    
    if current_chunk:
        chunks.append((chunk_idx, "\n".join(current_chunk)))
    
    return chunks

def process_document(file_path: str, doc_id: int, filename: str) -> int:
    """完整处理流程：解析→分块→向量化存储
    返回分块数量
    """
    doc_type = detect_file_type(filename)
    text = extract_text(file_path, doc_type)
    
    if not text.strip():
        return 0
    
    chunks = semantic_chunk(text)
    
    from database.chroma_client import chroma_mgr
    chroma_mgr.add_chunks(chunks, doc_id, metadata={
        "filename": filename,
        "doc_type": doc_type
    })
    
    return len(chunks)
```

**`backend/documents/router.py`**
```python
import shutil
import uuid
from fastapi import APIRouter, UploadFile, File, Depends, HTTPException
from fastapi.responses import FileResponse

from auth.dependencies import get_current_user
from database.sqlite_db import add_document, list_documents, update_doc_status
from database.chroma_client import chroma_mgr
from documents.processor import process_document, detect_file_type
from config import DOC_DIR

router = APIRouter()

@router.post("/upload")
async def upload_file(
    file: UploadFile = File(...),
    user = Depends(get_current_user)
):
    # 生成唯一文件名
    ext = file.filename.split('.')[-1] if '.' in file.filename else ''
    stored_name = f"{uuid.uuid4().hex}.{ext}" if ext else uuid.uuid4().hex
    file_path = DOC_DIR / stored_name
    
    # 保存文件
    with open(file_path, "wb") as f:
        shutil.copyfileobj(file.file, f)
    
    doc_type = detect_file_type(file.filename)
    file_size = file_path.stat().st_size
    
    # 记录到数据库
    doc_id = add_document(
        filename=stored_name,
        original_name=file.filename,
        file_size=file_size,
        doc_type=doc_type,
        uploaded_by=user["username"]
    )
    
    # 异步处理文档（实际生产用 Celery，这里同步处理）
    try:
        update_doc_status(doc_id, "processing")
        chunk_count = process_document(str(file_path), doc_id, file.filename)
        update_doc_status(doc_id, "done", chunk_count)
    except Exception as e:
        update_doc_status(doc_id, "error")
        raise HTTPException(status_code=500, detail=f"文档处理失败: {str(e)}")
    
    return {
        "id": doc_id,
        "filename": file.filename,
        "status": "done",
        "chunks": chunk_count
    }

@router.get("/list")
async def get_documents(user = Depends(get_current_user)):
    return list_documents()

@router.delete("/{doc_id}")
async def delete_document(doc_id: int, user = Depends(get_current_user)):
    if not user.get("is_admin"):
        raise HTTPException(status_code=403, detail="需要管理员权限")
    
    # 删除向量
    chroma_mgr.delete_by_doc(doc_id)
    
    # TODO: 删除文件和数据库记录
    return {"message": "删除成功"}
```

### 5.6 知识库问答核心

**`backend/knowledge/retriever.py`**
```python
from database.chroma_client import chroma_mgr
from config import TOP_K, RETRIEVAL_THRESHOLD

def retrieve_context(question: str) -> list:
    """检索相关上下文"""
    chunks = chroma_mgr.query(question, top_k=TOP_K, threshold=RETRIEVAL_THRESHOLD)
    return chunks

def format_context(chunks: list) -> str:
    """将检索结果格式化为上下文"""
    if not chunks:
        return ""
    
    context_parts = []
    for i, chunk in enumerate(chunks, 1):
        source = chunk["metadata"].get("source", "未知来源")
        text = chunk["text"].strip()
        context_parts.append(f"[来源{i}: {source}]\n{text}")
    
    return "\n\n---\n\n".join(context_parts)
```

**`backend/knowledge/generator.py`**
```python
import requests
import json
from config import LLM_MODEL, LLM_BASE_URL

def build_prompt(question: str, context: str) -> str:
    """构建提示词模板"""
    if context:
        return f"""你是一个专业的企业知识助手。请基于以下参考信息回答用户问题。
如果参考信息不足以回答问题，请明确说明"根据现有资料无法回答"。

参考信息：
{context}

用户问题：{question}

请用中文回答，保持简洁准确："""
    else:
        return f"""你是一个专业的企业知识助手。请回答用户问题。

用户问题：{question}

请用中文回答，保持简洁准确："""

def generate_answer(question: str, context: str) -> dict:
    """调用本地大模型生成回答"""
    prompt = build_prompt(question, context)
    
    try:
        # Ollama API 调用
        response = requests.post(
            f"{LLM_BASE_URL}/api/generate",
            json={
                "model": LLM_MODEL,
                "prompt": prompt,
                "stream": False,
                "options": {
                    "temperature": 0.7,
                    "top_p": 0.9,
                    "num_ctx": 4096
                }
            },
            timeout=120
        )
        response.raise_for_status()
        data = response.json()
        
        return {
            "answer": data.get("response", "").strip(),
            "model": LLM_MODEL,
            "source": "local"
        }
    except requests.exceptions.ConnectionError:
        return {
            "answer": "错误：无法连接到本地大模型服务。请确认 Ollama 已启动。",
            "model": LLM_MODEL,
            "source": "error"
        }
    except Exception as e:
        return {
            "answer": f"生成回答时出错: {str(e)}",
            "model": LLM_MODEL,
            "source": "error"
        }
```

**`backend/knowledge/router.py`**
```python
from fastapi import APIRouter, Depends
from pydantic import BaseModel

from auth.dependencies import get_current_user
from knowledge.retriever import retrieve_context, format_context
from knowledge.generator import generate_answer

router = APIRouter()

class QuestionRequest(BaseModel):
    question: str
    top_k: int = 5

class QuestionResponse(BaseModel):
    answer: str
    sources: list
    model: str

@router.post("/ask", response_model=QuestionResponse)
async def ask_question(data: QuestionRequest, user = Depends(get_current_user)):
    # 1. 检索相关文档
    chunks = retrieve_context(data.question)
    
    # 2. 格式化上下文
    context = format_context(chunks)
    
    # 3. 生成回答
    result = generate_answer(data.question, context)
    
    # 4. 组装来源信息
    sources = []
    for chunk in chunks:
        sources.append({
            "source": chunk["metadata"].get("source", "未知"),
            "similarity": chunk["similarity"],
            "snippet": chunk["text"][:200] + "..."
        })
    
    return {
        "answer": result["answer"],
        "sources": sources,
        "model": result["model"]
    }

@router.get("/search")
async def search_only(q: str, user = Depends(get_current_user)):
    """仅检索，不生成回答（用于调试）"""
    chunks = retrieve_context(q)
    return {
        "query": q,
        "results": chunks,
        "count": len(chunks)
    }
```

---

## 六、前端界面（最小可用版）

如果你不想单独写前端，直接用 Swagger UI 即可（FastAPI 自带）：
`http://localhost:8000/docs`

如果要给用户用，提供一个单页 HTML：

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>AI知识库</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: -apple-system, sans-serif; background: #f5f5f5; }
        .container { max-width: 900px; margin: 0 auto; padding: 20px; }
        .header { text-align: center; padding: 30px; }
        .header h1 { color: #333; }
        .chat-box { background: #fff; border-radius: 12px; padding: 20px; min-height: 400px; }
        .input-area { display: flex; gap: 10px; margin-top: 20px; }
        .input-area input { flex: 1; padding: 12px 16px; border: 1px solid #ddd; border-radius: 8px; font-size: 15px; }
        .input-area button { padding: 12px 24px; background: #1890ff; color: #fff; border: none; border-radius: 8px; cursor: pointer; }
        .message { margin-bottom: 16px; }
        .message.user { text-align: right; }
        .message .bubble { display: inline-block; padding: 12px 16px; border-radius: 12px; max-width: 80%; }
        .message.user .bubble { background: #1890ff; color: #fff; }
        .message.ai .bubble { background: #f0f0f0; color: #333; }
        .sources { font-size: 12px; color: #888; margin-top: 8px; }
        .login-form { max-width: 400px; margin: 100px auto; background: #fff; padding: 40px; border-radius: 12px; }
        .login-form input { width: 100%; padding: 12px; margin-bottom: 16px; border: 1px solid #ddd; border-radius: 6px; }
        .login-form button { width: 100%; padding: 12px; background: #1890ff; color: #fff; border: none; border-radius: 6px; }
        .nav { display: flex; gap: 20px; margin-bottom: 20px; }
        .nav a { color: #1890ff; text-decoration: none; }
        .upload-zone { border: 2px dashed #ddd; padding: 40px; text-align: center; border-radius: 8px; margin-bottom: 20px; }
        .doc-list { background: #fff; border-radius: 8px; padding: 20px; }
        .doc-item { display: flex; justify-content: space-between; padding: 12px; border-bottom: 1px solid #f0f0f0; }
    </style>
</head>
<body>
    <div id="app"></div>
    <script>
        const API_BASE = 'http://localhost:8000/api';
        let token = localStorage.getItem('token') || '';

        function render() {
            const app = document.getElementById('app');
            if (!token) {
                app.innerHTML = `
                    <div class="login-form">
                        <h2 style="text-align:center;margin-bottom:24px;">AI知识库登录</h2>
                        <input id="username" placeholder="用户名" value="admin">
                        <input id="password" type="password" placeholder="密码" value="admin123">
                        <button onclick="login()">登录</button>
                    </div>`;
                return;
            }
            app.innerHTML = `
                <div class="container">
                    <div class="header">
                        <h1>AI 知识库</h1>
                        <div class="nav">
                            <a href="#" onclick="showChat()">智能问答</a>
                            <a href="#" onclick="showDocs()">文档管理</a>
                            <a href="#" onclick="logout()">退出</a>
                        </div>
                    </div>
                    <div id="content"></div>
                </div>`;
            showChat();
        }

        async function login() {
            const u = document.getElementById('username').value;
            const p = document.getElementById('password').value;
            const res = await fetch(`${API_BASE}/auth/login`, {
                method: 'POST', headers: {'Content-Type': 'application/json'},
                body: JSON.stringify({username: u, password: p})
            });
            const data = await res.json();
            if (data.access_token) { token = data.access_token; localStorage.setItem('token', token); render(); }
            else { alert('登录失败'); }
        }

        function logout() { token = ''; localStorage.removeItem('token'); render(); }

        function showChat() {
            document.getElementById('content').innerHTML = `
                <div class="chat-box" id="chatBox"></div>
                <div class="input-area">
                    <input id="question" placeholder="输入问题..." onkeypress="if(event.key==='Enter')ask()">
                    <button onclick="ask()">发送</button>
                </div>`;
        }

        async function ask() {
            const q = document.getElementById('question').value.trim();
            if (!q) return;
            document.getElementById('question').value = '';
            const box = document.getElementById('chatBox');
            box.innerHTML += `<div class="message user"><div class="bubble">${q}</div></div>`;
            box.innerHTML += `<div class="message ai" id="loading"><div class="bubble">思考中...</div></div>`;
            
            const res = await fetch(`${API_BASE}/knowledge/ask`, {
                method: 'POST', headers: {'Content-Type': 'application/json', 'Authorization': `Bearer ${token}`},
                body: JSON.stringify({question: q})
            });
            const data = await res.json();
            document.getElementById('loading').remove();
            
            let sources = '';
            if (data.sources && data.sources.length > 0) {
                sources = '<div class="sources">来源: ' + data.sources.map(s => s.source).join(', ') + '</div>';
            }
            box.innerHTML += `<div class="message ai"><div class="bubble">${data.answer.replace(/\n/g, '<br>')}</div>${sources}</div>`;
            box.scrollTop = box.scrollHeight;
        }

        function showDocs() {
            document.getElementById('content').innerHTML = `
                <div class="upload-zone" id="dropZone">
                    <p>点击上传或拖拽文件到此处</p>
                    <input type="file" id="fileInput" style="display:none" onchange="upload(this.files[0])">
                </div>
                <div class="doc-list" id="docList">加载中...</div>`;
            document.getElementById('dropZone').onclick = () => document.getElementById('fileInput').click();
            loadDocs();
        }

        async function upload(file) {
            if (!file) return;
            const form = new FormData();
            form.append('file', file);
            await fetch(`${API_BASE}/documents/upload`, {
                method: 'POST', headers: {'Authorization': `Bearer ${token}`}, body: form
            });
            loadDocs();
        }

        async function loadDocs() {
            const res = await fetch(`${API_BASE}/documents/list`, {headers: {'Authorization': `Bearer ${token}`}});
            const docs = await res.json();
            const html = docs.map(d => `
                <div class="doc-item">
                    <span>${d.original_name} (${d.doc_type}) - ${d.status} ${d.chunk_count > 0 ? '(' + d.chunk_count + '块)' : ''}</span>
                    <span>${new Date(d.created_at).toLocaleString()}</span>
                </div>
            `).join('') || '<div style="padding:20px;color:#888;text-align:center">暂无文档</div>';
            document.getElementById('docList').innerHTML = html;
        }

        render();
    </script>
</body>
</html>
```

保存为 `frontend/index.html`，直接浏览器打开即可。

---

## 七、启动与部署

### 7.1 开发环境启动

```bash
# 1. 启动 Ollama
ollama serve

# 2. 启动后端（新终端）
cd backend
python main.py

# 3. 打开前端（浏览器）
# 直接打开 frontend/index.html
# 或 python -m http.server 3000
```

### 7.2 生产部署（systemd）

**`/etc/systemd/system/ai-kb.service`**
```ini
[Unit]
Description=AI Knowledge Base
After=network.target

[Service]
Type=simple
User=www-data
WorkingDirectory=/opt/ai-knowledge-base/backend
Environment="PATH=/opt/ai-knowledge-base/venv/bin"
Environment="SECRET_KEY=your-production-secret-key"
Environment="ADMIN_PASSWORD=your-admin-password"
ExecStart=/opt/ai-knowledge-base/venv/bin/python main.py
Restart=always

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl enable ai-kb
sudo systemctl start ai-kb
```

### 7.3 Nginx 反向代理（可选）

```nginx
server {
    listen 80;
    server_name kb.yourcompany.local;
    
    location / {
        root /opt/ai-knowledge-base/frontend;
        index index.html;
    }
    
    location /api/ {
        proxy_pass http://127.0.0.1:8000/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

---

## 八、与域环境集成

### 8.1 LDAP 认证（可选）

替换 `auth/router.py` 的登录逻辑：

```python
import ldap3
from config import LDAP_SERVER, LDAP_BASE_DN

def ldap_auth(username, password):
    server = ldap3.Server(LDAP_SERVER)
    conn = ldap3.Connection(server, f"{username}@{LDAP_BASE_DN}", password, auto_bind=True)
    return conn.bound
```

### 8.2 Windows 部署脚本

给 300 台机器统一部署，提供一个 PowerShell 安装脚本：

```powershell
# install-ai-kb.ps1
$installDir = "C:\ProgramData\AI-KnowledgeBase"
$port = 8000

# 创建目录
New-Item -ItemType Directory -Force -Path $installDir

# 下载 Python 安装包（如果未安装）
# ...

# 创建虚拟环境
python -m venv "$installDir\venv"
& "$installDir\venv\Scripts\pip.exe" install -r requirements.txt

# 创建服务
$serviceScript = @"
import subprocess
subprocess.run(["$installDir\venv\Scripts\python.exe", "$installDir\backend\main.py"])
"@

# 使用 NSSM 注册 Windows 服务
# nssm install AI-KnowledgeBase python.exe "$installDir\backend\main.py"
```

---

## 九、进阶优化

| 优化项 | 方案 | 优先级 |
|--------|------|--------|
| **多轮对话** | 维护对话历史，传入上下文 | 高 |
| **混合检索** | 向量检索 + 关键词 BM25 融合 | 中 |
| **文档解析增强** | 表格、图片 OCR、PDF 版面分析 | 中 |
| **权限控制** | 文档级/知识库级访问控制 | 中 |
| **增量更新** | 文档修改后只更新变更分块 | 低 |
| **对话记录** | 保存问答历史，支持回溯 | 低 |
| **多模型路由** | 根据问题类型切换不同模型 | 低 |

---

## 十、常见问题

**Q: 嵌入模型下载失败？**  
内网机器无法访问 HuggingFace，需要提前在外网下载好模型文件，复制到内网 `~/.cache/huggingface/` 目录。

**Q: Ollama 模型下载慢？**  
同样提前在外网 `ollama pull` 下载，模型文件在 `~/.ollama/models/`，整体复制到内网服务器。

**Q: 大模型回答质量差？**  
1. 检查检索结果是否相关（调低 `RETRIEVAL_THRESHOLD`）
2. 优化提示词模板（`build_prompt`）
3. 换更大的模型（`qwen2.5:14b` 或 `32b`）

**Q: 并发性能不够？**  
1. 使用 `vLLM` 替代 Ollama 部署大模型
2. 后端启用多 worker：`uvicorn main:app --workers 4`
3. 向量数据库换 Milvus（支持分布式）

---

## 参考资源

- [LangChain 文档](https://python.langchain.com/)
- [Chroma 文档](https://docs.trychroma.com/)
- [Ollama 文档](https://github.com/ollama/ollama)
- [FastAPI 文档](https://fastapi.tiangolo.com/)
- [BGE-M3 模型](https://huggingface.co/BAAI/bge-m3)

---

*文档版本: 1.0 | 2026-06-01*
*适配: 企业内网 / 域环境 / 300人规模*
