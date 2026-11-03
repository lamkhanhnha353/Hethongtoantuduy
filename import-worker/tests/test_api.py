import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent))

if sys.stdout.encoding != 'utf-8':
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

from fastapi.testclient import TestClient
from app.main import app

client = TestClient(app)


def test_api_health():
    res = client.get("/health")
    assert res.status_code == 200
    assert res.json() == {"status": "healthy"}


def test_api_process_file():
    sample_docx = Path("./tests/data/de_thi_toan_tu_duy.docx")
    assert sample_docx.exists()

    with open(sample_docx, "rb") as f:
        response = client.post(
            "/api/parser/process-file",
            files={"file": ("de_thi_toan_tu_duy.docx", f, "application/vnd.openxmlformats-officedocument.wordprocessingml.document")},
            data={"bank_id": "1"}
        )

    assert response.status_code == 202
    data = response.json()
    job_id = data["job_id"]
    assert job_id is not None
    assert data["status"] == "PENDING"
    print(f"\n[API TEST] Đã tạo Job ID thành công: {job_id}")

    # Chờ background worker xử lý (khoảng 0.5 - 1s cho file mẫu)
    max_wait = 10
    start = time.time()
    job_data = None

    while time.time() - start < max_wait:
        poll_res = client.get(f"/api/parser/job/{job_id}")
        assert poll_res.status_code == 200
        job_data = poll_res.json()
        if job_data["status"] in ["COMPLETED", "FAILED"]:
            break
        time.sleep(0.3)

    assert job_data["status"] == "COMPLETED", f"Job thất bại: {job_data.get('error_message')}"
    assert job_data["total_questions"] == 3
    assert len(job_data["questions"]) == 3
    assert job_data["questions"][0]["bankId"] == 1
    assert job_data["questions"][0]["imageUrl"] is not None
    print(f"[API TEST] Hoàn thành Job API: Đã lấy về {len(job_data['questions'])} câu hỏi hợp lệ!")


if __name__ == "__main__":
    test_api_health()
    test_api_process_file()
