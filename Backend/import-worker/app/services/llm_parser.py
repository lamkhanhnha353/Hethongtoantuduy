import json
import logging
import re
from pathlib import Path
from typing import Optional, List
from app.config import settings
from app.schemas.question import QuestionItem, QuestionType, Difficulty
from app.services.chunker import QuestionChunk

logger = logging.getLogger(__name__)

SYSTEM_PROMPT = """Bạn là Chuyên gia Thẩm định & Số hóa Đề thi Toán tư duy (AI Question Structuring Specialist).
Nhiệm vụ của bạn là bóc tách các bài toán tư duy (từ tài liệu Word, PDF, hình ảnh bài tập) thành cấu trúc JSON chuẩn xác để nạp vào Ngân hàng câu hỏi của Hệ thống Toán Tư Duy.

BẮT BUỘC TUÂN THỦ NGHIÊM NGẶT CÁC QUI ƯỚC CỦA MODULE TeacherQuestionsView.vue:

1. PHÂN LOẠI CÂU HỎI (`type`):
   - "MULTIPLE_CHOICE": Câu hỏi trắc nghiệm khách quan.
   - "FILL_IN": Câu hỏi điền số, điền từ, điền dấu so sánh, đếm số hình hoặc điền kết quả vào chỗ trống (...).
   - "ESSAY": Câu hỏi tự luận, phân loại nhóm, nối cặp, hoặc thử thách logic suy luận nhiều bước.

2. QUI ƯỚC ĐẶC BIỆT CHO TRẮC NGHIỆM (`MULTIPLE_CHOICE`):
   - Trường `content`: Chứa toàn bộ nội dung đề bài và BẮT BUỘC kết thúc bằng đúng 4 phương án lựa chọn A, B, C, D trên 4 dòng riêng biệt theo đúng cú pháp:
     A. [Nội dung A]
     B. [Nội dung B]
     C. [Nội dung C]
     D. [Nội dung D]
     * Chú ý chuyển đổi số lượng phương án:
       + Nếu đề bài gốc có 5 phương án A, B, C, D, E (ví dụ bài tập hình ảnh): BẮT BUỘC quy đổi về đúng 4 phương án A, B, C, D (luôn giữ phương án đúng, chọn 3 phương án nhiễu còn lại).
       + Nếu đề bài gốc chỉ có 3 phương án A, B, C: BẮT BUỘC bổ sung thêm phương án D hợp lý để đủ 4 phương án.
       + Nếu đề bài gốc là dạng chọn số trong ô/nút tròn như worksheet mầm non (ví dụ "2 - 1 = ?", các số "3, 2, 1"): Chuyển thành 4 lựa chọn A, B, C, D.
     * TUYỆT ĐỐI KHÔNG để lộ đáp án đúng hay lời giải trong trường `content`.
   - Trường `answer`: BẮT BUỘC có định dạng chuẩn:
     <Ký tự đáp án đúng A, B, C hoặc D>. <Nội dung phương án đúng>
     - Lời giải: <Giải thích chi tiết các bước tư duy, lập luận sư phạm rõ ràng>

3. QUI ƯỚC CHO CÂU HỎI ĐIỀN SỐ / ĐIỀN KHUYẾT (`FILL_IN`):
   - Trường `content`: Chứa đề bài và các ý a, b, c, d nếu có (giữ nguyên yêu cầu).
   - Trường `answer`: BẮT BUỘC có định dạng:
     <Đáp số/Kết quả chuẩn ngắn gọn cần điền>
     - Hướng dẫn giải: <Các bước giải thích chi tiết, cách tính toán hoặc quy luật>

4. QUI ƯỚC CHO CÂU HỎI TỰ LUẬN (`ESSAY`):
   - Trường `content`: Đề bài đầy đủ và các yêu cầu trình bày.
   - Trường `answer`: BẮT BUỘC gồm:
     <Bài giải mẫu hoàn chỉnh / Kết quả phân loại>
     - Tiêu chí chấm điểm: <Các bước lập luận và thang điểm tương ứng>

5. QUI ƯỚC ĐỘ KHÓ (`difficulty`) & ĐIỂM SỐ CHUẨN CỦA HỆ THỐNG:
   - "EASY" (Quy định 1.0 điểm): Nhận biết cơ bản, đếm hình đơn lẻ không lồng nhau, nhận biết số trước sau, dãy số tăng giảm 1 đơn vị, so sánh số nhỏ hơn 20, phép tính 1 bước phạm vi 10.
   - "MEDIUM" (Quy định 1.5 điểm): Thông hiểu, quy luật dãy số nhảy bước (cách 2, cách 5), đếm hình tam giác lồng nhau (ví dụ: 5 tam giác), toán có lời văn 1-2 phép tính, tách gộp số, bài toán đồng hồ thời gian.
   - "HARD" (Quy định 2.0 điểm): Vận dụng cao, đếm hình trong ma trận 3x3 (ví dụ: 14 hình vuông), ghép hình/tư duy không gian (ghép tam giác thành hình vuông/chữ nhật), tìm số thỏa mãn nhiều điều kiện đồng thời, thử thách logic suy luận bảng (ai có đồ chơi gì, ai về đích trước).

6. TÔN TRỌNG ĐÁP ÁN GỐC TỪ TÀI LIỆU (ANSWER HINT):
   - Nếu trong yêu cầu có cung cấp `[Đáp án chuẩn từ tài liệu gốc]`, BẮT BUỘC phải dùng kết quả đó làm đáp án cốt lõi (không được thay đổi kết quả), đồng thời viết thêm phần giải thích logic sư phạm chi tiết.
"""


