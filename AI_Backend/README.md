# AI_Backend — RAG Chatbot Service

FastAPI service độc lập cho chatbot RAG của Hệ thống Toán Tư Duy. Giai đoạn M1 gồm khung service, xác thực JWT dùng chung với Backend .NET, SQLite và health check. Giai đoạn M2 bổ sung ingest văn bản vào ChromaDB và truy vấn RAG. Giai đoạn M3 bổ sung chat streaming SSE, history do client gửi lên và nguồn trích dẫn. Giai đoạn M4 bổ sung lưu hội thoại AI, quản lý context theo token và tóm tắt lịch sử. Giai đoạn M5 bổ sung prefix embedding Nomic, script reindex và đánh giá chất lượng retrieval. Giai đoạn M6 bổ sung registry tài liệu trong SQLite, upload `.txt/.md/.docx/.pdf` và API quản lý tri thức cho Admin.

## Yêu cầu

- Python 3.10+
- Ollama (cho M2-M5): `ollama pull qwen2.5:3b-instruct` và `ollama pull nomic-embed-text`

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
- Quản lý tài liệu tri thức (chỉ Admin): `GET/POST /api/ai/documents`, `GET/PATCH/DELETE /api/ai/documents/{doc_id}`, `POST /api/ai/documents/upload`, `POST /api/ai/documents/{doc_id}/reingest`, `GET /api/ai/documents/{doc_id}/chunks`, `GET /api/ai/documents/stats`, `POST /api/ai/documents/backfill`
- Chat RAG: `POST http://localhost:8001/api/chat`
- Chat RAG streaming (mọi role đã đăng nhập): `POST http://localhost:8001/api/ai/chat/stream`
- Quản lý hội thoại AI: `GET/POST /api/ai/conversations`, `GET/PATCH/DELETE /api/ai/conversations/{id}`, `GET /api/ai/conversations/{id}/messages`

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

## Quản lý tài liệu tri thức (M6)

`ChromaDB` chỉ lưu chunk và vector; metadata tài liệu được lưu trong bảng SQLite `knowledge_documents` (trạng thái, số chunk, nguồn file, lỗi ingest). Tất cả endpoint dưới đây cần Bearer token .NET của tài khoản **Admin**.

```text
GET    /api/ai/documents?page=1&page_size=20&search=&status=&category=
POST   /api/ai/documents                 # ingest văn bản trực tiếp
POST   /api/ai/documents/upload          # multipart: file + doc_id/title/description/category
GET    /api/ai/documents/stats
POST   /api/ai/documents/backfill        # tạo registry từ dữ liệu Chroma hiện có
GET    /api/ai/documents/{doc_id}
GET    /api/ai/documents/{doc_id}/chunks
PATCH  /api/ai/documents/{doc_id}        # title/description/category
POST   /api/ai/documents/{doc_id}/reingest
DELETE /api/ai/documents/{doc_id}
```

- `doc_id` chỉ gồm chữ, số, `.`, `_`, `-` và tối đa 200 ký tự. Nếu bỏ trống khi upload, backend tự sinh.
- Định dạng upload hỗ trợ: `.txt`, `.md`, `.markdown`, `.docx`, `.pdf`; dung lượng tối đa `MAX_DOCUMENT_FILE_SIZE_MB` (mặc định `20`).
- File được lưu tại `DOCUMENTS_DIR/{doc_id}/{tên_file}` để có thể reingest sau này.
- `reingest` đọc lại `raw_content` (hoặc file đã lưu) và tạo lại toàn bộ chunk/vector.
- `backfill` dùng khi nâng cấp từ phiên bản chỉ có Chroma; chạy một lần sau khi deploy để registry có dữ liệu cũ.
- `GET /stats` trả tổng số tài liệu, số chunk và số lượng theo trạng thái.

Upload bằng PowerShell:

```powershell
$token = "<access_token_của_Admin>"
Invoke-RestMethod `
  -Uri http://localhost:8001/api/ai/documents/upload `
  -Method Post `
  -Headers @{ Authorization = "Bearer $token" } `
  -Form @{
    file = Get-Item .\tai-lieu.pdf
    title = "Tài liệu hướng dẫn"
    category = "Hướng dẫn"
  }
