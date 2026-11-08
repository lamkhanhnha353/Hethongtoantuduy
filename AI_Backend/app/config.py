from pathlib import Path

from pydantic import field_validator
from pydantic_settings import BaseSettings, SettingsConfigDict

BASE_DIR = Path(__file__).resolve().parent.parent
STORAGE_DIR = BASE_DIR / "storage"


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=BASE_DIR / ".env",
        env_file_encoding="utf-8",
        extra="ignore",
    )

    APP_NAME: str = "AI RAG Backend Service"
    APP_HOST: str = "0.0.0.0"
    APP_PORT: int = 8001

    JWT_SECRET_KEY: str = "your_super_secret_key_here_at_least_32_bytes"
    JWT_ALGORITHM: str = "HS256"
    JWT_ISSUER: str = "ToanTuDuy"
    JWT_AUDIENCE: str = "ToanTuDuyClient"

    LLM_PROVIDER: str = "ollama"
    LLM_BASE_URL: str = ""
    LLM_API_KEY: str = ""
    LLM_CHAT_MODEL: str = ""
    LLM_SUMMARY_MODEL: str = ""
    LLM_TIMEOUT_SECONDS: float = 60.0
    LLM_MAX_RETRIES: int = 2
    LLM_MAX_TOKENS: int = 512
    LLM_FALLBACK_ENABLED: bool = True

    OLLAMA_BASE_URL: str = "http://localhost:11434"
    OLLAMA_CHAT_MODEL: str = "qwen2.5:3b-instruct"
    OLLAMA_SUMMARY_MODEL: str = "qwen2.5:3b-instruct"
    OLLAMA_CHAT_KEEP_ALIVE: str = "5m"

    EMBEDDING_PROVIDER: str = "ollama"
    NOMIC_API_KEY: str = ""
    NOMIC_BASE_URL: str = "https://api-atlas.nomic.ai/v1"
    NOMIC_EMBEDDING_MODEL: str = "nomic-embed-text-v1.5"
    NOMIC_EMBEDDING_DIMENSIONS: int = 768
    NOMIC_TIMEOUT_SECONDS: float = 30.0
    NOMIC_MAX_RETRIES: int = 2

    # Legacy aliases, dùng khi chưa cấu hình LLM_CHAT_MODEL/LLM_SUMMARY_MODEL.
    CHAT_MODEL: str = "qwen2.5:3b-instruct"
    SUMMARY_MODEL: str = "qwen2.5:3b-instruct"
    EMBEDDING_MODEL: str = "nomic-embed-text"
    EMBEDDING_QUERY_PREFIX: str = "search_query:"
    EMBEDDING_DOCUMENT_PREFIX: str = "search_document:"

    CHROMA_PERSIST_DIRECTORY: str = str(STORAGE_DIR / "chroma")
    SQLITE_URL: str = f"sqlite:///{(STORAGE_DIR / 'app.db').as_posix()}"
    DOCUMENTS_DIR: str = str(STORAGE_DIR / "documents")
    MAX_DOCUMENT_FILE_SIZE_MB: int = 20

    CORS_ORIGINS: str = "*"

    TOP_K: int = 4
    CHUNK_SIZE: int = 900
    CHUNK_OVERLAP: int = 150

    CHAT_MAX_PROMPT_CHARS: int = 2000
    CHAT_MAX_HISTORY_MESSAGES: int = 20
    CHAT_MAX_HISTORY_MESSAGE_CHARS: int = 4000
    CHAT_MAX_HISTORY_CHARS: int = 12000
    CHAT_NUM_PREDICT: int = 512
    CHAT_TEMPERATURE: float = 0.3

    CONTEXT_TOKEN_BUDGET: int = 6000
    RECENT_TURNS_TO_KEEP: int = 10
    MIN_RECENT_TURNS: int = 2
    TOKEN_ESTIMATE_CHARS_PER_TOKEN: float = 3.0
    MESSAGE_TOKEN_OVERHEAD: int = 4
    SUMMARY_MAX_TOKENS: int = 400

    @field_validator(
        "CONTEXT_TOKEN_BUDGET",
        "RECENT_TURNS_TO_KEEP",
        "MIN_RECENT_TURNS",
        "MESSAGE_TOKEN_OVERHEAD",
        "SUMMARY_MAX_TOKENS",
        "MAX_DOCUMENT_FILE_SIZE_MB",
        "LLM_MAX_TOKENS",
        "NOMIC_EMBEDDING_DIMENSIONS",
        mode="after",
    )
    @classmethod
    def validate_positive_int(cls, value: int) -> int:
        if value < 1:
            raise ValueError("Giá trị cấu hình phải lớn hơn 0")
        return value

    @field_validator("LLM_TIMEOUT_SECONDS", "NOMIC_TIMEOUT_SECONDS", mode="after")
    @classmethod
    def validate_timeout(cls, value: float) -> float:
        if value <= 0:
            raise ValueError("Timeout phải lớn hơn 0")
        return value

    @field_validator("LLM_MAX_RETRIES", "NOMIC_MAX_RETRIES", mode="after")
    @classmethod
    def validate_max_retries(cls, value: int) -> int:
        if value < 0:
            raise ValueError("Số lần retry phải >= 0")
        return value

    @field_validator("TOKEN_ESTIMATE_CHARS_PER_TOKEN", mode="after")
    @classmethod
    def validate_chars_per_token(cls, value: float) -> float:
        if value <= 0:
            raise ValueError("TOKEN_ESTIMATE_CHARS_PER_TOKEN phải lớn hơn 0")
        return value

    @field_validator("CHROMA_PERSIST_DIRECTORY", "DOCUMENTS_DIR", mode="after")
    @classmethod
    def resolve_storage_path(cls, value: str) -> str:
        path = Path(value)
        if not path.is_absolute():
            path = BASE_DIR / path
        return str(path)

    @field_validator("SQLITE_URL", mode="after")
    @classmethod
    def resolve_sqlite_url(cls, value: str) -> str:
        prefix = "sqlite:///"
        if not value.startswith(prefix):
            return value

        db_path = value[len(prefix):]
        if db_path == ":memory:":
            return value

        path = Path(db_path)
        if not path.is_absolute():
            path = BASE_DIR / path
        return prefix + path.as_posix()

    @property
    def cors_origin_list(self) -> list[str]:
        origins = [origin.strip() for origin in self.CORS_ORIGINS.split(",") if origin.strip()]
        return origins or ["*"]

    @property
    def llm_provider_name(self) -> str:
        return (self.LLM_PROVIDER or "ollama").strip().lower()

    @property
    def chat_model(self) -> str:
        if self.llm_provider_name in {"ollama", "local"}:
            return self.OLLAMA_CHAT_MODEL or self.CHAT_MODEL
        return self.LLM_CHAT_MODEL

    @property
    def summary_model(self) -> str:
        if self.llm_provider_name in {"ollama", "local"}:
            return self.OLLAMA_SUMMARY_MODEL or self.SUMMARY_MODEL
        return self.LLM_SUMMARY_MODEL or self.LLM_CHAT_MODEL

    @property
    def embedding_provider_name(self) -> str:
        return (self.EMBEDDING_PROVIDER or "ollama").strip().lower()

    @property
    def embedding_model(self) -> str:
        if self.embedding_provider_name == "nomic":
            return self.NOMIC_EMBEDDING_MODEL
        return self.EMBEDDING_MODEL


settings = Settings()
