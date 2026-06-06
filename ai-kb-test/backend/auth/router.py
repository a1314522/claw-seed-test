from fastapi import APIRouter, HTTPException
from database.sqlite_db import get_user, create_user
from auth.models import UserLogin, Token
from auth.dependencies import verify_password, create_access_token, get_password_hash
from config import ADMIN_USERNAME, ADMIN_PASSWORD

router = APIRouter()

@router.post("/login", response_model=Token)
async def login(data: UserLogin):
    user = get_user(data.username)
    if not user:
        # 首次启动自动创建管理员
        if data.username == ADMIN_USERNAME and data.password == ADMIN_PASSWORD:
            create_user(ADMIN_USERNAME, get_password_hash(ADMIN_PASSWORD), is_admin=True)
            user = get_user(ADMIN_USERNAME)
        else:
            raise HTTPException(status_code=401, detail="用户名或密码错误")
    
    if not verify_password(data.password, user["password_hash"]):
        raise HTTPException(status_code=401, detail="用户名或密码错误")
    
    token = create_access_token({
        "sub": user["username"],
        "is_admin": user["is_admin"]
    })
    return {"access_token": token}
