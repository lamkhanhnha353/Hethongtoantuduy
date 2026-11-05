import shutil
from pathlib import Path
from typing import Optional
import uuid

from fastapi import APIRouter, UploadFile, File, Form, BackgroundTasks, HTTPException, Body, status
from app.config import settings
from app.schemas.api import (
    ProcessFileResponse,
    JobDetailResponse,
    JobStatus,
    ProcessFileRequest,
)
from app.services.job_manager import job_manager

router = APIRouter(prefix="/api/parser", tags=["Question Parser Worker"])


@router.post("/process-file", response_model=ProcessFileResponse, status_code=status.HTTP_202_ACCEPTED)
async def process_file_upload(
    background_tasks: BackgroundTasks,
    file: Optional[UploadFile] = File(None),
    bank_id: int = Form(settings.DEFAULT_BANK_ID),
    job_id: Optional[str] = Form(None),
):
    """
    Tiếp nhận tệp đề thi (.docx, .pdf) qua Multipart Form Upload.
    Khởi chạy pipeline xử lý ngầm (Asynchronous Worker).
    """
    if not file:
        raise HTTPException(status_code=400, detail="Vui lòng đính kèm file (.docx hoặc .pdf)")

    allowed_exts = [".docx", ".pdf"]
    ext = Path(file.filename).suffix.lower()
    if ext not in allowed_exts:
        raise HTTPException(
            status_code=400,
            detail=f"Định dạng '{ext}' không được hỗ trợ. Chỉ chấp nhận các file: {', '.join(allowed_exts)}"
        )

    # Đảm bảo thư mục lưu trữ tồn tại
    settings.ensure_directories()

    # Tạo Job ID
    assigned_job_id = job_id or str(uuid.uuid4())
    save_filename = f"{assigned_job_id}_{file.filename}"
    saved_file_path = settings.UPLOAD_DIR / save_filename

    # Lưu tệp upload ra đĩa
    with open(saved_file_path, "wb") as buffer:
        shutil.copyfileobj(file.file, buffer)

    # Khởi tạo Job trong hệ thống
    job_manager.create_job(file_name=file.filename, bank_id=bank_id, custom_job_id=assigned_job_id)

    # Đẩy pipeline xử lý vào tác vụ nền (Background Task)
    background_tasks.add_task(
        job_manager.run_pipeline,
        job_id=assigned_job_id,
        file_path=saved_file_path,
        bank_id=bank_id
    )

    return ProcessFileResponse(
        job_id=assigned_job_id,
        status=JobStatus.PENDING,
        message="Tệp đã được tiếp nhận và đang được xử lý trong nền.",
        file_name=file.filename,
        bank_id=bank_id,
        check_status_url=f"/api/parser/job/{assigned_job_id}"
    )


@router.post("/process-path", response_model=ProcessFileResponse, status_code=status.HTTP_202_ACCEPTED)
async def process_file_by_path(
    request: ProcessFileRequest,
    background_tasks: BackgroundTasks,
):
    """
    Tiếp nhận yêu cầu xử lý từ Backend chính với đường dẫn tệp đã lưu sẵn trên server.
    """
    if not request.file_path:
        raise HTTPException(status_code=400, detail="Thiếu tham số 'file_path'")

    file_path = Path(request.file_path)
    if not file_path.exists():
        raise HTTPException(status_code=404, detail=f"Không tìm thấy tệp tại đường dẫn: {request.file_path}")

    assigned_job_id = request.job_id or str(uuid.uuid4())
    job_manager.create_job(file_name=file_path.name, bank_id=request.bank_id, custom_job_id=assigned_job_id)

    background_tasks.add_task(
        job_manager.run_pipeline,
        job_id=assigned_job_id,
        file_path=file_path,
        bank_id=request.bank_id
    )

    return ProcessFileResponse(
        job_id=assigned_job_id,
        status=JobStatus.PENDING,
        message="Yêu cầu đã được tiếp nhận và bắt đầu xử lý.",
        file_name=file_path.name,
        bank_id=request.bank_id,
        check_status_url=f"/api/parser/job/{assigned_job_id}"
    )


@router.get("/job/{job_id}", response_model=JobDetailResponse)
async def get_job_status(job_id: str):
    """
    Kiểm tra trạng thái tiến độ xử lý và lấy danh sách câu hỏi staging đã parse xong.
    """
    job = job_manager.get_job(job_id)
    if not job:
        raise HTTPException(status_code=404, detail=f"Không tìm thấy Job có mã: {job_id}")

    return job
