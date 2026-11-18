import asyncio
import logging
from collections.abc import AsyncIterator
from typing import Any

from fastapi.concurrency import run_in_threadpool

from app.config import settings
from app.db.database import SessionLocal
from app.db.models import AIChatLog
from app.services import conversation_service, memory
from app.services.conversation_service import ConversationNotFoundError
from app.services.llm_provider import (
    create_fallback_provider,
    create_primary_provider,
    should_fallback,
)
from app.services.rag_service import rag_service

logger = logging.getLogger(__name__)

HistoryMessage = dict[str, str]
RAGSource = dict[str, Any]
StreamEvent = dict[str, Any]


def build_messages(
    prompt: str,
    history: list[HistoryMessage] | None,
    sources: list[RAGSource],
) -> list[dict[str, str]]:
    messages: list[dict[str, str]] = [
        {"role": "system", "content": memory.build_system_prompt(sources)}
    ]

    for item in history or []:
        role = item.get("role")
        content = (item.get("content") or "").strip()
        if role in {"user", "assistant"} and content:
            messages.append({"role": role, "content": content})

    messages.append({"role": "user", "content": prompt})
    return messages


def log_chat(account_id: str, prompt: str, response: str) -> None:
    db = SessionLocal()
    try:
        db.add(
            AIChatLog(
                account_id=account_id,
                prompt=prompt,
                response=response,
            )
        )
        db.commit()
    except Exception:
        db.rollback()
        logger.exception("Không thể ghi AIChatLog")
    finally:
        db.close()


def _source_payload(source: RAGSource) -> dict[str, Any]:
    return {
        "docId": source.get("doc_id", ""),
        "title": source.get("title", ""),
        "chunkIndex": source.get("chunk_index", 0),
    }


def _error_event(
    message: str,
    conversation_id: int | None = None,
) -> StreamEvent:
    data: dict[str, Any] = {"message": message}
    if conversation_id is not None:
        data["conversationId"] = conversation_id
    return {"event": "error", "data": data}


