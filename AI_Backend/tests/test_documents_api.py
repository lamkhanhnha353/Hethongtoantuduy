import tempfile
import unittest
from datetime import datetime, timedelta, timezone
from unittest.mock import Mock, patch

import httpx
from jose import jwt
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker
from sqlalchemy.pool import StaticPool

from app.config import settings
from app.db.database import Base
from app.db.models import KnowledgeDocument
from app.main import app
from app.services import document_service


class DocumentApiTests(unittest.IsolatedAsyncioTestCase):
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
        self.session_patch = patch.object(
            document_service,
            "SessionLocal",
            self.Session,
        )
        self.session_patch.start()
        self._clear_tables()

    def tearDown(self):
        self.session_patch.stop()

    def _clear_tables(self):
        db = self.Session()
        try:
            db.query(KnowledgeDocument).delete()
            db.commit()
        finally:
            db.close()

    def _token(self, account_id, role="Admin"):
        payload = {
            "sub": account_id,
            "iss": settings.JWT_ISSUER,
            "aud": settings.JWT_AUDIENCE,
            "exp": datetime.now(timezone.utc) + timedelta(hours=1),
            "role": role,
        }
        return jwt.encode(
            payload,
            settings.JWT_SECRET_KEY,
            algorithm=settings.JWT_ALGORITHM,
        )

    def _headers(self, account_id, role="Admin"):
        return {"Authorization": f"Bearer {self._token(account_id, role)}"}

    async def test_document_crud_and_permissions(self):
        ingest_mock = Mock(return_value={"total_chunks_ingested": 2})
        update_metadata_mock = Mock(return_value=2)
        chunks_mock = Mock(
            return_value=[
                {
                    "id": "doc-1_chunk_0",
                    "content": "Nội dung",
                    "chunkIndex": 0,
                    "title": "Học phí",
                }
            ]
        )
        delete_mock = Mock(return_value=2)

        transport = httpx.ASGITransport(app=app)
        async with httpx.AsyncClient(
            transport=transport,
            base_url="http://test",
        ) as client:
            forbidden = await client.post(
                "/api/ai/documents",
                json={
                    "doc_id": "doc-1",
                    "title": "Học phí",
                    "content": "Nội dung",
                },
                headers=self._headers("acc-teacher", role="Teacher"),
            )
            self.assertEqual(forbidden.status_code, 403)

            with (
                patch.object(
                    document_service.rag_service,
                    "ingest_text_document",
                    ingest_mock,
                ),
                patch.object(
                    document_service.rag_service,
                    "update_document_metadata",
                    update_metadata_mock,
                ),
                patch.object(
                    document_service.rag_service,
                    "get_document_chunks",
                    chunks_mock,
                ),
                patch.object(
                    document_service.rag_service,
                    "delete_document",
                    delete_mock,
                ),
            ):
                created = await client.post(
                    "/api/ai/documents",
                    json={
                        "doc_id": "doc-1",
                        "title": "Học phí",
                        "content": "Học phí khóa Soroban là 2.000.000đ.",
                        "category": "Học phí",
                    },
                    headers=self._headers("acc-admin"),
                )
                self.assertEqual(created.status_code, 201)
                self.assertEqual(created.json()["status"], "ready")
                self.assertEqual(created.json()["chunkCount"], 2)

                listing = await client.get(
                    "/api/ai/documents",
                    headers=self._headers("acc-admin"),
                )
                self.assertEqual(listing.status_code, 200)
                self.assertEqual(listing.json()["total"], 1)

                detail = await client.get(
                    "/api/ai/documents/doc-1",
                    headers=self._headers("acc-admin"),
                )
                self.assertEqual(detail.status_code, 200)
                self.assertEqual(detail.json()["docId"], "doc-1")

                chunks = await client.get(
                    "/api/ai/documents/doc-1/chunks",
                    headers=self._headers("acc-admin"),
                )
                self.assertEqual(chunks.status_code, 200)
                self.assertEqual(len(chunks.json()["items"]), 1)

                updated = await client.patch(
                    "/api/ai/documents/doc-1",
                    json={"title": "Học phí mới", "category": "Tài chính"},
                    headers=self._headers("acc-admin"),
                )
                self.assertEqual(updated.status_code, 200)
                self.assertEqual(updated.json()["title"], "Học phí mới")
                update_metadata_mock.assert_called_once_with("doc-1", "Học phí mới")

                reingested = await client.post(
                    "/api/ai/documents/doc-1/reingest",
                    headers=self._headers("acc-admin"),
                )
                self.assertEqual(reingested.status_code, 200)
                self.assertEqual(ingest_mock.call_count, 2)

                deleted = await client.delete(
                    "/api/ai/documents/doc-1",
                    headers=self._headers("acc-admin"),
                )
                self.assertEqual(deleted.status_code, 204)
                delete_mock.assert_called_once_with("doc-1")

                missing = await client.get(
                    "/api/ai/documents/doc-1",
                    headers=self._headers("acc-admin"),
                )
                self.assertEqual(missing.status_code, 404)

    async def test_upload_document(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            with (
                patch.object(settings, "DOCUMENTS_DIR", temp_dir),
                patch.object(
                    document_service.rag_service,
                    "ingest_text_document",
                    Mock(return_value={"total_chunks_ingested": 1}),
                ),
            ):
                transport = httpx.ASGITransport(app=app)
                async with httpx.AsyncClient(
                    transport=transport,
                    base_url="http://test",
                ) as client:
                    response = await client.post(
                        "/api/ai/documents/upload",
                        data={"title": "Tài liệu"},
                        files={
                            "file": (
                                "tai-lieu.txt",
                                "Nội dung tài liệu".encode("utf-8"),
                                "text/plain",
                            )
                        },
                        headers=self._headers("acc-admin"),
                    )

                self.assertEqual(response.status_code, 201)
                self.assertEqual(response.json()["sourceType"], "file")
                self.assertEqual(response.json()["fileName"], "tai-lieu.txt")

    async def test_backfill_and_stats(self):
        with patch.object(
            document_service.rag_service,
            "list_document_metadata",
            Mock(
                return_value=[
                    {"doc_id": "doc-old", "title": "Tài liệu cũ", "chunk_count": 3}
                ]
            ),
        ):
            transport = httpx.ASGITransport(app=app)
            async with httpx.AsyncClient(
                transport=transport,
                base_url="http://test",
            ) as client:
                backfill = await client.post(
                    "/api/ai/documents/backfill",
                    headers=self._headers("acc-admin"),
                )
                self.assertEqual(backfill.status_code, 200)
                self.assertEqual(backfill.json()["created"], 1)

                stats = await client.get(
                    "/api/ai/documents/stats",
                    headers=self._headers("acc-admin"),
                )
                self.assertEqual(stats.status_code, 200)
                self.assertEqual(stats.json()["totalDocuments"], 1)
                self.assertEqual(stats.json()["totalChunks"], 3)


if __name__ == "__main__":
    unittest.main()
