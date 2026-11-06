from fastapi import APIRouter, Depends, HTTPException, status
from pydantic import BaseModel, Field

from app.core.security import verify_jwt_token
from app.services.rag_service import rag_service

router = APIRouter(prefix="/api/ai", tags=["RAG Ingestion"])


class IngestRequest(BaseModel):
    doc_id: str = Field(max_length=200)
    title: str = Field(max_length=500)
    content: str


def require_admin(user: dict = Depends(verify_jwt_token)) -> dict:
    if user.get("role") != "Admin":
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail="Chỉ Admin được phép nạp tài liệu",
        )
    return user


@router.post("/ingest")
def ingest_document(request: IngestRequest, user: dict = Depends(require_admin)):
    try:
        result = rag_service.ingest_text_document(
            doc_id=request.doc_id,
            title=request.title,
            content=request.content,
        )
        return {
            "success": True,
            "message": "Nạp tài liệu vào ChromaDB thành công",
            "data": result,
        }
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except Exception as exc:
        raise HTTPException(
            status_code=500,
            detail=f"Lỗi khi ingest tài liệu: {exc}",
        ) from exc
