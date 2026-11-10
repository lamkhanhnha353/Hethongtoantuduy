import unittest
from types import SimpleNamespace
from unittest.mock import patch

from app.config import settings
from app.services.llm_provider import (
    LLMProviderError,
    OllamaProvider,
    OpenAICompatibleProvider,
    create_fallback_provider,
    create_primary_provider,
    should_fallback,
)


class FakeAsyncStream:
    def __init__(self, chunks):
        self._chunks = iter(chunks)

    def __aiter__(self):
        return self

    async def __anext__(self):
        try:
            return next(self._chunks)
        except StopIteration as exc:
            raise StopAsyncIteration from exc


class FakeCompletions:
    def __init__(self, chunks=None):
        self.chunks = chunks or []
        self.calls: list[dict] = []

    async def create(self, **kwargs):
        self.calls.append(kwargs)
        if kwargs.get("stream"):
            return FakeAsyncStream(self.chunks)
        return SimpleNamespace(
            choices=[
                SimpleNamespace(
                    message=SimpleNamespace(content="Xin chào")
                )
            ]
        )


class FakeOpenAIClient:
    def __init__(self, chunks=None):
        self.chat = SimpleNamespace(
            completions=FakeCompletions(chunks)
        )
        self.closed = False

    async def close(self):
        self.closed = True


class FakeOllamaClient:
    def __init__(self, chunks=None):
        self.chunks = chunks or []
        self.calls: list[dict] = []
        self.closed = False

    async def chat(self, **kwargs):
        self.calls.append(kwargs)
        if kwargs.get("stream"):
            return FakeAsyncStream(self.chunks)
        return {"message": {"content": "Xin chào"}}

    async def close(self):
        self.closed = True


def _delta(content: str):
    return SimpleNamespace(
        choices=[SimpleNamespace(delta=SimpleNamespace(content=content))]
    )


class LLMProviderTests(unittest.IsolatedAsyncioTestCase):
    def test_default_provider_is_ollama(self):
        with patch.object(settings, "LLM_PROVIDER", "ollama"):
            provider = create_primary_provider()
            self.assertEqual(provider.name, "ollama")

    def test_ollama_models_ignore_external_model_settings(self):
        with (
            patch.object(settings, "LLM_PROVIDER", "ollama"),
            patch.object(settings, "LLM_CHAT_MODEL", "groq-chat"),
            patch.object(settings, "LLM_SUMMARY_MODEL", "groq-summary"),
            patch.object(settings, "OLLAMA_CHAT_MODEL", "ollama-chat"),
            patch.object(settings, "OLLAMA_SUMMARY_MODEL", "ollama-summary"),
        ):
            self.assertEqual(settings.chat_model, "ollama-chat")
            self.assertEqual(settings.summary_model, "ollama-summary")

    def test_groq_requires_api_key(self):
        with (
            patch.object(settings, "LLM_PROVIDER", "groq"),
            patch.object(settings, "LLM_API_KEY", ""),
            patch.object(settings, "LLM_CHAT_MODEL", "openai/gpt-oss-120b"),
        ):
            with self.assertRaises(LLMProviderError):
                create_primary_provider()

    async def test_groq_provider_uses_openai_compatible_client(self):
        with (
            patch.object(settings, "LLM_PROVIDER", "groq"),
            patch.object(settings, "LLM_API_KEY", "test-key"),
            patch.object(settings, "LLM_CHAT_MODEL", "openai/gpt-oss-120b"),
            patch.object(settings, "LLM_BASE_URL", ""),
        ):
            provider = create_primary_provider()
            self.assertEqual(provider.name, "groq")
            self.assertIsInstance(provider, OpenAICompatibleProvider)
            await provider.close()

    def test_fallback_is_none_for_ollama(self):
        with patch.object(settings, "LLM_PROVIDER", "ollama"):
            self.assertIsNone(create_fallback_provider())

    def test_fallback_is_none_when_disabled(self):
        with (
            patch.object(settings, "LLM_PROVIDER", "groq"),
            patch.object(settings, "LLM_FALLBACK_ENABLED", False),
        ):
            self.assertIsNone(create_fallback_provider())

    def test_fallback_is_ollama_for_external_provider(self):
        with (
            patch.object(settings, "LLM_PROVIDER", "groq"),
            patch.object(settings, "LLM_FALLBACK_ENABLED", True),
        ):
            provider = create_fallback_provider()
            self.assertIsNotNone(provider)
            self.assertEqual(provider.name, "ollama")

    def test_should_fallback_for_generic_error(self):
        self.assertTrue(should_fallback(RuntimeError("network down")))

    async def test_openai_provider_chat_and_stream(self):
        client = FakeOpenAIClient(chunks=[_delta("Xin "), _delta("chào")])
        provider = OpenAICompatibleProvider.__new__(OpenAICompatibleProvider)
        provider.name = "groq"
        provider._client = client

        answer = await provider.chat(
            messages=[{"role": "user", "content": "hi"}],
            model="openai/gpt-oss-120b",
            temperature=0.3,
            max_tokens=64,
        )
        self.assertEqual(answer, "Xin chào")

        chunks = [
            chunk
            async for chunk in provider.chat_stream(
                messages=[{"role": "user", "content": "hi"}],
                model="openai/gpt-oss-120b",
                temperature=0.3,
                max_tokens=64,
            )
        ]
        self.assertEqual(chunks, ["Xin ", "chào"])

        await provider.close()
        self.assertTrue(client.closed)

    async def test_ollama_provider_chat_and_stream(self):
        client = FakeOllamaClient(
            chunks=[{"message": {"content": "Xin "}}, {"message": {"content": "chào"}}]
        )
        provider = OllamaProvider.__new__(OllamaProvider)
        provider.name = "ollama"
        provider.keep_alive = "5m"
        provider._client = client

        answer = await provider.chat(
            messages=[{"role": "user", "content": "hi"}],
            model="qwen2.5:3b-instruct",
            temperature=0.3,
            max_tokens=64,
        )
        self.assertEqual(answer, "Xin chào")

        chunks = [
            chunk
            async for chunk in provider.chat_stream(
                messages=[{"role": "user", "content": "hi"}],
                model="qwen2.5:3b-instruct",
                temperature=0.3,
                max_tokens=64,
            )
        ]
        self.assertEqual(chunks, ["Xin ", "chào"])

        await provider.close()
        self.assertTrue(client.closed)


if __name__ == "__main__":
    unittest.main()
