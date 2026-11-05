import re
from dataclasses import dataclass, field
from typing import List, Optional, Dict, Tuple
from app.extractors.base import ExtractedDocument, ExtractedBlock


@dataclass
class QuestionChunk:
    """Đại diện cho một khối câu hỏi thô kèm các hình ảnh và metadata tương ứng."""
    chunk_index: int
    label: str
    raw_text: str
    image_urls: List[str] = field(default_factory=list)
    image_paths: List[str] = field(default_factory=list)
    answer_hint: Optional[str] = None
    section_title: Optional[str] = None
    topic_title: Optional[str] = None

    @property
    def primary_image_url(self) -> Optional[str]:
        return self.image_urls[0] if self.image_urls else None


class QuestionChunker:
    """
    Phân đoạn văn bản thô từ tài liệu đề thi Toán tư duy thành các khối câu hỏi độc lập.
    Hỗ trợ:
    1. Bóc tách và ánh xạ Bảng Đáp Án (Answer Key) ở cuối tài liệu vào từng câu hỏi.
    2. Nhận diện ranh giới câu hỏi (Bài X, Câu X, hoặc phép tính Worksheet).
    3. Lọc bỏ Header, Footer, Bìa, Thông tin học sinh (Tên, Lớp, Ngày).
    4. Nhận diện thẻ bài tập dạng hình ảnh (Image Cards) và gắn thẻ hình ảnh chính xác.
    """

    QUESTION_SPLIT_REGEX = re.compile(
        r'(?=(?:^|\n)\s*(?:Bài|Câu|Question|Problem)\s*(\d+)[\.:\)\s-])',
        re.IGNORECASE
    )

    QUESTION_HEADER_PATTERN = re.compile(
        r'^\s*(?:Bài|Câu|Question|Problem)\s*(\d+)[\.:\)\s-]',
        re.IGNORECASE
    )

    # Nhận diện phép tính dạng Worksheet mầm non (PDF 1): ví dụ "2 - 1 = ?", "2 + 3 ="
    WORKSHEET_MATH_REGEX = re.compile(
        r'(?:^|\n)\s*(\d+\s*[-+x:×÷]\s*\d+\s*=\s*(?:\?|__|\.\.\.|\b))',
        re.IGNORECASE
    )

    # Header / Metadata hành chính cần lọc bỏ
    NOISE_PATTERNS = [
        re.compile(r'^\s*ILA\s*•\s*Toán tư duy.*', re.IGNORECASE),
        re.compile(r'^\s*TOÁN TƯ DUY LỚP\s*\d+.*', re.IGNORECASE),
        re.compile(r'^\s*\d+\s+BÀI TẬP CÓ ĐÁP ÁN.*', re.IGNORECASE),
        re.compile(r'^\s*(?:Họ và tên|Tên|Lớp|Ngày)[\s.:_/-]+.*', re.IGNORECASE),
        re.compile(r'^\s*Từ cơ bản đến nâng cao.*', re.IGNORECASE),
        re.compile(r'^\s*Trang\s*\d+(?:/\d+)?\s*$', re.IGNORECASE),
        re.compile(r'^\s*-\s*\d+\s*-\s*$', re.IGNORECASE),
    ]

    SECTION_PATTERN = re.compile(r'^\s*(PHẦN\s*\d+[\.:\s]+[^\n]+)', re.IGNORECASE)
    TOPIC_PATTERN = re.compile(r'^\s*(Dạng\s*\d+[\.:\s]+[^\n]+)', re.IGNORECASE)

    def chunk(self, doc: ExtractedDocument) -> List[QuestionChunk]:
        """
        Xử lý toàn bộ tài liệu, trích xuất bảng đáp án nếu có,
        phân tách các câu hỏi và gắn hình ảnh cùng answer hint.
        """
        # Bước 1: Trích xuất Bảng đáp án ở cuối tài liệu (nếu có)
        answer_key_dict, answer_page_indexes = self._extract_answer_keys(doc)

        # Bước 2: Lọc các blocks nội dung (loại bỏ trang bảng đáp án)
        content_blocks = [
            b for b in doc.blocks
            if b.page_or_index not in answer_page_indexes
        ]

        # Bước 3: Phân đoạn các khối câu hỏi theo dòng văn bản và hình ảnh
        raw_chunks = self._segment_document(content_blocks, answer_key_dict)

        # Fallback nếu không tách được câu hỏi nào bằng thuật toán chính
        if not raw_chunks:
            raw_chunks = self._fallback_chunking(doc, answer_key_dict)

        # Chuyển đổi thành QuestionChunk hoàn chỉnh
        result: List[QuestionChunk] = []
        for idx, item in enumerate(raw_chunks, start=1):
            combined_text = item["text"].strip()
            # Bỏ qua nếu không có cả chữ lẫn ảnh
            if not combined_text and not item["image_urls"]:
                continue

            q_num = item.get("question_number")
            answer_hint = item.get("answer_hint")
            if not answer_hint and q_num and q_num in answer_key_dict:
                answer_hint = answer_key_dict[q_num]

            result.append(
                QuestionChunk(
                    chunk_index=idx,
                    label=item.get("label", f"Câu {idx}"),
                    raw_text=combined_text,
                    image_urls=item.get("image_urls", []),
                    image_paths=item.get("image_paths", []),
                    answer_hint=answer_hint,
                    section_title=item.get("section_title"),
                    topic_title=item.get("topic_title"),
                )
            )

        return result

    def _extract_answer_keys(self, doc: ExtractedDocument) -> Tuple[Dict[int, str], List[int]]:
        """
        Phát hiện và trích xuất Bảng Đáp Án tập trung (thường ở trang cuối cùng).
        Trả về dictionary {số_bài: đáp_án} và danh sách trang chứa bảng đáp án.
        """
        answer_dict: Dict[int, str] = {}
        answer_pages: List[int] = []

        total_pages = doc.metadata.get("total_pages", 1)

        for block in doc.blocks:
            if block.block_type != "text":
                continue

            text = block.content.strip()
            matches_count = len(re.findall(r'Bài\s*\d+', text, re.IGNORECASE))
            is_answer_block = (
                matches_count >= 5 and (
                    "ĐÁP ÁN" in text.upper() or
                    "BẢNG" in text.upper() or
                    block.page_or_index >= max(1, total_pages - 1)
                )
            )

            if is_answer_block:
                answer_pages.append(block.page_or_index)

                matches = re.findall(
                    r'Bài\s*(\d+)[\s.:-]*\n?\s*([^\n]+(?:\n(?!(?:Bài\s*\d+|\Z))[^\n]+)*)',
                    text,
                    re.IGNORECASE
                )
                for num_str, ans_content in matches:
                    try:
                        q_num = int(num_str)
                        cleaned_ans = ans_content.strip().replace("\n", " ")
                        cleaned_ans = re.sub(r'\s+', ' ', cleaned_ans)
                        if cleaned_ans:
                            answer_dict[q_num] = cleaned_ans
                    except ValueError:
                        continue

        return answer_dict, list(set(answer_pages))

    def _segment_document(
        self,
        blocks: List[ExtractedBlock],
        answer_key_dict: Dict[int, str]
    ) -> List[dict]:
        """
        Duyệt và phân đoạn các khối nội dung câu hỏi kết hợp Text và Image Cards.
        """
        chunks: List[dict] = []
        current_section = ""
        current_topic = ""
        last_question_number = 0

        pages_dict: Dict[int, List[ExtractedBlock]] = {}
        for b in blocks:
            pages_dict.setdefault(b.page_or_index, []).append(b)

        for page_num in sorted(pages_dict.keys()):
            page_blocks = pages_dict[page_num]

            # Kiểm tra nếu là trang bìa thông tin chung
            all_page_text = " ".join([b.content for b in page_blocks if b.block_type == "text"])
            if page_num == 1 and ("HỌ VÀ TÊN" in all_page_text.upper() or "BÀI TẬP CÓ ĐÁP ÁN" in all_page_text.upper()):
                continue

            # Lọc các ảnh kích thước bài tập (loại bỏ logo nhỏ < 250px)
            page_large_images = [
                b for b in page_blocks
                if b.block_type == "image" and (b.extra and b.extra.get("width", 0) >= 250)
            ]
            page_all_images = [b for b in page_blocks if b.block_type == "image"]
            page_text_blocks = [b for b in page_blocks if b.block_type == "text"]

            # Ghép văn bản của trang và theo dõi Section / Topic
            page_lines = []
            for tb in page_text_blocks:
                for line in tb.content.split("\n"):
                    trimmed = line.strip()
                    if not trimmed:
                        continue

                    if any(p.match(trimmed) for p in self.NOISE_PATTERNS):
                        continue

                    sec_match = self.SECTION_PATTERN.match(trimmed)
                    if sec_match:
                        current_section = sec_match.group(1).strip()
                        continue

                    topic_match = self.TOPIC_PATTERN.match(trimmed)
                    if topic_match:
                        current_topic = topic_match.group(1).strip()
                        continue

                    page_lines.append(trimmed)

            cleaned_page_text = "\n".join(page_lines)
            question_matches = list(self.QUESTION_SPLIT_REGEX.finditer(cleaned_page_text))

            # ─── XỬ LÝ ẢNH BÀI TẬP (IMAGE CARDS) TRÊN TRANG ───
            # Nếu trang có các ảnh bài tập lớn (ví dụ Bài 10, 11 trên trang 3; Bài 13, 14 trên trang 4...)
            # Ta tạo chunk cho các ảnh bài tập này trước
            if page_large_images:
                for img_idx, img in enumerate(page_large_images):
                    last_question_number += 1
                    predicted_q_num = last_question_number
                    ans_hint = answer_key_dict.get(predicted_q_num)

                    card_text = (
                        f"Bài {predicted_q_num}. (Bài toán tư duy hình ảnh tại Trang {page_num})\n"
                        f"Phần: {current_section}\n"
                        f"Dạng: {current_topic}"
                    )

                    chunks.append({
                        "label": f"Bài {predicted_q_num}",
                        "question_number": predicted_q_num,
                        "text": card_text,
                        "image_urls": [img.image_url] if img.image_url else [],
                        "image_paths": [img.content],
                        "answer_hint": ans_hint,
                        "section_title": current_section,
                        "topic_title": current_topic,
                    })

            # ─── XỬ LÝ CÂU HỎI VĂN BẢN (TEXT QUESTIONS) TRÊN TRANG ───
            if question_matches:
                for i, match in enumerate(question_matches):
                    start_pos = match.start()
                    end_pos = question_matches[i + 1].start() if i + 1 < len(question_matches) else len(cleaned_page_text)
                    question_raw_text = cleaned_page_text[start_pos:end_pos].strip()

                    q_num = None
                    hdr_match = self.QUESTION_HEADER_PATTERN.match(question_raw_text)
                    if hdr_match:
                        try:
                            q_num = int(hdr_match.group(1))
                            last_question_number = q_num
                        except ValueError:
                            pass

                    # Nếu trang không có large card images riêng thì gán ảnh nhỏ phụ (nếu có)
                    matching_imgs = []
                    matching_paths = []
                    if not page_large_images and page_all_images:
                        for img in page_all_images:
                            if img.image_url:
                                matching_imgs.append(img.image_url)
                            matching_paths.append(img.content)

                    chunks.append({
                        "label": f"Bài {q_num}" if q_num else f"Câu {len(chunks) + 1}",
                        "question_number": q_num,
                        "text": question_raw_text,
                        "image_urls": matching_imgs if (len(question_matches) == 1 or i == 0) else [],
                        "image_paths": matching_paths if (len(question_matches) == 1 or i == 0) else [],
                        "answer_hint": answer_key_dict.get(q_num) if q_num else None,
                        "section_title": current_section,
                        "topic_title": current_topic,
                    })

            # ─── XỬ LÝ DẠNG WORKSHEET MẦM NON (PDF 1) ───
            # Khi trang không có cả Bài/Câu lẫn large image card nhưng có phép tính
            if not question_matches and not page_large_images:
                math_matches = list(self.WORKSHEET_MATH_REGEX.finditer(cleaned_page_text))
                if math_matches:
                    for m_idx, m_match in enumerate(math_matches):
                        last_question_number += 1
                        math_expr = m_match.group(1).strip()
                        chunks.append({
                            "label": f"Bài {last_question_number}",
                            "question_number": last_question_number,
                            "text": f"Thực hiện phép tính: {math_expr}",
                            "image_urls": [img.image_url for img in page_all_images if img.image_url],
                            "image_paths": [img.content for img in page_all_images],
                            "answer_hint": None,
                            "section_title": current_section,
                            "topic_title": current_topic,
                        })

        return chunks

    def _fallback_chunking(
        self,
        doc: ExtractedDocument,
        answer_key_dict: Dict[int, str]
    ) -> List[dict]:
        """Chia đoạn dự phòng khi không nhận diện được mẫu câu hỏi rõ ràng."""
        text = doc.full_text
        split_pattern = re.compile(r'(?=(?:Câu|Bài|Question)\s*\d+[:.])', re.IGNORECASE)
        segments = split_pattern.split(text)

        chunks = []
        for i, seg in enumerate(segments):
            seg_text = seg.strip()
            if not seg_text:
                continue

            all_imgs = [b.image_url for b in doc.blocks if b.block_type == "image" and b.image_url]
            all_paths = [b.content for b in doc.blocks if b.block_type == "image"]

            chunks.append({
                "label": f"Câu {i + 1}",
                "question_number": i + 1,
                "text": seg_text,
                "image_urls": all_imgs if i == 0 else [],
                "image_paths": all_paths if i == 0 else [],
                "answer_hint": answer_key_dict.get(i + 1),
                "section_title": "",
                "topic_title": "",
            })

        return chunks
