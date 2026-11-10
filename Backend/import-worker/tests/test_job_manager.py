import asyncio
import sys
import unittest
from pathlib import Path
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).parent.parent))

from app.schemas.api import JobStatus
from app.schemas.question import Difficulty, QuestionItem, QuestionType
from app.extractors.factory import ExtractorFactory
from app.services.chunker import QuestionChunk
from app.services.job_manager import JobManager


class JobManagerTests(unittest.IsolatedAsyncioTestCase):
    async def test_parses_chunks_with_bounded_concurrency_and_preserves_order(self):
        chunks = [
            QuestionChunk(chunk_index=index, label=f"Question {index}", raw_text=str(index))
            for index in range(1, 5)
        ]

        class Extractor:
            def extract(self, file_path, job_id):
                return object()

        class Chunker:
            def chunk(self, document):
                return chunks

        class Parser:
            provider = "mock"

            def __init__(self):
                self.active_calls = 0
                self.max_active_calls = 0

            async def parse_chunk(self, chunk, bank_id):
                self.active_calls += 1
                self.max_active_calls = max(self.max_active_calls, self.active_calls)
                await asyncio.sleep(0.01)
                self.active_calls -= 1
                return QuestionItem(
                    bankId=bank_id,
                    type=QuestionType.FILL_IN,
                    content=chunk.raw_text,
                    difficulty=Difficulty.EASY,
                    answer=chunk.raw_text,
                )

        manager = JobManager()
        manager.chunker = Chunker()
        manager.parser = Parser()
        job_id = manager.create_job("sample.docx", bank_id=4)

        with patch.object(ExtractorFactory, "get_extractor", return_value=Extractor()):
            await manager.run_pipeline(job_id, Path("sample.docx"), bank_id=4)

        job = manager.get_job(job_id)
        self.assertEqual(job.status, JobStatus.COMPLETED)
        self.assertEqual([question.content for question in job.questions], ["1", "2", "3", "4"])
        self.assertEqual(job.total_questions, 4)
        self.assertEqual(manager.parser.max_active_calls, 2)


if __name__ == "__main__":
    unittest.main()
