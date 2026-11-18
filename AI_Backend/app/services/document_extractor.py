from pathlib import Path

SUPPORTED_TEXT_EXTENSIONS = {".txt", ".md", ".markdown"}
SUPPORTED_DOCUMENT_EXTENSIONS = SUPPORTED_TEXT_EXTENSIONS | {".docx", ".pdf"}


class DocumentExtractionError(Exception):
    pass


def _decode_text(data: bytes) -> str:
    for encoding in ("utf-8-sig", "utf-8", "cp1258"):
        try:
            return data.decode(encoding)
        except UnicodeDecodeError:
            continue
    return data.decode("utf-8", errors="replace")


def _extract_txt(file_path: Path) -> str:
    return _decode_text(file_path.read_bytes())


def _extract_docx(file_path: Path) -> str:
    try:
        import docx
    except ImportError as exc:
        raise DocumentExtractionError(
            "Thiếu thư viện python-docx để đọc tệp .docx. "
            "Chạy: pip install python-docx"
        ) from exc

    try:
        document = docx.Document(str(file_path))
    except Exception as exc:
        raise DocumentExtractionError(
            f"Không thể mở tệp Word (.docx): {exc}"
        ) from exc

    parts: list[str] = []
    for paragraph in document.paragraphs:
        text = paragraph.text.strip()
        if text:
            parts.append(text)

    for table in document.tables:
        for row in table.rows:
            cells = [cell.text.strip() for cell in row.cells if cell.text.strip()]
            if cells:
                parts.append(" | ".join(cells))

    content = "\n".join(parts).strip()
    if not content:
        raise DocumentExtractionError("Tệp Word không có nội dung văn bản để nạp.")
    return content


def _extract_pdf(file_path: Path) -> str:
    try:
        from pypdf import PdfReader
    except ImportError as exc:
        raise DocumentExtractionError(
            "Thiếu thư viện pypdf để đọc tệp .pdf. Chạy: pip install pypdf"
        ) from exc

    try:
        reader = PdfReader(str(file_path))
        if reader.is_encrypted:
            try:
                decrypted = reader.decrypt("")
            except Exception as exc:
                raise DocumentExtractionError(
                    "Tệp PDF được bảo vệ bằng mật khẩu, không thể đọc."
                ) from exc
            if not decrypted:
                raise DocumentExtractionError(
                    "Tệp PDF được bảo vệ bằng mật khẩu, không thể đọc."
                )

        parts: list[str] = []
        for page in reader.pages:
            text = (page.extract_text() or "").strip()
            if text:
                parts.append(text)
    except DocumentExtractionError:
        raise
    except Exception as exc:
        raise DocumentExtractionError(
            f"Không thể đọc tệp PDF: {exc}"
        ) from exc

    content = "\n\n".join(parts).strip()
    if not content:
        raise DocumentExtractionError(
            "Tệp PDF không có lớp văn bản (có thể là bản scan). "
            "Cần OCR trước khi nạp vào knowledge base."
        )
    return content


def extract_text(file_path: Path) -> str:
    suffix = file_path.suffix.lower()
    if suffix in SUPPORTED_TEXT_EXTENSIONS:
        return _extract_txt(file_path)
    if suffix == ".docx":
        return _extract_docx(file_path)
    if suffix == ".pdf":
        return _extract_pdf(file_path)
    raise DocumentExtractionError(
        f"Định dạng '{suffix}' không được hỗ trợ. "
        f"Chỉ hỗ trợ: {', '.join(sorted(SUPPORTED_DOCUMENT_EXTENSIONS))}."
    )
