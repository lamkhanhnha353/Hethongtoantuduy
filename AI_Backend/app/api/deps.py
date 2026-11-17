from fastapi import Depends, HTTPException, status

from app.core.security import verify_jwt_token


def require_admin(user: dict = Depends(verify_jwt_token)) -> dict:
    role = str(user.get("role") or "").strip().lower()
    if role != "admin":
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail="Chỉ Admin được phép quản lý tài liệu",
        )
    return user