class LLMParserService:
    """Service chịu trách nhiệm gọi LLM multimodal và bóc tách thành QuestionItem chuẩn hóa."""

    def __init__(self):
        self.provider = settings.LLM_PROVIDER.lower()
        self._init_clients()

    def _init_clients(self):
        self.gemini_client = None
        self.openai_client = None

        if self.provider == "gemini" and settings.GEMINI_API_KEY:
            try:
                from google import genai
                self.gemini_client = genai.Client(api_key=settings.GEMINI_API_KEY)
                logger.info(f"Google GenAI SDK Client initialized with model {settings.GEMINI_MODEL}.")
            except ImportError:
                try:
                    import google.generativeai as gai
                    gai.configure(api_key=settings.GEMINI_API_KEY)
                    self.gemini_legacy = gai
                    logger.info("Google GenerativeAI (legacy) configured.")
                except ImportError:
                    logger.warning("No Google Gemini package found.")

        elif self.provider == "openai" and settings.OPENAI_API_KEY:
            try:
                from openai import OpenAI
                self.openai_client = OpenAI(api_key=settings.OPENAI_API_KEY)
                logger.info("OpenAI SDK Client initialized successfully.")
            except ImportError:
                logger.warning("OpenAI SDK not installed.")

    async def parse_chunk(self, chunk: QuestionChunk, bank_id: int) -> QuestionItem:
        """
        Nhận vào 1 QuestionChunk và trả về một QuestionItem đã validate 100%.
        """
        image_url = chunk.primary_image_url

        if self.provider == "gemini" and self.gemini_client:
            return await self._parse_with_gemini(chunk, bank_id, image_url)
        elif self.provider == "openai" and self.openai_client:
            return await self._parse_with_openai(chunk, bank_id, image_url)
        else:
            logger.info("Using Heuristic/Rule-based Parser fallback.")
            return self._parse_with_heuristic(chunk, bank_id, image_url)

    async def _parse_with_gemini(self, chunk: QuestionChunk, bank_id: int, image_url: Optional[str]) -> QuestionItem:
        from google.genai import types

        context_info = []
        if chunk.section_title:
            context_info.append(f"Chuyên đề/Phần: {chunk.section_title}")
        if chunk.topic_title:
            context_info.append(f"Dạng bài: {chunk.topic_title}")
        if chunk.answer_hint:
            context_info.append(f"[ĐÁP ÁN CHUẨN TỪ TÀI LIỆU GỐC]: {chunk.answer_hint}")

        context_str = "\n".join(context_info) if context_info else "Không có ngữ cảnh bổ sung"

        prompt = f"""Hãy phân tích bài toán tư duy sau và cấu trúc hóa thành JSON theo đúng Schema QuestionItem:
ID ngân hàng câu hỏi (bankId): {bank_id}
Ảnh minh họa (imageUrl): {image_url or "null"}
Thông tin bổ trợ & Đáp án gốc:
{context_str}

NỘI DUNG VĂN BẢN/BÀI TẬP:
\"\"\"{chunk.raw_text}\"\"\"

LƯU Ý QUAN TRỌNG:
- Nếu bài toán được cung cấp kèm hình ảnh, hãy đọc kỹ nội dung, hình vẽ, sơ đồ trong ảnh để xây dựng đề bài hoàn chỉnh và chính xác.
- Nếu là Trắc nghiệm (MULTIPLE_CHOICE): Đảm bảo trường 'content' kết thúc bằng đúng 4 lựa chọn A, B, C, D trên 4 dòng riêng biệt. Trường 'answer' phải bắt đầu bằng '<Key>. <Nội dung>\n- Lời giải: <giải thích>'.
- Nếu là Điền số (FILL_IN): Trường 'answer' phải có dạng '<Đáp số>\n- Hướng dẫn giải: <giải thích>'.
"""
        contents = [prompt]

        # Nạp ảnh thật vào Gemini nếu có (Multimodal Vision)
        if chunk.image_paths and Path(chunk.image_paths[0]).exists():
            try:
                from PIL import Image
                pil_img = Image.open(chunk.image_paths[0])
                contents.append(pil_img)
            except Exception as img_err:
                logger.warning(f"Không thể nạp ảnh vào Gemini: {img_err}")

        try:
            response = self.gemini_client.models.generate_content(
                model=settings.GEMINI_MODEL,
                contents=contents,
                config=types.GenerateContentConfig(
                    system_instruction=SYSTEM_PROMPT,
                    response_mime_type="application/json",
                    response_schema=QuestionItem,
                    temperature=0.1,
                ),
            )
            data = json.loads(response.text)
            data["bankId"] = bank_id
            if image_url:
                data["imageUrl"] = image_url
            return QuestionItem.model_validate(data)

        except Exception as e:
            logger.error(f"Error calling Gemini API: {str(e)}. Falling back to heuristic parser.")
            return self._parse_with_heuristic(chunk, bank_id, image_url)

    async def _parse_with_openai(self, chunk: QuestionChunk, bank_id: int, image_url: Optional[str]) -> QuestionItem:
        prompt = f"""Hãy phân tích bài toán tư duy sau:
ID ngân hàng câu hỏi (bankId): {bank_id}
Ảnh minh họa (imageUrl): {image_url or "null"}
Gợi ý đáp án gốc: {chunk.answer_hint or "Không có"}
VĂN BẢN BÀI TOÁN:
\"\"\"{chunk.raw_text}\"\"\"
"""
        try:
            completion = self.openai_client.beta.chat.completions.parse(
                model=settings.OPENAI_MODEL,
                messages=[
                    {"role": "system", "content": SYSTEM_PROMPT},
                    {"role": "user", "content": prompt},
                ],
                response_format=QuestionItem,
                temperature=0.1,
            )
            item = completion.choices[0].message.parsed
            item.bank_id = bank_id
            if image_url:
                item.image_url = image_url
            return item
        except Exception as e:
            logger.error(f"Error calling OpenAI API: {str(e)}. Falling back to heuristic parser.")
            return self._parse_with_heuristic(chunk, bank_id, image_url)

    def _parse_with_heuristic(self, chunk: QuestionChunk, bank_id: int, image_url: Optional[str]) -> QuestionItem:
        """
        Bộ phân tích luật (Rule-based Heuristic) tuân thủ 100% quy ước của TeacherQuestionsView.vue.
        Sử dụng khi chạy offline, test cục bộ hoặc khi gặp sự cố mạng gọi LLM.
        """
        text = chunk.raw_text.strip()
        ans_hint = chunk.answer_hint or ""

        # 1. Tách phần đề bài và đáp án có sẵn trong text nếu có
        answer_split_pattern = re.compile(
            r'(?:Đáp án|Lời giải|Hướng dẫn giải|Đáp số|Giải|Answer|Solution)[\s.:\-]+',
            re.IGNORECASE
        )
        parts = answer_split_pattern.split(text, maxsplit=1)
        if len(parts) > 1:
            content_part = parts[0].strip()
            answer_text = parts[1].strip()
        else:
            content_part = text
            answer_text = ans_hint if ans_hint else "Chưa có lời giải chi tiết."

        # 2. Nhận diện loại câu hỏi (type)
        has_mc_choices = bool(re.search(r'(?:^|\n)\s*[A-D]\s*[\.\)]\s*', content_part))
        is_mc_by_hint = ans_hint.strip() in ['A', 'B', 'C', 'D', 'E'] or ans_hint.startswith(('A.', 'B.', 'C.', 'D.'))

        # Phán đoán độ khó
        lower_content = (content_part + " " + (chunk.topic_title or "")).lower()
        if any(w in lower_content for w in ["ma trận", "14 hình vuông", "thử thách logic", "suy luận vị trí", "ghép hình", "khó", "tư duy"]):
            difficulty = Difficulty.HARD
        elif any(w in lower_content for w in ["tam giác", "quy luật", "tách – gộp", "lời văn", "đồng hồ", "đo lường", "sắp xếp"]):
            difficulty = Difficulty.MEDIUM
        else:
            difficulty = Difficulty.EASY

        # Xử lý trường hợp TRẮC NGHIỆM (MULTIPLE_CHOICE)
        if has_mc_choices or is_mc_by_hint:
            q_type = QuestionType.MULTIPLE_CHOICE
            # Đảm bảo có đủ 4 phương án A, B, C, D
            if not has_mc_choices:
                # Nếu chưa có phương án trong text (ví dụ bài tập hình ảnh), bổ sung 4 phương án tiêu chuẩn
                content_part = (
                    f"{content_part}\n"
                    f"A. Phương án A\n"
                    f"B. Phương án B\n"
                    f"C. Phương án C\n"
                    f"D. Phương án D"
                )

            # Chuẩn hóa đáp án đúng
            correct_key = "A"
            if ans_hint and ans_hint[0].upper() in ['A', 'B', 'C', 'D']:
                correct_key = ans_hint[0].upper()

            final_answer = f"{correct_key}. Phương án {correct_key}\n- Lời giải: Dựa vào quy luật và hình ảnh bài toán, ta chọn đáp án {correct_key}."

        # Xử lý trường hợp TỰ LUẬN (ESSAY)
        elif any(w in lower_content for w in ["chia các hình", "nối các đồ vật", "thử thách logic", "hỏi mỗi bạn có đồ chơi gì"]):
            q_type = QuestionType.ESSAY
            final_answer = f"{ans_hint if ans_hint else answer_text}\n- Tiêu chí chấm điểm: Trình bày đầy đủ các bước suy luận, mỗi lập luận đúng được 0.5 - 1.0 điểm."

        # Mặc định là ĐIỀN SỐ / ĐIỀN KHUYẾT (FILL_IN)
        else:
            q_type = QuestionType.FILL_IN
            clean_res = ans_hint if ans_hint else answer_text
            final_answer = f"{clean_res}\n- Hướng dẫn giải: Thực hiện theo quy luật toán học để tìm ra kết quả: {clean_res}."

        return QuestionItem(
            bankId=bank_id,
            type=q_type,
            content=content_part,
            imageUrl=image_url,
            difficulty=difficulty,
            answer=final_answer,
        )
