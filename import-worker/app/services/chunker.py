import re
from dataclasses import dataclass, field
from typing import List, Optional
from app.extractors.base import ExtractedDocument, ExtractedBlock


@dataclass
class QuestionChunk:
    """Đại diện cho một khối câu hỏi thô kèm các hình ảnh tương ứng."""
    chunk_index: int
    label: str
    raw_text: str
    image_urls: List[str] = field(default_factory=list)
    image_paths: List[str] = field(default_factory=list)

    @property
    def primary_image_url(self) -> Optional[str]:
        return self.image_urls[0] if self.image_urls else None


class QuestionChunker:
    """
    Phân đoạn văn bản thô thành các khối câu hỏi dựa trên Regex và
    gắn thẻ (tagging) hình ảnh thuộc về từng câu hỏi tương ứng.
    """

    # Biểu thức chính quy nhận diện tiêu đề câu hỏi/bài tập
    QUESTION_PATTERNS = [
        # Ví dụ: "Câu 1:", "Câu 1.", "Câu 1 -", "Câu 1)", "CÂU 1:"
        re.compile(r'^\s*(?:câu|bài|question|item|problem)\s*(\d+)[\s.:\)\-]', re.IGNORECASE),
        # Ví dụ: "Câu I:", "Bài II."
        re.compile(r'^\s*(?:câu|bài)\s*([IVXLCDM]+)[\s.:\)\-]', re.IGNORECASE),
        # Ví dụ: "1.", "1)", "1:" ở đầu dòng/đoạn
        re.compile(r'^\s*(\d+)[\.\)]\s+', re.IGNORECASE),
    ]

    def chunk(self, doc: ExtractedDocument) -> List[QuestionChunk]:
        """
        Duyệt qua danh sách các block theo thứ tự xuất hiện,
        nhóm text và image vào từng khối câu hỏi.
        """
        raw_chunks: List[dict] = []
        current_chunk = None
        preamble_texts: List[str] = []

        for block in doc.blocks:
            if block.block_type == "text":
                text = block.content.strip()
                if not text:
                    continue

                label, is_new_question = self._detect_question_start(text)

                if is_new_question:
                    # Hoàn tất chunk trước đó nếu có
                    if current_chunk:
                        raw_chunks.append(current_chunk)

                    current_chunk = {
                        "label": label or f"Câu {len(raw_chunks) + 1}",
                        "text_parts": [text],
                        "image_urls": [],
                        "image_paths": [],
                    }
                else:
                    if current_chunk is None:
                        # Đoạn văn bản trước câu hỏi đầu tiên (Tiêu đề đề thi, hướng dẫn chung...)
                        preamble_texts.append(text)
                    else:
                        current_chunk["text_parts"].append(text)

            elif block.block_type == "image":
                # Ảnh thuộc về chunk hiện tại
                if current_chunk is not None:
                    if block.image_url:
                        current_chunk["image_urls"].append(block.image_url)
                    current_chunk["image_paths"].append(block.content)
                else:
                    # Nếu ảnh xuất hiện trước khi nhận diện câu hỏi, lưu tạm
                    # và sẽ chuyển cho câu hỏi đầu tiên xuất hiện
                    pass

        if current_chunk:
            raw_chunks.append(current_chunk)

        # Fallback: Nếu không phân tách được bằng regex (ví dụ chỉ có 1 khối hoặc regex không khớp)
        if not raw_chunks and (preamble_texts or doc.full_text.strip()):
            raw_chunks = self._fallback_chunking(doc)

        # Chuyển đổi thành List[QuestionChunk]
        result: List[QuestionChunk] = []
        for idx, item in enumerate(raw_chunks, start=1):
            combined_text = "\n".join(item["text_parts"]).strip()
            # Bỏ qua các khối rỗng không có chữ lẫn ảnh
            if not combined_text and not item["image_urls"]:
                continue

            result.append(
                QuestionChunk(
                    chunk_index=idx,
                    label=item.get("label", f"Câu {idx}"),
                    raw_text=combined_text,
                    image_urls=item.get("image_urls", []),
                    image_paths=item.get("image_paths", []),
                )
            )

        return result

    def _detect_question_start(self, text: str) -> (Optional[str], bool):
        """Kiểm tra xem dòng/đoạn text có phải là bắt đầu một câu hỏi mới không."""
        # Kiểm tra dòng đầu tiên của text
        first_line = text.split("\n")[0].strip()

        for pattern in self.QUESTION_PATTERNS:
            match = pattern.match(first_line)
            if match:
                return match.group(0).strip(" .:)-"), True

        return None, False

    def _fallback_chunking(self, doc: ExtractedDocument) -> List[dict]:
        """Chia đoạn dự phòng khi không nhận diện được mẫu Regex chuẩn."""
        # Thử tìm các từ khóa 'Câu' hoặc 'Bài' ở giữa văn bản
        text = doc.full_text
        split_pattern = re.compile(r'(?=(?:Câu|Bài|Question)\s*\d+[:.])', re.IGNORECASE)
        segments = split_pattern.split(text)

        chunks = []
        for i, seg in enumerate(segments):
            seg_text = seg.strip()
            if not seg_text:
                continue

            # Gắn toàn bộ ảnh nếu có vào chunk đầu tiên hoặc chia đều
            img_urls = [b.image_url for b in doc.blocks if b.block_type == "image" and b.image_url]
            img_paths = [b.content for b in doc.blocks if b.block_type == "image"]

            chunks.append({
                "label": f"Câu {i + 1}",
                "text_parts": [seg_text],
                "image_urls": img_urls if i == 0 else [],
                "image_paths": img_paths if i == 0 else [],
            })

        if not chunks and text.strip():
            chunks.append({
                "label": "Câu 1",
                "text_parts": [text],
                "image_urls": [b.image_url for b in doc.blocks if b.block_type == "image" and b.image_url],
                "image_paths": [b.content for b in doc.blocks if b.block_type == "image"],
            })

        return chunks
