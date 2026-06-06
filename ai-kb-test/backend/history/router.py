from fastapi import APIRouter, Depends, HTTPException
from pydantic import BaseModel
from database.sqlite_db import add_search_history, get_search_history, clear_search_history
from auth.dependencies import get_current_user

router = APIRouter(tags=["history"])

class HistoryEntry(BaseModel):
    question: str
    answer: str
    sources: list = []

@router.post("/")
def save_history(data: HistoryEntry, user=Depends(get_current_user)):
    add_search_history(user["id"], data.question, data.answer, data.sources)
    return {"message": "已保存"}

@router.get("/")
def list_history(limit: int = 10, user=Depends(get_current_user)):
    return get_search_history(user["id"], limit)

@router.delete("/")
def clear_history(user=Depends(get_current_user)):
    clear_search_history(user["id"])
    return {"message": "已清空"}
