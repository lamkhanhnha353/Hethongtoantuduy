# Question Import Worker (Python Service)

Service Worker độc lập viết bằng Python chịu trách nhiệm bóc tách đề thi/bài tập từ file Word (`.docx`) và PDF (`.pdf`), phân đoạn câu hỏi kèm hình ảnh nhúng, và sử dụng LLM để trích xuất có cấu trúc vào cơ sở dữ liệu ngân hàng câu hỏi theo chuẩn schema của Backend ASP.NET Core.

---

## 1. Sơ đồ kiến trúc & Luồng hoạt động

Dựa theo thiết kế kiến trúc hệ thống:
```text
[Frontend (Upload, tiến độ, review, duyệt)]
          │                     │
          ▼                     ▼
[Backend chính (ASP.NET Core)] ──commit──> [Question Service (Đã có sẵn)]
     │             │                                  ▲
     │             ▼                                  │
     │      [Message Queue]                           │
     ▼             │                                  │
[Object Storage]   │ (jobId / file_path)              │
     │             ▼                                  │
┌────┼────────────────────────────────────────────────┼──────────┐
│    │     Import Worker (Python - Module này)        │          │
│    ▼                                                │          │
│  [ Extract ] ── (Lỗi / Scan không text) ──> [ FAILURE BRANCH ] │
│  (DOCX, PDF)                                                   │
│        │                                                       │
│        ▼                                                       │
│  [ Chunking ] (Phân đoạn Regex + Gắn thẻ ảnh nhúng)            │
│        │                                                       │
│        ▼                                                       │
│  [ LLM + Validate ] (Pydantic v2 + Structured Output)          │
│        │                                                       │
│        ▼                                                       │
│  [ Staging Result / Database ] ────────────────────────────────┘
└────────────────────────────────────────────────────────────────┘
```

### Chi tiết các bước xử lý:
1. **Extract (`app/extractors/`)**:
   - `.docx`: Đọc từng đoạn văn và bảng biểu bằng `python-docx`. Bóc tách toàn bộ ảnh nhúng (DrawingML/VML blip relations), lưu vào `storage/images/` và đánh dấu vị trí tương đối.
   - `.pdf`: Đọc trang bằng `PyMuPDF` (`fitz`), trích xuất text block và stream hình ảnh `extract_image`.
   - **Failure branch**: Nếu phát hiện tài liệu scan không có text layer (hoặc file hỏng), worker kích hoạt Failure branch: chuyển job sang trạng thái `FAILED` với mã lỗi `[SCAN_DETECTED]` để chuyển tiếp sang pipeline OCR hoặc cảnh báo người dùng.
2. **Chunking (`app/services/chunker.py`)**:
   - Sử dụng Regex đa mẫu (`Câu \d+[:.]`, `Bài \d+[:.]`, `Question \d+[:.]`, số thứ tự La Mã,...) để phân chia các khối câu hỏi.
   - Gắn thẻ (tagging) các ảnh đã bóc tách thuộc về đúng câu hỏi tương ứng.
3. **LLM + Validate (`app/services/llm_parser.py`)**:
   - Sử dụng `Pydantic v2` và chế độ Structured Outputs với `google-genai` (`gemini-2.0-flash`) hoặc `openai` (`gpt-4o-mini`).
   - AI nhận diện `type`: `MULTIPLE_CHOICE` (trắc nghiệm), `FILL_IN` (điền đáp số/từ), `TRUE_FALSE` (đúng/sai), `TIMED_SEQUENCE` (tính nhẩm theo nhịp) và `ESSAY` (tự luận).
   - `TIMED_SEQUENCE` cần nội dung theo mẫu `Tính nhẩm theo nhịp: 1, +2, -3`; app trình chiếu mỗi số trong 1 giây. `ESSAY` có thể lưu vào ngân hàng nhưng chưa hỗ trợ nộp/chấm qua app.
   - Tự động đánh giá `difficulty`: `EASY`, `MEDIUM`, `HARD` theo độ phức tạp toán tư duy.
   - Phân tách rõ ràng đề bài (`content`) và đáp án / lời giải chi tiết (`answer`).
