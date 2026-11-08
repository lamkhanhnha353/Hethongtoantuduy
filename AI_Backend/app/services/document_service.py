import hashlib
import re
import unicodedata
import uuid
from datetime import datetime
from pathlib import Path
from typing import Any

from sqlalchemy import func, or_

from app.config import settings
from app.db.database import SessionLocal
from app.db.models import KnowledgeDocument
from app.services.document_extractor import (
    SUPPORTED_DOCUMENT_EXTENSIONS,
    DocumentExtractionError,
    extract_text,
)
from app.services.rag_service import rag_service

MAX_PAGE_SIZE = 100

STATUS_PENDING = "pending"
STATUS_PROCESSING = "processing"
STATUS_READY = "ready"
STATUS_FAILED = "failed"

DOC_ID_PATTERN = re.compile(r"^[A-Za-z0-9._-]{1,200}$")

_UNSET = object()


class DocumentNotFoundError(Exception):
    pass


class DocumentValidationError(Exception):
    pass


def _now() -> datetime:
    return datetime.utcnow()


def _iso(value: datetime | None) -> str | None:
    return value.isoformat() if value else None


def _clean_optional(value: str | None) -> str | None:
    if value is None:
        return None
    cleaned = value.strip()
    return cleaned or None


def _normalize_doc_id(doc_id: str | None) -> str:
    cleaned = (doc_id or "").strip()
    if not cleaned:
        raise DocumentValidationError("doc_id không được để trống")
    if not DOC_ID_PATTERN.fullmatch(cleaned):
        raise DocumentValidationError(
            "doc_id chỉ được chứa chữ cái, chữ số, dấu chấm, gạch dưới, gạch ngang "
            "và tối đa 200 ký tự"
        )
    return cleaned


def _normalize_title(title: str | None, fallback: str | None = None) -> str:
    cleaned = (title or "").strip()
    if not cleaned:
        cleaned = (fallback or "").strip()
    if not cleaned:
        raise DocumentValidationError("title không được để trống")
    return cleaned[:500]


def _slugify(value: str) -> str:
    normalized = unicodedata.normalize("NFKD", value)
    ascii_value = normalized.encode("ascii", "ignore").decode("ascii")
    slug = re.sub(r"[^A-Za-z0-9._-]+", "-", ascii_value).strip("-._").lower()
    return slug or "document"


def _generate_doc_id(filename: str) -> str:
    stem = _slugify(Path(filename).stem)
    return f"{stem}-{uuid.uuid4().hex[:8]}"


def _hash_content(content: str) -> str:
    return hashlib.sha256(content.encode("utf-8")).hexdigest()


def _serialize_document(document: KnowledgeDocument) -> dict[str, Any]:
    return {
        "docId": document.doc_id,
        "title": document.title,
        "description": document.description,
        "category": document.category,
        "sourceType": document.source_type,
        "sourceUrl": document.source_url,
        "fileName": Path(document.file_path).name if document.file_path else None,
        "contentHash": document.content_hash,
        "status": document.status,
        "chunkCount": document.chunk_count,
        "embeddingModel": document.embedding_model,
        "errorMessage": document.error_message,
        "createdBy": document.created_by,
        "createdAt": _iso(document.created_at),
        "updatedAt": _iso(document.updated_at),
    }


def _get_or_raise(db, doc_id: str) -> KnowledgeDocument:
    document = (
        db.query(KnowledgeDocument)
        .filter(KnowledgeDocument.doc_id == doc_id)
        .first()
    )
    if document is None:
        raise DocumentNotFoundError(f"Không tìm thấy tài liệu '{doc_id}'")
    return document


def _mark_ready(doc_id: str, chunk_count: int) -> None:
    db = SessionLocal()
    try:
        document = _get_or_raise(db, doc_id)
        document.status = STATUS_READY
        document.chunk_count = chunk_count
        document.error_message = None
        document.updated_at = _now()
        db.commit()
    finally:
        db.close()


def _mark_failed(doc_id: str, error: Exception) -> None:
    try:
        chunk_count = rag_service.count_document_chunks(doc_id)
    except Exception:
        chunk_count = 0

    db = SessionLocal()
    try:
        document = _get_or_raise(db, doc_id)
        document.status = STATUS_FAILED
        document.chunk_count = chunk_count
        document.error_message = str(error)[:2000]
        document.updated_at = _now()
        db.commit()
    finally:
        db.close()


