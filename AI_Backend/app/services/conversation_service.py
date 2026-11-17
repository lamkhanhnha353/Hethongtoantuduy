import json
from datetime import datetime
from typing import Any

from sqlalchemy import func

from app.db.database import SessionLocal
from app.db.models import AIConversation, AIMessage

DEFAULT_TITLE = "Hội thoại mới"
MAX_PAGE_SIZE = 100


class ConversationNotFoundError(Exception):
    pass


def _now() -> datetime:
    return datetime.utcnow()


def _iso(value: datetime | None) -> str | None:
    return value.isoformat() if value else None


def _clean_title(title: str | None) -> str:
    if title is None:
        return DEFAULT_TITLE
    cleaned = title.strip()
    return cleaned[:255] if cleaned else DEFAULT_TITLE


def _serialize_conversation(conversation: AIConversation) -> dict[str, Any]:
    return {
        "id": conversation.id,
        "title": conversation.title,
        "lastMessageAt": _iso(conversation.last_message_at),
        "createdAt": _iso(conversation.created_at),
        "updatedAt": _iso(conversation.updated_at),
    }


def _serialize_message(message: AIMessage) -> dict[str, Any]:
    sources: list[Any] = []
    if message.sources_json:
        try:
            parsed = json.loads(message.sources_json)
            if isinstance(parsed, list):
                sources = parsed
        except json.JSONDecodeError:
            sources = []

    return {
        "id": message.id,
        "conversationId": message.conversation_id,
        "role": message.role,
        "content": message.content,
        "tokenCount": message.token_count,
        "sources": sources,
        "status": message.status,
        "createdAt": _iso(message.created_at),
    }


def _get_conversation(
    db,
    account_id: str,
    conversation_id: int,
) -> AIConversation:
    conversation = (
        db.query(AIConversation)
        .filter(
            AIConversation.id == conversation_id,
            AIConversation.account_id == account_id,
        )
        .first()
    )
    if conversation is None:
        raise ConversationNotFoundError("Không tìm thấy hội thoại")
    return conversation


def conversation_exists(account_id: str, conversation_id: int) -> bool:
    db = SessionLocal()
    try:
        return (
            db.query(AIConversation.id)
            .filter(
                AIConversation.id == conversation_id,
                AIConversation.account_id == account_id,
            )
            .first()
            is not None
        )
    finally:
        db.close()


def get_conversation_state(
    account_id: str,
    conversation_id: int,
) -> dict[str, Any] | None:
    db = SessionLocal()
    try:
        conversation = (
            db.query(AIConversation)
            .filter(
                AIConversation.id == conversation_id,
                AIConversation.account_id == account_id,
            )
            .first()
        )
        if conversation is None:
            return None

        state = _serialize_conversation(conversation)
        state["summary"] = conversation.summary
        state["summaryUntilMessageId"] = conversation.summary_until_message_id
        return state
    finally:
        db.close()


def create_conversation(
    account_id: str,
    title: str | None = None,
) -> dict[str, Any]:
    db = SessionLocal()
    try:
        now = _now()
        conversation = AIConversation(
            account_id=account_id,
            title=_clean_title(title),
            last_message_at=now,
            created_at=now,
            updated_at=now,
        )
        db.add(conversation)
        db.commit()
        db.refresh(conversation)
        return _serialize_conversation(conversation)
    finally:
        db.close()


def list_conversations(
    account_id: str,
    page: int = 1,
    page_size: int = 20,
) -> dict[str, Any]:
    page = max(1, page)
    page_size = max(1, min(page_size, MAX_PAGE_SIZE))
    db = SessionLocal()
    try:
        query = db.query(AIConversation).filter(
            AIConversation.account_id == account_id
        )
        total = query.count()
        conversations = (
            query.order_by(
                AIConversation.last_message_at.desc(),
                AIConversation.id.desc(),
            )
            .offset((page - 1) * page_size)
            .limit(page_size)
            .all()
        )

        items: list[dict[str, Any]] = []
        for conversation in conversations:
            last_message = (
                db.query(AIMessage)
                .filter(AIMessage.conversation_id == conversation.id)
                .order_by(AIMessage.id.desc())
                .first()
            )
            message_count = (
                db.query(func.count(AIMessage.id))
                .filter(AIMessage.conversation_id == conversation.id)
                .scalar()
                or 0
            )
            item = _serialize_conversation(conversation)
            item["lastMessage"] = (
                last_message.content[:200] if last_message else None
            )
            item["messageCount"] = message_count
            items.append(item)

        return {
            "items": items,
            "page": page,
            "pageSize": page_size,
            "total": total,
            "hasMore": page * page_size < total,
        }
    finally:
        db.close()


