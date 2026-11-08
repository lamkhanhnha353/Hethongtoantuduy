# Hệ thống Toán Tư Duy

Nền tảng quản lý học tập toán tư duy: khóa học, lớp học, buổi học, bài tập, điểm, chuyên cần và học phí — dành cho phụ huynh, học sinh và giáo viên.

---

## Tổng quan

| Module | Thư mục | Vai trò |
| --- | --- | --- |
| Backend API | `Backend/` | REST API (.NET 10), xử lý nghiệp vụ, SQL Server |
| AI Backend | `AI_Backend/` | RAG chatbot (FastAPI + Ollama + ChromaDB) |
| Import Worker | `import-worker/` | Trích xuất tài liệu (PDF/DOCX) và nhập vào vector DB |
| Mobile App | `Mobile_App/` | App iOS/Android (Expo 58, React Native 0.88) |
| Web Frontend | `Web_Frontend/` | SPA quản trị (Vue 3, Vite) |

---

## Công nghệ

### Backend — `Backend/`

- **.NET 10** · ASP.NET Core Web API · Entity Framework Core 10
- **SQL Server 2022** · ASP.NET Core Identity · JWT Bearer
- OpenAPI (Swagger) ở môi trường Development

### AI Backend — `AI_Backend/`

- **FastAPI** · Uvicorn · Python
- **Ollama** (chat model + embedding model cục bộ)
- **ChromaDB** (vector store, lưu trên disk)
- SQLAlchemy · Pydantic v2 · python-jose (JWT)

### Import Worker — `import-worker/`

- **FastAPI** · Uvicorn · Python
- Trích xuất PDF (`PyMuPDF`) và DOCX (`python-docx`)
- Tích hợp LLM: **Google Gemini** (`google-genai`) · **OpenAI** · Ollama
- Pydantic v2 · aiofiles · httpx

### Mobile App — `Mobile_App/`

- **Expo 58** · React Native 0.88 · React 19 · TypeScript 6
- **NativeWind 4** (Tailwind CSS cho React Native)
- Expo Router (file-based routing)
- **Zustand** (state management) · **TanStack Query** (data fetching)
- **Skia** (đồ họa) · SignalR (realtime) · Axios · jwt-decode
- EAS Build (build & deploy trên cloud)

### Web Frontend — `Web_Frontend/`

- **Vue 3** · Vue Router 5 · Vite 8 · JavaScript (ESM)
- **Element Plus** (UI component library) · Heroicons
- **Tailwind CSS 4** · Axios · Google Sign-In

---

## Chạy từng module

### Backend

Yêu cầu: .NET SDK 10 · SQL Server đang chạy.

```powershell
cd Backend\ToanTuDuy
dotnet ef database update   # áp dụng migration
dotnet run
```

API: `http://localhost:5114` / `https://localhost:7206`

Ghi đè chuỗi kết nối (một lần duy nhất):

```powershell
[Environment]::SetEnvironmentVariable(
  'ConnectionStrings__DefaultConnection',
  'Server=.;Database=ToanTuDuyDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true',
  'User')
```

### AI Backend

Yêu cầu: Python 3.11+ · Ollama đang chạy cục bộ.

```bash
cd AI_Backend
python -m venv venv && source venv/bin/activate  # Windows: venv\Scripts\activate
pip install -r requirements.txt
cp .env.example .env   # điền các biến cần thiết
uvicorn app.main:app --reload
```

### Import Worker

Yêu cầu: Python 3.11+.

```bash
cd import-worker
python -m venv venv && source venv/bin/activate
pip install -r requirements.txt
cp .env.example .env
uvicorn app.main:app --reload --port 8001
```

### Mobile App

Yêu cầu: Node.js 22+ · Expo CLI.

```bash
cd Mobile_App
npm install
npx expo start          # mở Expo Go hoặc dev client
npx expo start --android
npx expo start --ios
```

> Không tạo thư mục `ios/` hoặc `android/` thủ công — dự án dùng Continuous Native Generation, mọi cấu hình native đặt trong `app.json`.

### Web Frontend

Yêu cầu: Node.js 22+.

```bash
cd Web_Frontend
npm install
npm run dev     # dev server, proxy /api → http://localhost:5114
npm run build
```

---

## Cơ sở dữ liệu

Database `ToanTuDuyDb` trên SQL Server, xác thực Windows. **24 bảng**: 17 bảng nghiệp vụ, 6 bảng Identity, 1 bảng migration.

Bảng nghiệp vụ: `ACCOUNT`, `PARENT_PROFILE`, `STUDENT_PROFILE`, `TEACHER_PROFILE`, `COURSE`, `CLASSES`, `CLASS_SCHEDULE`, `CLASS_STUDENT`, `LESSON`, `ATTENDANCE`, `LEAVE_REQUEST`, `ASSIGNMENT`, `STUDENT_ASSIGNMENT`, `QUESTION`, `INVOICE`, `BADGE`, `STUDENT_BADGE`.

Lệnh migration:

```powershell
cd Backend\ToanTuDuy
dotnet ef migrations add TenMigrationMoi
dotnet ef database update
```

---

## API hiện có

| Method | Endpoint | Mô tả |
| --- | --- | --- |
| POST | `/api/auth/register` | Đăng ký (Parent / Teacher) |
| POST | `/api/auth/login` | Đăng nhập, trả access + refresh token |
| POST | `/api/chat` | Chat với AI (yêu cầu JWT) |
| GET | `/openapi/v1.json` | Tài liệu OpenAPI (dev only) |

---

## Quy trình Git

Nhánh theo dạng `<module>/feat/<mo-ta>`, ví dụ:

- `backend/feat/course-endpoints`
- `mobile/feat/login-screen`
- `ai/feat/rag-service`

Commit hoặc `git stash -u` trước khi merge để tránh lỗi trên cây làm việc bẩn.

---

## Trạng thái

| Module | Trạng thái |
| --- | --- |
| Backend | Xác thực hoạt động; endpoint nghiệp vụ (khóa học, lớp, bài tập…) chưa phát triển |
| AI Backend | RAG chatbot hoạt động với Ollama + ChromaDB |
| Import Worker | Trích xuất PDF/DOCX, hỗ trợ Gemini / OpenAI / Ollama |
| Mobile | Cấu trúc khung; màn hình đăng nhập chưa hoàn thiện |
| Web | Khung dựng; chưa có tích hợp API |
