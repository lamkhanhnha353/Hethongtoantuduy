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
}

OLLAMA_PROVIDER_NAMES = {"ollama", "local"}
OPENAI_COMPATIBLE_PROVIDER_NAMES = {"groq", "openai", "openai_compatible"}


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
        image_base64: str | None = None,
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
        image_base64: str | None = None,
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
        api_keys: list[str] | str,
        timeout: float,
        max_retries: int,
    ):
        self.name = name
        self.base_url = base_url
        self.timeout = timeout
        self.max_retries = max_retries

        if isinstance(api_keys, str):
            raw_keys = [k.strip() for k in api_keys.split(",") if k.strip()]
        else:
            raw_keys = [k.strip() for k in api_keys if k and k.strip()]

        self.api_keys = raw_keys if raw_keys else [""]
        self._current_index = 0
        self._clients: dict[int, AsyncOpenAI] = {}

    def _get_current_client(self) -> tuple[AsyncOpenAI, str]:
        index = self._current_index % len(self.api_keys)
        if index not in self._clients:
            self._clients[index] = AsyncOpenAI(
                api_key=self.api_keys[index],
                base_url=self.base_url,
                timeout=self.timeout,
                max_retries=self.max_retries,
            )
        return self._clients[index], self.api_keys[index]

    def _rotate_to_next_key(self) -> str:
        self._current_index = (self._current_index + 1) % len(self.api_keys)
        next_key = self.api_keys[self._current_index]
        logger.warning(
            "[%s] Tự động chuyển sang key dự phòng (vị trí %s/%s, đuôi ...%s)",
            self.name,
            self._current_index + 1,
            len(self.api_keys),
            next_key[-6:] if len(next_key) >= 6 else next_key,
        )
        return next_key

    async def chat(
        self,
        messages: Sequence[LLMMessage],
        model: str,
        temperature: float,
        max_tokens: int,
    ) -> str:
        total_keys = len(self.api_keys)
        last_error = None

        for attempt in range(total_keys):
            client, key = self._get_current_client()
            try:
                response = await client.chat.completions.create(
                    model=model,
                    messages=list(messages),
                    temperature=temperature,
                    max_completion_tokens=max_tokens,
                    stream=False,
                )
                return _openai_content(response)
            except Exception as exc:
                last_error = exc
                logger.warning(
                    "[%s] Key đuôi ...%s gặp lỗi: %s",
                    self.name,
                    key[-6:] if len(key) >= 6 else key,
                    exc,
                )
                if attempt < total_keys - 1:
                    self._rotate_to_next_key()
                else:
                    raise last_error

        if last_error:
            raise last_error
        return ""

    async def chat_stream(
        self,
        messages: Sequence[LLMMessage],
        model: str,
        temperature: float,
        max_tokens: int,
        image_base64: str | None = None,
    ) -> AsyncIterator[str]:
        total_keys = len(self.api_keys)
        last_error = None
        yielded_any = False

        for attempt in range(total_keys):
            client, key = self._get_current_client()
            try:
                formatted_messages = list(messages)
                if image_base64 and image_base64.strip() and formatted_messages:
                    clean_b64 = image_base64.strip()
                    data_url = clean_b64 if clean_b64.startswith("data:") else f"data:image/jpeg;base64,{clean_b64}"
                    last_msg = dict(formatted_messages[-1])
                    if last_msg.get("role") == "user":
                        last_msg["content"] = [
                            {"type": "text", "text": str(last_msg.get("content", ""))},
                            {"type": "image_url", "image_url": {"url": data_url}},
                        ]
                        formatted_messages[-1] = last_msg

                stream = await client.chat.completions.create(
                    model=model,
                    messages=formatted_messages,
                    temperature=temperature,
                    max_completion_tokens=max_tokens,
                    stream=True,
                )
                async for chunk in stream:
                    content = _openai_delta(chunk)
                    if content:
                        yielded_any = True
                        yield content
                return
            except Exception as exc:
                last_error = exc
                logger.warning(
                    "[%s] Stream với key đuôi ...%s gặp lỗi: %s",
                    self.name,
                    key[-6:] if len(key) >= 6 else key,
                    exc,
                )
                if not yielded_any and attempt < total_keys - 1:
                    self._rotate_to_next_key()
                else:
                    raise last_error

        if last_error:
            raise last_error

    async def health(self) -> bool:
        try:
            client, _ = self._get_current_client()
            await client.models.list()
            return True
        except Exception:
            return False

    async def close(self) -> None:
        for client in self._clients.values():
            try:
                await client.close()
            except Exception:
                pass
        self._clients.clear()


