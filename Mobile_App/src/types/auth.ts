export type UserRole = 'Parent' | 'Teacher';
export type AuthRole = UserRole | 'Admin';

export type LoginRequestDto = {
  phone: string;
  password: string;
};
export type RegisterRequestDto = {
  phone: string;
  password: string;
  role: UserRole;
  fullName: string;
  email?: string;
};
export type AuthResponseDto = {
  accessToken: string;
  refreshToken: string;
  role: AuthRole;
  profileId: number;
  email?: string | null;
  fullName?: string | null;
};
export type ChangePasswordRequestDto = {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
};
export type ChangePasswordResponseDto = {
  message: string;
};
export type ForgotPasswordRequestDto = {
  email: string;
};
export type ForgotPasswordResponseDto = {
  message: string;
};
export type StoredSession = AuthResponseDto & {
  phone: string;
  fullName: string;
};