async def stream_chat(
    prompt: str,
    history: list[HistoryMessage] | None,
    account_id: str,
    conversation_id: int | None = None,
) -> AsyncIterator[StreamEvent]:
    yield {"event": "status", "data": {"stage": "retrieving"}}

    conversation_state: dict[str, Any] | None = None
    conversation_title: str | None = None
    user_message_id: int | None = None

    if conversation_id is not None:
        try:
            conversation_state = await run_in_threadpool(
                conversation_service.get_conversation_state,
                account_id,
                conversation_id,
            )
        except Exception:
            logger.exception("Không thể tải hội thoại")
            yield _error_event(
                "Không thể tải hội thoại. Vui lòng thử lại.",
                conversation_id,
            )
            return

        if conversation_state is None:
            yield _error_event("Không tìm thấy hội thoại", conversation_id)
            return

        try:
            user_result = await run_in_threadpool(
                conversation_service.append_message,
                account_id,
                conversation_id,
                "user",
                prompt,
                memory.estimate_tokens(prompt),
                None,
                "done",
            )
            user_message_id = user_result["message"]["id"]
            conversation_title = user_result["conversationTitle"]
        except ConversationNotFoundError:
            yield _error_event("Không tìm thấy hội thoại", conversation_id)
            return
        except Exception:
            logger.exception("Không thể lưu tin nhắn người dùng")
            yield _error_event(
                "Không thể lưu tin nhắn. Vui lòng thử lại.",
                conversation_id,
            )
            return

    try:
        sources = await run_in_threadpool(
            rag_service.query_rag_with_sources,
            prompt,
        )
    except Exception:
        logger.exception("Không thể truy vấn tài liệu RAG")
        yield _error_event(
            "Không thể tìm kiếm tài liệu. Vui lòng thử lại.",
            conversation_id,
        )
        return

    yield {
        "event": "sources",
        "data": {
            "sources": [_source_payload(source) for source in sources],
            "sourceChunksUsed": len(sources),
        },
    }

    try:
        if conversation_id is not None:
            context_messages = await run_in_threadpool(
                conversation_service.get_context_messages,
                account_id,
                conversation_id,
                user_message_id,
            )
            if memory.should_summarize(
                prompt,
                sources,
                conversation_state["summary"],
                context_messages,
                conversation_state["summaryUntilMessageId"],
            ):
                yield {"event": "status", "data": {"stage": "summarizing"}}

            prepared = await memory.prepare_context(
                prompt=prompt,
                sources=sources,
                summary=conversation_state["summary"],
                messages=context_messages,
                summary_until_message_id=conversation_state[
                    "summaryUntilMessageId"
                ],
            )

            if prepared.summarized:
                try:
                    await run_in_threadpool(
                        conversation_service.update_summary,
                        account_id,
                        conversation_id,
                        prepared.summary,
                        prepared.summary_until_message_id,
                    )
                except Exception:
                    logger.exception("Không thể lưu tóm tắt hội thoại")
        else:
            prepared = memory.prepare_stateless_context(
                prompt,
                history or [],
                sources,
            )
    except Exception:
        logger.exception("Không thể xây dựng context chat")
        yield _error_event(
            "Không thể chuẩn bị câu trả lời. Vui lòng thử lại.",
            conversation_id,
        )
        return

    try:
        primary = create_primary_provider()
        fallback = create_fallback_provider()
    except Exception:
        logger.exception("Cấu hình LLM provider không hợp lệ")
        yield _error_event(
            "Cấu hình mô hình AI chưa đúng. Vui lòng kiểm tra LLM_PROVIDER/LLM_API_KEY.",
            conversation_id,
        )
        return

    answer_parts: list[str] = []
    assistant_message_id: int | None = None
    failed = False
    cancelled = False
    first_delta = False

    try:
        try:
            async for content in primary.chat_stream(
                messages=prepared.messages,
                model=settings.chat_model,
                temperature=settings.CHAT_TEMPERATURE,
                max_tokens=settings.LLM_MAX_TOKENS,
            ):
                first_delta = True
                answer_parts.append(content)
                yield {"event": "delta", "data": {"content": content}}
            if not first_delta:
                raise RuntimeError("Mô hình AI trả về nội dung rỗng")
        except asyncio.CancelledError:
            raise
        except Exception as exc:
            if first_delta or fallback is None or not should_fallback(exc):
                raise
            logger.warning(
                "Provider %s lỗi trước khi có delta, fallback sang Ollama: %s",
                primary.name,
                exc,
            )
            async for content in fallback.chat_stream(
                messages=prepared.messages,
                model=settings.OLLAMA_CHAT_MODEL,
                temperature=settings.CHAT_TEMPERATURE,
                max_tokens=settings.CHAT_NUM_PREDICT,
            ):
                first_delta = True
                answer_parts.append(content)
                yield {"event": "delta", "data": {"content": content}}
            if not first_delta:
                raise RuntimeError("Mô hình AI trả về nội dung rỗng")
    except asyncio.CancelledError:
        cancelled = True
        logger.info("Client đã ngắt kết nối chat stream")
        raise
    except Exception:
        failed = True
        logger.exception("Lỗi khi stream câu trả lời từ LLM provider")
        yield _error_event(
            "Không thể kết nối tới mô hình AI. Vui lòng thử lại.",
            conversation_id,
        )
    finally:
        answer = "".join(answer_parts).strip()
        if answer:
            if conversation_id is not None:
                message_status = (
                    "done" if not failed and not cancelled else "partial"
                )
                try:
                    assistant_result = conversation_service.append_message(
                        account_id,
                        conversation_id,
                        "assistant",
                        answer,
                        memory.estimate_tokens(answer),
                        [_source_payload(source) for source in sources],
                        message_status,
                    )
                    assistant_message_id = assistant_result["message"]["id"]
                except Exception:
                    logger.exception("Không thể lưu câu trả lời của AI")

            if not failed and not cancelled:
                try:
                    await run_in_threadpool(
                        log_chat,
                        account_id,
                        prompt,
                        answer,
                    )
                except Exception:
                    logger.exception("Không thể ghi log chat")

        for provider in (primary, fallback):
            if provider is None:
                continue
            try:
                await provider.close()
            except asyncio.CancelledError:
                pass
            except Exception:
                logger.exception("Không thể đóng LLM provider %s", provider.name)

    if not failed:
        done_data: dict[str, Any] = {
            "content": answer,
            "sourceChunksUsed": len(sources),
        }
        if conversation_id is not None:
            done_data.update(
                {
                    "conversationId": conversation_id,
                    "messageId": assistant_message_id,
                    "title": conversation_title,
                }
            )
        yield {"event": "done", "data": done_data}
