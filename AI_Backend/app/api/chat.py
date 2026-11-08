import asyncio
import json
import logging
from collections.abc import AsyncIterator
from typing import Any, Literal

from fastapi import APIRouter, Depends, HTTPException, status
from fastapi.concurrency import run_in_threadpool
from fastapi.responses import StreamingResponse
from pydantic import BaseModel, Field, field_validator, model_validator

from app.config import settings
from app.core.security import verify_jwt_token
from app.services import conversation_service
from app.services.chat_service import stream_chat

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/api/ai", tags=["AI Chat"])


class ChatHistoryMessage(BaseModel):
    role: Literal["user", "assistant"]
    content: str = Field(
        min_length=1,
        max_length=settings.CHAT_MAX_HISTORY_MESSAGE_CHARS,
    )

    @field_validator("content")
    @classmethod
    def strip_content(cls, value: str) -> str:
        content = value.strip()
        if not content:
            raise ValueError("Nội dung lịch sử không được để trống")
        return content


class ChatStreamRequest(BaseModel):
    prompt: str = Field(
        min_length=1,
        max_length=settings.CHAT_MAX_PROMPT_CHARS,
    )
    history: list[ChatHistoryMessage] = Field(default_factory=list)
    conversation_id: int | None = Field(default=None, ge=1)

    @field_validator("prompt")
    @classmethod
    def strip_prompt(cls, value: str) -> str:
        prompt = value.strip()
        if not prompt:
            raise ValueError("Câu hỏi không được để trống")
        return prompt

    @model_validator(mode="after")
    def validate_history_length(self) -> "ChatStreamRequest":
        if self.conversation_id is not None:
            return self

        if len(self.history) > settings.CHAT_MAX_HISTORY_MESSAGES:
            raise ValueError(
                "Lịch sử không được vượt quá "
                f"{settings.CHAT_MAX_HISTORY_MESSAGES} message"
            )

        total_chars = sum(len(item.content) for item in self.history)
        if total_chars > settings.CHAT_MAX_HISTORY_CHARS:
            raise ValueError(
                "Tổng độ dài lịch sử không được vượt quá "
                f"{settings.CHAT_MAX_HISTORY_CHARS} ký tự"
            )
        return self


def _sse_event(event: str, data: dict[str, Any]) -> str:
    payload = json.dumps(data, ensure_ascii=False, separators=(",", ":"))
    return f"event: {event}\ndata: {payload}\n\n"


@router.post("/chat/stream")
async def chat_stream(
    request: ChatStreamRequest,
    user: dict = Depends(verify_jwt_token),
):
    if request.conversation_id is not None:
        exists = await run_in_threadpool(
            conversation_service.conversation_exists,
            user["accountId"],
            request.conversation_id,
        )
        if not exists:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail="Không tìm thấy hội thoại",
            )

    async def event_generator() -> AsyncIterator[str]:
        try:
            async for event in stream_chat(
                prompt=request.prompt,
                history=[item.model_dump() for item in request.history],
                account_id=user["accountId"],
                conversation_id=request.conversation_id,
            ):
                yield _sse_event(event["event"], event["data"])
        except asyncio.CancelledError:
            raise
        except Exception:
            logger.exception("Lỗi không mong đợi trong chat stream")
            error_data: dict[str, Any] = {
                "message": "Đã xảy ra lỗi không mong đợi. Vui lòng thử lại."
            }
            if request.conversation_id is not None:
                error_data["conversationId"] = request.conversation_id
            yield _sse_event("error", error_data)

    return StreamingResponse(
        event_generator(),
        media_type="text/event-stream",
        headers={
            "Cache-Control": "no-cache, no-transform",
            "X-Accel-Buffering": "no",
        },
    )