def _start_ingest(
    *,
    doc_id: str,
    title: str,
    content: str,
    description: str | None,
    category: str | None,
    source_type: str,
    source_url: str | None,
    file_path: str | None,
    created_by: str | None,
) -> dict[str, Any]:
    db = SessionLocal()
    try:
        document = (
            db.query(KnowledgeDocument)
            .filter(KnowledgeDocument.doc_id == doc_id)
            .first()
        )
        if document is None:
            document = KnowledgeDocument(
                doc_id=doc_id,
                created_by=created_by,
                created_at=_now(),
            )
            db.add(document)

        document.title = title
        document.description = description
        document.category = category
        document.source_type = source_type
        document.source_url = source_url
        document.file_path = file_path
        document.raw_content = content
        document.content_hash = _hash_content(content)
        document.status = STATUS_PROCESSING
        document.chunk_count = 0
        document.error_message = None
        document.embedding_model = settings.EMBEDDING_MODEL
        document.updated_at = _now()
        db.commit()
    finally:
        db.close()

    try:
        result = rag_service.ingest_text_document(
            doc_id=doc_id,
            title=title,
            content=content,
        )
    except ValueError as exc:
        _mark_failed(doc_id, exc)
        raise DocumentValidationError(str(exc)) from exc
    except Exception as exc:
        _mark_failed(doc_id, exc)
        raise

    _mark_ready(doc_id, int(result.get("total_chunks_ingested", 0)))
    return get_document(doc_id)


def upsert_document_from_text(
    *,
    doc_id: str,
    title: str,
    content: str,
    description: str | None = None,
    category: str | None = None,
    created_by: str | None = None,
) -> dict[str, Any]:
    normalized_doc_id = _normalize_doc_id(doc_id)
    normalized_title = _normalize_title(title, fallback=normalized_doc_id)
    normalized_content = (content or "").strip()
    if not normalized_content:
        raise DocumentValidationError("Nội dung tài liệu không được để trống")

    return _start_ingest(
        doc_id=normalized_doc_id,
        title=normalized_title,
        content=normalized_content,
        description=_clean_optional(description),
        category=_clean_optional(category),
        source_type="text",
        source_url=None,
        file_path=None,
        created_by=created_by,
    )


def upload_document(
    *,
    filename: str,
    data: bytes,
    doc_id: str | None = None,
    title: str | None = None,
    description: str | None = None,
    category: str | None = None,
    created_by: str | None = None,
) -> dict[str, Any]:
    if not filename:
        raise DocumentValidationError("Tên tệp không được để trống")
    if not data:
        raise DocumentValidationError("Tệp tải lên rỗng")

    suffix = Path(filename).suffix.lower()
    if suffix not in SUPPORTED_DOCUMENT_EXTENSIONS:
        raise DocumentValidationError(
            f"Định dạng '{suffix}' không được hỗ trợ. "
            f"Chỉ hỗ trợ: {', '.join(sorted(SUPPORTED_DOCUMENT_EXTENSIONS))}."
        )

    max_bytes = settings.MAX_DOCUMENT_FILE_SIZE_MB * 1024 * 1024
    if len(data) > max_bytes:
        raise DocumentValidationError(
            f"Tệp vượt quá giới hạn {settings.MAX_DOCUMENT_FILE_SIZE_MB}MB"
        )

    normalized_doc_id = (
        _normalize_doc_id(doc_id) if doc_id else _generate_doc_id(filename)
    )
    target_dir = Path(settings.DOCUMENTS_DIR) / normalized_doc_id
    target_dir.mkdir(parents=True, exist_ok=True)
    target_path = target_dir / Path(filename).name

    db = SessionLocal()
    try:
        existing = (
            db.query(KnowledgeDocument)
            .filter(KnowledgeDocument.doc_id == normalized_doc_id)
            .first()
        )
        previous_file_path = existing.file_path if existing else None
    finally:
        db.close()

    target_path.write_bytes(data)

    try:
        content = extract_text(target_path)
    except DocumentExtractionError:
        target_path.unlink(missing_ok=True)
        raise

    result = _start_ingest(
        doc_id=normalized_doc_id,
        title=_normalize_title(title, fallback=Path(filename).stem),
        content=content,
        description=_clean_optional(description),
        category=_clean_optional(category),
        source_type="file",
        source_url=None,
        file_path=str(target_path),
        created_by=created_by,
    )

    if previous_file_path and previous_file_path != str(target_path):
        try:
            Path(previous_file_path).unlink(missing_ok=True)
        except OSError:
            pass

    return result


def list_documents(
    *,
    page: int = 1,
    page_size: int = 20,
    search: str | None = None,
    status: str | None = None,
    category: str | None = None,
) -> dict[str, Any]:
    page = max(1, page)
    page_size = max(1, min(page_size, MAX_PAGE_SIZE))

    db = SessionLocal()
    try:
        query = db.query(KnowledgeDocument)

        cleaned_search = (search or "").strip()
        if cleaned_search:
            pattern = f"%{cleaned_search}%"
            query = query.filter(
                or_(
                    KnowledgeDocument.doc_id.ilike(pattern),
                    KnowledgeDocument.title.ilike(pattern),
                )
            )

        cleaned_status = (status or "").strip()
        if cleaned_status:
            query = query.filter(KnowledgeDocument.status == cleaned_status)

        cleaned_category = (category or "").strip()
        if cleaned_category:
            query = query.filter(KnowledgeDocument.category == cleaned_category)

        total = query.count()
        documents = (
            query.order_by(
                KnowledgeDocument.updated_at.desc(),
                KnowledgeDocument.id.desc(),
            )
            .offset((page - 1) * page_size)
            .limit(page_size)
            .all()
        )

        return {
            "items": [_serialize_document(document) for document in documents],
            "page": page,
            "pageSize": page_size,
            "total": total,
            "hasMore": page * page_size < total,
        }
    finally:
        db.close()


