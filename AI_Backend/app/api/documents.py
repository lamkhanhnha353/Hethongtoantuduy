from typing import NoReturn

from fastapi import (
    APIRouter,
    Depends,
    File,
    Form,
    HTTPException,
    Query,
    UploadFile,
    status,
)
from pydantic import BaseModel, Field, field_validator
from starlette.concurrency import run_in_threadpool

from app.api.deps import require_admin
from app.services import document_service
from app.services.document_extractor import DocumentExtractionError
from app.services.document_service import (
    DocumentNotFoundError,
    DocumentValidationError,
)

router = APIRouter(prefix="/api/ai/documents", tags=["AI Knowledge Documents"])


class DocumentCreateRequest(BaseModel):
    doc_id: str = Field(min_length=1, max_length=200)
    title: str = Field(min_length=1, max_length=500)
    content: str = Field(min_length=1)
    description: str | None = Field(default=None, max_length=2000)
    category: str | None = Field(default=None, max_length=100)

    @field_validator("doc_id", "title", "content")
    @classmethod
    def strip_required(cls, value: str) -> str:
        cleaned = value.strip()
        if not cleaned:
            raise ValueError("Giá trị không được để trống")
        return cleaned


class DocumentUpdateRequest(BaseModel):
    title: str | None = Field(default=None, max_length=500)
    description: str | None = Field(default=None, max_length=2000)
    category: str | None = Field(default=None, max_length=100)

    @field_validator("title")
    @classmethod
    def strip_title(cls, value: str | None) -> str | None:
        if value is None:
            return None
        cleaned = value.strip()
        if not cleaned:
            raise ValueError("Tiêu đề không được để trống")
        return cleaned


def _raise_http(exc: Exception) -> NoReturn:
    if isinstance(exc, DocumentNotFoundError):
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=str(exc),
        ) from exc
    if isinstance(exc, (DocumentValidationError, DocumentExtractionError)):
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=str(exc),
        ) from exc
    raise exc


@router.get("")
def list_documents(
    page: int = Query(default=1, ge=1),
    page_size: int = Query(default=20, ge=1, le=100),
    search: str | None = Query(default=None, max_length=200),
    document_status: str | None = Query(default=None, alias="status", max_length=20),
    category: str | None = Query(default=None, max_length=100),
    user: dict = Depends(require_admin),
):
    return document_service.list_documents(
        page=page,
        page_size=page_size,
        search=search,
        status=document_status,
        category=category,
    )


@router.get("/stats")
def get_document_stats(user: dict = Depends(require_admin)):
    return document_service.get_document_stats()


@router.post("/backfill")
def backfill_documents(user: dict = Depends(require_admin)):
    return document_service.backfill_documents_from_chroma(
        created_by=user["accountId"],
    )


@router.post("", status_code=status.HTTP_201_CREATED)
def create_document(
    request: DocumentCreateRequest,
    user: dict = Depends(require_admin),
):
    try:
        return document_service.upsert_document_from_text(
            doc_id=request.doc_id,
            title=request.title,
            content=request.content,
            description=request.description,
            category=request.category,
            created_by=user["accountId"],
        )
    except (DocumentValidationError, DocumentExtractionError) as exc:
        _raise_http(exc)


@router.post("/upload", status_code=status.HTTP_201_CREATED)
async def upload_document(
    file: UploadFile = File(...),
    doc_id: str | None = Form(default=None),
    title: str | None = Form(default=None),
    description: str | None = Form(default=None),
    category: str | None = Form(default=None),
    user: dict = Depends(require_admin),
):
    data = await file.read()
    try:
        return await run_in_threadpool(
            document_service.upload_document,
            filename=file.filename or "",
            data=data,
            doc_id=doc_id,
            title=title,
            description=description,
            category=category,
            created_by=user["accountId"],
        )
    except (DocumentValidationError, DocumentExtractionError) as exc:
        _raise_http(exc)


@router.get("/{doc_id}")
def get_document(doc_id: str, user: dict = Depends(require_admin)):
    try:
        return document_service.get_document(doc_id)
    except DocumentNotFoundError as exc:
        _raise_http(exc)


@router.get("/{doc_id}/chunks")
def get_document_chunks(doc_id: str, user: dict = Depends(require_admin)):
    try:
        return document_service.get_document_chunks(doc_id)
    except DocumentNotFoundError as exc:
        _raise_http(exc)


@router.patch("/{doc_id}")
def update_document(
    doc_id: str,
    request: DocumentUpdateRequest,
    user: dict = Depends(require_admin),
):
    updates = request.model_dump(exclude_unset=True)
    try:
        return document_service.update_document(doc_id, **updates)
    except (DocumentNotFoundError, DocumentValidationError) as exc:
        _raise_http(exc)


@router.post("/{doc_id}/reingest")
def reingest_document(doc_id: str, user: dict = Depends(require_admin)):
    try:
        return document_service.reingest_document(doc_id)
    except (DocumentNotFoundError, DocumentValidationError, DocumentExtractionError) as exc:
        _raise_http(exc)


@router.delete("/{doc_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_document(doc_id: str, user: dict = Depends(require_admin)):
    try:
        document_service.delete_document(doc_id)
    except DocumentNotFoundError as exc:
        _raise_http(exc)
    return None
