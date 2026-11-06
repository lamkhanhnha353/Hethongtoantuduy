# AI_Backend — RAG Chatbot Service

FastAPI service độc lập cho chatbot RAG của Hệ thống Toán Tư Duy. Giai đoạn M1 gồm khung service, xác thực JWT dùng chung với Backend .NET, SQLite và health check. Giai đoạn M2 bổ sung ingest văn bản vào ChromaDB và truy vấn RAG. Giai đoạn M3 bổ sung chat streaming SSE, history do client gửi lên và nguồn trích dẫn.

## Yêu cầu

- Python 3.10+
- Ollama (cho M2/M3): `ollama pull qwen2.5:3b-instruct` và `ollama pull nomic-embed-text`

## Cài đặt

```powershell
cd AI_Backend
python -m venv venv
.\venv\Scripts\Activate.ps1
python -m pip install --upgrade pip
pip install -r requirements.txt
Copy-Item .env.example .env
```

Cập nhật `JWT_SECRET_KEY` trong `.env` trùng `Jwt:Key` của `Backend/ToanTuDuy/appsettings.json`. `JWT_ISSUER` và `JWT_AUDIENCE` phải trùng cấu hình .NET.

## Chạy service

```powershell
uvicorn app.main:app --host 0.0.0.0 --port 8001 --reload
```

- Swagger: http://localhost:8001/docs
- Health (cần Bearer token .NET): http://localhost:8001/api/ai/health
- Ingest (chỉ Admin): `POST http://localhost:8001/api/ai/ingest`
- Chat RAG: `POST http://localhost:8001/api/chat`
- Chat RAG streaming (mọi role đã đăng nhập): `POST http://localhost:8001/api/ai/chat/stream`

## Ingest tài liệu (M2)

```powershell
$token = "<access_token_của_Admin>"
$body = @{
  doc_id = "doc-001"
  title = "Tài liệu mẫu"
  content = "Nội dung tài liệu cần nạp vào ChromaDB..."
} | ConvertTo-Json

Invoke-RestMethod `
  -Uri http://localhost:8001/api/ai/ingest `
  -Method Post `
  -Headers @{ Authorization = "Bearer $token"; "Content-Type" = "application/json" } `
  -Body $body
```

Ingest lại cùng `doc_id` sẽ cập nhật các chunk mới và tự xóa các chunk cũ không còn dùng. Nội dung rỗng trả `400`; tài khoản không phải Admin trả `403`.

## Chat streaming (M3)

Endpoint: `POST /api/ai/chat/stream` (cần Bearer token .NET, mọi role đã đăng nhập).

Request:

```json
{
  "prompt": "Học phí khóa Soroban là bao nhiêu?",
  "history": [
    { "role": "user", "content": "Trung tâm có khóa nào?" },
    { "role": "assistant", "content": "Trung tâm có Finger Math và Soroban." }
  ]
}
```

- `prompt`: bắt buộc, tối đa 2000 ký tự.
- `history`: tùy chọn, tối đa 20 message, mỗi message tối đa 4000 ký tự, tổng tối đa 12000 ký tự.
- History do client giữ; backend không lưu hội thoại.

Response là SSE (`text/event-stream`):

```text
event: status
data: {"stage":"retrieving"}

event: sources
data: {"sources":[{"docId":"doc-001","title":"Tài liệu mẫu","chunkIndex":0}],"sourceChunksUsed":4}

event: delta
data: {"content":"Học phí"}

event: delta
data: {"content":" khóa Soroban..."}

event: done
data: {"content":"Học phí khóa Soroban...","sourceChunksUsed":4}
```

Nếu xảy ra lỗi sau khi bắt đầu stream:

```text
event: error
data: {"message":"Không thể kết nối tới mô hình AI. Vui lòng thử lại."}
```

Kiểm tra nhanh bằng Python:

```python
import httpx

token = "<access_token_của_người_dùng>"
with httpx.stream(
    "POST",
    "http://localhost:8001/api/ai/chat/stream",
    headers={"Authorization": f"Bearer {token}"},
    json={"prompt": "Trung tâm có khóa học nào?", "history": []},
) as response:
    response.raise_for_status()
    for line in response.iter_lines():
        print(line)
```

Sau khi stream kết thúc, backend ghi log vào bảng `ai_chat_logs` (best-effort).

## Kiểm tra JWT

Lấy access token bằng API đăng nhập của Backend .NET, sau đó:

```powershell
curl http://localhost:8001/api/ai/health -H "Authorization: Bearer <access_token>"
```

Kết quả mong đợi: `{"status":"ok","ollama":"ok","chroma":"ok","sqlite":"ok",...}`. Nếu chưa cài Ollama, `ollama` sẽ là `error` và `status` là `degraded`.

## Cấu trúc

```text
AI_Backend/
├── app/
│   ├── main.py          # FastAPI app, CORS, lifespan, DB init
│   ├── config.py        # Pydantic Settings từ .env
│   ├── api/health.py    # Health check Ollama/Chroma/SQLite
│   ├── api/rag.py       # API ingest tài liệu (Admin)
│   ├── api/chat.py      # API chat streaming SSE
│   ├── core/security.py # Verify JWT .NET (HS256)
│   ├── services/        # RAGService, chat service, Ollama streaming
│   └── db/              # SQLite engine, session, models
├── storage/             # Tự tạo: chroma/, documents/, app.db
├── check_chroma.py      # Script kiểm tra dữ liệu Chroma
├── requirements.txt
└── .env.example
```
