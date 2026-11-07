from pathlib import Path
from app.extractors.base import BaseExtractor, ExtractionException
from app.extractors.docx_extractor import DocxExtractor
from app.extractors.pdf_extractor import PdfExtractor


class ExtractorFactory:
    """Factory xác định bộ trích xuất tương ứng với định dạng tệp."""

    @staticmethod
    def get_extractor(file_path: Path) -> BaseExtractor:
        suffix = file_path.suffix.lower()
        if suffix == ".docx":
            return DocxExtractor()
        elif suffix == ".pdf":
            return PdfExtractor()
        elif suffix == ".doc":
            raise ExtractionException(
                "Định dạng tệp .doc (Word 97-2003) nhị phân cũ không được hỗ trợ trực tiếp. "
                "Vui lòng lưu lại dưới dạng .docx hoặc .pdf trước khi tải lên."
            )
        else:
            raise ExtractionException(f"Định dạng tệp '{suffix}' không được hỗ trợ. Chỉ hỗ trợ .docx và .pdf.")
