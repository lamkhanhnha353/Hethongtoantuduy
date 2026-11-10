import asyncio
import json
import sys
from pathlib import Path

# Đảm bảo import được module app
sys.path.insert(0, str(Path(__file__).parent.parent))

if sys.stdout.encoding != 'utf-8':
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

from app.config import settings
from app.extractors.factory import ExtractorFactory
from app.extractors.base import ScannedDocumentException
from app.services.chunker import QuestionChunker
from app.services.llm_parser import LLMParserService
from app.services.job_manager import JobManager
from app.schemas.question import QuestionItem


async def test_docx_pipeline():
    print("==================================================")
    print(" BẮT ĐẦU TEST TOÀN TRÌNH VỚI FILE WORD (.DOCX)")
    print("==================================================")

    docx_file = Path("./tests/data/de_thi_toan_tu_duy.docx")
    assert docx_file.exists(), f"Không tìm thấy file mẫu tại {docx_file}"

    job_id = "test-job-docx-001"
    bank_id = 1

    # 1. Test Extractor
    print("\n--- BƯỚC 1: TRÍCH XUẤT (EXTRACTOR) ---")
    extractor = ExtractorFactory.get_extractor(docx_file)
    doc = extractor.extract(docx_file, job_id)
    print(f"Tổng số block trích xuất: {len(doc.blocks)}")
    print(f"Tổng số ảnh bóc tách được: {len(doc.images)}")
    for img in doc.images:
        print(f" -> Ảnh đã bóc tách: {img}")

    assert len(doc.blocks) > 0, "Doc blocks rỗng!"
    assert len(doc.images) >= 1, "Không bóc tách được ảnh từ Word!"

    # 2. Test Chunker & Image Tagging
    print("\n--- BƯỚC 2: PHÂN ĐOẠN & GẮN THẺ ẢNH (CHUNKING) ---")
    chunker = QuestionChunker()
    chunks = chunker.chunk(doc)
    print(f"Số lượng khối câu hỏi nhận diện được: {len(chunks)}")
    for c in chunks:
        print(f"[{c.label}] Text dài {len(c.raw_text)} ký tự | Ảnh đính kèm: {len(c.image_urls)}")
        if c.image_urls:
            print(f"   -> Image URL: {c.primary_image_url}")

    assert len(chunks) == 3, f"Kỳ vọng 3 câu hỏi, nhận diện được {len(chunks)}"
    assert chunks[0].primary_image_url is not None, "Câu 1 phải được gắn ảnh minh họa hình vuông!"

    # 3. Test LLM Parser & Structured Output
    print("\n--- BƯỚC 3: LLM PARSER & SCHEMA VALIDATION ---")
    parser = LLMParserService()
    parser.provider = "mock"
    questions: list[QuestionItem] = []

    for c in chunks:
        q_item = await parser.parse_chunk(c, bank_id=bank_id)
        questions.append(q_item)

    print("\n--- KẾT QUẢ ĐÃ VALIDATE 100% THEO JSON SCHEMA ---")
    for idx, q in enumerate(questions, 1):
        print(f"\n--- CÂU {idx} ({q.type.value} - {q.difficulty.value}) ---")
        print(json.dumps(q.model_dump(by_alias=True), indent=2, ensure_ascii=False))

    # Kiểm tra cấu trúc câu 1
    q1 = questions[0]
    assert q1.bank_id == 1
    assert q1.type.value == "MULTIPLE_CHOICE"
    assert q1.image_url is not None and "http" in q1.image_url
    assert "A." in q1.answer or "81" in q1.answer

    # Kiểm tra câu 2 (Điền số)
    q2 = questions[1]
    assert q2.type.value == "FILL_IN"

    # Kiểm tra câu 3 (Tự luận)
    q3 = questions[2]
    assert q3.type.value in ["ESSAY", "FILL_IN"]

    print("\n[SUCCESS] Toàn bộ câu hỏi khớp chuẩn 100% với Target Schema của CSDL!")


async def test_failure_branch_scanned_pdf():
    print("\n==================================================")
    print(" TEST FAILURE BRANCH: PHÁT HIỆN TÀI LIỆU SCAN/ẢNH")
    print("==================================================")

    # Tạo 1 file PDF trắng / chỉ có ảnh không có text layer
    scanned_pdf = Path("./tests/data/scanned_sample.pdf")
    try:
        import fitz
        doc = fitz.open()
        page = doc.new_page(width=595, height=842)
        # Chỉ chèn ảnh hoặc để trang trống không có text
        img_rect = fitz.Rect(50, 50, 350, 250)
        page.insert_image(img_rect, filename=str(Path("./tests/data/sample_geometry.png")))
        doc.save(str(scanned_pdf))
        doc.close()

        extractor = ExtractorFactory.get_extractor(scanned_pdf)
        try:
            extractor.extract(scanned_pdf, "job-scan-fail")
            assert False, "Đáng lẽ phải raise ScannedDocumentException!"
        except ScannedDocumentException as e:
            print(f"[OK] Đã kích hoạt Failure Branch thành công: {str(e)}")

    except ImportError:
        print("[SKIP] PyMuPDF chưa sẵn sàng để test PDF.")


async def main():
    await test_docx_pipeline()
    await test_failure_branch_scanned_pdf()


if __name__ == "__main__":
    asyncio.run(main())
