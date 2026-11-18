import hashlib
import logging
import time
import httpx
from app.config import settings

logger = logging.getLogger(__name__)


def upload_image_to_cloudinary(image_base64: str) -> str | None:
    """
    Tải ảnh base64 lên Cloudinary (dùng chung tài khoản dys3ii0ow với Chat Realtime).
    Trả về secure HTTPS URL của ảnh trên Cloudinary để lưu vào database vĩnh viễn.
    """
    if not image_base64 or not image_base64.strip():
        return None

    # Nếu chuỗi đã là URL (http/https), giữ nguyên
    clean_data = image_base64.strip()
    if clean_data.startswith("http://") or clean_data.startswith("https://"):
        return clean_data

    cloud_name = getattr(settings, "CLOUDINARY_CLOUD_NAME", "dys3ii0ow")
    api_key = getattr(settings, "CLOUDINARY_API_KEY", "447982152694634")
    api_secret = getattr(settings, "CLOUDINARY_API_SECRET", "w7QvtmYLzZ11sMVk9En1X7kXyFo")
    folder = getattr(settings, "CLOUDINARY_FOLDER", "chat/ai_attachments")

    if not clean_data.startswith("data:"):
        clean_data = f"data:image/jpeg;base64,{clean_data}"

    timestamp = int(time.time())
    string_to_sign = f"folder={folder}&timestamp={timestamp}{api_secret}"
    signature = hashlib.sha1(string_to_sign.encode("utf-8")).hexdigest()

    data = {
        "file": clean_data,
        "api_key": api_key,
        "timestamp": str(timestamp),
        "folder": folder,
        "signature": signature,
    }

    url = f"https://api.cloudinary.com/v1_1/{cloud_name}/image/upload"
    try:
        with httpx.Client(timeout=30.0) as client:
            resp = client.post(url, data=data)
            if resp.status_code == 200:
                result = resp.json()
                secure_url = result.get("secure_url")
                logger.info("Upload ảnh AI chat lên Cloudinary thành công: %s", secure_url)
                return secure_url
            else:
                logger.error("Cloudinary upload thất bại [%d]: %s", resp.status_code, resp.text)
    except Exception as exc:
        logger.exception("Lỗi khi kết nối Cloudinary: %s", exc)

    # Dự phòng: Nếu mạng gián đoạn, giữ lại chuỗi base64 data url để ảnh không bị mất
    return clean_data
