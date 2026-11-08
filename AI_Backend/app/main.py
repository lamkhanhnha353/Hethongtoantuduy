import logging
from contextlib import asynccontextmanager
from pathlib import Path

from fastapi import Depends, FastAPI, HTTPException
from fastapi.concurrency import run_in_threadpool
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel

from app.api.chat import router as chat_router
from app.api.conversations import router as conversations_router
from app.api.documents import router as documents_router
from app.api.health import router as health_router
from app.api.rag import router as rag_router
from app.config import settings
from app.core.security import verify_jwt_token
from app.db.database import init_db
from app.services.chat_service import build_messages
from app.services.llm_provider import (
    create_fallback_provider,
    create_primary_provider,
    should_fallback,
)
from app.services.rag_service import rag_service

logger = logging.getLogger(__name__)

@asynccontextmanager
async def lifespan(app: FastAPI):
    Path(settings.DOCUMENTS_DIR).mkdir(parents=True, exist_ok=True)
    Path(settings.CHROMA_PERSIST_DIRECTORY).mkdir(parents=True, exist_ok=True)
    init_db()
    try:
        yield
    finally:
        rag_service.close()


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
app.include_router(documents_router)
app.include_router(chat_router)
app.include_router(conversations_router)

class ChatRequest(BaseModel):
    prompt: str


@app.get("/")
def root():
    return {
        "service": settings.APP_NAME,
        "status": "running",
        "docs_url": "/docs",
        "llm_provider": settings.llm_provider_name,
        "chat_model": settings.chat_model,
        "embedding_provider": settings.embedding_provider_name,
        "embedding_model": settings.embedding_model,
    }


@app.post("/api/chat")
async def chat_with_ai(request: ChatRequest, user: dict = Depends(verify_jwt_token)):
    primary = None
    fallback = None
    try:
        sources = await run_in_threadpool(
            rag_service.query_rag_with_sources,
            request.prompt,
        )
        messages = build_messages(request.prompt, [], sources)

        primary = create_primary_provider()
        fallback = create_fallback_provider()

        try:
            answer = await primary.chat(
                messages=messages,
                model=settings.chat_model,
                temperature=settings.CHAT_TEMPERATURE,
                max_tokens=settings.LLM_MAX_TOKENS,
            )
            if not answer:
                raise RuntimeError("Mô hình AI trả về nội dung rỗng")
        except Exception as exc:
            if fallback is None or not should_fallback(exc):
                raise
            logger.warning(
                "Provider %s lỗi, fallback sang Ollama: %s",
                primary.name,
                exc,
            )
            answer = await fallback.chat(
                messages=messages,
                model=settings.OLLAMA_CHAT_MODEL,
                temperature=settings.CHAT_TEMPERATURE,
                max_tokens=settings.CHAT_NUM_PREDICT,
            )

        return {
            "success": True,
            "answer": answer,
            "source_chunks_used": len(sources),
        }
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc
    finally:
        for provider in (primary, fallback):
            if provider is None:
                continue
            try:
                await provider.close()
            except Exception:
                logger.exception(
                    "Không thể đóng LLM provider %s",
                    provider.name,
                )


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(
        "app.main:app",
        host=settings.APP_HOST,
        port=settings.APP_PORT,
        reload=True,
    )
