import { apiClient } from '@/services/apiClient';
import { ApiError } from '@/services/apiError';
import type { AuthResponseDto, LoginRequestDto, RegisterRequestDto } from '@/types/auth';

export async function login(payload: LoginRequestDto): Promise<AuthResponseDto> {
  try {
    const { data } = await apiClient.post<AuthResponseDto>('/api/auth/login', payload);
    return data;
  } catch (error) {
    if (error instanceof ApiError) {
      throw error;
    }

    throw new ApiError(0, 'Đăng nhập thất bại, vui lòng thử lại.');
  }
}
export async function register(payload: RegisterRequestDto): Promise<AuthResponseDto> {
  try {
    const { data } = await apiClient.post<AuthResponseDto>('/api/auth/register', payload);
    return data;
  } catch (error) {
    if (error instanceof ApiError) {
      throw error;
    }
    throw new ApiError(0, 'Đăng ký thất bại, vui lòng thử lại.');
  }
}
