from contextlib import asynccontextmanager
from pathlib import Path

import ollama
from fastapi import Depends, FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel

from app.api.health import router as health_router
from app.api.rag import router as rag_router
from app.config import settings
from app.core.security import verify_jwt_token
from app.db.database import init_db
# Import rag_service từ module services của bạn
from app.services.rag_service import rag_service

@asynccontextmanager
async def lifespan(app: FastAPI):
    Path(settings.DOCUMENTS_DIR).mkdir(parents=True, exist_ok=True)
    Path(settings.CHROMA_PERSIST_DIRECTORY).mkdir(parents=True, exist_ok=True)
    init_db()
    yield


app = FastAPI(
    title=settings.APP_NAME,
    version="1.0",
    lifespan=lifespan,
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=settings.cors_origin_list,
    allow_credentials=False,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(health_router)
app.include_router(rag_router)

class ChatRequest(BaseModel):
    prompt: str


@app.get("/")
def root():
    return {
        "service": settings.APP_NAME,
        "status": "running",
        "docs_url": "/docs",
        "chat_model": settings.CHAT_MODEL,
        "embedding_model": settings.EMBEDDING_MODEL,
    }


@app.post("/api/chat")
def chat_with_ai(request: ChatRequest, user: dict = Depends(verify_jwt_token)):
    try:
        # Bước 1: Tìm kiếm tài liệu liên quan trong ChromaDB dựa trên câu hỏi
        relevant_chunks = rag_service.query_rag(request.prompt)
        
        # Bước 2: Gom các đoạn tài liệu lại làm ngữ cảnh (Context)
        context_text = "\n\n".join(relevant_chunks)
        
        # Bước 3: Xây dựng System Prompt kết hợp tài liệu
        system_prompt = f"""Bạn là một trợ lý AI thông minh. Hãy trả lời câu hỏi của người dùng dựa CHÍNH XÁC vào các tài liệu được cung cấp dưới đây. Nếu thông tin không có trong tài liệu, hãy nói rằng bạn không biết, tuyệt đối không được tự bịa ra.

Tài liệu tham khảo:
{context_text}
"""

        # Bước 4: Gửi cho LLM (qwen2.5) kèm ngữ cảnh
        response = ollama.chat(
            model=settings.CHAT_MODEL,
            messages=[
                {'role': 'system', 'content': system_prompt},
                {'role': 'user', 'content': request.prompt},
            ],
        )
        
        return {
            "success": True,
            "answer": response["message"]["content"],
            "source_chunks_used": len(relevant_chunks) # Trả về số lượng đoạn tài liệu đã dùng tham khảo
        }
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(
        "app.main:app",
        host=settings.APP_HOST,
        port=settings.APP_PORT,
        reload=True,
    )
