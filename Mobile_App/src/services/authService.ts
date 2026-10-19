import { apiClient } from '@/services/apiClient';
import type {
  AuthResponseDto,
  ChangePasswordRequestDto,
  ChangePasswordResponseDto,
  ForgotPasswordRequestDto,
  ForgotPasswordResponseDto,
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

export async function forgotPassword(
  payload: ForgotPasswordRequestDto,
): Promise<ForgotPasswordResponseDto> {
  const { data } = await apiClient.post<ForgotPasswordResponseDto>(
    '/api/auth/forgot-password',
    payload,
  );
  return data;
}
