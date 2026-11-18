from fastapi import APIRouter, Depends, HTTPException, Query, status
from pydantic import BaseModel, Field, field_validator

from app.core.security import verify_jwt_token
from app.services import conversation_service
from app.services.conversation_service import ConversationNotFoundError

router = APIRouter(prefix="/api/ai", tags=["AI Conversations"])


class ConversationCreateRequest(BaseModel):
    title: str | None = Field(default=None, max_length=255)

    @field_validator("title")
    @classmethod
    def strip_title(cls, value: str | None) -> str | None:
        if value is None:
            return None
        title = value.strip()
        return title or None


class ConversationRenameRequest(BaseModel):
    title: str = Field(min_length=1, max_length=255)

    @field_validator("title")
    @classmethod
    def strip_title(cls, value: str) -> str:
        title = value.strip()
        if not title:
            raise ValueError("Tên hội thoại không được để trống")
        return title


def _not_found() -> HTTPException:
    return HTTPException(
        status_code=status.HTTP_404_NOT_FOUND,
        detail="Không tìm thấy hội thoại",
    )


@router.get("/conversations")
def list_conversations(
    page: int = Query(default=1, ge=1),
    page_size: int = Query(default=20, ge=1, le=100),
    user: dict = Depends(verify_jwt_token),
):
    return conversation_service.list_conversations(
        account_id=user["accountId"],
        page=page,
        page_size=page_size,
    )


@router.post("/conversations", status_code=status.HTTP_201_CREATED)
def create_conversation(
    request: ConversationCreateRequest | None = None,
    user: dict = Depends(verify_jwt_token),
):
    return conversation_service.create_conversation(
        account_id=user["accountId"],
        title=request.title if request else None,
    )


@router.get("/conversations/{conversation_id}")
def get_conversation(
    conversation_id: int,
    user: dict = Depends(verify_jwt_token),
):
    try:
        return conversation_service.get_conversation(
            account_id=user["accountId"],
            conversation_id=conversation_id,
        )
    except ConversationNotFoundError as exc:
        raise _not_found() from exc


@router.get("/conversations/{conversation_id}/messages")
def get_messages(
    conversation_id: int,
    page: int = Query(default=1, ge=1),
    page_size: int = Query(default=30, ge=1, le=100),
    user: dict = Depends(verify_jwt_token),
):
    try:
        return conversation_service.get_messages(
            account_id=user["accountId"],
            conversation_id=conversation_id,
            page=page,
            page_size=page_size,
        )
    except ConversationNotFoundError as exc:
        raise _not_found() from exc


@router.patch("/conversations/{conversation_id}")
def rename_conversation(
    conversation_id: int,
    request: ConversationRenameRequest,
    user: dict = Depends(verify_jwt_token),
):
    try:
        return conversation_service.rename_conversation(
            account_id=user["accountId"],
            conversation_id=conversation_id,
            title=request.title,
        )
    except ConversationNotFoundError as exc:
        raise _not_found() from exc


@router.delete(
    "/conversations/{conversation_id}",
    status_code=status.HTTP_204_NO_CONTENT,
)
def delete_conversation(
    conversation_id: int,
    user: dict = Depends(verify_jwt_token),
):
    try:
        conversation_service.delete_conversation(
            account_id=user["accountId"],
            conversation_id=conversation_id,
        )
    except ConversationNotFoundError as exc:
        raise _not_found() from exc
    return None
