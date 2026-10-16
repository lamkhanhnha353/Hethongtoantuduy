import { useMutation } from '@tanstack/react-query';

import { login, register } from '@/services/authService';
import { useAuthStore } from '@/store/authStore';
import type { LoginRequestDto, RegisterRequestDto, StoredSession } from '@/types/auth';

type LoginInput = LoginRequestDto & { fullName?: string };
type RegisterInput = RegisterRequestDto;

/**
 * Mutation đăng nhập: nhận payload, gọi service, rồi lưu session vào authStore
 * (đồng thời persist xuống SecureStore) để interceptor tự gắn token cho các
 * request sau và mọi màn hình đọc được phiên mới ngay lập tức.
 */
export function useLoginMutation() {
  const setSession = useAuthStore((state) => state.setSession);

  return useMutation<StoredSession, unknown, LoginInput>({
    mutationFn: async ({ fullName, ...payload }) => {
      const result = await login(payload);
      const session: StoredSession = {
        ...result,
        phone: payload.phone,
        fullName: fullName ?? '',
      };
      await setSession(session);
      return session;
    },
  });
}

/**
 * Mutation đăng ký: tương tự login, lưu session để vào thẳng màn hình chính.
 */
export function useRegisterMutation() {
  const setSession = useAuthStore((state) => state.setSession);

  return useMutation<StoredSession, unknown, RegisterInput>({
    mutationFn: async (payload) => {
      const result = await register(payload);
      const session: StoredSession = {
        ...result,
        phone: payload.phone,
        fullName: payload.fullName,
      };
      await setSession(session);
      return session;
    },
  });
}
