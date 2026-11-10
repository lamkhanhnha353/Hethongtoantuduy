import asyncio
import logging
from datetime import datetime
from pathlib import Path
from typing import Dict, Optional
import uuid

from app.schemas.api import JobStatus, JobDetailResponse
from app.schemas.question import QuestionItem
from app.extractors.factory import ExtractorFactory
from app.extractors.base import ExtractionException, ScannedDocumentException
from app.services.chunker import QuestionChunk, QuestionChunker
from app.services.llm_parser import LLMParserService

logger = logging.getLogger(__name__)

MAX_CONCURRENT_QUESTION_PARSING = 2


class JobManager:
    """
    Quản lý hàng đợi và vòng đời của các tác vụ bóc tách câu hỏi.
    Duy trì trạng thái Staging cho hệ thống chính (ASP.NET Core) thăm dò hoặc nhận callback.
    """

    def __init__(self):
        self._jobs: Dict[str, JobDetailResponse] = {}
        self.chunker = QuestionChunker()
        self.parser = LLMParserService()

    def create_job(self, file_name: str, bank_id: int, custom_job_id: Optional[str] = None) -> str:
        job_id = custom_job_id or str(uuid.uuid4())
        job = JobDetailResponse(
            job_id=job_id,
            status=JobStatus.PENDING,
            progress_percentage=0,
            bank_id=bank_id,
            parser_provider=self.parser.provider,
            file_name=file_name,
            total_chunks=0,
            total_questions=0,
            questions=[],
            error_message=None,
            created_at=datetime.now().isoformat(),
            completed_at=None,
        )
        self._jobs[job_id] = job
        return job_id

    def get_job(self, job_id: str) -> Optional[JobDetailResponse]:
        return self._jobs.get(job_id)

    async def run_pipeline(self, job_id: str, file_path: Path, bank_id: int):
        job = self.get_job(job_id)
        if not job:
            logger.error(f"Job {job_id} not found.")
            return

        try:
            # BƯỚC 1: EXTRACT (DOCX, PDF, OCR)
            job.status = JobStatus.EXTRACTING
            job.progress_percentage = 15
            logger.info(f"[{job_id}] Bắt đầu bóc tách tệp: {file_path}")

            extractor = ExtractorFactory.get_extractor(file_path)
            # Chạy trích xuất trong thread pool nếu thao tác I/O nặng
            doc = await asyncio.to_thread(extractor.extract, file_path, job_id)

            # BƯỚC 2: CHUNKING (Theo câu hoặc mục)
            job.status = JobStatus.CHUNKING
            job.progress_percentage = 35
            logger.info(f"[{job_id}] Bắt đầu phân đoạn (chunking)...")

            chunks = await asyncio.to_thread(self.chunker.chunk, doc)
            job.total_chunks = len(chunks)

            if not chunks:
                raise ExtractionException("Không thể phân tách được câu hỏi nào từ nội dung tài liệu.")

            # BƯỚC 3: LLM + VALIDATE (JSON THEO SCHEMA)
            job.status = JobStatus.PARSING
            total_chunks = len(chunks)
            job.progress_percentage = 40
            job.total_questions = 0

            logger.info(f"[{job_id}] Tìm thấy {total_chunks} khối câu hỏi. Bắt đầu gọi LLM...")

            semaphore = asyncio.Semaphore(MAX_CONCURRENT_QUESTION_PARSING)
            progress_lock = asyncio.Lock()
            completed_chunks = 0

            async def parse_one_chunk(index: int, chunk: QuestionChunk) -> QuestionItem:
                nonlocal completed_chunks
                async with semaphore:
                    question_item = await self.parser.parse_chunk(chunk, bank_id)

                async with progress_lock:
                    completed_chunks += 1
                    job.total_questions = completed_chunks
                    job.progress_percentage = min(
                        int(40 + completed_chunks / total_chunks * 55),
                        98,
                    )
                    logger.info(
                        "[%s] Đã phân tích xong khối %s/%s.",
                        job_id,
                        index + 1,
                        total_chunks,
                    )
                return question_item

            results = await asyncio.gather(
                *(parse_one_chunk(index, chunk) for index, chunk in enumerate(chunks)),
                return_exceptions=True,
            )
            parsed_questions: list[QuestionItem] = []
            for result in results:
                if isinstance(result, Exception):
                    raise result
                parsed_questions.append(result)

            # BƯỚC 4: HOÀN TẤT VÀ LƯU STAGING
            job.status = JobStatus.COMPLETED
            job.progress_percentage = 100
            job.total_questions = len(parsed_questions)
            job.questions = parsed_questions
            job.completed_at = datetime.now().isoformat()
            logger.info(f"[{job_id}] Hoàn thành bóc tách thành công {len(parsed_questions)} câu hỏi.")

        except ScannedDocumentException as sde:
            # Xử lý FAILURE BRANCH đối với tài liệu Scan/Ảnh
            logger.warning(f"[{job_id}] Failure Branch kích hoạt: {str(sde)}")
            job.status = JobStatus.FAILED
            job.error_message = f"[SCAN_DETECTED] {str(sde)}"
            job.completed_at = datetime.now().isoformat()

        except ExtractionException as ee:
            logger.error(f"[{job_id}] Lỗi bóc tách: {str(ee)}")
            job.status = JobStatus.FAILED
            job.error_message = f"[EXTRACT_ERROR] {str(ee)}"
            job.completed_at = datetime.now().isoformat()

        except Exception as e:
            logger.exception(f"[{job_id}] Lỗi hệ thống không lường trước: {str(e)}")
            job.status = JobStatus.FAILED
            job.error_message = f"[SYSTEM_ERROR] {str(e)}"
            job.completed_at = datetime.now().isoformat()


# Global Singleton instance
job_manager = JobManager()
