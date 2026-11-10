import os
import json
from pathlib import Path
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        extra="ignore"
    )

    # Server configuration
    APP_NAME: str = "Question Import Worker"
    APP_HOST: str = "0.0.0.0"
    APP_PORT: int = 8000
    BASE_URL: str = "http://127.0.0.1:8000"

    # LLM Settings
    LLM_PROVIDER: str = "gemini"  # "gemini", "openai", or "mock" (rule-based development mode)
    GEMINI_API_KEYS: str = ""
    GEMINI_API_KEY: str = ""
    GEMINI_MODEL: str = "gemini-3.5-flash-lite"

    OPENAI_API_KEY: str = ""
    OPENAI_MODEL: str = "gpt-4o-mini"

    # Storage paths
    STORAGE_DIR: Path = Path("./storage")
    UPLOAD_DIR: Path = Path("./storage/uploads")
    IMAGE_DIR: Path = Path("./storage/images")

    # Default values
    DEFAULT_BANK_ID: int = 1

    def ensure_directories(self) -> None:
        """Ensure all required runtime storage directories exist."""
        self.STORAGE_DIR.mkdir(parents=True, exist_ok=True)
        self.UPLOAD_DIR.mkdir(parents=True, exist_ok=True)
        self.IMAGE_DIR.mkdir(parents=True, exist_ok=True)

    def get_gemini_api_keys(self) -> list[str]:
        """Read a JSON or delimited key list, retaining legacy single-key support."""
        raw_keys = self.GEMINI_API_KEYS.strip()
        if raw_keys.startswith("["):
            try:
                configured_keys = json.loads(raw_keys)
            except json.JSONDecodeError:
                configured_keys = raw_keys.replace(";", ",").replace("\n", ",").split(",")
        else:
            configured_keys = raw_keys.replace(";", ",").replace("\n", ",").split(",")
        keys = [str(key).strip().strip("\"'") for key in configured_keys if str(key).strip().strip("\"'")]
        if self.GEMINI_API_KEY.strip() and self.GEMINI_API_KEY.strip() not in keys:
            keys.append(self.GEMINI_API_KEY.strip())
        return keys


settings = Settings()
