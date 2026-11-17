from fastapi import HTTPException, Security, status
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer
from jose import JWTError, jwt

from app.config import settings

security = HTTPBearer(auto_error=False)

DOTNET_ROLE_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"


def verify_jwt_token(
    credentials: HTTPAuthorizationCredentials | None = Security(security),
) -> dict:
    if credentials is None:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Thiếu Authorization header",
        )

    try:
        payload = jwt.decode(
            credentials.credentials,
            settings.JWT_SECRET_KEY,
            algorithms=[settings.JWT_ALGORITHM],
            issuer=settings.JWT_ISSUER,
            audience=settings.JWT_AUDIENCE,
        )
    except JWTError as exc:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="JWT không hợp lệ hoặc đã hết hạn",
        ) from exc

    account_id = payload.get("sub") or payload.get("nameid")
    role = payload.get("role") or payload.get(DOTNET_ROLE_CLAIM)
    if isinstance(role, list):
        role = role[0] if role else None

    if not account_id:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Token không chứa thông tin định danh người dùng (sub/nameid)",
        )

    return {
        "accountId": str(account_id),
        "role": role,
        "claims": payload,
    }
