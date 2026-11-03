import json
import logging
import re
from typing import Optional, List
from app.config import settings
from app.schemas.question import QuestionItem, QuestionType, Difficulty, QuestionBatch
from app.services.chunker import QuestionChunk

logger = logging.getLogger(__name__)

SYSTEM_PROMPT = """Bạn là một Chuyên gia Thẩm định và Số hóa Đề thi Toán tư duy (AI Question Structuring Specialist).
Nhiệm vụ của bạn là nhận vào một đoạn văn bản thô của một bài toán/câu hỏi và chuyển đổi nó thành một đối tượng JSON có cấu trúc chuẩn xác theo Schema quy định.

CÁC NGUYÊN TẮC BẮT BUỘC:
1. Phân loại loại câu hỏi (`type`):
   - "MULTIPLE_CHOICE": Nếu đề bài có các phương án lựa chọn (A, B, C, D...). Đặt toàn bộ đề bài và danh sách các lựa chọn vào trường `content`.
   - "FILL_IN": Nếu đề bài yêu cầu học sinh điền số, điền từ, điền vào chỗ trống hoặc chỉ trả lời một đáp số duy nhất mà không có 4 đáp án A-B-C-D.
   - "ESSAY": Nếu là bài toán tự luận, yêu cầu trình bày lời giải chi tiết, chứng minh hoặc bài toán mở.

2. Đánh giá độ khó (`difficulty`):
   - "EASY": Dạng toán cơ bản, áp dụng trực tiếp 1 phép tính hoặc quy luật trực quan đơn giản.
   - "MEDIUM": Dạng toán cần từ 2-3 bước suy luận, toán đố có lời văn hoặc kết hợp nhiều dữ kiện.
   - "HARD": Toán tư duy nâng cao (Kangaroo, SASMO, TIMO...), suy luận logic sâu, bài toán tìm quy luật phức tạp, toán tổ hợp hoặc hình học nâng cao.

3. Tách biệt Đề bài và Lời giải:
   - `content`: CHỈ chứa nội dung đề bài và các phương án A, B, C, D (nếu trắc nghiệm). TUYỆT ĐỐI KHÔNG để lộ đáp án hay lời giải vào trường `content`.
   - `answer`: Chứa đáp án chính xác (ví dụ "A. 81 cm2" hoặc "Đáp số: 42") KÈM THEO lời giải, các bước giải thích chi tiết, lập luận logic để học sinh hiểu cách làm. Nếu văn bản gốc chưa có lời giải chi tiết, bạn hãy tự suy luận và viết lời giải chi tiết, chuẩn mực sư phạm.

4. Dữ liệu ngữ cảnh:
   - `bankId`: Giữ nguyên giá trị bankId được chỉ định.
   - `imageUrl`: Giữ nguyên đường dẫn URL hình ảnh được cung cấp (nếu có). Nếu không có ảnh thì để null.
"""


class LLMParserService:
    """Service chịu trách nhiệm gọi LLM và bóc tách thành QuestionItem chuẩn hóa."""

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
                logger.info("Google GenAI SDK Client initialized successfully.")
            except ImportError:
                # Fallback to google.generativeai if google-genai is not yet installed
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

        # Kiểm tra nếu dùng Gemini
        if self.provider == "gemini" and self.gemini_client:
            return await self._parse_with_gemini(chunk, bank_id, image_url)
        elif self.provider == "openai" and self.openai_client:
            return await self._parse_with_openai(chunk, bank_id, image_url)
        else:
            # Chế độ Mock / Heuristic (cho Local Test hoặc khi chưa cấu hình API Key)
            logger.info("Using Heuristic/Mock Parser (no API key configured).")
            return self._parse_with_heuristic(chunk, bank_id, image_url)

    async def _parse_with_gemini(self, chunk: QuestionChunk, bank_id: int, image_url: Optional[str]) -> QuestionItem:
        from google.genai import types

        prompt = f"""Hãy phân tích và cấu trúc hóa câu hỏi sau thành JSON theo đúng Schema:
ID ngân hàng câu hỏi (bankId): {bank_id}
Ảnh minh họa (imageUrl): {image_url or "null"}

VĂN BẢN CÂU HỎI CẦN XỬ LÝ:
\"\"\"{chunk.raw_text}\"\"\"
"""
        try:
            response = self.gemini_client.models.generate_content(
                model=settings.GEMINI_MODEL,
                contents=prompt,
                config=types.GenerateContentConfig(
                    system_instruction=SYSTEM_PROMPT,
                    response_mime_type="application/json",
                    response_schema=QuestionItem,
                    temperature=0.1,
                ),
            )
            data = json.loads(response.text)
            # Đảm bảo bank_id và image_url khớp
            data["bankId"] = bank_id
            if image_url:
                data["imageUrl"] = image_url
            return QuestionItem.model_validate(data)

        except Exception as e:
            logger.error(f"Error calling Gemini API: {str(e)}. Falling back to heuristic parser.")
            return self._parse_with_heuristic(chunk, bank_id, image_url)

    async def _parse_with_openai(self, chunk: QuestionChunk, bank_id: int, image_url: Optional[str]) -> QuestionItem:
        prompt = f"""Hãy phân tích và cấu trúc hóa câu hỏi sau:
ID ngân hàng câu hỏi (bankId): {bank_id}
Ảnh minh họa (imageUrl): {image_url or "null"}

VĂN BẢN CÂU HỎI:
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
        """Bộ phân tích luật (Rule-based / Heuristic) phục vụ dev, test và fallback an toàn."""
        text = chunk.raw_text

        # 1. Tách đáp án / lời giải nếu trong text có chứa các từ khóa
        answer_split_pattern = re.compile(
            r'(?:Đáp án|Lời giải|Hướng dẫn giải|Đáp số|Giải|Answer|Solution)[\s.:\-]+',
            re.IGNORECASE
        )
        parts = answer_split_pattern.split(text, maxsplit=1)

        if len(parts) > 1:
            content_part = parts[0].strip()
            answer_part = parts[1].strip()
        else:
            content_part = text.strip()
            answer_part = "Chưa có lời giải chi tiết trong tài liệu gốc."

        # 2. Nhận diện loại câu hỏi (type)
        has_choices = bool(re.search(r'[A-D]\s*[\.\)]\s*', content_part))
        has_fill_keywords = bool(re.search(r'(?:điền|số thích hợp|kết quả là|\.\.\.|\?|\bbao nhiêu\b)', content_part, re.IGNORECASE))

        if has_choices:
            q_type = QuestionType.MULTIPLE_CHOICE
        elif has_fill_keywords:
            q_type = QuestionType.FILL_IN
        else:
            q_type = QuestionType.ESSAY

        # 3. Đánh giá độ khó (difficulty)
        lower_content = content_part.lower()
        if any(w in lower_content for w in ["khó", "tư duy", "nâng cao", "chứng minh", "tìm quy luật", "olympiad"]):
            difficulty = Difficulty.HARD
        elif any(w in lower_content for w in ["biết rằng", "nếu", "hỏi", "tính", "diện tích", "chu vi"]):
            difficulty = Difficulty.MEDIUM
        else:
            difficulty = Difficulty.EASY

        return QuestionItem(
            bankId=bank_id,
            type=q_type,
            content=content_part,
            imageUrl=image_url,
            difficulty=difficulty,
            answer=answer_part,
        )