4. **Staging / API Controller (`app/api/` & `app/services/job_manager.py`)**:
   - Nhận file qua HTTP multipart hoặc qua file path trên server.
   - Quản lý Job ngầm (Asynchronous Worker).
   - Phục vụ API kiểm tra trạng thái và trả về danh sách câu hỏi staging chuẩn 100% schema.

---

## 2. Schema câu hỏi mục tiêu (Khớp CSDL SQL Server)

```json
{
  "bankId": 1,
  "type": "MULTIPLE_CHOICE",
  "content": "Cho hình vuông ABCD có chu vi 36 cm. Diện tích của hình vuông là:\nA. 81 cm2\nB. 36 cm2\nC. 72 cm2\nD. 18 cm2",
  "imageUrl": "http://127.0.0.1:8000/static/images/job123_img_1.png",
  "difficulty": "EASY",
  "answer": "A. 81 cm2. Lời giải: Cạnh hình vuông là 36:4=9 (cm). Diện tích là 9x9=81 (cm2)."
}
```

---

## 3. Cấu trúc thư mục

```text
import-worker/
├── app/
│   ├── config.py              # Cấu hình Pydantic Settings & Biến môi trường
│   ├── main.py                # FastAPI App, CORS, Static Files mount
│   ├── schemas/
│   │   ├── question.py        # QuestionItem, QuestionType, Difficulty (Pydantic v2)
│   │   └── api.py             # Schema Request & Response cho các API endpoint
│   ├── extractors/
│   │   ├── base.py            # BaseExtractor, ExtractedDocument, ExtractedBlock, Exceptions
│   │   ├── docx_extractor.py  # Trích xuất DOCX + bóc tách ảnh nhúng DrawingML/VML
│   │   ├── pdf_extractor.py   # Trích xuất PDF (fitz) + phát hiện Failure branch (Scan PDF)
│   │   └── factory.py         # Factory chọn extractor theo phần mở rộng tệp
│   ├── services/
│   │   ├── chunker.py         # Regex Chunking & gán hình ảnh vào câu hỏi
│   │   ├── llm_parser.py      # LLM Structured Output (Gemini/OpenAI/Heuristic)
│   │   └── job_manager.py     # Background Pipeline Orchestrator & Staging Store
│   └── api/
│       └── routes.py          # FastAPI Endpoints: /process-file, /process-path, /job/{id}
├── question-bank-samples/     # Câu hỏi mẫu để sao chép vào Word
├── tools/
│   └── gemini-key-check.html  # Trang kiểm tra key Gemini đã cấu hình
├── storage/
│   ├── uploads/               # Thư mục chứa file tải lên tạm thời
│   └── images/                # Thư mục chứa hình ảnh bóc tách từ đề thi
├── tests/
│   ├── data/                  # Dữ liệu test
│   ├── create_sample_files.py # Script sinh đề thi mẫu Word có ảnh nhúng
│   └── test_pipeline.py       # Integration test toàn diện
├── .env.example               # Mẫu biến môi trường
├── requirements.txt           # Danh mục thư viện phụ thuộc
└── README.md
```

---

## 4. Hướng dẫn cài đặt & Chạy ứng dụng

### 4.1 Cài đặt môi trường
Yêu cầu: **Python 3.10+**

```bash
# Từ thư mục gốc repository
cd Backend\import-worker

# Cài đặt các thư viện cần thiết
pip install -r requirements.txt
```

