import unittest
from unittest.mock import Mock, call

from app.config import settings
from app.services.rag_service import RAGService


class RAGServiceTests(unittest.TestCase):
    def _service(self) -> RAGService:
        service = RAGService.__new__(RAGService)
        service.collection_name = "test_collection"
        service.collection = Mock()
        service._embedding_provider = Mock()
        service._embedding_provider.uses_prefixes = True
        service._embedding_provider.embed.return_value = [0.1, 0.2]
        return service

    def test_embedding_prefix_is_normalized(self):
        self.assertEqual(
            RAGService._with_embedding_prefix("  câu hỏi  ", "  search_query:  "),
            "search_query: câu hỏi",
        )
        self.assertEqual(
            RAGService._with_embedding_prefix("nội dung", ""),
            "nội dung",
        )

    def test_query_and_document_embeddings_use_their_prefixes(self):
        service = self._service()

        self.assertEqual(service._embed_query("học phí"), [0.1, 0.2])
        service._embedding_provider.embed.assert_called_once_with(
            "search_query: học phí",
            task_type="search_query",
        )

        service._embedding_provider.embed.reset_mock()
        self.assertEqual(service._embed_document("học phí"), [0.1, 0.2])
        service._embedding_provider.embed.assert_called_once_with(
            "search_document: học phí",
            task_type="search_document",
        )

    def test_nomic_embeddings_do_not_add_prefixes(self):
        service = self._service()
        service._embedding_provider.uses_prefixes = False

        self.assertEqual(service._embed_query("học phí"), [0.1, 0.2])
        service._embedding_provider.embed.assert_called_once_with(
            "học phí",
            task_type="search_query",
        )

    def test_embed_rejects_empty_text(self):
        service = self._service()

        with self.assertRaises(ValueError):
            service._embed_query("   ")

    def test_ingest_embeds_document_prefix_and_keeps_raw_chunks(self):
        service = self._service()
        service.simple_chunking = Mock(return_value=["đoạn một", "đoạn hai"])
        service._embed_document = Mock(side_effect=[[1.0], [2.0]])
        service.collection.get.return_value = {"ids": []}

        result = service.ingest_text_document("doc-1", "Tiêu đề", "nội dung")

        self.assertEqual(result["total_chunks_ingested"], 2)
        self.assertEqual(
            service._embed_document.call_args_list,
            [call("đoạn một"), call("đoạn hai")],
        )
        service.collection.upsert.assert_called_once_with(
            ids=["doc-1_chunk_0", "doc-1_chunk_1"],
            documents=["đoạn một", "đoạn hai"],
            embeddings=[[1.0], [2.0]],
            metadatas=[
                {"doc_id": "doc-1", "title": "Tiêu đề", "chunk_index": 0},
                {"doc_id": "doc-1", "title": "Tiêu đề", "chunk_index": 1},
            ],
        )

    def test_reindex_embeddings_updates_existing_chunks(self):
        service = self._service()
        service.collection.get.return_value = {
            "ids": ["chunk-1", "chunk-2"],
            "documents": ["nội dung một", "nội dung hai"],
        }
        service._embed_document = Mock(side_effect=[[1.0], [2.0]])

        result = service.reindex_embeddings()

        self.assertEqual(result["reindexed_chunks"], 2)
        self.assertEqual(result["embedding_model"], settings.embedding_model)
        service.collection.update.assert_called_once_with(
            ids=["chunk-1", "chunk-2"],
            embeddings=[[1.0], [2.0]],
        )

    def test_reindex_empty_collection_is_noop(self):
        service = self._service()
        service.collection.get.return_value = {"ids": [], "documents": []}

        result = service.reindex_embeddings()

        self.assertEqual(result["reindexed_chunks"], 0)
        service.collection.update.assert_not_called()

    def test_query_uses_query_embedding(self):
        service = self._service()
        service.collection.count.return_value = 1
        service.collection.query.return_value = {
            "documents": [["nội dung"]],
            "metadatas": [
                [{"doc_id": "doc-1", "title": "Tiêu đề", "chunk_index": 0}]
            ],
            "distances": [[0.1]],
        }
        service._embed_query = Mock(return_value=[0.5])

        sources = service.query_rag_with_sources("câu hỏi", n_results=1)

        service._embed_query.assert_called_once_with("câu hỏi")
        self.assertEqual(sources[0]["doc_id"], "doc-1")
        self.assertEqual(sources[0]["distance"], 0.1)


if __name__ == "__main__":
    unittest.main()
