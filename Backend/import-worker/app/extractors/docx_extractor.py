import os
import uuid
from pathlib import Path
from typing import List, Tuple, Optional
import docx
from docx.document import Document
from docx.oxml.ns import qn, nsmap
from docx.text.paragraph import Paragraph
from docx.table import Table

# Đăng ký namespace bổ sung cho VML / DrawingML nếu thiếu
if 'v' not in nsmap:
    nsmap['v'] = 'urn:schemas-microsoft-com:vml'
if 'a' not in nsmap:
    nsmap['a'] = 'http://schemas.openxmlformats.org/drawingml/2006/main'

from app.config import settings
from app.extractors.base import (
    BaseExtractor,
    ExtractedDocument,
    ExtractedBlock,
    ExtractionException,
)


class DocxExtractor(BaseExtractor):
    """
    Trích xuất nội dung văn bản và hình ảnh nhúng từ tệp Microsoft Word (.docx).
    Bảo toàn thứ tự xuất hiện của hình ảnh và đoạn văn để phục vụ chunking chính xác.
    """

    def extract(self, file_path: Path, job_id: str) -> ExtractedDocument:
        if not file_path.exists():
            raise ExtractionException(f"Tệp không tồn tại: {file_path}")

        try:
            doc = docx.Document(str(file_path))
        except Exception as e:
            raise ExtractionException(f"Không thể mở tệp Word (.docx). Định dạng có thể bị hỏng hoặc mã hóa: {str(e)}")

        blocks: List[ExtractedBlock] = []
        extracted_image_paths: List[str] = []
        order_counter = 0
        img_counter = 0

        # Duyệt qua các phần tử con trong body để đảm bảo đúng thứ tự văn bản & bảng
        body_elements = doc.element.body

        for child in body_elements:
            if child.tag.endswith('p'):
                # Đoạn văn bản
                para = Paragraph(child, doc)
                para_blocks, img_counter, order_counter = self._process_paragraph(
                    para, doc, job_id, img_counter, order_counter
                )
                for b in para_blocks:
                    blocks.append(b)
                    if b.block_type == "image":
                        extracted_image_paths.append(b.content)

            elif child.tag.endswith('tbl'):
                # Bảng biểu
                table = Table(child, doc)
                for row_idx, row in enumerate(table.rows):
                    row_texts = []
                    for cell in row.cells:
                        for p in cell.paragraphs:
                            cell_blocks, img_counter, order_counter = self._process_paragraph(
                                p, doc, job_id, img_counter, order_counter
                            )
                            for b in cell_blocks:
                                blocks.append(b)
                                if b.block_type == "image":
                                    extracted_image_paths.append(b.content)
                                elif b.block_type == "text" and b.content.strip():
                                    row_texts.append(b.content.strip())
                    if row_texts:
                        # Ghi nhận một dòng text dạng bảng
                        order_counter += 1
                        table_row_str = " | ".join(row_texts)
                        blocks.append(
                            ExtractedBlock(
                                block_type="text",
                                content=f"[Bảng] {table_row_str}",
                                order=order_counter,
                                page_or_index=row_idx,
                            )
                        )

        # Kiểm tra nội dung bóc tách
        total_text_length = sum(len(b.content) for b in blocks if b.block_type == "text")
        if total_text_length == 0 and not extracted_image_paths:
            raise ExtractionException("Tệp Word rỗng hoặc không chứa nội dung văn bản/hình ảnh có thể đọc.")

        return ExtractedDocument(
            file_path=file_path,
            file_type="docx",
            blocks=blocks,
            images=extracted_image_paths,
            metadata={
                "total_blocks": len(blocks),
                "total_images": len(extracted_image_paths),
                "total_text_length": total_text_length,
            }
        )

    def _process_paragraph(
        self,
        para: Paragraph,
        doc: Document,
        job_id: str,
        img_counter: int,
        order_counter: int
    ) -> Tuple[List[ExtractedBlock], int, int]:
        """Xử lý từng paragraph, bóc tách text và ảnh nhúng trong runs."""
        results: List[ExtractedBlock] = []
        para_text = para.text.strip()

        # Kiểm tra ảnh nhúng trong paragraph
        # 1. OpenXML DrawingML (a:blip/@r:embed)
        blip_rels = para._element.xpath('.//a:blip/@r:embed')
        # 2. VML shapes (v:imagedata/@r:id)
        vml_rels = para._element.xpath('.//v:imagedata/@r:id')

        all_rids = blip_rels + vml_rels

        # Nếu có text, ghi nhận block text
        if para_text:
            order_counter += 1
            results.append(
                ExtractedBlock(
                    block_type="text",
                    content=para_text,
                    order=order_counter,
                )
            )

        # Bóc tách ảnh nhúng
        for r_id in all_rids:
            try:
                rel = doc.part.rels.get(r_id)
                if not rel or not hasattr(rel, "target_part"):
                    continue

                image_part = rel.target_part
                content_type = getattr(image_part, "content_type", "")
                ext = self._get_image_ext(content_type)

                img_counter += 1
                img_filename = f"{job_id}_img_{img_counter}{ext}"
                settings.IMAGE_DIR.mkdir(parents=True, exist_ok=True)
                img_save_path = settings.IMAGE_DIR / img_filename

                # Lưu ảnh ra đĩa
                with open(img_save_path, "wb") as f:
                    f.write(image_part.blob)

                img_url = f"{settings.BASE_URL}/static/images/{img_filename}"
                order_counter += 1
                results.append(
                    ExtractedBlock(
                        block_type="image",
                        content=str(img_save_path),
                        order=order_counter,
                        image_url=img_url,
                        extra={"filename": img_filename, "size_bytes": len(image_part.blob)}
                    )
                )
            except Exception as e:
                # Ghi nhận cảnh báo nếu không lưu được ảnh
                import logging
                logging.getLogger(__name__).warning(f"Không thể trích xuất ảnh rId '{r_id}': {e}")
                continue

        return results, img_counter, order_counter

    def _get_image_ext(self, content_type: str) -> str:
        mapping = {
            "image/jpeg": ".jpg",
            "image/jpg": ".jpg",
            "image/png": ".png",
            "image/gif": ".gif",
            "image/bmp": ".bmp",
            "image/webp": ".webp",
            "image/x-wmf": ".wmf",
            "image/x-emf": ".emf",
        }
        return mapping.get(content_type.lower(), ".png")
