from fastapi import APIRouter, Depends, HTTPException
from pydantic import BaseModel
from database.sqlite_db import (
    create_category, list_categories, get_category, delete_category, update_category
)
from auth.dependencies import get_current_user

router = APIRouter(tags=["categories"])

class CategoryCreate(BaseModel):
    name: str
    description: str = ""

class CategoryUpdate(BaseModel):
    name: str
    description: str = ""

@router.post("/")
def create(cat: CategoryCreate, user=Depends(get_current_user)):
    cid = create_category(cat.name, cat.description)
    return {"id": cid, "name": cat.name, "description": cat.description}

@router.get("/")
def list_all(user=Depends(get_current_user)):
    return list_categories()

@router.get("/{cat_id}")
def get_one(cat_id: int, user=Depends(get_current_user)):
    cat = get_category(cat_id)
    if not cat:
        raise HTTPException(404, "Category not found")
    return cat

@router.put("/{cat_id}")
def update(cat_id: int, cat: CategoryUpdate, user=Depends(get_current_user)):
    update_category(cat_id, cat.name, cat.description)
    return {"id": cat_id, "name": cat.name, "description": cat.description}

@router.delete("/{cat_id}")
def delete(cat_id: int, user=Depends(get_current_user)):
    if cat_id == 1:
        raise HTTPException(400, "Cannot delete default category")
    delete_category(cat_id)
    return {"message": "Deleted"}
