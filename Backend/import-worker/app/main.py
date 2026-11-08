import logging
from contextlib import asynccontextmanager
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles

from app.config import settings
from app.api.routes import router as parser_router

# Thiết lập Logging chuẩn hóa
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s"
)
logger = logging.getLogger("QuestionImportWorker")


@asynccontextmanager
async def lifespan(app: FastAPI):
    # Khởi tạo thư mục và tài nguyên khi khởi động
    settings.ensure_directories()
    logger.info("==================================================")
    logger.info(f" Khởi động {settings.APP_NAME}")
    logger.info(f" Thư mục ảnh tĩnh: {settings.IMAGE_DIR.resolve()}")
    logger.info(f" Thư mục upload: {settings.UPLOAD_DIR.resolve()}")
    logger.info(f" LLM Provider: {settings.LLM_PROVIDER}")
    logger.info("==================================================")
    yield
    logger.info(f"Đang dừng {settings.APP_NAME}...")


app = FastAPI(
    title=settings.APP_NAME,
    description="Python Service Worker bóc tách đề thi/bài tập (.docx, .pdf) và trích xuất có cấu trúc vào CSDL",
    version="1.0.0",
    lifespan=lifespan,
)

# Cấu hình CORS để Backend ASP.NET Core và Frontend có thể giao tiếp không bị chặn
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Mount thư mục phục vụ ảnh tĩnh bóc tách được
settings.ensure_directories()
app.mount("/static/images", StaticFiles(directory=str(settings.IMAGE_DIR)), name="static_images")

# Đăng ký Router
app.include_router(parser_router)


@app.get("/", tags=["System"])
async def root():
    return {
        "service": settings.APP_NAME,
        "status": "online",
        "docs_url": "/docs",
        "provider": settings.LLM_PROVIDER,
    }


@app.get("/health", tags=["System"])
async def health_check():
    return {"status": "healthy"}


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(
        "app.main:app",
        host=settings.APP_HOST,
        port=settings.APP_PORT,
        reload=True
    )
