from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from fastapi.responses import FileResponse
from contextlib import asynccontextmanager
from pathlib import Path

from auth.router import router as auth_router
from documents.router import router as doc_router
from knowledge.router import router as kb_router
from categories.router import router as cat_router
from users.router import router as user_router
from history.router import router as hist_router
from database.sqlite_db import init_db
from config import DOC_DIR, BASE_DIR

FRONTEND_DIR = Path(__file__).resolve().parent.parent / "frontend"

@asynccontextmanager
async def lifespan(app: FastAPI):
    init_db()
    yield

app = FastAPI(
    title="AI知识库系统",
    description="企业内网智能知识检索问答系统",
    version="1.0.0",
    lifespan=lifespan
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.mount("/files", StaticFiles(directory=DOC_DIR), name="files")

app.include_router(auth_router, prefix="/api/auth", tags=["认证"])
app.include_router(doc_router, prefix="/api/documents", tags=["文档管理"])
app.include_router(kb_router, prefix="/api/knowledge", tags=["知识库"])
app.include_router(cat_router, prefix="/api/categories", tags=["分类管理"])
app.include_router(user_router, prefix="/api/users", tags=["用户管理"])
app.include_router(hist_router, prefix="/api/history", tags=["搜索历史"])

@app.get("/api/health")
async def health_check():
    return {"status": "ok", "version": "1.0.0"}

@app.get("/")
async def root():
    return FileResponse(FRONTEND_DIR / "index.html")

@app.get("/{path:path}")
async def serve_frontend(path: str):
    if path.startswith("api/") or path.startswith("files/"):
        raise HTTPException(status_code=404)
    return FileResponse(FRONTEND_DIR / "index.html")

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("main:app", host="0.0.0.0", port=8000, reload=True)
