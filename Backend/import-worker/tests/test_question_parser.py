import json
import sys
import threading
import unittest
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import AsyncMock, patch

sys.path.insert(0, str(Path(__file__).parent.parent))

from app.config import settings
from app.extractors.base import ExtractedBlock, ExtractedDocument
from app.schemas.question import Difficulty, QuestionItem, QuestionType
from app.services.chunker import QuestionChunk, QuestionChunker
from app.services.llm_parser import (
    LLMConfigurationError,
    LLMParserService,
    LLMProcessingError,
)


class QuestionParserTests(unittest.IsolatedAsyncioTestCase):
    async def test_mock_parser_recognizes_true_false_without_exposing_answer(self):
        chunk = QuestionChunk(
            chunk_index=1,
            label="Câu 1",
            raw_text="Câu 1: Nhận định sau đúng hay sai: 3 + 4 = 8.\nĐáp án giáo viên: Sai.",
        )
        with patch.object(settings, "LLM_PROVIDER", "mock"):
            result = await LLMParserService().parse_chunk(chunk, bank_id=12)

        self.assertEqual(result.type, QuestionType.TRUE_FALSE)
        self.assertIn("3 + 4 = 8", result.content)
        self.assertNotIn("Đáp án giáo viên", result.content)
        self.assertTrue(result.answer.startswith("Sai"))

    async def test_mock_parser_recognizes_timed_sequence_and_keeps_answer_separate(self):
        chunk = QuestionChunk(
            chunk_index=1,
            label="Câu 1",
            raw_text="Câu 1: Tính nhẩm theo nhịp: 1, +2, -1. Kết quả cuối cùng là bao nhiêu?\n"
            "Đáp án giáo viên: 2",
        )
        with patch.object(settings, "LLM_PROVIDER", "mock"):
            result = await LLMParserService().parse_chunk(chunk, bank_id=12)

        self.assertEqual(result.type, QuestionType.TIMED_SEQUENCE)
        self.assertIn("Tính nhẩm theo nhịp: 1, +2, -1", result.content)
        self.assertNotIn("Đáp án giáo viên", result.content)
        self.assertEqual(result.answer, "2")

    async def test_explicit_timed_sequence_format_overrides_model_type(self):
        chunk = QuestionChunk(
            chunk_index=1,
            label="Câu 1",
            raw_text="Câu 1: Tính nhẩm theo nhịp: 9, -3, +1. Kết quả cuối cùng là bao nhiêu?",
        )
        model_result = QuestionItem(
            bankId=12,
            type=QuestionType.FILL_IN,
            content="Tính nhẩm theo nhịp: 9, -3, +1. Kết quả cuối cùng là bao nhiêu?",
            difficulty=Difficulty.EASY,
            answer="7",
        )

        result = LLMParserService._apply_explicit_question_type(chunk, model_result)

        self.assertEqual(result.type, QuestionType.TIMED_SEQUENCE)

    async def test_mock_parser_preserves_multiple_choice_options(self):
        chunk = QuestionChunk(
            chunk_index=1,
            label="Câu 1",
            raw_text="Câu 1: 2 + 3 = ?\nA. 4\nB. 5\nC. 6\nĐáp án giáo viên: B. 5",
        )
        with patch.object(settings, "LLM_PROVIDER", "mock"):
            result = await LLMParserService().parse_chunk(chunk, bank_id=1)

        self.assertEqual(result.type, QuestionType.MULTIPLE_CHOICE)
        self.assertIn("C. 6", result.content)
        self.assertNotIn("D. Phương án D", result.content)
        self.assertIn("B. 5", result.answer)

    async def test_real_provider_without_key_reports_configuration_error(self):
        chunk = QuestionChunk(chunk_index=1, label="Câu 1", raw_text="Câu 1: 1 + 1 = ?")
        with (
            patch.object(settings, "LLM_PROVIDER", "gemini"),
            patch.object(settings, "GEMINI_API_KEYS", ""),
            patch.object(settings, "GEMINI_API_KEY", ""),
        ):
            with self.assertRaises(LLMConfigurationError):
                await LLMParserService().parse_chunk(chunk, bank_id=1)

    async def test_ai_failure_is_not_replaced_by_heuristic_result(self):
        chunk = QuestionChunk(chunk_index=1, label="Câu 1", raw_text="Câu 1: 1 + 1 = ?")
        with patch.object(settings, "LLM_PROVIDER", "mock"):
            parser = LLMParserService()
        parser.provider = "openai"
        parser.openai_client = object()
        with (
            patch.object(settings, "OPENAI_API_KEY", "test-key"),
            patch.object(parser, "_parse_with_openai", new=AsyncMock(side_effect=LLMProcessingError("failed"))),
            patch.object(parser, "_parse_with_heuristic") as heuristic_parser,
        ):
            with self.assertRaises(LLMProcessingError):
                await parser.parse_chunk(chunk, bank_id=1)
        heuristic_parser.assert_not_called()

    async def test_gemini_key_rotation_retries_quota_and_advances_after_success(self):
        class QuotaError(Exception):
            status_code = 429

        class ModelClient:
            def __init__(self, response=None, error=None):
                self.response = response
                self.error = error
                self.calls = 0
                self.models = self

            def generate_content(self, **kwargs):
                self.calls += 1
                if self.error:
                    raise self.error
                return self.response

        with (
            patch.object(settings, "LLM_PROVIDER", "mock"),
            patch.object(settings, "GEMINI_API_KEYS", "first,second"),
            patch.object(settings, "GEMINI_API_KEY", ""),
        ):
            parser = LLMParserService()
        first = ModelClient(error=QuotaError("quota exceeded"))
        second = ModelClient(response=object())
        parser.gemini_clients = [first, second]

        self.assertIs(parser._generate_with_gemini([], {}), second.response)
        self.assertEqual(first.calls, 1)
        self.assertEqual(second.calls, 1)
        self.assertEqual(parser._gemini_next_index, 0)

    async def test_gemini_retries_temporary_server_error_on_same_key(self):
        class ServerError(Exception):
            status_code = 503

        class ModelClient:
            def __init__(self):
                self.models = self
                self.calls = 0

            def generate_content(self, **kwargs):
                self.calls += 1
                if self.calls == 1:
                    raise ServerError("service unavailable")
                return "response"

        with patch.object(settings, "LLM_PROVIDER", "mock"):
            parser = LLMParserService()
        client = ModelClient()
        parser.gemini_clients = [client]

        with patch("app.services.llm_parser.time.sleep"):
            result = parser._generate_with_gemini([], {})

        self.assertEqual(result, "response")
        self.assertEqual(client.calls, 2)

    async def test_gemini_reports_overload_after_bounded_retries(self):
        class ServerError(Exception):
            status_code = 503

        class ModelClient:
            def __init__(self):
                self.models = self
                self.calls = 0

            def generate_content(self, **kwargs):
                self.calls += 1
                raise ServerError("service unavailable")

        with patch.object(settings, "LLM_PROVIDER", "mock"):
            parser = LLMParserService()
        client = ModelClient()
        parser.gemini_clients = [client]

        with patch("app.services.llm_parser.time.sleep"):
            with self.assertRaisesRegex(LLMProcessingError, "quá tải"):
                parser._generate_with_gemini([], {})

        self.assertEqual(client.calls, 3)

    async def test_gemini_network_call_runs_off_the_event_loop_thread(self):
        chunk = QuestionChunk(chunk_index=1, label="Question 1", raw_text="1 + 1 = ?")
        parser = LLMParserService()
        call_thread_ids = []
        response = SimpleNamespace(
            text=json.dumps({
                "bankId": 1,
                "type": "FILL_IN",
                "content": "1 + 1 = ?",
                "difficulty": "EASY",
                "answer": "2",
            })
        )

        def generate_with_gemini(*args, **kwargs):
            call_thread_ids.append(threading.get_ident())
            return response

        with patch.object(parser, "_generate_with_gemini", side_effect=generate_with_gemini):
            result = await parser._parse_with_gemini(chunk, bank_id=1, image_url=None)

        self.assertEqual(result.answer, "2")
        self.assertNotEqual(call_thread_ids[0], threading.get_ident())

    async def test_gemini_key_list_supports_delimiters_and_legacy_key(self):
        with (
            patch.object(settings, "GEMINI_API_KEYS", "first; second\nthird"),
            patch.object(settings, "GEMINI_API_KEY", "legacy"),
        ):
            keys = settings.get_gemini_api_keys()
        self.assertEqual(keys, ["first", "second", "third", "legacy"])

    async def test_gemini_key_list_supports_json_array(self):
        with (
            patch.object(settings, "GEMINI_API_KEYS", '["first", "second"]'),
            patch.object(settings, "GEMINI_API_KEY", ""),
        ):
            keys = settings.get_gemini_api_keys()
        self.assertEqual(keys, ["first", "second"])

    async def test_parse_batch_mock_returns_all_items(self):
        chunks = [
            QuestionChunk(chunk_index=1, label="Câu 1", raw_text="Câu 1: 1 + 1 = ?\nĐáp án: 2"),
            QuestionChunk(chunk_index=2, label="Câu 2", raw_text="Câu 2: 2 + 2 = ?\nĐáp án: 4"),
        ]
        with patch.object(settings, "LLM_PROVIDER", "mock"):
            parser = LLMParserService()
            results = await parser.parse_batch(chunks, bank_id=7)

        self.assertEqual(len(results), 2)
        self.assertEqual(results[0].bank_id, 7)
        self.assertEqual(results[1].bank_id, 7)
        self.assertIn("1 + 1 = ?", results[0].content)
        self.assertIn("2 + 2 = ?", results[1].content)

    async def test_parse_batch_gemini_uses_single_request(self):
        chunks = [
            QuestionChunk(chunk_index=1, label="Câu 1", raw_text="Câu 1: 1 + 1 = ?"),
            QuestionChunk(chunk_index=2, label="Câu 2", raw_text="Câu 2: 2 + 2 = ?"),
        ]
        call_count = 0
        response = SimpleNamespace(
            text=json.dumps({
                "questions": [
                    {
                        "bankId": 10,
                        "type": "FILL_IN",
                        "content": "1 + 1 = ?",
                        "difficulty": "EASY",
                        "answer": "2",
                    },
                    {
                        "bankId": 10,
                        "type": "FILL_IN",
                        "content": "2 + 2 = ?",
                        "difficulty": "EASY",
                        "answer": "4",
                    },
                ]
            })
        )

        def mock_generate(*args, **kwargs):
            nonlocal call_count
            call_count += 1
            return response

        parser = LLMParserService()
        parser.provider = "gemini"
        parser.gemini_clients = [object()]

        with (
            patch.object(settings, "GEMINI_API_KEYS", "dummy-key"),
            patch.object(parser, "_generate_with_gemini", side_effect=mock_generate),
        ):
            results = await parser.parse_batch(chunks, bank_id=10)

        self.assertEqual(len(results), 2)
        # 2 câu hỏi chỉ tốn đúng 1 request generate duy nhất!
        self.assertEqual(call_count, 1)
        self.assertEqual(results[0].answer, "2")
        self.assertEqual(results[1].answer, "4")

    async def test_parse_batch_gemini_fallback_on_error(self):
        chunks = [
            QuestionChunk(chunk_index=1, label="Câu 1", raw_text="Câu 1: 1 + 1 = ?"),
            QuestionChunk(chunk_index=2, label="Câu 2", raw_text="Câu 2: 2 + 2 = ?"),
        ]
        parser = LLMParserService()
        parser.provider = "gemini"
        parser.gemini_clients = [object()]

        # Mock batch call failing, but single chunk call succeeding
        single_item = QuestionItem(
            bankId=10,
            type=QuestionType.FILL_IN,
            content="test",
            difficulty=Difficulty.EASY,
            answer="test",
        )

        with (
            patch.object(settings, "GEMINI_API_KEYS", "dummy-key"),
            patch.object(parser, "_generate_with_gemini", side_effect=Exception("Batch failed")),
            patch.object(parser, "_parse_with_gemini", new=AsyncMock(return_value=single_item)) as mock_single,
        ):
            results = await parser.parse_batch(chunks, bank_id=10)

        self.assertEqual(len(results), 2)
        self.assertEqual(mock_single.call_count, 2)

    async def test_chunk_and_parse_grade2_mc_with_tail_answer_and_solution_section(self):
        sample_text = """BỘ CÂU HỎI TRẮC NGHIỆM TOÁN LỚP 2
Câu 1: Bình có 34 viên bi. Bình có nhiều hơn An 18 viên bi. Hỏi cả hai bạn có tất cả bao nhiêu viên bi?
A. 16 viên bi
B. 50 viên bi
C. 52 viên bi
D. 68 viên bi
Câu 2: Đường gấp khúc ABCD gồm ba đoạn thẳng có độ dài lần lượt là AB = 15 cm, BC = 2 dm, CD = 18 cm. Độ dài đường gấp khúc ABCD là bao nhiêu?
A. 35 cm
B. 53 cm
C. 35 dm
D. 53 dm
Câu 3: Ngày 12 tháng 5 là thứ Tư. Hỏi ngày 21 tháng 5 cùng năm đó là thứ mấy?
A. Thứ Năm
B. Thứ Sáu
C. Thứ Bảy
D. Chủ Nhật
Câu 4: Trên một đoạn đường thẳng, người ta trồng 6 cây xanh ở hai đầu đường và cách đều nhau. Biết khoảng cách giữa hai cây liên tiếp là 5 m. Hỏi đoạn đường đó dài bao nhiêu mét?
A. 30 m
B. 25 m
C. 20 m
D. 35 m
Câu 5: Can thứ nhất đựng 18 l nước, can thứ hai đựng 12 l nước. Hỏi phải rót từ can thứ nhất sang can thứ hai bao nhiêu lít nước để số nước ở hai can bằng nhau?
A. 6 l
B. 3 l
C. 2 l
D. 4 l
Câu 6: Số lớn nhất có ba chữ số khác nhau mà tổng các chữ số bằng 12 là số nào?
A. 930
B. 921
C. 840
D. 903
Câu 7: Tìm một số biết rằng nếu lấy số đó chia cho 5 thì được thương là số chẵn lớn nhất có một chữ số. Số cần tìm là bao nhiêu?
A. 45
B. 35
C. 40
D. 30
Câu 8: Trên một chiếc cân thăng bằng, đĩa cân bên trái có 1 quả dưa hấu và 1 quả cân 2 kg, đĩa cân bên phải có 3 quả cân loại 5 kg. Hỏi quả dưa hấu nặng bao nhiêu ki-lô-gam?
A. 17 kg
B. 13 kg
C. 15 kg
D. 10 kg
Câu 9: Một hình chữ nhật lớn được chia bởi một đoạn thẳng nối hai cạnh đối diện thành hai hình chữ nhật nhỏ bên trong. Hỏi trong hình vẽ đó có tất cả bao nhiêu hình tứ giác?
A. 2 hình tứ giác
B. 3 hình tứ giác
C. 4 hình tứ giác
D. 5 hình tứ giác
Câu 10: Trong hộp kín có 4 viên bi xanh và 3 viên bi đỏ. Không nhìn vào hộp, Lan lấy ra cùng một lúc 5 viên bi. Sự kiện nào sau đây là chắc chắn xảy ra?
A. Lấy được 5 viên bi xanh.
B. Lấy được 5 viên bi đỏ.
C. Lấy được ít nhất một viên bi đỏ.
D. Lấy được ít nhất hai viên bi đỏ.
----------------------------------------
ĐÁP ÁN VÀ HƯỚNG DẪN GIẢI:
Câu 1: B 
Giải: Số bi của An là: 34 - 18 = 16 (viên). Cả hai bạn có: 34 + 16 = 50 (viên).
Câu 2: B
Giải: Đổi 2 dm = 20 cm. Độ dài đường gấp khúc là: 15 + 20 + 18 = 53 cm.
Câu 3: B
Giải: Ngày 12 cộng thêm 7 ngày là ngày 19 (vẫn là thứ Tư). Vậy ngày 20 là thứ Năm, ngày 21 là thứ Sáu.
Câu 4: B
Giải: 6 cây xanh trồng cách đều và trồng ở hai đầu đoạn đường sẽ tạo ra 5 khoảng trống. Độ dài đoạn đường là: 5 \\times 5 = 25 m.
Câu 5: B
Giải: Tổng số lít nước ở 2 can là: 18 + 12 = 30 (l). Để bằng nhau thì mỗi can phải có: 30 : 2 = 15 (l). Cần rót từ can thứ nhất: 18 - 15 = 3 (l).
Câu 6: A
Giải: Các chữ số cần là lớn nhất có thể. Chọn hàng trăm là 9, tổng còn lại 12 - 9 = 3. Để lớn nhất, hàng chục là 3, hàng đơn vị là 0. Chữ số là 9, 3, 0 đều khác nhau.
Câu 7: C
Giải: Số chẵn lớn nhất có một chữ số là 8. Số cần tìm là: 8 \\times 5 = 40.
Câu 8: B
Giải: Bên phải nặng:  5 = 15 kg. Bên trái cân bằng nên cũng nặng 15kg. Quả dưa hấu nặng: 15 - 2 = 13kg
Câu 9: B
Giải: Có 2 hình chữ nhật (hình tứ giác) nhỏ và 1 hình chữ nhật lớn bao quanh bên ngoài. Tổng cộng có 3 hình tứ giác.
Câu 10: C
Giải: Hộp chỉ có tối đa 4 viên bi xanh, nếu Lan lấy ra 5 viên thì nhiều nhất cũng chỉ lấy được 4 viên xanh. Bắt buộc viên thứ 5 phải là bi đỏ. Do đó, "Lấy được ít nhất một viên bi đỏ" là chắc chắn xảy ra."""

        blocks = [
            ExtractedBlock(block_type="text", content=line, order=idx + 1, page_or_index=0)
            for idx, line in enumerate(sample_text.split("\n"))
            if line.strip()
        ]
        doc = ExtractedDocument(file_path=Path("sample.docx"), file_type="docx", blocks=blocks)
        chunks = QuestionChunker().chunk(doc)

        self.assertEqual(len(chunks), 10)
        for chunk in chunks:
            self.assertTrue(chunk.is_multiple_choice)
            self.assertIsNotNone(chunk.answer_hint)
            self.assertNotIn("ĐÁP ÁN VÀ HƯỚNG DẪN GIẢI", chunk.raw_text)
            self.assertNotIn("----", chunk.raw_text)

        self.assertIn("B. 50 viên bi", chunks[0].answer_hint)
        self.assertIn("34 + 16 = 50", chunks[0].answer_hint)
        self.assertIn("C. Lấy được ít nhất một viên bi đỏ", chunks[9].answer_hint)

        with patch.object(settings, "LLM_PROVIDER", "mock"):
            parser = LLMParserService()
            parsed_items = await parser.parse_batch(chunks, bank_id=2)

        self.assertEqual(len(parsed_items), 10)
        for item in parsed_items:
            self.assertEqual(item.type, QuestionType.MULTIPLE_CHOICE)

        # Kiểm tra prompt gửi LLM có gắn cờ trắc nghiệm
        prompt_text = parser._build_prompt(chunks[0], bank_id=2, image_url=None)
        self.assertIn("[CỜ TRẮC NGHIỆM: MULTIPLE_CHOICE]", prompt_text)
        self.assertIn("[ĐÁP ÁN TRẮC NGHIỆM CHUẨN TỪ TÀI LIỆU GỐC (MULTIPLE_CHOICE)]", prompt_text)

    async def test_chunk_unlabeled_paragraph_questions_ending_with_abcd(self):
        unlabeled_text = """Bình có 34 viên bi. Bình có nhiều hơn An 18 viên bi. Hỏi cả hai bạn có tất cả bao nhiêu viên bi?
A. 16 viên bi
B. 50 viên bi
C. 52 viên bi
D. 68 viên bi
Đường gấp khúc ABCD gồm ba đoạn thẳng có độ dài lần lượt là AB = 15 cm, BC = 2 dm, CD = 18 cm. Độ dài đường gấp khúc ABCD là bao nhiêu?
A. 35 cm    B. 53 cm    C. 35 dm    D. 53 dm
Bài 3: Tìm một số biết rằng nếu lấy số đó chia cho 5 thì được 8.
Đáp số: 40
Một hình chữ nhật lớn được chia bởi một đoạn thẳng nối hai cạnh đối diện thành hai hình chữ nhật nhỏ bên trong. Hỏi có tất cả bao nhiêu hình tứ giác?
A. 2 hình tứ giác
B. 3 hình tứ giác
C. 4 hình tứ giác
D. 5 hình tứ giác"""

        blocks = [
            ExtractedBlock(block_type="text", content=line, order=idx + 1, page_or_index=1)
            for idx, line in enumerate(unlabeled_text.split("\n"))
            if line.strip()
        ]
        doc = ExtractedDocument(file_path=Path("mixed.docx"), file_type="docx", blocks=blocks)
        chunks = QuestionChunker().chunk(doc)

        self.assertEqual(len(chunks), 4)
        self.assertTrue(chunks[0].is_multiple_choice)
        self.assertIn("Bình có 34 viên bi", chunks[0].raw_text)
        self.assertIn("D. 68 viên bi", chunks[0].raw_text)
        self.assertNotIn("Đường gấp khúc", chunks[0].raw_text)

        self.assertTrue(chunks[1].is_multiple_choice)
        self.assertIn("Đường gấp khúc ABCD", chunks[1].raw_text)
        self.assertIn("D. 53 dm", chunks[1].raw_text)

        self.assertFalse(chunks[2].is_multiple_choice)
        self.assertIn("Bài 3:", chunks[2].raw_text)

        self.assertTrue(chunks[3].is_multiple_choice)
        self.assertIn("Một hình chữ nhật lớn", chunks[3].raw_text)
        self.assertIn("D. 5 hình tứ giác", chunks[3].raw_text)

        # Kiểm tra cờ trắc nghiệm tự động ép kiểu kết quả từ model nếu model trả nhầm FILL_IN
        wrong_model_item = QuestionItem(
            bankId=1,
            type=QuestionType.FILL_IN,
            content=chunks[0].raw_text,
            difficulty=Difficulty.EASY,
            answer="B. 50 viên bi",
        )
        corrected = LLMParserService._apply_explicit_question_type(chunks[0], wrong_model_item)
        self.assertEqual(corrected.type, QuestionType.MULTIPLE_CHOICE)


if __name__ == "__main__":
    unittest.main()
