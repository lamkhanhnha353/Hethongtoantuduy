import unittest
from datetime import datetime, timedelta, timezone
from unittest.mock import patch

import httpx
from jose import jwt
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker
from sqlalchemy.pool import StaticPool

from app.config import settings
from app.db.database import Base
from app.main import app
from app.services import conversation_service as service


class ConversationApiTests(unittest.IsolatedAsyncioTestCase):
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

    def _token(self, account_id):
        payload = {
            "sub": account_id,
            "iss": settings.JWT_ISSUER,
            "aud": settings.JWT_AUDIENCE,
            "exp": datetime.now(timezone.utc) + timedelta(hours=1),
        }
        return jwt.encode(
            payload,
            settings.JWT_SECRET_KEY,
            algorithm=settings.JWT_ALGORITHM,
        )

    def _headers(self, account_id):
        return {"Authorization": f"Bearer {self._token(account_id)}"}

    async def test_conversation_crud_and_ownership(self):
        transport = httpx.ASGITransport(app=app)
        async with httpx.AsyncClient(
            transport=transport,
            base_url="http://test",
        ) as client:
            created = await client.post(
                "/api/ai/conversations",
                json={"title": "Tư vấn"},
                headers=self._headers("acc-1"),
            )
            self.assertEqual(created.status_code, 201)
            conversation = created.json()
            self.assertEqual(conversation["title"], "Tư vấn")

            service.append_message(
                "acc-1",
                conversation["id"],
                "user",
                "Học phí?",
                2,
            )

            listing = await client.get(
                "/api/ai/conversations",
                headers=self._headers("acc-1"),
            )
            self.assertEqual(listing.status_code, 200)
            self.assertEqual(listing.json()["total"], 1)

            messages = await client.get(
                f"/api/ai/conversations/{conversation['id']}/messages",
                headers=self._headers("acc-1"),
            )
            self.assertEqual(messages.status_code, 200)
            self.assertEqual(messages.json()["items"][0]["content"], "Học phí?")

            other_user = await client.get(
                f"/api/ai/conversations/{conversation['id']}",
                headers=self._headers("acc-2"),
            )
            self.assertEqual(other_user.status_code, 404)

            deleted = await client.delete(
                f"/api/ai/conversations/{conversation['id']}",
                headers=self._headers("acc-1"),
            )
            self.assertEqual(deleted.status_code, 204)

            missing = await client.get(
                f"/api/ai/conversations/{conversation['id']}",
                headers=self._headers("acc-1"),
            )
            self.assertEqual(missing.status_code, 404)

            missing_stream = await client.post(
                "/api/ai/chat/stream",
                json={"conversation_id": conversation["id"], "prompt": "Học phí?"},
                headers=self._headers("acc-1"),
            )
            self.assertEqual(missing_stream.status_code, 404)


if __name__ == "__main__":
    unittest.main()
