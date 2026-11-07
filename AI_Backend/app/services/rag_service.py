import logging

import chromadb
import ollama

from app.config import settings

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
        self.ollama_client = ollama.Client(host=settings.OLLAMA_BASE_URL)

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

    def _embed(self, text: str, prefix: str = "") -> list[float]:
        prompt = self._with_embedding_prefix(text, prefix)
        response = self.ollama_client.embeddings(
            model=settings.EMBEDDING_MODEL,
            prompt=prompt,
        )
        return response["embedding"]

    def _embed_document(self, text: str) -> list[float]:
        return self._embed(text, settings.EMBEDDING_DOCUMENT_PREFIX)

    def _embed_query(self, text: str) -> list[float]:
        return self._embed(text, settings.EMBEDDING_QUERY_PREFIX)

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
                "embedding_model": settings.EMBEDDING_MODEL,
                "document_prefix": settings.EMBEDDING_DOCUMENT_PREFIX,
            }

        embeddings: list[list[float]] = []
        for chunk_id, document in zip(ids, documents):
            if not document or not document.strip():
                raise ValueError(f"Chunk {chunk_id} không có nội dung để reindex")
            embeddings.append(self._embed_document(document))

        self.collection.update(ids=ids, embeddings=embeddings)
        logger.info(
            "Đã reindex %s chunk bằng model %s với prefix %r",
            len(ids),
            settings.EMBEDDING_MODEL,
            settings.EMBEDDING_DOCUMENT_PREFIX,
        )

        return {
            "status": "success",
            "collection": self.collection_name,
            "reindexed_chunks": len(ids),
            "embedding_model": settings.EMBEDDING_MODEL,
            "document_prefix": settings.EMBEDDING_DOCUMENT_PREFIX,
        }

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
