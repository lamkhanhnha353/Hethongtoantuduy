import { apiClient } from '@/services/apiClient';
import type {
  ProfileResponseDto,
  SetPinRequestDto,
  UpdateProfileRequestDto,
  VerifyPinRequestDto,
  VerifyPinResponseDto,
} from '@/types/profile';

export async function getProfile(): Promise<ProfileResponseDto> {
  const { data } = await apiClient.get<ProfileResponseDto>('/api/profile');
  return data;
}

export async function updateProfile(payload: UpdateProfileRequestDto): Promise<ProfileResponseDto> {
  const { data } = await apiClient.put<ProfileResponseDto>('/api/profile', payload);
  return data;
}

export async function setPin(payload: SetPinRequestDto): Promise<void> {
  await apiClient.put('/api/profile/pin', payload);
}

export async function verifyPin(payload: VerifyPinRequestDto): Promise<VerifyPinResponseDto> {
  const { data } = await apiClient.post<VerifyPinResponseDto>('/api/profile/pin/verify', payload);
  return data;
}
