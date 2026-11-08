import os
from pathlib import Path
from typing import List
from app.config import settings
from app.extractors.base import (
    BaseExtractor,
    ExtractedDocument,
    ExtractedBlock,
    ExtractionException,
    ScannedDocumentException,
)


class PdfExtractor(BaseExtractor):
    """
    Trích xuất nội dung văn bản và hình ảnh từ tệp PDF bằng PyMuPDF (fitz)
    với cơ chế phát hiện tài liệu scan không có text layer (Failure branch).
    """

    def extract(self, file_path: Path, job_id: str) -> ExtractedDocument:
        if not file_path.exists():
            raise ExtractionException(f"Tệp không tồn tại: {file_path}")

        try:
            import fitz  # PyMuPDF
        except ImportError:
            raise ExtractionException("Thư viện PyMuPDF (fitz) chưa được cài đặt. Vui lòng chạy: pip install pymupdf")

        try:
            doc = fitz.open(str(file_path))
        except Exception as e:
            raise ExtractionException(f"Không thể mở tệp PDF. Tệp có thể bị hỏng hoặc có mật khẩu bảo vệ: {str(e)}")

        if len(doc) == 0:
            doc.close()
            raise ExtractionException("Tệp PDF không có trang nào.")

        blocks: List[ExtractedBlock] = []
        extracted_image_paths: List[str] = []
        order_counter = 0
        img_counter = 0
        total_text_chars = 0

        total_pages = len(doc)
        try:
            for page_index in range(total_pages):
                page = doc[page_index]

                # 1. Trích xuất text blocks từ trang
                text_page = page.get_text("blocks")
                # text_page trả về list các tuple: (x0, y0, x1, y1, "text", block_no, block_type)
                # block_type = 0 là text, 1 là image
                for b in text_page:
                    if b[6] == 0:  # text block
                        text_content = b[4].strip()
                        if text_content:
                            order_counter += 1
                            total_text_chars += len(text_content)
                            blocks.append(
                                ExtractedBlock(
                                    block_type="text",
                                    content=text_content,
                                    order=order_counter,
                                    page_or_index=page_index + 1,
                                    extra={"bbox": b[:4]}
                                )
                            )

                # 2. Trích xuất hình ảnh nhúng từ trang
                image_list = page.get_images(full=True)
                for img_info in image_list:
                    xref = img_info[0]
                    base_image = doc.extract_image(xref)
                    image_bytes = base_image.get("image")
                    image_ext = base_image.get("ext", "png")

                    # Lọc bỏ các ảnh quá nhỏ (icon, đường kẻ trang trí < 1000 bytes)
                    if not image_bytes or len(image_bytes) < 1024:
                        continue

                    img_counter += 1
                    img_filename = f"{job_id}_p{page_index + 1}_img_{img_counter}.{image_ext}"
                    settings.IMAGE_DIR.mkdir(parents=True, exist_ok=True)
                    img_save_path = settings.IMAGE_DIR / img_filename

                    with open(img_save_path, "wb") as f:
                        f.write(image_bytes)

                    img_url = f"{settings.BASE_URL}/static/images/{img_filename}"
                    order_counter += 1
                    extracted_image_paths.append(str(img_save_path))

                    blocks.append(
                        ExtractedBlock(
                            block_type="image",
                            content=str(img_save_path),
                            order=order_counter,
                            page_or_index=page_index + 1,
                            image_url=img_url,
                            extra={"width": base_image.get("width"), "height": base_image.get("height")}
                        )
                    )

        finally:
            doc.close()

        # FAILURE BRANCH: Kiểm tra tài liệu scan/ảnh không chứa text layer
        # Nếu trung bình mỗi trang có ít hơn 15 ký tự text, coi là tài liệu scan
        avg_chars_per_page = total_text_chars / max(1, total_pages)
        if total_text_chars < 50 or avg_chars_per_page < 15:
            raise ScannedDocumentException(
                f"Tài liệu PDF là dạng scan/ảnh chụp hoặc vector thuần không có text layer "
                f"(Tổng ký tự đọc được: {total_text_chars} trên {total_pages} trang). "
                f"Cần chuyển qua pipeline OCR trước khi trích xuất."
            )

        return ExtractedDocument(
            file_path=file_path,
            file_type="pdf",
            blocks=blocks,
            images=extracted_image_paths,
            metadata={
                "total_pages": total_pages,
                "total_blocks": len(blocks),
                "total_images": len(extracted_image_paths),
                "total_text_chars": total_text_chars,
            }
        )