```

## Chất lượng RAG

`nomic-embed-text` dùng task prefix khác nhau cho tài liệu và câu hỏi:

```dotenv
EMBEDDING_QUERY_PREFIX=search_query:
EMBEDDING_DOCUMENT_PREFIX=search_document:
```

Khi ingest, nội dung chunk được embed với prefix tài liệu; khi truy vấn, câu hỏi được embed với prefix truy vấn. Nếu đổi `EMBEDDING_MODEL` hoặc prefix, phải reindex toàn bộ collection:

```powershell
python scripts\reindex_embeddings.py
```

Đánh giá retrieval bằng bộ case tiếng Việt trong `evals/retrieval_cases.json`:

```powershell
python scripts\evaluate_retrieval.py --k 3 --verbose
```

Script in `hit@1`, `hit@k`, `MRR@k`; dùng `--json-out <path>` để lưu kết quả chi tiết. Khi knowledge base có thêm tài liệu, bổ sung case vào file JSON để theo dõi hồi quy chất lượng.

## Quản lý hội thoại AI (M4)

Tất cả endpoint cần Bearer token .NET và chỉ trả hội thoại của tài khoản hiện tại.

```text
GET    /api/ai/conversations?page=1&page_size=20
POST   /api/ai/conversations
GET    /api/ai/conversations/{conversation_id}
GET    /api/ai/conversations/{conversation_id}/messages?page=1&page_size=30
PATCH  /api/ai/conversations/{conversation_id}
DELETE /api/ai/conversations/{conversation_id}
```

Tạo hội thoại:

```json
{
  "title": "Tư vấn khóa Soroban"
}
```

`title` có thể bỏ trống. Khi người dùng gửi câu hỏi đầu tiên, nếu tiêu đề vẫn là mặc định thì backend lấy 50 ký tự đầu của câu hỏi làm tiêu đề.

Load lịch sử:

```text
GET /api/ai/conversations/12/messages?page=1&page_size=30
```

Message trả về gồm `id`, `conversationId`, `role`, `content`, `tokenCount`, `sources`, `status`, `createdAt`.

## Chat streaming (M3/M4)

Endpoint: `POST /api/ai/chat/stream` (cần Bearer token .NET, mọi role đã đăng nhập).

Request có lưu hội thoại:

```json
{
  "conversation_id": 12,
  "prompt": "Học phí khóa Soroban là bao nhiêu?"
}
```

Request stateless kiểu cũ vẫn được hỗ trợ:

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
- `conversation_id`: tùy chọn. Khi có, backend tự lưu user message và assistant message.
- `history`: chỉ dùng cho chế độ stateless. Nếu có `conversation_id`, backend bỏ qua `history`.
- `history`: tối đa 20 message, mỗi message tối đa 4000 ký tự, tổng tối đa 12000 ký tự.

Response là SSE (`text/event-stream`):

```text
event: status
data: {"stage":"retrieving"}

event: status
data: {"stage":"summarizing"}

event: sources
data: {"sources":[{"docId":"doc-001","title":"Tài liệu mẫu","chunkIndex":0}],"sourceChunksUsed":4}

event: delta
data: {"content":"Học phí"}

event: delta
data: {"content":" khóa Soroban..."}

event: done
data: {"content":"Học phí khóa Soroban...","sourceChunksUsed":4,"conversationId":12,"messageId":45,"title":"Học phí khóa Soroban"}
```

`status: summarizing` chỉ xuất hiện khi lịch sử vượt ngân sách token và backend phải tóm tắt các lượt cũ.

Nếu xảy ra lỗi sau khi bắt đầu stream:

```text
event: error
data: {"message":"Không thể kết nối tới mô hình AI. Vui lòng thử lại.","conversationId":12}
```

Kiểm tra nhanh bằng Python:

```python
import httpx

token = "<access_token_của_người_dùng>"
with httpx.stream(
    "POST",
    "http://localhost:8001/api/ai/chat/stream",
    headers={"Authorization": f"Bearer {token}"},
    json={"conversation_id": 12, "prompt": "Trung tâm có khóa học nào?"},
) as response:
    response.raise_for_status()
    for line in response.iter_lines():
        print(line)
```

Backend ghi log audit vào bảng `ai_chat_logs` (best-effort) và lưu hội thoại vào `ai_conversations`, `ai_messages`.

## Quản lý context theo token

- Backend ước lượng token theo ký tự, cấu hình bằng `TOKEN_ESTIMATE_CHARS_PER_TOKEN` (mặc định `3.0`).
- Luôn cố giữ 10 cặp `user + assistant` gần nhất (`RECENT_TURNS_TO_KEEP=10`).
- Nếu còn ngân sách token, các lượt cũ chưa tóm tắt vẫn được đưa vào context.
- Khi tổng context vượt `CONTEXT_TOKEN_BUDGET` (mặc định `6000`), các lượt cũ hơn sẽ được tóm tắt bằng `SUMMARY_MODEL` (mặc định `qwen2.5:3b-instruct`).
- Tóm tắt được lưu vào `ai_conversations.summary` và mốc `summary_until_message_id`, nên không phải tóm tắt lại từ đầu.
- Nếu tóm tắt lỗi, backend fallback sang cắt bớt history để vẫn trả lời được.
- Nếu vẫn vượt ngân sách, backend giảm số lượt gần nhất nhưng không thấp hơn `MIN_RECENT_TURNS`, sau đó cắt bớt nội dung message dài.
- Chế độ stateless chỉ cắt history theo ngân sách, không tóm tắt và không lưu DB.

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
│   ├── api/documents.py # API quản lý tài liệu tri thức (Admin)
│   ├── api/chat.py      # API chat streaming SSE
│   ├── api/conversations.py # API quản lý hội thoại AI
│   ├── api/deps.py      # Dependency xác thực/role dùng chung
│   ├── core/security.py # Verify JWT .NET (HS256)
│   ├── services/        # RAG, document, chat, conversation, memory
│   └── db/              # SQLite engine, session, models
├── storage/             # Tự tạo: chroma/, documents/, app.db
├── check_chroma.py      # Script kiểm tra dữ liệu Chroma
├── requirements.txt
└── .env.example
```
