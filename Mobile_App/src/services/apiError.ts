import type { AuthResult, LoginPayload, RegisterPayload } from '@/types/auth';

export type ApiErrorPayload = {
  code?: number;
  message?: string;
  details?: string;
  errors?: Record<string, string[]>;
};

export class ApiError extends Error {
  status: number;
  fieldErrors: Record<string, string[]>;

  constructor(status: number, message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

const MOCK_LATENCY_MS = 600;

function delay(ms: number) {
  return new Promise<void>((resolve) => setTimeout(resolve, ms));
}

const mockAccounts = new Map<string, { password: string; result: AuthResult; fullName: string }>();

function buildMockResult(phone: string, role: 'Parent' | 'Teacher'): AuthResult {
  return {
    accessToken: `mock-access-token-${phone}-${Date.now()}`,
    refreshToken: `mock-refresh-token-${phone}-${Date.now()}`,
    role,
    profileId: mockAccounts.size + 1,
  };
}

async function mockLogin(payload: LoginPayload): Promise<AuthResult> {
  await delay(MOCK_LATENCY_MS);

  if (payload.phone !== '0987654321') {
    throw new ApiError(401, 'Số điện thoại hoặc mật khẩu không chính xác.');
  }

  if (payload.password !== 'Password123!') {
    throw new ApiError(401, 'Số điện thoại hoặc mật khẩu không chính xác.');
  }

  return buildMockResult(payload.phone, 'Parent');
}

async function mockRegister(payload: RegisterPayload): Promise<AuthResult> {
  await delay(MOCK_LATENCY_MS);

  if (payload.phone.length < 9) {
    throw new ApiError(400, 'Số điện thoại không hợp lệ.', {
      Phone: ['Số điện thoại không hợp lệ.'],
    });
  }

  if (payload.password.length < 6) {
    throw new ApiError(400, 'Mật khẩu phải có ít nhất 6 ký tự.', {
      Password: ['Mật khẩu phải có ít nhất 6 ký tự.'],
    });
  }

  if (payload.role === 'Teacher' && !payload.email) {
    throw new ApiError(400, 'Email là thông tin bắt buộc đối với Teacher.', {
      Email: ['Email là thông tin bắt buộc đối với Teacher.'],
    });
  }

  if (mockAccounts.has(payload.phone)) {
    throw new ApiError(400, 'Lỗi tạo tài khoản: Số điện thoại đã tồn tại.', {
      Phone: ['Số điện thoại đã tồn tại.'],
    });
  }

  const result = buildMockResult(payload.phone, payload.role);
  mockAccounts.set(payload.phone, { password: payload.password, result, fullName: payload.fullName });

  return result;
}

export { mockLogin, mockRegister };
