export type UserRole = 'Parent' | 'Teacher';

export type LoginPayload = {
  phone: string;
  password: string;
};

export type RegisterPayload = {
  phone: string;
  password: string;
  role: UserRole;
  fullName: string;
  email?: string;
};

export type AuthResult = {
  accessToken: string;
  refreshToken: string;
  role: UserRole;
  profileId: number;
};

export type StoredSession = AuthResult & {
  phone: string;
  fullName: string;
};
