import unittest
from unittest.mock import patch

from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker
from sqlalchemy.pool import StaticPool

from app.db.database import Base
from app.services import conversation_service as service


class ConversationServiceTests(unittest.TestCase):
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

    def _create_with_messages(self, account_id="acc-1"):
        conversation = service.create_conversation(account_id)
        user = service.append_message(
            account_id,
            conversation["id"],
            "user",
            "Học phí khóa Soroban?",
            10,
        )
        assistant = service.append_message(
            account_id,
            conversation["id"],
            "assistant",
            "Học phí khóa Soroban là 2.000.000đ.",
            20,
            [{"docId": "doc-1", "title": "Học phí", "chunkIndex": 0}],
        )
        return conversation, user, assistant

    def test_create_append_list_and_load_messages(self):
        conversation, user, assistant = self._create_with_messages()

        self.assertEqual(user["conversationTitle"], "Học phí khóa Soroban?")
        self.assertEqual(assistant["message"]["role"], "assistant")
        self.assertEqual(
            assistant["message"]["sources"],
            [{"docId": "doc-1", "title": "Học phí", "chunkIndex": 0}],
        )

        listing = service.list_conversations("acc-1")
        self.assertEqual(listing["total"], 1)
        self.assertEqual(listing["items"][0]["id"], conversation["id"])
        self.assertEqual(listing["items"][0]["messageCount"], 2)
        self.assertEqual(
            listing["items"][0]["lastMessage"],
            "Học phí khóa Soroban là 2.000.000đ.",
        )

        messages = service.get_messages("acc-1", conversation["id"])
        self.assertEqual([item["role"] for item in messages["items"]], ["user", "assistant"])
        self.assertEqual(messages["total"], 2)

        context = service.get_context_messages(
            "acc-1",
            conversation["id"],
            exclude_message_id=user["message"]["id"],
        )
        self.assertEqual(len(context), 1)
        self.assertEqual(context[0]["role"], "assistant")

    def test_summary_and_rename(self):
        conversation, user, _ = self._create_with_messages()
        service.update_summary(
            "acc-1",
            conversation["id"],
            "Người dùng hỏi học phí Soroban.",
            user["message"]["id"],
        )
        state = service.get_conversation_state("acc-1", conversation["id"])
        self.assertEqual(state["summary"], "Người dùng hỏi học phí Soroban.")
        self.assertEqual(state["summaryUntilMessageId"], user["message"]["id"])

        renamed = service.rename_conversation("acc-1", conversation["id"], "Học phí")
        self.assertEqual(renamed["title"], "Học phí")

    def test_other_account_cannot_access_conversation(self):
        conversation, _, _ = self._create_with_messages()

        self.assertIsNone(service.get_conversation_state("acc-2", conversation["id"]))
        self.assertFalse(service.conversation_exists("acc-2", conversation["id"]))
        with self.assertRaises(service.ConversationNotFoundError):
            service.get_messages("acc-2", conversation["id"])

    def test_delete_conversation_removes_messages(self):
        conversation, _, _ = self._create_with_messages()
        service.delete_conversation("acc-1", conversation["id"])

        self.assertFalse(service.conversation_exists("acc-1", conversation["id"]))
        db = self.Session()
        try:
            remaining = (
                db.query(service.AIMessage)
                .filter(service.AIMessage.conversation_id == conversation["id"])
                .count()
            )
        finally:
            db.close()
        self.assertEqual(remaining, 0)
        with self.assertRaises(service.ConversationNotFoundError):
            service.get_messages("acc-1", conversation["id"])


if __name__ == "__main__":
    unittest.main()
