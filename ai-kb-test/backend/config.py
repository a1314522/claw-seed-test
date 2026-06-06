import os
import json
import hashlib
from datetime import datetime
from pathlib import Path
from typing import List, Dict, Any

BASE_DIR = Path(__file__).resolve().parent.parent
DATA_DIR = BASE_DIR / "data"
DOC_DIR = DATA_DIR / "documents"
DATA_DIR.mkdir(exist_ok=True)
DOC_DIR.mkdir(exist_ok=True)

EMBEDDING_MODEL = os.getenv("EMBEDDING_MODEL", "BAAI/bge-small-zh")
LLM_MODEL = os.getenv("LLM_MODEL", "qwen2.5:7b")
SECRET_KEY = os.getenv("SECRET_KEY", "test-secret-key")
ALGORITHM = "HS256"
ACCESS_TOKEN_EXPIRE_MINUTES = 480

ADMIN_USERNAME = os.getenv("ADMIN_USERNAME", "admin")
ADMIN_PASSWORD = os.getenv("ADMIN_PASSWORD", "admin123")

CHUNK_SIZE = 512
CHUNK_OVERLAP = 50
TOP_K = 5
RETRIEVAL_THRESHOLD = 0.7
