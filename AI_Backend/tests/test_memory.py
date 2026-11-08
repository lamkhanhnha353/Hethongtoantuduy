import unittest
from unittest.mock import AsyncMock, patch

from app.config import settings
from app.services import memory


class MemoryTests(unittest.IsolatedAsyncioTestCase):
    def test_estimate_tokens(self):
        self.assertEqual(memory.estimate_tokens(""), 0)
        self.assertEqual(memory.estimate_tokens("abc"), 1)
        self.assertEqual(memory.estimate_tokens("a" * 300), 100)

    def test_split_recent_messages_keeps_last_ten_turns(self):
        messages = []
        for index in range(12):
            messages.append(
                {"id": index * 2 + 1, "role": "user", "content": f"u{index}"}
            )
            messages.append(
                {"id": index * 2 + 2, "role": "assistant", "content": f"a{index}"}
            )

        older, recent = memory.split_recent_messages(messages, 10)
        self.assertEqual(len(older), 4)
        self.assertEqual(len(recent), 20)
        self.assertEqual(recent[0]["id"], 5)
        self.assertEqual(recent[-1]["id"], 24)

    async def test_prepare_context_summarizes_old_turns(self):
        messages = []
        for index in range(12):
            messages.append(
                {
                    "id": index * 2 + 1,
                    "role": "user",
                    "content": f"câu hỏi {index} " * 20,
                    "status": "done",
                }
            )
            messages.append(
                {
                    "id": index * 2 + 2,
                    "role": "assistant",
                    "content": f"câu trả lời {index} " * 20,
                    "status": "done",
                }
            )

        summary_mock = AsyncMock(return_value="TÓM TẮT LỊCH SỬ")
        with (
            patch.object(settings, "CONTEXT_TOKEN_BUDGET", 200),
            patch.object(settings, "MIN_RECENT_TURNS", 2),
            patch.object(memory, "summarize_messages", summary_mock),
        ):
            self.assertTrue(
                memory.should_summarize("câu hỏi mới", [], None, messages, None)
            )
            prepared = await memory.prepare_context(
                prompt="câu hỏi mới",
                sources=[],
                summary=None,
                messages=messages,
                summary_until_message_id=None,
            )

        self.assertTrue(prepared.summarized)
        self.assertEqual(prepared.summary, "TÓM TẮT LỊCH SỬ")
        self.assertEqual(prepared.summary_until_message_id, 4)
        self.assertEqual(prepared.messages[0]["role"], "system")
        self.assertEqual(prepared.messages[1]["content"], "Tóm tắt lịch sử hội thoại:\nTÓM TẮT LỊCH SỬ")
        self.assertEqual(prepared.messages[-1], {"role": "user", "content": "câu hỏi mới"})
        summary_mock.assert_awaited_once()

    async def test_prepare_context_keeps_older_messages_when_within_budget(self):
        messages = []
        for index in range(12):
            messages.append(
                {"id": index * 2 + 1, "role": "user", "content": f"u{index}"}
            )
            messages.append(
                {"id": index * 2 + 2, "role": "assistant", "content": f"a{index}"}
            )

        prepared = await memory.prepare_context(
            prompt="câu hỏi mới",
            sources=[],
            summary=None,
            messages=messages,
            summary_until_message_id=None,
        )

        self.assertFalse(prepared.summarized)
        self.assertIsNone(prepared.summary)
        contents = [message["content"] for message in prepared.messages]
        self.assertIn("u0", contents)
        self.assertIn("a0", contents)
        self.assertIn("u11", contents)
        self.assertEqual(prepared.messages[-1], {"role": "user", "content": "câu hỏi mới"})

    async def test_prepare_context_falls_back_when_summary_fails(self):
        messages = [
            {"id": index + 1, "role": "user" if index % 2 == 0 else "assistant", "content": "x" * 100}
            for index in range(24)
        ]

        with (
            patch.object(settings, "CONTEXT_TOKEN_BUDGET", 100),
            patch.object(settings, "MIN_RECENT_TURNS", 2),
            patch.object(
                memory,
                "summarize_messages",
                AsyncMock(side_effect=RuntimeError("ollama down")),
            ),
        ):
            prepared = await memory.prepare_context(
                prompt="câu hỏi mới",
                sources=[],
                summary=None,
                messages=messages,
                summary_until_message_id=None,
            )

        self.assertFalse(prepared.summarized)
        self.assertIsNone(prepared.summary)
        self.assertIsNone(prepared.summary_until_message_id)
        self.assertEqual(prepared.messages[-1], {"role": "user", "content": "câu hỏi mới"})

    def test_prepare_stateless_context_has_no_summary(self):
        history = [
            {"role": "user", "content": "câu hỏi cũ"},
            {"role": "assistant", "content": "câu trả lời cũ"},
        ]
        context = memory.prepare_stateless_context("câu hỏi mới", history, [])
        self.assertIsNone(context.summary)
        self.assertFalse(context.summarized)
        self.assertEqual(context.messages[-2]["content"], "câu trả lời cũ")
        self.assertEqual(context.messages[-1]["content"], "câu hỏi mới")


if __name__ == "__main__":
    unittest.main()
