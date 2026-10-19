import { apiClient } from '@/services/apiClient';
import type {
  AuthResponseDto,
  ChangePasswordRequestDto,
  ChangePasswordResponseDto,
  LoginRequestDto,
  RegisterRequestDto,
} from '@/types/auth';

export async function login(payload: LoginRequestDto): Promise<AuthResponseDto> {
  const { data } = await apiClient.post<AuthResponseDto>('/api/auth/login', payload);
  return data;
}

export async function register(payload: RegisterRequestDto): Promise<AuthResponseDto> {
  const { data } = await apiClient.post<AuthResponseDto>('/api/auth/register', payload);
  return data;
}

export async function changePassword(
  payload: ChangePasswordRequestDto,
): Promise<ChangePasswordResponseDto> {
  const { data } = await apiClient.post<ChangePasswordResponseDto>(
    '/api/auth/change-password',
    payload,
  );
  return data;
}