class GoogleGenAIProvider:
    name = "gemini"

    def __init__(
        self,
        *,
        api_keys: list[str] | str,
        timeout: float = 60.0,
    ):
        try:
            from google import genai
            self._genai = genai
        except ImportError as exc:
            raise LLMProviderError("Chưa cài thư viện google-genai. Chạy: pip install google-genai") from exc

        if isinstance(api_keys, str):
            raw_keys = [k.strip() for k in api_keys.split(",") if k.strip()]
        else:
            raw_keys = [k.strip() for k in api_keys if k and k.strip()]

        self.api_keys = raw_keys if raw_keys else [""]
        self._current_index = 0
        self.timeout = timeout
        self._clients: dict[int, Any] = {}

    def _get_current_client(self) -> tuple[Any, str]:
        index = self._current_index % len(self.api_keys)
        if index not in self._clients:
            self._clients[index] = self._genai.Client(api_key=self.api_keys[index])
        return self._clients[index], self.api_keys[index]

    def _rotate_to_next_key(self) -> str:
        self._current_index = (self._current_index + 1) % len(self.api_keys)
        next_key = self.api_keys[self._current_index]
        logger.warning(
            "[gemini] Tự động đổi sang key tiếp theo (vị trí %s/%s, đuôi ...%s)",
            self._current_index + 1,
            len(self.api_keys),
            next_key[-6:] if len(next_key) >= 6 else next_key,
        )
        return next_key

    @staticmethod
    def _prepare_contents_and_config(
        messages: Sequence[LLMMessage],
        temperature: float,
        max_tokens: int,
        image_base64: str | None = None,
    ):
        contents = []
        system_instruction = None
        for m in messages:
            role = m.get("role", "user")
            content = (m.get("content") or "").strip()
            if not content:
                continue
            if role == "system":
                system_instruction = content
            elif role == "assistant":
                contents.append({"role": "model", "parts": [{"text": content}]})
            else:
                contents.append({"role": "user", "parts": [{"text": content}]})

        if not contents:
            contents = [{"role": "user", "parts": [{"text": "Xin chào"}]}]

        if image_base64 and image_base64.strip():
            try:
                import base64
                from google.genai import types

                clean_b64 = image_base64.strip()
                mime_type = "image/jpeg"
                if "," in clean_b64:
                    header, body = clean_b64.split(",", 1)
                    clean_b64 = body
                    if "image/png" in header:
                        mime_type = "image/png"
                    elif "image/webp" in header:
                        mime_type = "image/webp"

                img_bytes = base64.b64decode(clean_b64)
                img_part = types.Part.from_bytes(data=img_bytes, mime_type=mime_type)

                attached = False
                for c in reversed(contents):
                    if c.get("role") == "user":
                        c["parts"].insert(0, img_part)
                        attached = True
                        break
                if not attached:
                    contents.append({
                        "role": "user",
                        "parts": [
                            img_part,
                            {"text": "Hãy quan sát hình ảnh bài toán này và giải chi tiết cho học sinh."},
                        ],
                    })
            except Exception as e:
                logger.warning("Không thể đính kèm ảnh base64 (%s), bỏ qua ảnh.", e)

        config = {
            "temperature": temperature,
            "max_output_tokens": max_tokens,
        }
        if system_instruction:
            config["system_instruction"] = system_instruction
        return contents, config

    async def chat(
        self,
        messages: Sequence[LLMMessage],
        model: str,
        temperature: float,
        max_tokens: int,
    ) -> str:
        total_keys = len(self.api_keys)
        last_error = None
        contents, config = self._prepare_contents_and_config(
            messages, temperature, max_tokens, image_base64=image_base64
        )

        for attempt in range(total_keys):
            client, key = self._get_current_client()
            try:
                response = await client.aio.models.generate_content(
                    model=model,
                    contents=contents,
                    config=config,
                )
                return (response.text or "").strip()
            except Exception as exc:
                last_error = exc
                logger.warning(
                    "[gemini] Key đuôi ...%s gặp lỗi: %s",
                    key[-6:] if len(key) >= 6 else key,
                    exc,
                )
                if attempt < total_keys - 1:
                    self._rotate_to_next_key()
                else:
                    raise last_error

        if last_error:
            raise last_error
        return ""

    async def chat_stream(
        self,
        messages: Sequence[LLMMessage],
        model: str,
        temperature: float,
        max_tokens: int,
        image_base64: str | None = None,
    ) -> AsyncIterator[str]:
        total_keys = len(self.api_keys)
        last_error = None
        contents, config = self._prepare_contents_and_config(
            messages, temperature, max_tokens, image_base64=image_base64
        )
        yielded_any = False

        for attempt in range(total_keys):
            client, key = self._get_current_client()
            try:
                stream = await client.aio.models.generate_content_stream(
                    model=model,
                    contents=contents,
                    config=config,
                )
                async for chunk in stream:
                    text = chunk.text
                    if text:
                        yielded_any = True
                        yield text
                return
            except Exception as exc:
                last_error = exc
                logger.warning(
                    "[gemini] Stream với key đuôi ...%s gặp lỗi: %s",
                    key[-6:] if len(key) >= 6 else key,
                    exc,
                )
                if not yielded_any and attempt < total_keys - 1:
                    self._rotate_to_next_key()
                else:
                    raise last_error

        if last_error:
            raise last_error

    async def health(self) -> bool:
        return bool(self.api_keys and self.api_keys[0])

    async def close(self) -> None:
        for client in self._clients.values():
            try:
                await client.aio.aclose()
            except Exception:
                pass
        self._clients.clear()


