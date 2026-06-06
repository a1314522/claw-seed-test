from fastapi import APIRouter, Depends, HTTPException
from pydantic import BaseModel
from database.sqlite_db import get_user, create_user, list_users, delete_user
from auth.dependencies import get_current_user
import bcrypt

router = APIRouter(tags=["users"])

class UserCreate(BaseModel):
    username: str
    password: str
    is_admin: bool = False

class UserResponse(BaseModel):
    id: int
    username: str
    is_admin: bool
    created_at: str

@router.post("/")
def create_user_api(data: UserCreate, user=Depends(get_current_user)):
    if not user.get("is_admin"):
        raise HTTPException(403, "需要管理员权限")
    existing = get_user(data.username)
    if existing:
        raise HTTPException(400, "用户名已存在")
    password_hash = bcrypt.hashpw(data.password.encode(), bcrypt.gensalt()).decode()
    user_id = create_user(data.username, password_hash, data.is_admin)
    return {"id": user_id, "username": data.username, "is_admin": data.is_admin}

@router.get("/")
def list_all_users(user=Depends(get_current_user)):
    if not user.get("is_admin"):
        raise HTTPException(403, "需要管理员权限")
    return list_users()

@router.delete("/{user_id}")
def delete_user_api(user_id: int, user=Depends(get_current_user)):
    if not user.get("is_admin"):
        raise HTTPException(403, "需要管理员权限")
    if user_id == 1:
        raise HTTPException(400, "不能删除默认管理员")
    delete_user(user_id)
    return {"message": "用户已删除"}
