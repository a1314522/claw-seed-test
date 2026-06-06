import shutil, uuid, os
from fastapi import APIRouter, UploadFile, File, Depends, HTTPException
from auth.dependencies import get_current_user
from database.sqlite_db import add_document, list_documents, update_doc_status, add_chunk, delete_doc_chunks
from knowledge.retriever import searcher
from documents.processor import process_document, detect_file_type
from config import DOC_DIR

router = APIRouter()

@router.post("/upload")
async def upload_file(file: UploadFile = File(...), category_id: int = 1, user = Depends(get_current_user)):
    ext = file.filename.split('.')[-1] if '.' in file.filename else ''
    stored_name = f"{uuid.uuid4().hex}.{ext}" if ext else uuid.uuid4().hex
    file_path = DOC_DIR / stored_name
    with open(file_path, "wb") as f:
        shutil.copyfileobj(file.file, f)
    doc_type = detect_file_type(file.filename)
    file_size = file_path.stat().st_size
    doc_id = add_document(filename=stored_name, original_name=file.filename, file_size=file_size, doc_type=doc_type, uploaded_by=user["username"], category_id=category_id)
    try:
        update_doc_status(doc_id, "processing")
        chunk_count = process_document(str(file_path), doc_id, file.filename)
        update_doc_status(doc_id, "done", chunk_count)
        searcher.refresh()
    except Exception as e:
        update_doc_status(doc_id, "error")
        raise HTTPException(status_code=500, detail=f"文档处理失败: {str(e)}")
    return {"id": doc_id, "filename": file.filename, "status": "done", "chunks": chunk_count, "category_id": category_id}

@router.get("/list")
async def get_documents(category_id: int = None, user = Depends(get_current_user)):
    return list_documents(category_id=category_id)

@router.put("/{doc_id}/category")
async def move_document(doc_id: int, data: dict, user = Depends(get_current_user)):
    from database.sqlite_db import update_doc_category
    category_id = data.get("category_id", 1)
    update_doc_category(doc_id, category_id)
    searcher.refresh()
    return {"message": "分类已更新"}

@router.delete("/{doc_id}")
async def delete_document(doc_id: int, user = Depends(get_current_user)):
    if not user.get("is_admin"):
        raise HTTPException(status_code=403, detail="需要管理员权限")
    delete_doc_chunks(doc_id)
    return {"message": "删除成功"}
