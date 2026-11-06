import chromadb
import requests
from fastapi import APIRouter, Depends
from sqlalchemy import text

from app.config import settings
from app.core.security import verify_jwt_token
from app.db.database import SessionLocal

router = APIRouter(prefix="/api/ai", tags=["Health"])


@router.get("/health")
def health_check(user: dict = Depends(verify_jwt_token)):
    result = {
        "status": "ok",
        "ollama": "error",
        "chroma": "error",
        "sqlite": "error",
        "models": {
            "chat": settings.CHAT_MODEL,
            "embedding": settings.EMBEDDING_MODEL,
        },
    }

    try:
        response = requests.get(f"{settings.OLLAMA_BASE_URL}/api/tags", timeout=2)
        result["ollama"] = "ok" if response.status_code == 200 else "error"
    except Exception:
        result["ollama"] = "error"

    try:
        client = chromadb.PersistentClient(
            path=settings.CHROMA_PERSIST_DIRECTORY,
            settings=chromadb.Settings(anonymized_telemetry=False),
        )
        client.heartbeat()
        result["chroma"] = "ok"
    except Exception:
        result["chroma"] = "error"

    try:
        db = SessionLocal()
        db.execute(text("SELECT 1"))
        db.close()
        result["sqlite"] = "ok"
    except Exception:
        result["sqlite"] = "error"

    if any(result[key] == "error" for key in ("ollama", "chroma", "sqlite")):
        result["status"] = "degraded"

    return result