### 4.2 Cấu hình biến môi trường
Tạo tệp `.env` từ `.env.example`:
```bash
cp .env.example .env
```
Nội dung `.env`:
```env
LLM_PROVIDER=gemini
GEMINI_API_KEYS=key_1,key_2
GEMINI_MODEL=gemini-2.0-flash

# Hoặc dùng OpenAI:
# LLM_PROVIDER=openai
# OPENAI_API_KEY=sk-...
# OPENAI_MODEL=gpt-4o-mini

APP_HOST=0.0.0.0
APP_PORT=8000
BASE_URL=http://127.0.0.1:8000
DEFAULT_BANK_ID=1
```
Để dùng AI thật, cần cấu hình `LLM_PROVIDER=gemini` hoặc `openai` cùng API key tương ứng. Nếu thiếu key hoặc gọi mô hình thất bại, worker sẽ báo job `FAILED` thay vì âm thầm trả kết quả từ bộ luật. Chỉ đặt `LLM_PROVIDER=mock` khi cần chạy thử offline; mock không sử dụng AI và không nên dùng để chứng minh mô hình AI đang hoạt động.

Các tài liệu mẫu để sao chép vào Word nằm trong `question-bank-samples/`. Nên lưu Word dạng `.docx` hoặc xuất PDF có lớp văn bản. Tài liệu scan chưa có OCR. Sau khi AI bóc tách, giáo viên cần xem lại và duyệt trước khi lưu vào ngân hàng.

Có thể cấu hình nhiều Gemini key bằng `GEMINI_API_KEYS=key_1,key_2` (phân cách bằng dấu phẩy, chấm phẩy hoặc xuống dòng). Worker sẽ luân phiên key và tự thử key kế tiếp khi Gemini báo key không hợp lệ, hết quota hoặc giới hạn tốc độ; lỗi máy chủ 5xx tạm thời sẽ được thử lại tối đa ba lần trên cùng key. Worker xử lý tối đa hai câu hỏi đồng thời để rút ngắn thời gian nhưng vẫn giới hạn tải lên Gemini. `GEMINI_API_KEY` vẫn được hỗ trợ để tương thích cấu hình một key cũ.

### Kiểm tra Gemini keys

1. Điền key vào `Backend/import-worker/.env` (không chia sẻ file này).
2. Từ thư mục gốc repository, chạy `cd Backend\import-worker` rồi `python -m uvicorn app.main:app --host 127.0.0.1 --port 8000`.
3. Mở `http://127.0.0.1:8000/api/parser/tools/gemini-key-check` trong trình duyệt và nhấn **Kiểm tra tất cả Gemini keys**.

Trang chỉ gọi endpoint kiểm tra cục bộ; endpoint từ chối yêu cầu không xuất phát từ localhost. Mỗi key tiêu thụ một yêu cầu thử nghiệm nhỏ. Kết quả không trả về nội dung key. Nếu key hợp lệ nhưng model không khả dụng, kiểm tra giá trị `GEMINI_MODEL` và quyền truy cập model.

