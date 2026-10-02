export type UserRole = 'Parent' | 'Teacher';

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
  role: UserRole;
  profileId: number;
};
export type StoredSession = AuthResponseDto & {
  phone: string;
  fullName: string;
};
