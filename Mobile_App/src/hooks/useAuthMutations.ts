import { useMutation, useQueryClient } from '@tanstack/react-query';

import { ApiError } from '@/services/apiError';
import { login, register } from '@/services/authService';
import { sessionQueryKey } from '@/hooks/useSession';
import { useAuthStore } from '@/store/authStore';
import type { LoginRequestDto, RegisterRequestDto, StoredSession } from '@/types/auth';

type LoginInput = LoginRequestDto & { fullName?: string };
type RegisterInput = RegisterRequestDto;

/**
 * Mutation đăng nhập: nhận payload, gọi service, rồi lưu session vào store
 * (đồng thời persist xuống SecureStore) để interceptor tự gắn token cho các
 * request sau. Ghi thêm vào cache của TanStack Query để màn hình `/` hiển thị
 * đúng ngay sau khi chuyển hướng.
 */
export function useLoginMutation() {
  const queryClient = useQueryClient();
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
    onSuccess: (session) => {
      queryClient.setQueryData(sessionQueryKey, session);
    },
  });
}

/**
 * Mutation đăng ký: tương tự login, lưu session để vào thẳng màn hình chính.
 */
export function useRegisterMutation() {
  const queryClient = useQueryClient();
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
    onSuccess: (session) => {
      queryClient.setQueryData(sessionQueryKey, session);
    },
  });
}

/**
 * Chuẩn hóa lỗi từ `mutation.error` về ApiError để tầng UI hiển thị thống nhất,
 * kể cả khi lỗi không phải ApiError (lỗi mạng, lỗi lạ).
 */
export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) {
    return error;
  }
  return new ApiError(0, 'Đã xảy ra lỗi, vui lòng thử lại.');
}