def should_fallback(exc: Exception) -> bool:
    # Cho phép fallback trong mọi tình huống lỗi của provider chính để không làm gián đoạn người dùng
    return True


def _build_provider(name: str) -> LLMProvider:
    # ollama provider
    if name in OLLAMA_PROVIDER_NAMES:
        return OllamaProvider()

    if name in {"gemini", "google", "genai"}:
        if not settings.LLM_API_KEY.strip():
            raise LLMProviderError(
                "Thiếu LLM_API_KEY để gọi provider 'gemini'. Cấu hình trong .env."
            )
        return GoogleGenAIProvider(
            api_keys=settings.LLM_API_KEY,
            timeout=settings.LLM_TIMEOUT_SECONDS,
        )

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
            api_keys=settings.LLM_API_KEY,
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

    # Nếu có cấu hình dự phòng Groq và provider chính không phải Groq
    if settings.GROQ_API_KEY.strip() and settings.llm_provider_name != "groq":
        return OpenAICompatibleProvider(
            name="groq",
            base_url=settings.GROQ_BASE_URL,
            api_keys=settings.GROQ_API_KEY,
            timeout=settings.LLM_TIMEOUT_SECONDS,
            max_retries=settings.LLM_MAX_RETRIES,
        )

    if settings.llm_provider_name in OLLAMA_PROVIDER_NAMES:
        return None
    return OllamaProvider()
