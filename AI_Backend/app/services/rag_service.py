import chromadb
import ollama

from app.config import settings


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

    def _embed(self, text: str) -> list[float]:
        response = self.ollama_client.embeddings(
            model=settings.EMBEDDING_MODEL,
            prompt=text,
        )
        return response["embedding"]

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
            vector = self._embed(chunk)
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

    def query_rag(self, user_query: str, n_results: int | None = None) -> list[str]:
        user_query = user_query.strip()
        if not user_query:
            return []

        total = self.collection.count()
        if total == 0:
            return []

        n_results = n_results or settings.TOP_K
        n_results = min(max(1, n_results), total)

        query_vector = self._embed(user_query)
        results = self.collection.query(
            query_embeddings=[query_vector],
            n_results=n_results,
        )

        documents = results.get("documents") or []
        if not documents or not documents[0]:
            return []
        return documents[0]


rag_service = RAGService()
