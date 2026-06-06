from fastapi import APIRouter, Depends
from pydantic import BaseModel
from auth.dependencies import get_current_user
from knowledge.retriever import searcher
from knowledge.generator import generate_answer

router = APIRouter()

class QuestionRequest(BaseModel):
    question: str
    top_k: int = 5
    category_id: int = None

class QuestionResponse(BaseModel):
    answer: str
    sources: list
    model: str

@router.post("/ask", response_model=QuestionResponse)
async def ask_question(data: QuestionRequest, user = Depends(get_current_user)):
    chunks = searcher.search(data.question, top_k=data.top_k, category_id=data.category_id)
    context_parts = []
    for i, chunk in enumerate(chunks):
        context_parts.append(f"[来源{i+1}]\n{chunk['text']}")
    context = "\n\n---\n\n".join(context_parts)
    result = generate_answer(data.question, context)
    sources = []
    for chunk in chunks:
        sources.append({
            "source": chunk["metadata"].get("source", "未知"),
            "similarity": chunk["similarity"],
            "snippet": chunk["text"][:200] + "..."
        })
    return {"answer": result["answer"], "sources": sources, "model": result["model"]}

@router.get("/search")
async def search_only(q: str, category_id: int = None, user = Depends(get_current_user)):
    chunks = searcher.search(q, category_id=category_id)
    return {"query": q, "results": chunks, "count": len(chunks)}
