import logging
from typing import Any, Protocol

import httpx
import ollama

from app.config import settings

logger = logging.getLogger(__name__)


class EmbeddingProviderError(Exception):
    pass


class EmbeddingProvider(Protocol):
    name: str
    model: str
    uses_prefixes: bool

    def embed(
        self,
        text: str,
        *,
        task_type: str = "search_document",
    ) -> list[float]: ...

    def health(self) -> bool: ...

    def close(self) -> None: ...


def _normalize_vector(vector: Any) -> list[float]:
    if not isinstance(vector, list) or not vector:
        raise EmbeddingProviderError(
            "Embedding rỗng hoặc sai định dạng từ provider"
        )
    try:
        return [float(value) for value in vector]
    except (TypeError, ValueError) as exc:
        raise EmbeddingProviderError(
            "Embedding chứa giá trị không phải số"
        ) from exc


class OllamaEmbeddingProvider:
    name = "ollama"
    uses_prefixes = True

    def __init__(
        self,
        model: str | None = None,
        base_url: str | None = None,
    ):
        self.model = model or settings.EMBEDDING_MODEL
        self.base_url = base_url or settings.OLLAMA_BASE_URL
        self._client = ollama.Client(host=self.base_url)

    def embed(
        self,
        text: str,
        *,
        task_type: str = "search_document",
    ) -> list[float]:
        response = self._client.embeddings(
            model=self.model,
            prompt=text,
        )
        if isinstance(response, dict):
            vector = response.get("embedding")
        else:
            vector = getattr(response, "embedding", None)
        return _normalize_vector(vector)

    def health(self) -> bool:
        try:
            response = httpx.get(
                f"{self.base_url}/api/tags",
                timeout=2,
            )
            return response.status_code == 200
        except Exception:
            return False

    def close(self) -> None:
        close = getattr(self._client, "close", None)
        if callable(close):
            close()


class NomicEmbeddingProvider:
    name = "nomic"
    uses_prefixes = False

    def __init__(
        self,
        *,
        api_key: str,
        base_url: str,
        model: str,
        dimensions: int,
        timeout: float,
        max_retries: int,
    ):
        self.api_key = api_key
        self.base_url = base_url.rstrip("/")
        self.model = model
        self.dimensions = dimensions
        self._client = httpx.Client(
            timeout=timeout,
            transport=httpx.HTTPTransport(retries=max_retries),
        )

    def embed(
        self,
        text: str,
        *,
        task_type: str = "search_document",
    ) -> list[float]:
        text = text.strip()
        if not text:
            raise EmbeddingProviderError(
                "Nội dung cần embedding không được để trống"
            )
        if not self.api_key.strip():
            raise EmbeddingProviderError(
                "Thiếu NOMIC_API_KEY để gọi Nomic API"
            )

        try:
            response = self._client.post(
                f"{self.base_url}/embedding/text",
                headers={"Authorization": f"Bearer {self.api_key}"},
                json={
                    "texts": [text],
                    "model": self.model,
                    "task_type": task_type,
                    "long_text_mode": "truncate",
                    "dimensionality": self.dimensions,
                },
            )
            response.raise_for_status()
        except httpx.HTTPStatusError as exc:
            detail = exc.response.text[:300]
            raise EmbeddingProviderError(
                "Nomic API lỗi "
                f"{exc.response.status_code}: {detail}"
            ) from exc
        except httpx.HTTPError as exc:
            raise EmbeddingProviderError(
                f"Không kết nối được Nomic API: {exc}"
            ) from exc

        try:
            data = response.json()
        except ValueError as exc:
            raise EmbeddingProviderError(
                "Nomic API trả về JSON không hợp lệ"
            ) from exc

        embeddings = data.get("embeddings") or []
        if not embeddings:
            raise EmbeddingProviderError(
                "Nomic API không trả về embedding"
            )

        vector = _normalize_vector(embeddings[0])
        if self.dimensions and len(vector) != self.dimensions:
            raise EmbeddingProviderError(
                "Số chiều embedding không khớp: "
                f"nhận {len(vector)}, cấu hình {self.dimensions}"
            )
        return vector

    def health(self) -> bool:
        return bool(self.api_key.strip())

    def close(self) -> None:
        self._client.close()


def create_embedding_provider() -> EmbeddingProvider:
    name = settings.embedding_provider_name

    if name in {"ollama", "local"}:
        return OllamaEmbeddingProvider()

    if name == "nomic":
        if not settings.NOMIC_API_KEY.strip():
            raise EmbeddingProviderError(
                "Thiếu NOMIC_API_KEY. Thêm key vào .env để dùng "
                "Nomic embeddings."
            )
        return NomicEmbeddingProvider(
            api_key=settings.NOMIC_API_KEY,
            base_url=settings.NOMIC_BASE_URL,
            model=settings.NOMIC_EMBEDDING_MODEL,
            dimensions=settings.NOMIC_EMBEDDING_DIMENSIONS,
            timeout=settings.NOMIC_TIMEOUT_SECONDS,
            max_retries=settings.NOMIC_MAX_RETRIES,
        )

    raise EmbeddingProviderError(
        f"EMBEDDING_PROVIDER '{name}' không được hỗ trợ. "
        "Chọn: ollama, nomic."
    )
