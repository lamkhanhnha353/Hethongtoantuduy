from enum import Enum
from typing import Optional, List, Any, Dict
from pydantic import BaseModel, Field
from app.schemas.question import QuestionItem


class JobStatus(str, Enum):
    PENDING = "PENDING"
    EXTRACTING = "EXTRACTING"
    CHUNKING = "CHUNKING"
    PARSING = "PARSING"
    COMPLETED = "COMPLETED"
    FAILED = "FAILED"


class ProcessFileRequest(BaseModel):
    file_path: Optional[str] = Field(None, description="Đường dẫn file trên server nếu đã lưu trước đó")
    job_id: Optional[str] = Field(None, description="Mã Job ID do backend chính cấp phát (tùy chọn)")
    bank_id: int = Field(1, description="ID của ngân hàng câu hỏi đích")


class ProcessFileResponse(BaseModel):
    job_id: str
    status: JobStatus
    message: str
    file_name: str
    bank_id: int
    check_status_url: str


class JobDetailResponse(BaseModel):
    job_id: str
    status: JobStatus
    progress_percentage: int = Field(0, ge=0, le=100)
    bank_id: int
    file_name: str
    total_chunks: int = 0
    total_questions: int = 0
    questions: List[QuestionItem] = Field(default_factory=list)
    error_message: Optional[str] = None
    created_at: str
    completed_at: Optional[str] = None