def get_conversation(account_id: str, conversation_id: int) -> dict[str, Any]:
    db = SessionLocal()
    try:
        conversation = _get_conversation(db, account_id, conversation_id)
        return _serialize_conversation(conversation)
    finally:
        db.close()


def get_messages(
    account_id: str,
    conversation_id: int,
    page: int = 1,
    page_size: int = 30,
) -> dict[str, Any]:
    page = max(1, page)
    page_size = max(1, min(page_size, MAX_PAGE_SIZE))
    db = SessionLocal()
    try:
        _get_conversation(db, account_id, conversation_id)
        query = db.query(AIMessage).filter(
            AIMessage.conversation_id == conversation_id
        )
        total = query.count()
        messages = (
            query.order_by(AIMessage.id.desc())
            .offset((page - 1) * page_size)
            .limit(page_size)
            .all()
        )
        messages.reverse()
        return {
            "items": [_serialize_message(message) for message in messages],
            "page": page,
            "pageSize": page_size,
            "total": total,
            "hasMore": page * page_size < total,
        }
    finally:
        db.close()


def get_context_messages(
    account_id: str,
    conversation_id: int,
    exclude_message_id: int | None = None,
) -> list[dict[str, Any]]:
    db = SessionLocal()
    try:
        _get_conversation(db, account_id, conversation_id)
        query = db.query(AIMessage).filter(
            AIMessage.conversation_id == conversation_id,
            AIMessage.status == "done",
        )
        if exclude_message_id is not None:
            query = query.filter(AIMessage.id != exclude_message_id)

        messages = query.order_by(AIMessage.id.asc()).all()
        return [
            {
                "id": message.id,
                "role": message.role,
                "content": message.content,
                "status": message.status,
            }
            for message in messages
        ]
    finally:
        db.close()


def append_message(
    account_id: str,
    conversation_id: int,
    role: str,
    content: str,
    token_count: int,
    sources: list[Any] | None = None,
    status: str = "done",
) -> dict[str, Any]:
    db = SessionLocal()
    try:
        conversation = _get_conversation(db, account_id, conversation_id)
        now = _now()

        if role == "user":
            user_message_count = (
                db.query(func.count(AIMessage.id))
                .filter(
                    AIMessage.conversation_id == conversation_id,
                    AIMessage.role == "user",
                )
                .scalar()
                or 0
            )
            if user_message_count == 0 and conversation.title == DEFAULT_TITLE:
                title = " ".join(content.split())[:50]
                conversation.title = title or DEFAULT_TITLE

        message = AIMessage(
            conversation_id=conversation_id,
            role=role,
            content=content,
            token_count=token_count,
            sources_json=(
                json.dumps(sources, ensure_ascii=False) if sources else None
            ),
            status=status,
            created_at=now,
        )
        conversation.last_message_at = now
        conversation.updated_at = now
        db.add(message)
        db.commit()
        db.refresh(message)
        db.refresh(conversation)

        return {
            "message": _serialize_message(message),
            "conversationTitle": conversation.title,
        }
    finally:
        db.close()


def update_summary(
    account_id: str,
    conversation_id: int,
    summary: str | None,
    summary_until_message_id: int | None,
) -> dict[str, Any]:
    db = SessionLocal()
    try:
        conversation = _get_conversation(db, account_id, conversation_id)
        conversation.summary = summary
        conversation.summary_until_message_id = summary_until_message_id
        conversation.updated_at = _now()
        db.commit()
        db.refresh(conversation)
        return _serialize_conversation(conversation)
    finally:
        db.close()


def rename_conversation(
    account_id: str,
    conversation_id: int,
    title: str,
) -> dict[str, Any]:
    db = SessionLocal()
    try:
        conversation = _get_conversation(db, account_id, conversation_id)
        conversation.title = _clean_title(title)
        conversation.updated_at = _now()
        db.commit()
        db.refresh(conversation)
        return _serialize_conversation(conversation)
    finally:
        db.close()


def delete_conversation(account_id: str, conversation_id: int) -> None:
    db = SessionLocal()
    try:
        conversation = _get_conversation(db, account_id, conversation_id)
        db.delete(conversation)
        db.commit()
    finally:
        db.close()