def get_document(doc_id: str) -> dict[str, Any]:
    db = SessionLocal()
    try:
        document = _get_or_raise(db, doc_id)
        return _serialize_document(document)
    finally:
        db.close()


def get_document_chunks(doc_id: str) -> dict[str, Any]:
    db = SessionLocal()
    try:
        _get_or_raise(db, doc_id)
    finally:
        db.close()

    return {
        "docId": doc_id,
        "items": rag_service.get_document_chunks(doc_id),
    }


def update_document(
    doc_id: str,
    *,
    title: Any = _UNSET,
    description: Any = _UNSET,
    category: Any = _UNSET,
) -> dict[str, Any]:
    db = SessionLocal()
    try:
        document = _get_or_raise(db, doc_id)
        new_title = (
            _normalize_title(title) if title is not _UNSET else document.title
        )

        if new_title != document.title:
            rag_service.update_document_metadata(document.doc_id, new_title)

        document.title = new_title
        if description is not _UNSET:
            document.description = _clean_optional(description)
        if category is not _UNSET:
            document.category = _clean_optional(category)
        document.updated_at = _now()
        db.commit()
        db.refresh(document)
        return _serialize_document(document)
    finally:
        db.close()


def reingest_document(doc_id: str) -> dict[str, Any]:
    db = SessionLocal()
    try:
        document = _get_or_raise(db, doc_id)
        content = document.raw_content
        if not content and document.file_path:
            path = Path(document.file_path)
            if not path.exists():
                raise DocumentValidationError(
                    "Tệp gốc không còn tồn tại, không thể nạp lại tài liệu"
                )
            content = extract_text(path)

        if not content or not content.strip():
            raise DocumentValidationError(
                "Tài liệu không có nội dung nguồn để nạp lại. "
                "Hãy upload lại tệp hoặc tạo lại từ văn bản."
            )

        fields = {
            "doc_id": document.doc_id,
            "title": document.title,
            "content": content,
            "description": document.description,
            "category": document.category,
            "source_type": document.source_type,
            "source_url": document.source_url,
            "file_path": document.file_path,
            "created_by": document.created_by,
        }
    finally:
        db.close()

    return _start_ingest(**fields)


def delete_document(doc_id: str, *, delete_file: bool = True) -> None:
    db = SessionLocal()
    try:
        document = _get_or_raise(db, doc_id)
        file_path = document.file_path

        rag_service.delete_document(doc_id)

        db.delete(document)
        db.commit()
    finally:
        db.close()

    if delete_file and file_path:
        path = Path(file_path)
        try:
            path.unlink(missing_ok=True)
            parent = path.parent
            if parent.is_dir() and not any(parent.iterdir()):
                parent.rmdir()
        except OSError:
            pass


def backfill_documents_from_chroma(
    *,
    created_by: str = "system",
) -> dict[str, int]:
    entries = rag_service.list_document_metadata()

    db = SessionLocal()
    created = 0
    try:
        existing_ids = {
            row[0] for row in db.query(KnowledgeDocument.doc_id).all()
        }
        now = _now()

        for entry in entries:
            doc_id = entry["doc_id"]
            if doc_id in existing_ids:
                continue

            title = (entry.get("title") or doc_id).strip() or doc_id
            document = KnowledgeDocument(
                doc_id=doc_id,
                title=title[:500],
                source_type="chroma",
                status=STATUS_READY,
                chunk_count=int(entry.get("chunk_count", 0)),
                embedding_model=settings.EMBEDDING_MODEL,
                created_by=created_by,
                created_at=now,
                updated_at=now,
            )
            db.add(document)
            created += 1

        db.commit()
        return {"scanned": len(entries), "created": created}
    finally:
        db.close()


def get_document_stats() -> dict[str, Any]:
    db = SessionLocal()
    try:
        total = db.query(func.count(KnowledgeDocument.id)).scalar() or 0
        total_chunks = (
            db.query(func.coalesce(func.sum(KnowledgeDocument.chunk_count), 0))
            .scalar()
            or 0
        )
        rows = (
            db.query(KnowledgeDocument.status, func.count(KnowledgeDocument.id))
            .group_by(KnowledgeDocument.status)
            .all()
        )
        return {
            "totalDocuments": int(total),
            "totalChunks": int(total_chunks),
            "byStatus": {status: int(count) for status, count in rows},
        }
    finally:
        db.close()
