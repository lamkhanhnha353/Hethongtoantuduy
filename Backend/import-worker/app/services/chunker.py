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
    is_multiple_choice: bool = False

    def __post_init__(self):
        if not self.is_multiple_choice:
            self.is_multiple_choice = QuestionChunker.detect_multiple_choice(
                self.raw_text, self.answer_hint
            )
        if self.is_multiple_choice and self.answer_hint:
            self.answer_hint = QuestionChunker.enrich_mc_answer_hint(
                self.raw_text, self.answer_hint
            )

    @property
    def primary_image_url(self) -> Optional[str]:
        return self.image_urls[0] if self.image_urls else None


class QuestionChunker:
    """
    Phân đoạn văn bản thô từ tài liệu đề thi Toán tư duy thành các khối câu hỏi độc lập.
    Hỗ trợ:
    1. Bóc tách và ánh xạ Bảng Đáp Án / Hướng dẫn giải ở cuối tài liệu vào từng câu hỏi.
    2. Nhận diện ranh giới câu hỏi đa định dạng:
       - Bắt đầu với Câu X, Bài X, Question X, Problem X.
       - Hoặc bắt đầu là một đoạn văn và kết thúc ở cuối các đáp án A, B, C, D.
       - Hoặc phép tính dạng Worksheet.
    3. Tự động phát hiện câu hỏi có nhiều đáp án (A, B, C, D) và gắn cờ trắc nghiệm (is_multiple_choice).
    4. Lọc bỏ Header, Footer, Bìa, Thông tin học sinh (Tên, Lớp, Ngày).
    5. Nhận diện thẻ bài tập dạng hình ảnh (Image Cards) và gắn thẻ hình ảnh chính xác.
    """

    QUESTION_SPLIT_REGEX = re.compile(
        r'(?=(?:^|\n)\s*(?:Bài|Câu|Question|Problem)\s*(\d+)(?:[\.:\)\/\s-]|$))',
        re.IGNORECASE
    )

    QUESTION_HEADER_PATTERN = re.compile(
        r'^\s*(Bài|Câu|Question|Problem)\s*(\d+)(?:[\.:\)\/\s-]|$)',
        re.IGNORECASE
    )

    # Nhận diện dòng phương án lựa chọn A, B, C, D (uppercase để không nhầm với ý a., b. của bài tự luận)
    OPTION_LINE_START_REGEX = re.compile(r'^\s*([A-D])\s*[\.\)\:]\s*\S+')
    INLINE_OPTIONS_REGEX = re.compile(r'(?:^|\n|\s)([A-D])\s*[\.\)\:]\s*\S+')
    INLINE_MULTI_OPTIONS_SPLIT_REGEX = re.compile(
        r'^(.+?[?:.])\s+(?=A\s*[\.\)\:]\s*\S+.*\s+B\s*[\.\)\:]\s*\S+)'
    )

    # Nhận diện dòng đáp án / lời giải đi kèm trực tiếp sau câu hỏi
    INLINE_ANSWER_PATTERN = re.compile(
        r'^\s*(?:Đáp\s*án(?:\s+giáo\s+viên)?|Đáp\s*số|Lời\s*giải|Hướng\s*dẫn(?:\s+giải)?|Giải|Answer|Solution)\s*[:.\-]\s*(?!\.{3,})',
        re.IGNORECASE
    )

    # Nhận diện tiêu đề phần Bảng đáp án / Đáp án và hướng dẫn giải ở cuối tài liệu
    ANSWER_SECTION_HEADER_REGEX = re.compile(
        r'^\s*(?:[-=_*]{3,}\s*)?(?:PHẦN\s+[IVX\d]+[\.:\s-]*)?'
        r'(?:'
        r'BẢNG\s+ĐÁP\s+ÁN.*|'
        r'ĐÁP\s+ÁN\s*(?:VÀ|&|-|–|—|/)\s*(?:HƯỚNG\s+DẪN(?:\s+GIẢI)?|LỜI\s+GIẢI).*|'
        r'HƯỚNG\s+DẪN(?:\s+GIẢI)?\s*(?:VÀ|&|-|–|—|/)\s*ĐÁP\s+ÁN.*|'
        r'ĐÁP\s+ÁN\s+CHI\s+TIẾT.*|'
        r'ĐÁP\s+ÁN\s+THAM\s+KHẢO.*|'
        r'ĐÁP\s+ÁN\s*:?\s*$'
        r')',
        re.IGNORECASE
    )

    DIVIDER_LINE_REGEX = re.compile(r'^\s*[-=_*]{3,}\s*$')

    # Nhận diện answer_hint mang cờ trắc nghiệm (chọn A, B, C hoặc D)
    MC_ANSWER_HINT_REGEX = re.compile(
        r'^\s*(?:Đáp\s*án(?:\s+đúng|\s+giáo\s+viên)?\s*[:.-]?\s*)?([A-D])'
        r'(?:\s*$|[\.\)]\s+|\s*(?:[-–—]|\n)?\s*(?:Giải|Lời\s+giải|Hướng\s+dẫn|Vì)\b)',
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
        re.compile(r'^\s*BỘ CÂU HỎI\s+(?:TRẮC NGHIỆM|TOÁN|ÔN TẬP).*', re.IGNORECASE),
        re.compile(r'^\s*ĐỀ\s+(?:KIỂM TRA|THI|ÔN TẬP|LUYỆN TẬP).*', re.IGNORECASE),
        re.compile(r'^\s*Thời gian làm bài\s*:.*', re.IGNORECASE),
        re.compile(r'^\s*Khoanh vào chữ cái đặt trước câu trả lời đúng.*', re.IGNORECASE),
        re.compile(r'^\s*\d+\s+BÀI TẬP CÓ ĐÁP ÁN.*', re.IGNORECASE),
        re.compile(r'^\s*(?:Họ và tên|Tên|Lớp|Ngày|Trường|Năm học|Môn)[\s.:_/-]+.*', re.IGNORECASE),
        re.compile(r'^\s*Thứ\s+\.+.*ngày.*năm.*', re.IGNORECASE),
        re.compile(r'^\s*Từ cơ bản đến nâng cao.*', re.IGNORECASE),
        re.compile(r'^\s*Trang\s*\d+(?:/\d+)?\s*$', re.IGNORECASE),
        re.compile(r'^\s*-\s*\d+\s*-\s*$', re.IGNORECASE),
        re.compile(r'^\s*[-=_*]{4,}\s*$'),
    ]

    SECTION_PATTERN = re.compile(r'^\s*(PHẦN\s*(?:[IVX]+|\d+)[\.:\s]+[^\n]+)', re.IGNORECASE)
    TOPIC_PATTERN = re.compile(r'^\s*(Dạng\s*\d+[\.:\s]+[^\n]+)', re.IGNORECASE)

    @classmethod
    def extract_mc_options_map(cls, text: str) -> Dict[str, str]:
        """Trích xuất danh sách nội dung các phương án A, B, C, D từ văn bản câu hỏi."""
        if not text:
            return {}
        # Tách bỏ phần Đáp án / Lời giải ở cuối (nếu có) trước khi quét phương án
        content_part = re.split(
            r'\n\s*(?:Đáp\s*án(?:\s+giáo\s+viên)?|Đáp\s*số|Lời\s*giải|Hướng\s*dẫn(?:\s+giải)?|Giải|Answer|Solution)\s*[:.\-]',
            text,
            maxsplit=1,
            flags=re.IGNORECASE,
        )[0]

        options: Dict[str, str] = {}
        # Tìm các vị trí bắt đầu phương án A., B., C., D. (chữ in hoa)
        pattern = re.compile(r'(?:^|\n|\s{2,}|\t)([A-D])\s*[\.\)\:]\s*')
        matches = list(pattern.finditer(content_part))
        if not matches:
            return {}

        # Chỉ lấy từ phương án A đầu tiên trở đi
        start_idx = next((i for i, m in enumerate(matches) if m.group(1) == 'A'), None)
        if start_idx is None:
            return {}

        valid_matches = matches[start_idx:]
        for i, m in enumerate(valid_matches):
            letter = m.group(1)
            val_start = m.end()
            val_end = valid_matches[i + 1].start() if i + 1 < len(valid_matches) else len(content_part)
            val = content_part[val_start:val_end].strip()
            if letter not in options and val:
                options[letter] = val
        return options

    @classmethod
    def detect_multiple_choice(cls, raw_text: str, answer_hint: Optional[str] = None) -> bool:
        """
        Kiểm tra xem khối câu hỏi có phải là dạng trắc nghiệm nhiều lựa chọn hay không
        dựa trên các phương án A, B, C, D trong đề hoặc từ answer_hint.
        """
        options_map = cls.extract_mc_options_map(raw_text or "")
        if 'A' in options_map and 'B' in options_map:
            return True

        if answer_hint and cls.MC_ANSWER_HINT_REGEX.match(answer_hint.strip()):
            return True

        # Kiểm tra cả phần đáp án đi kèm trực tiếp trong raw_text
        inline_match = re.search(
            r'(?:Đáp\s*án(?:\s+giáo\s+viên)?|Answer)\s*[:.\-]\s*([A-D])(?:\b|[\.\)])',
            raw_text or "",
            re.IGNORECASE,
        )
        if inline_match and ('A' in options_map or 'B' in options_map):
            return True

        return False

    @classmethod
    def enrich_mc_answer_hint(cls, raw_text: str, answer_hint: str) -> str:
        """
        Chuẩn hóa và bổ sung nội dung phương án đúng vào answer_hint của câu hỏi trắc nghiệm
        nếu bảng đáp án chỉ ghi chữ cái (ví dụ: 'B' hoặc 'B. Giải: ...').
        """
        if not answer_hint:
            return answer_hint

        hint = answer_hint.strip()
        m = cls.MC_ANSWER_HINT_REGEX.match(hint)
        if not m:
            return hint

        letter = m.group(1).upper()
        options_map = cls.extract_mc_options_map(raw_text or "")
        option_text = options_map.get(letter)
        if not option_text:
            return hint

        # Kiểm tra xem sau chữ cái A/B/C/D trong hint là gì
        rest = re.sub(
            r'^\s*(?:Đáp\s*án(?:\s+đúng|\s+giáo\s+viên)?\s*[:.-]?\s*)?[A-D][\s.\)\-–—]*',
            '',
            hint,
            count=1,
            flags=re.IGNORECASE,
        ).strip()

        if not rest:
            return f"{letter}. {option_text}"

        # Nếu rest bắt đầu bằng Giải / Lời giải / Hướng dẫn mà chưa có nội dung phương án
        if re.match(r'^(?:Giải|Lời\s+giải|Hướng\s+dẫn|Vì)\b', rest, re.IGNORECASE):
            opt_clean = option_text.rstrip('.')
            return f"{letter}. {opt_clean}. {rest}"

        return hint

    def chunk(self, doc: ExtractedDocument) -> List[QuestionChunk]:
        """
        Xử lý toàn bộ tài liệu, trích xuất bảng đáp án nếu có,
        phân tách các câu hỏi và gắn hình ảnh cùng answer hint và cờ trắc nghiệm.
        """
        # Bước 1 & 2: Trích xuất Bảng đáp án / Hướng dẫn giải và lọc các blocks nội dung đề bài
        answer_key_dict, content_blocks = self._extract_answer_keys_and_filter_blocks(doc)

        # Bước 3: Phân đoạn các khối câu hỏi theo dòng văn bản và hình ảnh
        raw_chunks = self._segment_document(content_blocks, answer_key_dict)

        # Fallback nếu không tách được câu hỏi nào bằng thuật toán chính
        if not raw_chunks:
            raw_chunks = self._fallback_chunking(content_blocks, answer_key_dict)

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

            is_mc = bool(
                item.get("is_multiple_choice")
                or self.detect_multiple_choice(combined_text, answer_hint)
            )

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
                    is_multiple_choice=is_mc,
                )
            )

        return result

    def _extract_answer_keys(self, doc: ExtractedDocument) -> Tuple[Dict[int, str], List[int]]:
        """Giữ tương thích ngược nếu có hàm gọi trực tiếp _extract_answer_keys."""
        answer_dict, content_blocks = self._extract_answer_keys_and_filter_blocks(doc)
        content_pages = {b.page_or_index for b in content_blocks}
        all_pages = {b.page_or_index for b in doc.blocks}
        return answer_dict, list(all_pages - content_pages)

    @classmethod
    def _parse_answer_entries(cls, text: str) -> Dict[int, str]:
        """Phân tích chuỗi văn bản vùng Đáp án thành từ điển {số_câu: đáp_án_kèm_lời_giải}."""
        answer_dict: Dict[int, str] = {}
        if not text or not text.strip():
            return answer_dict

        matches = re.findall(
            r'(?:^|\n)\s*(?:Bài|Câu|Question|Problem)\s*(\d+)\b[\s.:)-]*'
            r'([^\n]*(?:\n(?!\s*(?:Bài|Câu|Question|Problem)\s*\d+\b)[^\n]*)*)',
            text,
            re.IGNORECASE,
        )
        for num_str, ans_content in matches:
            try:
                q_num = int(num_str)
                lines = [ln.strip() for ln in ans_content.strip().split("\n") if ln.strip()]
                if not lines:
                    continue
                # Nếu dòng đầu chỉ là chữ cái đáp án A/B/C/D và dòng sau là Giải: ...
                if len(lines) >= 2 and re.match(r'^[A-D][\.\)]?$', lines[0], re.IGNORECASE):
                    letter = lines[0][0].upper()
                    rest = " ".join(lines[1:])
                    cleaned_ans = f"{letter}. {rest}"
                else:
                    cleaned_ans = " ".join(lines)
                cleaned_ans = re.sub(r'\s+', ' ', cleaned_ans).strip()
                if cleaned_ans:
                    answer_dict[q_num] = cleaned_ans
            except ValueError:
                continue

        # Dự phòng nếu bảng đáp án chỉ đánh số 1. A, 2. B... không có chữ Câu/Bài
        if not answer_dict:
            num_matches = re.findall(
                r'(?:^|\n|\s)(\d+)\s*[\.:\)]\s*([A-D]\b[^\n]*)',
                text,
                re.IGNORECASE,
            )
            for num_str, ans_content in num_matches:
                try:
                    q_num = int(num_str)
                    answer_dict[q_num] = re.sub(r'\s+', ' ', ans_content.strip())
                except ValueError:
                    continue

        return answer_dict

    def _extract_answer_keys_and_filter_blocks(
        self, doc: ExtractedDocument
    ) -> Tuple[Dict[int, str], List[ExtractedBlock]]:
        """
        Phát hiện và trích xuất Bảng Đáp Án / Hướng dẫn giải ở cuối tài liệu (hoặc cuối trang),
        đồng thời loại bỏ phần bảng đáp án khỏi danh sách các block nội dung câu hỏi.
        """
        answer_dict: Dict[int, str] = {}
        answer_pages: set[int] = set()
        total_pages = doc.metadata.get("total_pages", 1)

        # 1. Kiểm tra các block/trang bảng đáp án tập trung kiểu PDF nhiều trang (như ILA PDF)
        for block in doc.blocks:
            if block.block_type != "text":
                continue
            text = block.content.strip()
            matches_count = len(re.findall(r'(?:Bài|Câu|Question|Problem)\s*\d+', text, re.IGNORECASE))
            is_answer_block = (
                matches_count >= 5 and (
                    "ĐÁP ÁN" in text.upper() or
                    "BẢNG" in text.upper() or
                    (total_pages > 1 and block.page_or_index >= max(1, total_pages - 1))
                )
            )
            # Chỉ coi cả trang là trang đáp án nếu block này bắt đầu bằng tiêu đề Đáp án hoặc ở trang cuối PDF
            if is_answer_block and (
                self.ANSWER_SECTION_HEADER_REGEX.search(text)
                or (total_pages > 1 and block.page_or_index >= max(1, total_pages - 1))
            ):
                answer_pages.add(block.page_or_index)
                answer_dict.update(self._parse_answer_entries(text))

        # 2. Quét tuần tự các block còn lại để phát hiện phần "ĐÁP ÁN VÀ HƯỚNG DẪN GIẢI" ở cuối tài liệu
        filtered_blocks: List[ExtractedBlock] = []
        in_answer_section = False
        tail_answer_lines: List[str] = []

        for block in doc.blocks:
            if block.page_or_index in answer_pages:
                continue

            if in_answer_section:
                if block.block_type == "text":
                    tail_answer_lines.append(block.content)
                continue

            if block.block_type != "text":
                filtered_blocks.append(block)
                continue

            lines = block.content.split("\n")
            kept_lines: List[str] = []
            for line in lines:
                if not in_answer_section and self.ANSWER_SECTION_HEADER_REGEX.match(line.strip()):
                    in_answer_section = True
                    # Nếu dòng ngay trước đó là đường kẻ phân cách (---...), loại bỏ nó khỏi câu hỏi cuối
                    if kept_lines and self.DIVIDER_LINE_REGEX.match(kept_lines[-1].strip()):
                        kept_lines.pop()
                    elif (
                        filtered_blocks
                        and filtered_blocks[-1].block_type == "text"
                        and self.DIVIDER_LINE_REGEX.match(filtered_blocks[-1].content.strip())
                    ):
                        filtered_blocks.pop()
                    continue

                if in_answer_section:
                    tail_answer_lines.append(line)
                else:
                    kept_lines.append(line)

            if kept_lines:
                new_content = "\n".join(kept_lines).strip()
                if new_content:
                    filtered_blocks.append(
                        ExtractedBlock(
                            block_type=block.block_type,
                            content=new_content,
                            order=block.order,
                            page_or_index=block.page_or_index,
                            image_url=block.image_url,
                            extra=block.extra,
                        )
                    )

        if tail_answer_lines:
            tail_text = "\n".join(tail_answer_lines)
            answer_dict.update(self._parse_answer_entries(tail_text))

        return answer_dict, filtered_blocks

    def _segment_text_questions(self, cleaned_page_text: str) -> List[dict]:
        """
        Tách văn bản trên trang thành danh sách các khối câu hỏi.
        Hỗ trợ:
        - Câu hỏi bắt đầu bằng 'Câu X', 'Bài X', 'Question X', 'Problem X'.
        - Câu hỏi không có tiêu đề 'Câu/Bài', bắt đầu bằng một đoạn văn và kết thúc ở cuối các đáp án A, B, C, D.
        """
        if not cleaned_page_text.strip():
            return []

        # Tiền xử lý: Nếu một dòng chứa cả câu hỏi lẫn tất cả các đáp án A. ... B. ... trên cùng 1 dòng,
        # tách phần đáp án xuống dòng để xử lý thống nhất.
        raw_lines = [ln.strip() for ln in cleaned_page_text.split("\n") if ln.strip()]
        normalized_lines: List[str] = []
        for ln in raw_lines:
            split_m = self.INLINE_MULTI_OPTIONS_SPLIT_REGEX.match(ln)
            if split_m and not self.OPTION_LINE_START_REGEX.match(ln):
                stem_part = split_m.group(1).strip()
                opts_part = ln[split_m.end():].strip()
                normalized_lines.append(stem_part)
                normalized_lines.append(opts_part)
            else:
                normalized_lines.append(ln)

        segments: List[dict] = []
        current: Optional[dict] = None

        def flush_current():
            nonlocal current
            if not current or not current["lines"]:
                current = None
                return
            is_mc = len(current["seen_options"]) >= 2 and "A" in current["seen_options"]
            # Chỉ giữ lại khối nếu có header tường minh (Câu/Bài) hoặc là câu hỏi trắc nghiệm hoàn chỉnh (có đáp án A, B...)
            if current["has_header"] or is_mc:
                segments.append({
                    "q_num": current["q_num"],
                    "prefix": current["prefix"],
                    "text": "\n".join(current["lines"]).strip(),
                    "is_multiple_choice": is_mc,
                })
            current = None

        total_lines = len(normalized_lines)
        for idx, line in enumerate(normalized_lines):
            hdr_match = self.QUESTION_HEADER_PATTERN.match(line)
            if hdr_match:
                flush_current()
                prefix_raw = hdr_match.group(1)
                prefix = "Bài" if prefix_raw.lower().startswith("b") else "Câu"
                try:
                    q_num = int(hdr_match.group(2))
                except ValueError:
                    q_num = None

                current = {
                    "q_num": q_num,
                    "prefix": prefix,
                    "has_header": True,
                    "lines": [line],
                    "in_options": False,
                    "seen_options": set(),
                    "has_inline_answer": False,
                }
                continue

            # Kiểm tra dòng Đáp án / Lời giải đi kèm trực tiếp sau câu hỏi
            if self.INLINE_ANSWER_PATTERN.match(line):
                if current is not None:
                    current["lines"].append(line)
                    current["has_inline_answer"] = True
                continue

            # Kiểm tra dòng phương án A., B., C., D.
            opt_start_match = self.OPTION_LINE_START_REGEX.match(line)
            if opt_start_match:
                first_letter = opt_start_match.group(1)
                inline_letters = self.INLINE_OPTIONS_REGEX.findall(line)
                if current is not None:
                    if not current["in_options"] and first_letter == "A":
                        current["in_options"] = True
                        current["seen_options"].update(inline_letters)
                        current["lines"].append(line)
                        continue
                    elif current["in_options"] and first_letter not in current["seen_options"]:
                        current["seen_options"].update(inline_letters)
                        current["lines"].append(line)
                        continue

            # Dòng văn bản thông thường (không phải header Câu/Bài, không phải A/B/C/D, không phải Đáp án/Lời giải)
            if current is None:
                current = {
                    "q_num": None,
                    "prefix": "Câu",
                    "has_header": False,
                    "lines": [line],
                    "in_options": False,
                    "seen_options": set(),
                    "has_inline_answer": False,
                }
                continue

            # Nếu câu hỏi hiện tại đã kết thúc phần đáp án A, B, C, D hoặc đã có dòng Đáp án/Lời giải
            # thì dòng văn bản mới này là sự bắt đầu của câu hỏi tiếp theo!
            options_completed = (
                current["in_options"]
                and "A" in current["seen_options"]
                and (
                    "D" in current["seen_options"]
                    or len(current["seen_options"]) >= 3
                    or (len(current["seen_options"]) >= 2 and not opt_start_match)
                )
            )

            # Hoặc nếu câu hỏi trước đó (không có đáp án A, B, C, D) đã kết thúc bằng dấu '?'
            # và ngay sau dòng hiện tại là phương án 'A. ...' của câu hỏi trắc nghiệm mới
            next_line_is_option_a = (
                idx + 1 < total_lines
                and bool(re.match(r'^\s*A\s*[\.\)\:]\s*\S+', normalized_lines[idx + 1]))
            )
            prev_question_ended_before_new_mc = (
                not current["in_options"]
                and len(current["lines"]) >= 1
                and current["lines"][-1].rstrip().endswith("?")
                and next_line_is_option_a
            )

            if options_completed or current["has_inline_answer"] or prev_question_ended_before_new_mc:
                flush_current()
                current = {
                    "q_num": None,
                    "prefix": "Câu",
                    "has_header": False,
                    "lines": [line],
                    "in_options": False,
                    "seen_options": set(),
                    "has_inline_answer": False,
                }
            else:
                current["lines"].append(line)

        flush_current()
        return segments

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
            question_segments = self._segment_text_questions(cleaned_page_text)

            # Kiểm tra nếu là trang bìa thông tin chung (chỉ bỏ qua khi trang không có câu hỏi văn bản nào)
            all_page_text = " ".join([b.content for b in page_text_blocks])
            if page_num == 1 and not question_segments and (
                "HỌ VÀ TÊN" in all_page_text.upper() or "BÀI TẬP CÓ ĐÁP ÁN" in all_page_text.upper()
            ):
                continue

            # ─── XỬ LÝ ẢNH BÀI TẬP (IMAGE CARDS) TRÊN TRANG ───
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
                        "is_multiple_choice": self.detect_multiple_choice(card_text, ans_hint),
                    })

            # ─── XỬ LÝ CÂU HỎI VĂN BẢN (TEXT QUESTIONS) TRÊN TRANG ───
            if question_segments:
                for i, seg in enumerate(question_segments):
                    question_raw_text = seg["text"]
                    q_num = seg["q_num"]
                    if q_num is not None:
                        last_question_number = q_num
                    else:
                        last_question_number += 1
                        q_num = last_question_number

                    # Nếu trang không có large card images riêng thì gán ảnh nhỏ phụ (nếu có)
                    matching_imgs = []
                    matching_paths = []
                    if not page_large_images and page_all_images:
                        for img in page_all_images:
                            if img.image_url:
                                matching_imgs.append(img.image_url)
                            matching_paths.append(img.content)

                    ans_hint = answer_key_dict.get(q_num) if q_num else None
                    is_mc = bool(
                        seg.get("is_multiple_choice")
                        or self.detect_multiple_choice(question_raw_text, ans_hint)
                    )

                    chunks.append({
                        "label": f"Bài {q_num}" if q_num else f"Câu {len(chunks) + 1}",
                        "question_number": q_num,
                        "text": question_raw_text,
                        "image_urls": matching_imgs if (len(question_segments) == 1 or i == 0) else [],
                        "image_paths": matching_paths if (len(question_segments) == 1 or i == 0) else [],
                        "answer_hint": ans_hint,
                        "section_title": current_section,
                        "topic_title": current_topic,
                        "is_multiple_choice": is_mc,
                    })

            # ─── XỬ LÝ DẠNG WORKSHEET MẦM NON (PDF 1) ───
            # Khi trang không có cả Bài/Câu lẫn large image card nhưng có phép tính
            if not question_segments and not page_large_images:
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
                            "is_multiple_choice": False,
                        })

        return chunks

    def _fallback_chunking(
        self,
        blocks: List[ExtractedBlock],
        answer_key_dict: Dict[int, str]
    ) -> List[dict]:
        """Chia đoạn dự phòng khi không nhận diện được mẫu câu hỏi rõ ràng."""
        text = "\n".join(b.content for b in blocks if b.block_type == "text")
        segments = self._segment_text_questions(text)
        if not segments:
            split_pattern = re.compile(r'(?=(?:Câu|Bài|Question)\s*\d+[:.])', re.IGNORECASE)
            raw_parts = [s.strip() for s in split_pattern.split(text) if s.strip()]
            segments = [
                {
                    "q_num": i + 1,
                    "prefix": "Câu",
                    "text": part,
                    "is_multiple_choice": self.detect_multiple_choice(part, answer_key_dict.get(i + 1)),
                }
                for i, part in enumerate(raw_parts)
            ]

        all_imgs = [b.image_url for b in blocks if b.block_type == "image" and b.image_url]
        all_paths = [b.content for b in blocks if b.block_type == "image"]

        chunks = []
        for i, seg in enumerate(segments):
            q_num = seg.get("q_num") or (i + 1)
            seg_text = seg["text"].strip()
            if not seg_text:
                continue
            ans_hint = answer_key_dict.get(q_num)
            chunks.append({
                "label": f"Câu {q_num}",
                "question_number": q_num,
                "text": seg_text,
                "image_urls": all_imgs if i == 0 else [],
                "image_paths": all_paths if i == 0 else [],
                "answer_hint": ans_hint,
                "section_title": "",
                "topic_title": "",
                "is_multiple_choice": bool(
                    seg.get("is_multiple_choice") or self.detect_multiple_choice(seg_text, ans_hint)
                ),
            })

        return chunks