### 4.3 Khởi chạy Service Worker
```bash
python -m uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
```
- Swagger API Docs: [http://127.0.0.1:8000/docs](http://127.0.0.1:8000/docs)
- Health Check: [http://127.0.0.1:8000/health](http://127.0.0.1:8000/health)


Trên Windows, có thể chạy nhanh bằng cách nhấp đúp `run-import-worker.bat` trong thư mục `import-worker`. Tệp sẽ tự chuyển vào đúng thư mục và chạy Worker tại `127.0.0.1:8000`; giữ cửa sổ terminal mở trong lúc sử dụng web và nhấn `Ctrl+C` để dừng.

---

## 5. Danh sách API Endpoints

### 5.1 Tải tệp đề thi lên xử lý
`POST /api/parser/process-file`
- Content-Type: `multipart/form-data`
- Body:
  - `file`: File `.docx` hoặc `.pdf`
  - `bank_id`: Số nguyên (VD: `1`)
  - `job_id`: (Tùy chọn) Mã Job do Backend ASP.NET cấp

**Response (HTTP 202 Accepted):**
```json
{
  "job_id": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "status": "PENDING",
  "message": "Tệp đã được tiếp nhận và đang được xử lý trong nền.",
  "file_name": "de_thi_toan_tu_duy.docx",
  "bank_id": 1,
  "check_status_url": "/api/parser/job/9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d"
}
```

### 5.2 Xử lý theo đường dẫn tệp trên Server
`POST /api/parser/process-path`
- Content-Type: `application/json`
- Body:
```json
{
  "file_path": "D:/Storage/uploads/de_thi_toan.docx",
  "bank_id": 1,
  "job_id": "job_custom_123"
}
```

### 5.3 Thăm dò tiến độ & Lấy danh sách câu hỏi Staging
`GET /api/parser/job/{job_id}`

**Response khi hoàn thành (HTTP 200 OK):**
```json
{
  "job_id": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "status": "COMPLETED",
  "progress_percentage": 100,
  "bank_id": 1,
  "parser_provider": "gemini",
  "file_name": "de_thi_toan_tu_duy.docx",
  "total_chunks": 3,
  "total_questions": 3,
  "questions": [
    {
      "bankId": 1,
      "type": "MULTIPLE_CHOICE",
      "content": "Câu 1: Cho hình vuông ABCD như hình vẽ bên dưới. Biết chu vi của hình vuông là 36 cm. Hãy tính diện tích của hình vuông ABCD?\nA. 81 cm2\nB. 36 cm2\nC. 72 cm2\nD. 18 cm2",
      "imageUrl": "http://127.0.0.1:8000/static/images/9b1deb4d_img_1.png",
      "difficulty": "EASY",
      "answer": "A. 81 cm2. Lời giải: Cạnh của hình vuông là 36 : 4 = 9 (cm). Diện tích hình vuông là 9 x 9 = 81 (cm2)."
    },
    {
      "bankId": 1,
      "type": "FILL_IN",
      "content": "Câu 2: Bạn An có một số viên bi. Sau khi An cho Bình một nửa số bi và thêm 2 viên nữa thì An còn lại 6 viên bi. Hỏi lúc đầu An có bao nhiêu viên bi?",
      "imageUrl": null,
      "difficulty": "MEDIUM",
      "answer": "Đáp số: 16 viên bi. Lời giải: Một nửa số bi của An là 6 + 2 = 8 viên. Lúc đầu An có 8 x 2 = 16 viên bi."
    },
    {
      "bankId": 1,
      "type": "ESSAY",
      "content": "Câu 3: Trong một cuộc thi cờ vua vòng tròn một lượt, có 8 kỳ thủ tham gia...",
      "imageUrl": null,
      "difficulty": "HARD",
      "answer": "Lời giải: Tổng số trận đấu trong giải là (8 x 7) : 2 = 28 trận..."
    }
  ],
  "error_message": null,
  "created_at": "2026-11-03T18:50:00.000000",
  "completed_at": "2026-11-03T18:50:04.000000"
}
```

**Response khi gặp Failure Branch (File scan hoặc file lỗi):**
```json
{
  "job_id": "job-scan-fail-123",
  "status": "FAILED",
  "progress_percentage": 15,
  "bank_id": 1,
  "file_name": "scan_document.pdf",
  "total_chunks": 0,
  "total_questions": 0,
  "questions": [],
  "error_message": "[SCAN_DETECTED] Tài liệu PDF là dạng scan/ảnh chụp không có text layer. Cần chuyển qua pipeline OCR trước khi trích xuất.",
  "created_at": "2026-11-03T18:50:00",
  "completed_at": "2026-11-03T18:50:01"
}
```

---

## 6. Chạy thử nghiệm với file đề mẫu (Tests)

### 6.1 Tạo file Word đề thi mẫu có chứa ảnh nhúng hình học
```bash
python tests/create_sample_files.py
```

### 6.2 Chạy Integration Test toàn trình
```bash
python tests/test_pipeline.py
```
Test sẽ tự động kiểm tra:
1. Trích xuất text và bóc tách ảnh nhúng `.png` từ file `.docx`.
2. Phân đoạn Regex và gán đúng link ảnh cho Câu 1.
3. Phân loại câu hỏi và kiểm tra schema trong chế độ mock offline.
4. Kiểm tra Failure Branch khi đưa vào file scan không có text layer.

Chạy các kiểm tra parser:
```bash
python -m unittest discover -s tests -p test_question_parser.py -v
```
