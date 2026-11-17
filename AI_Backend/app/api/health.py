import chromadb
import httpx
from fastapi import APIRouter, Depends
from fastapi.concurrency import run_in_threadpool
from sqlalchemy import text

from app.config import settings
from app.core.security import verify_jwt_token
from app.db.database import SessionLocal
from app.services.embedding_provider import create_embedding_provider
from app.services.llm_provider import create_primary_provider

router = APIRouter(prefix="/api/ai", tags=["Health"])


def _check_chroma() -> bool:
    try:
        client = chromadb.PersistentClient(
            path=settings.CHROMA_PERSIST_DIRECTORY,
            settings=chromadb.Settings(anonymized_telemetry=False),
        )
        client.heartbeat()
        return True
    except Exception:
        return False


def _check_sqlite() -> bool:
    try:
        db = SessionLocal()
        try:
            db.execute(text("SELECT 1"))
        finally:
            db.close()
        return True
    except Exception:
        return False


@router.get("/health")
async def health_check(user: dict = Depends(verify_jwt_token)):
    ollama_required = (
        settings.llm_provider_name in {"ollama", "local"}
        or settings.embedding_provider_name in {"ollama", "local"}
        or (
            settings.LLM_FALLBACK_ENABLED
            and settings.llm_provider_name not in {"ollama", "local"}
        )
    )
    result = {
        "status": "ok",
        "llm": "error",
        "llm_provider": settings.llm_provider_name,
        "embedding": "error",
        "embedding_provider": settings.embedding_provider_name,
        "ollama": "error" if ollama_required else "not_used",
        "chroma": "error",
        "sqlite": "error",
        "models": {
            "chat": settings.chat_model,
            "summary": settings.summary_model,
            "embedding": settings.embedding_model,
        },
    }

    primary = None
    try:
        primary = create_primary_provider()
        result["llm"] = "ok" if await primary.health() else "error"
    except Exception:
        result["llm"] = "error"
    finally:
        if primary is not None:
            try:
                await primary.close()
            except Exception:
                pass

    embedding_provider = None
    try:
        embedding_provider = create_embedding_provider()
        healthy = await run_in_threadpool(embedding_provider.health)
        result["embedding"] = "ok" if healthy else "error"
    except Exception:
        result["embedding"] = "error"
    finally:
        if embedding_provider is not None:
            try:
                embedding_provider.close()
            except Exception:
                pass

    if ollama_required:
        try:
            async with httpx.AsyncClient(timeout=2) as client:
                response = await client.get(f"{settings.OLLAMA_BASE_URL}/api/tags")
                result["ollama"] = (
                    "ok" if response.status_code == 200 else "error"
                )
        except Exception:
            result["ollama"] = "error"

    result["chroma"] = (
        "ok" if await run_in_threadpool(_check_chroma) else "error"
    )
    result["sqlite"] = (
        "ok" if await run_in_threadpool(_check_sqlite) else "error"
    )

    required_checks = ["llm", "embedding", "chroma", "sqlite"]
    if ollama_required:
        required_checks.append("ollama")

    if any(result[key] == "error" for key in required_checks):
        result["status"] = "degraded"

    return result
