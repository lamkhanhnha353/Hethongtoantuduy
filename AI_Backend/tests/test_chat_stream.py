import unittest
from unittest.mock import AsyncMock, MagicMock, patch

from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker
from sqlalchemy.pool import StaticPool

from app.config import settings
from app.db.database import Base
from app.services import chat_service, memory
from app.services import conversation_service as service


class FakeLLMProvider:
    def __init__(
        self,
        chunks: list[str] | None = None,
        error: Exception | None = None,
        name: str = "fake",
    ):
        self.chunks = chunks if chunks is not None else ["Xin ", "chào"]
        self.error = error
        self.name = name
        self.closed = False
        self.calls: list[dict] = []

    async def chat(self, **kwargs):
        self.calls.append(kwargs)
        if self.error:
            raise self.error
        return "".join(self.chunks)

    async def chat_stream(self, **kwargs):
        self.calls.append(kwargs)
        if self.error:
            raise self.error
        for chunk in self.chunks:
            yield chunk

    async def health(self) -> bool:
        return True

    async def close(self) -> None:
        self.closed = True


class FailingAfterDeltaProvider(FakeLLMProvider):
    async def chat_stream(self, **kwargs):
        self.calls.append(kwargs)
        yield "Phần"
        raise RuntimeError("stream broke")


