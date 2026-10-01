import { apiClient } from '@/services/apiClient';
import { ApiError } from '@/services/apiError';
import type { AuthResult, LoginPayload, RegisterPayload } from '@/types/auth';

function toAuthResult(payload: Record<string, unknown>): AuthResult {
  return {
    accessToken: String(payload.accessToken ?? payload.AccessToken ?? ''),
    refreshToken: String(payload.refreshToken ?? payload.RefreshToken ?? ''),
    role: (payload.role ?? payload.Role) as AuthResult['role'],
    profileId: Number(payload.profileId ?? payload.ProfileId ?? 0),
  };
}

export async function login(payload: LoginPayload): Promise<AuthResult> {
  try {
    const { data } = await apiClient.post<Record<string, unknown>>('/api/auth/login', payload);

    return toAuthResult(data);
  } catch (error) {
    if (error instanceof ApiError) {
      throw error;
    }

    throw new ApiError(0, 'Đăng nhập thất bại, vui lòng thử lại.');
  }
}

export async function register(payload: RegisterPayload): Promise<AuthResult> {
  try {
    const { data } = await apiClient.post<Record<string, unknown>>('/api/auth/register', payload);

    return toAuthResult(data);
  } catch (error) {
    if (error instanceof ApiError) {
      throw error;
    }

    throw new ApiError(0, 'Đăng ký thất bại, vui lòng thử lại.');
  }
}
