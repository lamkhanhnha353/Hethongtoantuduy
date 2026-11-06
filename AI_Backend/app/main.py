from contextlib import asynccontextmanager
from pathlib import Path

import ollama
from fastapi import Depends, FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel

from app.api.health import router as health_router
from app.config import settings
from app.core.security import verify_jwt_token
from app.db.database import init_db


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
        response = ollama.chat(
            model=settings.CHAT_MODEL,
            messages=[
                {
                    "role": "user",
                    "content": request.prompt,
                },
            ],
        )
        return {
            "success": True,
            "answer": response["message"]["content"],
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
