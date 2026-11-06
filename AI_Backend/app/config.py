from pathlib import Path

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

    OLLAMA_BASE_URL: str = "http://localhost:11434"
    CHAT_MODEL: str = "qwen2.5:3b-instruct"
    EMBEDDING_MODEL: str = "nomic-embed-text"

    CHROMA_PERSIST_DIRECTORY: str = str(STORAGE_DIR / "chroma")
    SQLITE_URL: str = f"sqlite:///{(STORAGE_DIR / 'app.db').as_posix()}"
    DOCUMENTS_DIR: str = str(STORAGE_DIR / "documents")

    CORS_ORIGINS: str = "*"

    TOP_K: int = 4
    CHUNK_SIZE: int = 900
    CHUNK_OVERLAP: int = 150

    @property
    def cors_origin_list(self) -> list[str]:
        origins = [origin.strip() for origin in self.CORS_ORIGINS.split(",") if origin.strip()]
        return origins or ["*"]


settings = Settings()
