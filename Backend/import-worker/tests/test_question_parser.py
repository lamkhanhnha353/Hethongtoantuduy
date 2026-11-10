import json
import sys
import threading
import unittest
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import AsyncMock, patch

sys.path.insert(0, str(Path(__file__).parent.parent))

from app.config import settings
from app.schemas.question import Difficulty, QuestionItem, QuestionType
from app.services.chunker import QuestionChunk
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


if __name__ == "__main__":
    unittest.main()
