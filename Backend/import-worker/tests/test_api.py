import sys
import time
from unittest.mock import AsyncMock, patch
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent))

if sys.stdout.encoding != 'utf-8':
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

from fastapi.testclient import TestClient
from app.main import app
from app.services.job_manager import job_manager

client = TestClient(app)
job_manager.parser.provider = "mock"


def test_api_health():
    res = client.get("/health")
    assert res.status_code == 200
    assert res.json() == {"status": "healthy"}


def test_gemini_key_check_page_is_served_by_local_worker():
    response = client.get("/api/parser/tools/gemini-key-check")
    assert response.status_code == 200
    assert "Kiểm tra Gemini API keys" in response.text


def test_gemini_key_check_is_local_and_does_not_return_keys():
    client = TestClient(
        app,
        base_url="http://127.0.0.1:8000",
        client=("127.0.0.1", 12345),
    )
    with (
        patch.object(job_manager.parser, "provider", "gemini"),
        patch.object(
            job_manager.parser,
            "test_gemini_keys",
            new=AsyncMock(return_value=[
                {"key_index": 1, "status": "working"},
                {"key_index": 2, "status": "failed", "message": "Key hết hạn mức hoặc bị giới hạn tốc độ."},
            ]),
        ),
    ):
        response = client.post(
            "/api/parser/test-gemini",
            json={},
            headers={"Origin": "http://127.0.0.1:8000"},
        )

    assert response.status_code == 200
    data = response.json()
    assert data["working_keys"] == 1
    assert "key_index" in str(data).lower()
    assert "api_key" not in str(data).lower()


def test_gemini_key_check_rejects_non_loopback_clients():
    client = TestClient(
        app,
        base_url="http://127.0.0.1:8000",
        client=("192.0.2.10", 12345),
    )
    response = client.post(
        "/api/parser/test-gemini",
        json={},
        headers={"Origin": "http://127.0.0.1:8000"},
    )
    assert response.status_code == 403


def test_gemini_key_check_rejects_cross_origin():
    client = TestClient(
        app,
        base_url="http://127.0.0.1:8000",
        client=("127.0.0.1", 12345),
    )
    response = client.post(
        "/api/parser/test-gemini",
        json={},
        headers={"Origin": "https://example.com"},
    )
    assert response.status_code == 403


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
    test_gemini_key_check_page_is_served_by_local_worker()
    test_gemini_key_check_is_local_and_does_not_return_keys()
    test_gemini_key_check_rejects_non_loopback_clients()
    test_gemini_key_check_rejects_cross_origin()
    test_api_process_file()
