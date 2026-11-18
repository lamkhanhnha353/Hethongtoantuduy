import tempfile
import unittest
from pathlib import Path
from unittest.mock import Mock, patch

from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker
from sqlalchemy.pool import StaticPool

from app.config import settings
from app.db.database import Base
from app.db.models import KnowledgeDocument
from app.services import document_service
from app.services.document_service import (
    DocumentNotFoundError,
    DocumentValidationError,
)


class DocumentServiceTests(unittest.TestCase):
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

    def test_upsert_list_update_chunks_and_delete(self):
        ingest_mock = Mock(return_value={"total_chunks_ingested": 2})
        update_metadata_mock = Mock(return_value=2)
        chunks_mock = Mock(
            return_value=[
                {"id": "doc-1_chunk_0", "content": "đoạn 1", "chunkIndex": 0, "title": "Học phí"},
                {"id": "doc-1_chunk_1", "content": "đoạn 2", "chunkIndex": 1, "title": "Học phí"},
            ]
        )
        delete_mock = Mock(return_value=2)

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
            created = document_service.upsert_document_from_text(
                doc_id="doc-1",
                title="Học phí",
                content="Học phí khóa Soroban là 2.000.000đ.",
                description="Bảng học phí",
                category="Học phí",
                created_by="admin-1",
            )

            self.assertEqual(created["docId"], "doc-1")
            self.assertEqual(created["status"], "ready")
            self.assertEqual(created["chunkCount"], 2)
            self.assertEqual(created["createdBy"], "admin-1")
            ingest_mock.assert_called_once()

            listing = document_service.list_documents(search="học phí")
            self.assertEqual(listing["total"], 1)
            self.assertEqual(listing["items"][0]["docId"], "doc-1")

            updated = document_service.update_document(
                "doc-1",
                title="Học phí mới",
                category="Tài chính",
            )
            self.assertEqual(updated["title"], "Học phí mới")
            self.assertEqual(updated["category"], "Tài chính")
            update_metadata_mock.assert_called_once_with("doc-1", "Học phí mới")

            cleared = document_service.update_document("doc-1", description="")
            self.assertIsNone(cleared["description"])

            chunks = document_service.get_document_chunks("doc-1")
            self.assertEqual(len(chunks["items"]), 2)
            chunks_mock.assert_called_once_with("doc-1")

            document_service.delete_document("doc-1")
            delete_mock.assert_called_once_with("doc-1")

        with self.assertRaises(DocumentNotFoundError):
            document_service.get_document("doc-1")

    def test_ingest_failure_marks_document_failed(self):
        with (
            patch.object(
                document_service.rag_service,
                "ingest_text_document",
                Mock(side_effect=RuntimeError("ollama down")),
            ),
            patch.object(
                document_service.rag_service,
                "count_document_chunks",
                Mock(return_value=0),
            ),
        ):
            with self.assertRaises(RuntimeError):
                document_service.upsert_document_from_text(
                    doc_id="doc-fail",
                    title="Lỗi",
                    content="Nội dung",
                )

        document = document_service.get_document("doc-fail")
        self.assertEqual(document["status"], "failed")
        self.assertIn("ollama down", document["errorMessage"])

    def test_upload_text_document_saves_file(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            with (
                patch.object(settings, "DOCUMENTS_DIR", temp_dir),
                patch.object(
                    document_service.rag_service,
                    "ingest_text_document",
                    Mock(return_value={"total_chunks_ingested": 1}),
                ),
            ):
                document = document_service.upload_document(
                    filename="tai-lieu.txt",
                    data="Nội dung tài liệu".encode("utf-8"),
                    title="Tài liệu",
                    created_by="admin-1",
                )

                self.assertEqual(document["sourceType"], "file")
                self.assertEqual(document["fileName"], "tai-lieu.txt")
                self.assertEqual(document["status"], "ready")

                file_path = Path(temp_dir) / document["docId"] / "tai-lieu.txt"
                self.assertTrue(file_path.exists())
                self.assertEqual(file_path.read_text(encoding="utf-8"), "Nội dung tài liệu")

    def test_upload_rejects_unsupported_extension(self):
        with self.assertRaises(DocumentValidationError):
            document_service.upload_document(
                filename="tai-lieu.exe",
                data=b"binary",
            )

    def test_backfill_from_chroma(self):
        with patch.object(
            document_service.rag_service,
            "list_document_metadata",
            Mock(
                return_value=[
                    {"doc_id": "doc-old", "title": "Tài liệu cũ", "chunk_count": 3}
                ]
            ),
        ):
            result = document_service.backfill_documents_from_chroma(
                created_by="admin-1",
            )

        self.assertEqual(result["scanned"], 1)
        self.assertEqual(result["created"], 1)

        document = document_service.get_document("doc-old")
        self.assertEqual(document["sourceType"], "chroma")
        self.assertEqual(document["status"], "ready")
        self.assertEqual(document["chunkCount"], 3)

    def test_reingest_requires_source_content(self):
        db = self.Session()
        try:
            db.add(
                KnowledgeDocument(
                    doc_id="doc-empty",
                    title="Rỗng",
                    source_type="chroma",
                    status="ready",
                )
            )
            db.commit()
        finally:
            db.close()

        with self.assertRaises(DocumentValidationError):
            document_service.reingest_document("doc-empty")

    def test_validation_errors(self):
        with self.assertRaises(DocumentValidationError):
            document_service.upsert_document_from_text(
                doc_id="doc 1",
                title="Tiêu đề",
                content="Nội dung",
            )

        with self.assertRaises(DocumentValidationError):
            document_service.upsert_document_from_text(
                doc_id="doc-1",
                title="Tiêu đề",
                content="   ",
            )


if __name__ == "__main__":
    unittest.main()
