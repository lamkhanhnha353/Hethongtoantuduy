from datetime import datetime

from sqlalchemy import Column, DateTime, ForeignKey, Integer, String, Text
from sqlalchemy.orm import relationship

from app.db.database import Base


class AIChatLog(Base):
    __tablename__ = "ai_chat_logs"

    id = Column(Integer, primary_key=True, index=True)
    account_id = Column(String(100), index=True)
    prompt = Column(Text)
    response = Column(Text)
    created_at = Column(DateTime, default=datetime.utcnow)


class AIConversation(Base):
    __tablename__ = "ai_conversations"

    id = Column(Integer, primary_key=True, index=True)
    account_id = Column(String(100), nullable=False, index=True)
    title = Column(String(255), nullable=False, default="Hội thoại mới")
    summary = Column(Text, nullable=True)
    summary_until_message_id = Column(Integer, nullable=True)
    last_message_at = Column(DateTime, nullable=False, default=datetime.utcnow)
    created_at = Column(DateTime, nullable=False, default=datetime.utcnow)
    updated_at = Column(
        DateTime,
        nullable=False,
        default=datetime.utcnow,
        onupdate=datetime.utcnow,
    )

    messages = relationship(
        "AIMessage",
        back_populates="conversation",
        cascade="all, delete-orphan",
        order_by="AIMessage.id",
    )


class KnowledgeDocument(Base):
    __tablename__ = "knowledge_documents"

    id = Column(Integer, primary_key=True, index=True)
    doc_id = Column(String(200), nullable=False, unique=True, index=True)
    title = Column(String(500), nullable=False)
    description = Column(Text, nullable=True)
    category = Column(String(100), nullable=True, index=True)
    source_type = Column(String(20), nullable=False, default="text")
    source_url = Column(String(1000), nullable=True)
    file_path = Column(String(1000), nullable=True)
    raw_content = Column(Text, nullable=True)
    content_hash = Column(String(64), nullable=True, index=True)
    status = Column(String(20), nullable=False, default="pending", index=True)
    chunk_count = Column(Integer, nullable=False, default=0)
    embedding_model = Column(String(100), nullable=True)
    error_message = Column(Text, nullable=True)
    created_by = Column(String(100), nullable=True)
    created_at = Column(DateTime, nullable=False, default=datetime.utcnow)
    updated_at = Column(
        DateTime,
        nullable=False,
        default=datetime.utcnow,
        onupdate=datetime.utcnow,
    )


class AIMessage(Base):
    __tablename__ = "ai_messages"

    id = Column(Integer, primary_key=True, index=True)
    conversation_id = Column(
        Integer,
        ForeignKey("ai_conversations.id", ondelete="CASCADE"),
        nullable=False,
        index=True,
    )
    role = Column(String(20), nullable=False)
    content = Column(Text, nullable=False)
    token_count = Column(Integer, nullable=False, default=0)
    sources_json = Column(Text, nullable=True)
    status = Column(String(20), nullable=False, default="done")
    created_at = Column(DateTime, nullable=False, default=datetime.utcnow)

    conversation = relationship("AIConversation", back_populates="messages")