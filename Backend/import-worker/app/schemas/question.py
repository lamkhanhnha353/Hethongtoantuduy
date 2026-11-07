from enum import Enum
from typing import Optional, List
from pydantic import BaseModel, ConfigDict, Field


class QuestionType(str, Enum):
    MULTIPLE_CHOICE = "MULTIPLE_CHOICE"
    FILL_IN = "FILL_IN"
    ESSAY = "ESSAY"


class Difficulty(str, Enum):
    EASY = "EASY"
    MEDIUM = "MEDIUM"
    HARD = "HARD"


class QuestionItem(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    bank_id: int = Field(
        ...,
        alias="bankId",
        description="ID của ngân hàng câu hỏi đích"
    )
    type: QuestionType = Field(
        ...,
        description="Loại câu hỏi: MULTIPLE_CHOICE (trắc nghiệm), FILL_IN (điền khuyết/đáp số), ESSAY (tự luận)"
    )
    content: str = Field(
        ...,
        description="Nội dung câu hỏi đề bài đầy đủ (bao gồm các phương án lựa chọn A, B, C, D nếu có)"
    )
    image_url: Optional[str] = Field(
        default=None,
        alias="imageUrl",
        description="Đường dẫn URL ảnh minh họa nếu câu hỏi có hình ảnh trích xuất"
    )
    difficulty: Difficulty = Field(
        ...,
        description="Độ khó nhận diện: EASY (dễ/cơ bản), MEDIUM (trung bình), HARD (nâng cao/tư duy sâu)"
    )
    answer: str = Field(
        ...,
        description="Đáp án chính xác kèm theo lời giải chi tiết, các bước suy luận toán học"
    )


class QuestionBatch(BaseModel):
    """Schema used for Structured Output when calling LLM."""
    questions: List[QuestionItem] = Field(
        default_factory=list,
        description="Danh sách các câu hỏi đã được trích xuất và chuẩn hóa"
    )
