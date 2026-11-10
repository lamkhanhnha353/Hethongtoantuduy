import logging

import chromadb

from app.config import settings
from app.services.embedding_provider import (
    EmbeddingProvider,
    create_embedding_provider,
)

logger = logging.getLogger(__name__)


class RAGService:
    def __init__(self):
        self.chroma_client = chromadb.PersistentClient(
            path=settings.CHROMA_PERSIST_DIRECTORY,
            settings=chromadb.Settings(anonymized_telemetry=False),
        )
        self.collection_name = "knowledge_base"
        self.collection = self.chroma_client.get_or_create_collection(
            name=self.collection_name
        )
        self._embedding_provider: EmbeddingProvider | None = None

    @property
    def embedding_provider(self) -> EmbeddingProvider:
        if self._embedding_provider is None:
            self._embedding_provider = create_embedding_provider()
        return self._embedding_provider

    def close(self) -> None:
        if self._embedding_provider is not None:
            self._embedding_provider.close()
            self._embedding_provider = None

    def simple_chunking(
        self,
        text: str,
        chunk_size: int | None = None,
        overlap: int | None = None,
    ) -> list[str]:
        chunk_size = chunk_size or settings.CHUNK_SIZE
        overlap = overlap if overlap is not None else settings.CHUNK_OVERLAP

        if chunk_size <= 0:
            raise ValueError("chunk_size phải lớn hơn 0")
        if overlap < 0 or overlap >= chunk_size:
            raise ValueError("overlap phải >= 0 và nhỏ hơn chunk_size")

        text = text.strip()
        if not text:
            return []

        chunks: list[str] = []
        step = chunk_size - overlap
        start = 0

        while start < len(text):
            end = min(start + chunk_size, len(text))
            chunk = text[start:end].strip()
            if chunk:
                chunks.append(chunk)
            if end >= len(text):
                break
            start += step

        return chunks

    @staticmethod
    def _with_embedding_prefix(text: str, prefix: str) -> str:
        text = text.strip()
        prefix = prefix.strip()
        return f"{prefix} {text}" if prefix else text

    def _embed(self, text: str, task_type: str) -> list[float]:
        text = text.strip()
        if not text:
            raise ValueError("Nội dung embedding không được để trống")

        provider = self.embedding_provider
        if provider.uses_prefixes:
            prefix = (
                settings.EMBEDDING_QUERY_PREFIX
                if task_type == "search_query"
                else settings.EMBEDDING_DOCUMENT_PREFIX
            )
            text = self._with_embedding_prefix(text, prefix)

        return provider.embed(text, task_type=task_type)

    def _embed_document(self, text: str) -> list[float]:
        return self._embed(text, "search_document")

    def _embed_query(self, text: str) -> list[float]:
        return self._embed(text, "search_query")

    def ingest_text_document(self, doc_id: str, title: str, content: str) -> dict:
        doc_id = doc_id.strip()
        title = title.strip()

        if not doc_id:
            raise ValueError("doc_id không được để trống")
        if not title:
            raise ValueError("title không được để trống")

        chunks = self.simple_chunking(content)
        if not chunks:
            raise ValueError("Nội dung tài liệu không được để trống")

        documents: list[str] = []
        embeddings: list[list[float]] = []
        metadatas: list[dict] = []
        ids: list[str] = []

        for idx, chunk in enumerate(chunks):
            vector = self._embed_document(chunk)
            chunk_id = f"{doc_id}_chunk_{idx}"
            documents.append(chunk)
            embeddings.append(vector)
            metadatas.append(
                {"doc_id": doc_id, "title": title, "chunk_index": idx}
            )
            ids.append(chunk_id)

        existing = self.collection.get(where={"doc_id": doc_id})
        existing_ids = set(existing.get("ids") or [])

        self.collection.upsert(
            ids=ids,
            documents=documents,
            embeddings=embeddings,
            metadatas=metadatas,
        )

        stale_ids = existing_ids - set(ids)
        if stale_ids:
            self.collection.delete(ids=list(stale_ids))

        return {
            "status": "success",
            "doc_id": doc_id,
            "title": title,
            "total_chunks_ingested": len(chunks),
            "deleted_stale_chunks": len(stale_ids),
        }

    def reindex_embeddings(self) -> dict:
        data = self.collection.get(include=["documents"])
        ids = data.get("ids") or []
        documents = data.get("documents") or []

        if len(ids) != len(documents):
            raise ValueError("Số lượng id và document trong collection không khớp")
        if not ids:
            return {
                "status": "success",
                "collection": self.collection_name,
                "reindexed_chunks": 0,
                "embedding_provider": settings.embedding_provider_name,
                "embedding_model": settings.embedding_model,
                "document_prefix": settings.EMBEDDING_DOCUMENT_PREFIX,
            }

        embeddings: list[list[float]] = []
        for chunk_id, document in zip(ids, documents):
            if not document or not document.strip():
                raise ValueError(f"Chunk {chunk_id} không có nội dung để reindex")
            embeddings.append(self._embed_document(document))

        self.collection.update(ids=ids, embeddings=embeddings)
        logger.info(
            "Đã reindex %s chunk bằng provider %s, model %s",
            len(ids),
            settings.embedding_provider_name,
            settings.embedding_model,
        )

        return {
            "status": "success",
            "collection": self.collection_name,
            "reindexed_chunks": len(ids),
            "embedding_provider": settings.embedding_provider_name,
            "embedding_model": settings.embedding_model,
            "document_prefix": settings.EMBEDDING_DOCUMENT_PREFIX,
        }

    def get_document_chunks(self, doc_id: str) -> list[dict]:
        data = self.collection.get(
            where={"doc_id": doc_id},
            include=["documents", "metadatas"],
        )
        ids = data.get("ids") or []
        documents = data.get("documents") or []
        metadatas = data.get("metadatas") or []

        chunks: list[dict] = []
        for index, chunk_id in enumerate(ids):
            metadata = metadatas[index] if index < len(metadatas) and metadatas[index] else {}
            document = documents[index] if index < len(documents) else ""
            try:
                chunk_index = int(metadata.get("chunk_index", 0))
            except (TypeError, ValueError):
                chunk_index = 0

            chunks.append(
                {
                    "id": chunk_id,
                    "content": document,
                    "chunkIndex": chunk_index,
                    "title": str(metadata.get("title", "")),
                }
            )

        chunks.sort(key=lambda chunk: chunk["chunkIndex"])
        return chunks

    def count_document_chunks(self, doc_id: str) -> int:
        data = self.collection.get(where={"doc_id": doc_id}, include=["metadatas"])
        return len(data.get("ids") or [])

    def delete_document(self, doc_id: str) -> int:
        data = self.collection.get(where={"doc_id": doc_id}, include=["metadatas"])
        ids = data.get("ids") or []
        if ids:
            self.collection.delete(ids=ids)
        return len(ids)

    def update_document_metadata(self, doc_id: str, title: str) -> int:
        data = self.collection.get(
            where={"doc_id": doc_id},
            include=["metadatas"],
        )
        ids = data.get("ids") or []
        metadatas = data.get("metadatas") or []
        if not ids:
            return 0

        updated_metadatas: list[dict] = []
        for metadata in metadatas:
            next_metadata = dict(metadata or {})
            next_metadata["doc_id"] = doc_id
            next_metadata["title"] = title
            updated_metadatas.append(next_metadata)

        self.collection.update(ids=ids, metadatas=updated_metadatas)
        return len(ids)

    def list_document_metadata(self) -> list[dict]:
        data = self.collection.get(include=["metadatas"])
        ids = data.get("ids") or []
        metadatas = data.get("metadatas") or []

        documents: dict[str, dict] = {}
        for index, chunk_id in enumerate(ids):
            metadata = metadatas[index] if index < len(metadatas) and metadatas[index] else {}
            doc_id = str(metadata.get("doc_id") or "").strip()
            if not doc_id:
                logger.warning("Bỏ qua chunk %s vì thiếu doc_id", chunk_id)
                continue

            entry = documents.setdefault(
                doc_id,
                {
                    "doc_id": doc_id,
                    "title": str(metadata.get("title") or ""),
                    "chunk_count": 0,
                },
            )
            entry["chunk_count"] += 1
            if not entry["title"]:
                entry["title"] = str(metadata.get("title") or "")

        return sorted(documents.values(), key=lambda item: item["doc_id"])

    def query_rag_with_sources(
        self,
        user_query: str,
        n_results: int | None = None,
    ) -> list[dict]:
        user_query = user_query.strip()
        if not user_query:
            return []

        total = self.collection.count()
        if total == 0:
            return []

        n_results = n_results or settings.TOP_K
        n_results = min(max(1, n_results), total)

        query_vector = self._embed_query(user_query)
        results = self.collection.query(
            query_embeddings=[query_vector],
            n_results=n_results,
            include=["documents", "metadatas", "distances"],
        )

        documents = results.get("documents") or []
        if not documents or not documents[0]:
            return []

        metadatas = results.get("metadatas") or []
        distances = results.get("distances") or []
        first_metadatas = metadatas[0] if metadatas and metadatas[0] else []
        first_distances = distances[0] if distances and distances[0] else []

        sources: list[dict] = []
        for index, document in enumerate(documents[0]):
            metadata = (
                first_metadatas[index]
                if index < len(first_metadatas) and first_metadatas[index]
                else {}
            )
            distance = (
                first_distances[index] if index < len(first_distances) else None
            )
            try:
                chunk_index = int(metadata.get("chunk_index", 0))
            except (TypeError, ValueError):
                chunk_index = 0

            sources.append(
                {
                    "text": document,
                    "doc_id": str(metadata.get("doc_id", "")),
                    "title": str(metadata.get("title", "")),
                    "chunk_index": chunk_index,
                    "distance": float(distance) if distance is not None else None,
                }
            )

        return sources

    def query_rag(self, user_query: str, n_results: int | None = None) -> list[str]:
        return [
            source["text"]
            for source in self.query_rag_with_sources(user_query, n_results)
        ]


rag_service = RAGService()
