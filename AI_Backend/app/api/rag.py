from fastapi import APIRouter, Depends, HTTPException, status
from pydantic import BaseModel, Field

from app.api.deps import require_admin
from app.services import document_service
from app.services.document_extractor import DocumentExtractionError
from app.services.document_service import DocumentValidationError

router = APIRouter(prefix="/api/ai", tags=["RAG Ingestion"])


class IngestRequest(BaseModel):
    doc_id: str = Field(max_length=200)
    title: str = Field(max_length=500)
    content: str
    description: str | None = Field(default=None, max_length=2000)
    category: str | None = Field(default=None, max_length=100)


@router.post("/ingest")
def ingest_document(request: IngestRequest, user: dict = Depends(require_admin)):
    try:
        document = document_service.upsert_document_from_text(
            doc_id=request.doc_id,
            title=request.title,
            content=request.content,
            description=request.description,
            category=request.category,
            created_by=user["accountId"],
        )
        return {
            "success": True,
            "message": "Nạp tài liệu vào ChromaDB thành công",
            "data": document,
        }
    except (DocumentValidationError, DocumentExtractionError) as exc:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=str(exc),
        ) from exc
    except Exception as exc:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Lỗi khi ingest tài liệu: {exc}",
        ) from exc
