import type { UserRole } from '@/types/auth';

export type ProfileResponseDto = {
  accountId: number;
  phone: string;
  role: UserRole;
  fullName: string;
  email: string | null;
  emailConfirm: boolean;
  avatar: string | null;
  hasPin: boolean;
};

export type UpdateProfileRequestDto = {
  fullName: string;
};

export type UpdateParentEmailRequestDto = {
  email: string;
};

export type SetPinRequestDto = {
  newPin: string;
  currentPin?: string;
};

export type VerifyPinRequestDto = {
  pin: string;
};

export type VerifyPinResponseDto = {
  valid: boolean;
};
