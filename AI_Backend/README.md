# AI_Backend — RAG Chatbot Service (M1)

FastAPI service độc lập cho chatbot RAG của Hệ thống Toán Tư Duy. Giai đoạn M1 gồm khung service, xác thực JWT dùng chung với Backend .NET, SQLite và health check.

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
│   ├── core/security.py # Verify JWT .NET (HS256)
│   └── db/              # SQLite engine, session, models
├── storage/             # Tự tạo: chroma/, documents/, app.db
├── requirements.txt
└── .env.example
```
