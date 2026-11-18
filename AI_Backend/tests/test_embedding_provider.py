import unittest
from unittest.mock import patch

import httpx

from app.config import settings
from app.services.embedding_provider import (
    EmbeddingProviderError,
    NomicEmbeddingProvider,
    OllamaEmbeddingProvider,
    create_embedding_provider,
)


class FakeResponse:
    def __init__(self, payload=None, error=None):
        self.payload = payload
        self.error = error

    def raise_for_status(self):
        if self.error is not None:
            raise self.error

    def json(self):
        return self.payload


class FakeClient:
    def __init__(self, response):
        self.response = response
        self.calls = []

    def post(self, url, **kwargs):
        self.calls.append((url, kwargs))
        return self.response

    def close(self):
        pass


def make_provider(response, dimensions=3) -> NomicEmbeddingProvider:
    provider = NomicEmbeddingProvider.__new__(NomicEmbeddingProvider)
    provider.api_key = "nk_test"
    provider.base_url = "https://api-atlas.nomic.ai/v1"
    provider.model = "nomic-embed-text-v1.5"
    provider.dimensions = dimensions
    provider._client = FakeClient(response)
    return provider


class EmbeddingProviderTests(unittest.TestCase):
    def test_provider_flags(self):
        self.assertTrue(OllamaEmbeddingProvider.uses_prefixes)
        self.assertFalse(NomicEmbeddingProvider.uses_prefixes)

    def test_nomic_embed_sends_task_type_and_parses_vector(self):
        response = FakeResponse({"embeddings": [[0.1, 0.2, 0.3]]})
        provider = make_provider(response)

        vector = provider.embed("học phí", task_type="search_query")

        self.assertEqual(vector, [0.1, 0.2, 0.3])
        url, kwargs = provider._client.calls[0]
        self.assertEqual(url, "https://api-atlas.nomic.ai/v1/embedding/text")
        self.assertEqual(
            kwargs["headers"]["Authorization"],
            "Bearer nk_test",
        )
        self.assertEqual(
            kwargs["json"],
            {
                "texts": ["học phí"],
                "model": "nomic-embed-text-v1.5",
                "task_type": "search_query",
                "long_text_mode": "truncate",
                "dimensionality": 3,
            },
        )

    def test_nomic_embed_rejects_dimension_mismatch(self):
        response = FakeResponse({"embeddings": [[0.1, 0.2]]})
        provider = make_provider(response, dimensions=3)

        with self.assertRaises(EmbeddingProviderError):
            provider.embed("học phí", task_type="search_query")

    def test_nomic_embed_rejects_empty_response(self):
        provider = make_provider(FakeResponse({"embeddings": []}))

        with self.assertRaises(EmbeddingProviderError):
            provider.embed("học phí", task_type="search_query")

    def test_nomic_embed_wraps_http_status_error(self):
        request = httpx.Request(
            "POST",
            "https://api-atlas.nomic.ai/v1/embedding/text",
        )
        http_error = httpx.HTTPStatusError(
            "unauthorized",
            request=request,
            response=httpx.Response(
                401,
                request=request,
                text="invalid api key",
            ),
        )
        provider = make_provider(FakeResponse(error=http_error))

        with self.assertRaises(EmbeddingProviderError) as context:
            provider.embed("học phí", task_type="search_query")

        self.assertIn("401", str(context.exception))

    def test_nomic_health_checks_api_key(self):
        provider = make_provider(FakeResponse({}))
        self.assertTrue(provider.health())

        provider.api_key = "   "
        self.assertFalse(provider.health())

    def test_create_nomic_provider_requires_api_key(self):
        with (
            patch.object(settings, "EMBEDDING_PROVIDER", "nomic"),
            patch.object(settings, "NOMIC_API_KEY", ""),
        ):
            with self.assertRaises(EmbeddingProviderError):
                create_embedding_provider()

    def test_create_nomic_provider_with_api_key(self):
        with (
            patch.object(settings, "EMBEDDING_PROVIDER", "nomic"),
            patch.object(settings, "NOMIC_API_KEY", "nk_test"),
        ):
            provider = create_embedding_provider()

        self.assertIsInstance(provider, NomicEmbeddingProvider)
        provider.close()

    def test_create_unknown_provider_raises(self):
        with patch.object(settings, "EMBEDDING_PROVIDER", "groq"):
            with self.assertRaises(EmbeddingProviderError):
                create_embedding_provider()


if __name__ == "__main__":
    unittest.main()