class ChatStreamTests(unittest.IsolatedAsyncioTestCase):
    @classmethod
    def setUpClass(cls):
        cls.engine = create_engine(
            "sqlite://",
            connect_args={"check_same_thread": False},
            poolclass=StaticPool,
        )
        Base.metadata.create_all(bind=cls.engine)
        cls.Session = sessionmaker(
            autocommit=False,
            autoflush=False,
            bind=cls.engine,
        )

    def setUp(self):
        self.session_patch = patch.object(service, "SessionLocal", self.Session)
        self.session_patch.start()
        self._clear_tables()

    def tearDown(self):
        self.session_patch.stop()

    def _clear_tables(self):
        db = self.Session()
        try:
            db.query(service.AIMessage).delete()
            db.query(service.AIConversation).delete()
            db.commit()
        finally:
            db.close()

    async def test_stream_with_conversation_persists_messages(self):
        conversation = service.create_conversation("acc-1")
        sources = [
            {
                "doc_id": "doc-1",
                "title": "Học phí",
                "chunk_index": 0,
                "text": "Học phí khóa Soroban là 2.000.000đ.",
            }
        ]

        with (
            patch.object(
                chat_service.rag_service,
                "query_rag_with_sources",
                return_value=sources,
            ),
            patch.object(
                chat_service,
                "create_primary_provider",
                return_value=FakeLLMProvider(name="groq"),
            ),
            patch.object(
                chat_service,
                "create_fallback_provider",
                return_value=None,
            ),
            patch.object(chat_service, "log_chat", MagicMock()),
        ):
            events = [
                event
                async for event in chat_service.stream_chat(
                    prompt="Học phí khóa Soroban?",
                    history=[],
                    account_id="acc-1",
                    conversation_id=conversation["id"],
                )
            ]

        done = [event for event in events if event["event"] == "done"][0]
        self.assertEqual(done["data"]["content"], "Xin chào")
        self.assertEqual(done["data"]["conversationId"], conversation["id"])
        self.assertIsNotNone(done["data"]["messageId"])
        self.assertEqual(done["data"]["title"], "Học phí khóa Soroban?")

        messages = service.get_messages("acc-1", conversation["id"])
        self.assertEqual([item["role"] for item in messages["items"]], ["user", "assistant"])
        self.assertEqual(messages["items"][0]["content"], "Học phí khóa Soroban?")
        self.assertEqual(messages["items"][1]["content"], "Xin chào")
        self.assertEqual(messages["items"][1]["sources"][0]["docId"], "doc-1")

    async def test_stream_summarizes_long_conversation(self):
        conversation = service.create_conversation("acc-1")
        for index in range(12):
            service.append_message(
                "acc-1",
                conversation["id"],
                "user",
                f"câu hỏi {index} " * 20,
                10,
            )
            service.append_message(
                "acc-1",
                conversation["id"],
                "assistant",
                f"câu trả lời {index} " * 20,
                10,
            )

        summary_mock = AsyncMock(return_value="TÓM TẮT LỊCH SỬ")
        with (
            patch.object(settings, "CONTEXT_TOKEN_BUDGET", 100),
            patch.object(settings, "MIN_RECENT_TURNS", 2),
            patch.object(memory, "summarize_messages", summary_mock),
            patch.object(
                chat_service.rag_service,
                "query_rag_with_sources",
                return_value=[],
            ),
            patch.object(
                chat_service,
                "create_primary_provider",
                return_value=FakeLLMProvider(name="groq"),
            ),
            patch.object(
                chat_service,
                "create_fallback_provider",
                return_value=None,
            ),
            patch.object(chat_service, "log_chat", MagicMock()),
        ):
            events = [
                event
                async for event in chat_service.stream_chat(
                    prompt="Câu hỏi mới",
                    history=[],
                    account_id="acc-1",
                    conversation_id=conversation["id"],
                )
            ]

        statuses = [
            event["data"]["stage"]
            for event in events
            if event["event"] == "status"
        ]
        self.assertIn("summarizing", statuses)
        summary_mock.assert_awaited_once()

        state = service.get_conversation_state("acc-1", conversation["id"])
        self.assertEqual(state["summary"], "TÓM TẮT LỊCH SỬ")
        self.assertIsNotNone(state["summaryUntilMessageId"])

        done = [event for event in events if event["event"] == "done"][0]
        self.assertEqual(done["data"]["content"], "Xin chào")

    async def test_stream_falls_back_before_first_delta(self):
        primary = FakeLLMProvider(
            error=RuntimeError("groq down"),
            name="groq",
        )
        fallback = FakeLLMProvider(chunks=["Dự phòng"], name="ollama")

        with (
            patch.object(
                chat_service.rag_service,
                "query_rag_with_sources",
                return_value=[],
            ),
            patch.object(
                chat_service,
                "create_primary_provider",
                return_value=primary,
            ),
            patch.object(
                chat_service,
                "create_fallback_provider",
                return_value=fallback,
            ),
            patch.object(chat_service, "log_chat", MagicMock()),
        ):
            events = [
                event
                async for event in chat_service.stream_chat(
                    prompt="Câu hỏi",
                    history=[],
                    account_id="acc-1",
                )
            ]

        deltas = [
            event["data"]["content"]
            for event in events
            if event["event"] == "delta"
        ]
        self.assertEqual("".join(deltas), "Dự phòng")
        done = [event for event in events if event["event"] == "done"][0]
        self.assertEqual(done["data"]["content"], "Dự phòng")
        self.assertFalse(
            any(event["event"] == "error" for event in events)
        )
        self.assertTrue(primary.closed)
        self.assertTrue(fallback.closed)

    async def test_stream_falls_back_when_primary_returns_no_content(self):
        primary = FakeLLMProvider(chunks=[], name="groq")
        fallback = FakeLLMProvider(chunks=["Dự phòng"], name="ollama")

        with (
            patch.object(
                chat_service.rag_service,
                "query_rag_with_sources",
                return_value=[],
            ),
            patch.object(
                chat_service,
                "create_primary_provider",
                return_value=primary,
            ),
            patch.object(
                chat_service,
                "create_fallback_provider",
                return_value=fallback,
            ),
            patch.object(chat_service, "log_chat", MagicMock()),
        ):
            events = [
                event
                async for event in chat_service.stream_chat(
                    prompt="Câu hỏi",
                    history=[],
                    account_id="acc-1",
                )
            ]

        done = [event for event in events if event["event"] == "done"][0]
        self.assertEqual(done["data"]["content"], "Dự phòng")
        self.assertFalse(
            any(event["event"] == "error" for event in events)
        )

    async def test_stream_does_not_fallback_after_first_delta(self):
        primary = FailingAfterDeltaProvider(name="groq")
        fallback = FakeLLMProvider(chunks=["không dùng"], name="ollama")

        with (
            patch.object(
                chat_service.rag_service,
                "query_rag_with_sources",
                return_value=[],
            ),
            patch.object(
                chat_service,
                "create_primary_provider",
                return_value=primary,
            ),
            patch.object(
                chat_service,
                "create_fallback_provider",
                return_value=fallback,
            ),
            patch.object(chat_service, "log_chat", MagicMock()),
        ):
            events = [
                event
                async for event in chat_service.stream_chat(
                    prompt="Câu hỏi",
                    history=[],
                    account_id="acc-1",
                )
            ]

        deltas = [
            event["data"]["content"]
            for event in events
            if event["event"] == "delta"
        ]
        self.assertEqual("".join(deltas), "Phần")
        self.assertTrue(
            any(event["event"] == "error" for event in events)
        )
        self.assertFalse(
            any(event["event"] == "done" for event in events)
        )
        self.assertEqual(fallback.calls, [])
        self.assertTrue(primary.closed)
        self.assertTrue(fallback.closed)


if __name__ == "__main__":
    unittest.main()
