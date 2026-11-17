import logging
from collections.abc import AsyncIterator, Mapping, Sequence
from typing import Any, Protocol

import httpx
import ollama
from openai import (
    AsyncOpenAI,
    AuthenticationError,
    BadRequestError,
    PermissionDeniedError,
)

from app.config import settings

logger = logging.getLogger(__name__)

LLMMessage = dict[str, str]

DEFAULT_BASE_URLS = {
    "groq": "https://api.groq.com/openai/v1",
    "openai": "https://api.openai.com/v1",
    "gemini": "https://generativelanguage.googleapis.com/v1beta/openai",
}

OLLAMA_PROVIDER_NAMES = {"ollama", "local"}
OPENAI_COMPATIBLE_PROVIDER_NAMES = {"groq", "openai", "gemini", "openai_compatible"}


class LLMProviderError(Exception):
    pass


class LLMProvider(Protocol):
    name: str

    async def chat(
        self,
        messages: Sequence[LLMMessage],
        model: str,
        temperature: float,
        max_tokens: int,
    ) -> str: ...

    def chat_stream(
        self,
        messages: Sequence[LLMMessage],
        model: str,
        temperature: float,
        max_tokens: int,
    ) -> AsyncIterator[str]: ...

    async def health(self) -> bool: ...

    async def close(self) -> None: ...


def _ollama_content(chunk: Any) -> str:
    if isinstance(chunk, Mapping):
        message = chunk.get("message") or {}
        if isinstance(message, Mapping):
            return str(message.get("content") or "")
        return ""

    message = getattr(chunk, "message", None)
    if message is None:
        return ""
    return str(getattr(message, "content", "") or "")


def _openai_content(response: Any) -> str:
    choices = getattr(response, "choices", None) or []
    if not choices:
        return ""
    message = getattr(choices[0], "message", None)
    if message is None:
        return ""
    return str(getattr(message, "content", "") or "").strip()


def _openai_delta(chunk: Any) -> str:
    choices = getattr(chunk, "choices", None) or []
    if not choices:
        return ""
    delta = getattr(choices[0], "delta", None)
    if delta is None:
        return ""
    return str(getattr(delta, "content", "") or "")


class OllamaProvider:
    name = "ollama"

    def __init__(self, base_url: str | None = None, keep_alive: str | None = None):
        self.base_url = base_url or settings.OLLAMA_BASE_URL
        self.keep_alive = keep_alive or settings.OLLAMA_CHAT_KEEP_ALIVE
        self._client: ollama.AsyncClient | None = None

    def _get_client(self) -> ollama.AsyncClient:
        if self._client is None:
            self._client = ollama.AsyncClient(host=self.base_url)
        return self._client

    async def chat(
        self,
        messages: Sequence[LLMMessage],
        model: str,
        temperature: float,
        max_tokens: int,
    ) -> str:
        response = await self._get_client().chat(
            model=model,
            messages=list(messages),
            stream=False,
            keep_alive=self.keep_alive,
            options={
                "temperature": temperature,
                "num_predict": max_tokens,
            },
        )
        return _ollama_content(response).strip()

    async def chat_stream(
        self,
        messages: Sequence[LLMMessage],
        model: str,
        temperature: float,
        max_tokens: int,
    ) -> AsyncIterator[str]:
        stream = await self._get_client().chat(
            model=model,
            messages=list(messages),
            stream=True,
            keep_alive=self.keep_alive,
            options={
                "temperature": temperature,
                "num_predict": max_tokens,
            },
        )
        async for chunk in stream:
            content = _ollama_content(chunk)
            if content:
                yield content

    async def health(self) -> bool:
        try:
            async with httpx.AsyncClient(timeout=2) as client:
                response = await client.get(f"{self.base_url}/api/tags")
            return response.status_code == 200
        except Exception:
            return False

    async def close(self) -> None:
        if self._client is not None:
            try:
                await self._client.close()
            finally:
                self._client = None


class OpenAICompatibleProvider:
    def __init__(
        self,
        *,
        name: str,
        base_url: str,
        api_key: str,
        timeout: float,
        max_retries: int,
    ):
        self.name = name
        self._client = AsyncOpenAI(
            api_key=api_key,
            base_url=base_url,
            timeout=timeout,
            max_retries=max_retries,
        )

    async def chat(
        self,
        messages: Sequence[LLMMessage],
        model: str,
        temperature: float,
        max_tokens: int,
    ) -> str:
        response = await self._client.chat.completions.create(
            model=model,
            messages=list(messages),
            temperature=temperature,
            max_completion_tokens=max_tokens,
            stream=False,
        )
        return _openai_content(response)

    async def chat_stream(
        self,
        messages: Sequence[LLMMessage],
        model: str,
        temperature: float,
        max_tokens: int,
    ) -> AsyncIterator[str]:
        stream = await self._client.chat.completions.create(
            model=model,
            messages=list(messages),
            temperature=temperature,
            max_completion_tokens=max_tokens,
            stream=True,
        )
        async for chunk in stream:
            content = _openai_delta(chunk)
            if content:
                yield content

    async def health(self) -> bool:
        try:
            await self._client.models.list()
            return True
        except Exception:
            return False

    async def close(self) -> None:
        await self._client.close()


def should_fallback(exc: Exception) -> bool:
    if isinstance(
        exc,
        (AuthenticationError, BadRequestError, PermissionDeniedError),
    ):
        return False
    return True


def _build_provider(name: str) -> LLMProvider:
    # ollama provider
    if name in OLLAMA_PROVIDER_NAMES:
        return OllamaProvider()

    if name in OPENAI_COMPATIBLE_PROVIDER_NAMES:
        if not settings.LLM_API_KEY.strip():
            raise LLMProviderError(
                "Thiếu LLM_API_KEY để gọi provider "
                f"'{name}'. Cấu hình trong .env."
            )
        if not settings.LLM_CHAT_MODEL.strip():
            raise LLMProviderError(
                "Thiếu LLM_CHAT_MODEL cho provider "
                f"'{name}'. Cấu hình trong .env."
            )

        base_url = settings.LLM_BASE_URL.strip() or DEFAULT_BASE_URLS.get(name, "")
        if not base_url:
            raise LLMProviderError(
                f"Thiếu LLM_BASE_URL cho provider '{name}'."
            )

        return OpenAICompatibleProvider(
            name=name,
            base_url=base_url,
            api_key=settings.LLM_API_KEY,
            timeout=settings.LLM_TIMEOUT_SECONDS,
            max_retries=settings.LLM_MAX_RETRIES,
        )

    raise LLMProviderError(
        f"LLM_PROVIDER '{name}' không được hỗ trợ. "
        "Chọn: ollama, groq, openai, gemini, openai_compatible."
    )


def create_primary_provider() -> LLMProvider:
    return _build_provider(settings.llm_provider_name)


def create_fallback_provider() -> LLMProvider | None:
    if not settings.LLM_FALLBACK_ENABLED:
        return None
    if settings.llm_provider_name in OLLAMA_PROVIDER_NAMES:
        return None
    return OllamaProvider()
