from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from pathlib import Path
from typing import List, Optional, Dict, Any


class ExtractionException(Exception):
    """Lỗi chung trong quá trình bóc tách tệp."""
    pass


class ScannedDocumentException(ExtractionException):
    """Lỗi khi tài liệu là dạng scan/ảnh không chứa text layer."""
    pass


@dataclass
class ExtractedBlock:
    """Đại diện cho một khối nội dung (văn bản hoặc hình ảnh) theo thứ tự xuất hiện."""
    block_type: str  # 'text' | 'image'
    content: str     # Văn bản hoặc đường dẫn tệp ảnh
    order: int       # Thứ tự xuất hiện trong tài liệu
    page_or_index: int = 0
    image_url: Optional[str] = None
    extra: Dict[str, Any] = field(default_factory=dict)


@dataclass
class ExtractedDocument:
    """Tài liệu sau khi đã bóc tách toàn bộ text và hình ảnh."""
    file_path: Path
    file_type: str
    blocks: List[ExtractedBlock] = field(default_factory=list)
    images: List[str] = field(default_factory=list)
    metadata: Dict[str, Any] = field(default_factory=dict)

    @property
    def full_text(self) -> str:
        return "\n".join(b.content for b in self.blocks if b.block_type == "text")


class BaseExtractor(ABC):
    """Interface trừu tượng cho tất cả các bộ bóc tách tài liệu."""

    @abstractmethod
    def extract(self, file_path: Path, job_id: str) -> ExtractedDocument:
        """
        Bóc tách text và hình ảnh từ tệp đầu vào.
        
        :param file_path: Đường dẫn tệp cần bóc tách
        :param job_id: Mã định danh job để đặt tên ảnh và thư mục
        :return: ExtractedDocument
        :raises ExtractionException: Khi gặp lỗi đọc tệp hoặc tệp không hợp lệ
        :raises ScannedDocumentException: Khi tệp là dạng scan không có text layer
        """
        pass
