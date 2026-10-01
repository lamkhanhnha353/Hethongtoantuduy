import { useMutation, useQueryClient } from '@tanstack/react-query';

import { ApiError } from '@/services/apiError';
import { login, register } from '@/services/authService';
import { saveSession } from '@/services/tokenStorage';
import { sessionQueryKey } from '@/hooks/useSession';
import type { LoginPayload, RegisterPayload, StoredSession } from '@/types/auth';

type LoginInput = LoginPayload & { fullName?: string };
type RegisterInput = RegisterPayload;

/**
 * Mutation đăng nhập: nhận payload, gọi service, rồi lưu session xuống SecureStore.
 * Việc lưu session nằm trong `mutationFn` để UI chỉ cần theo dõi trạng thái
 * `isPending` / `isError` / `isSuccess` mà không cần quản lý `loading` thủ công.
 * Đồng thời ghi session vào cache để màn hình `/` hiển thị đúng ngay sau khi chuyển hướng.
 */
export function useLoginMutation() {
  const queryClient = useQueryClient();

  return useMutation<StoredSession, unknown, LoginInput>({
    mutationFn: async ({ fullName, ...payload }) => {
      const result = await login(payload);
      const session: StoredSession = {
        ...result,
        phone: payload.phone,
        fullName: fullName ?? '',
      };
      await saveSession(session);
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

  return useMutation<StoredSession, unknown, RegisterInput>({
    mutationFn: async (payload) => {
      const result = await register(payload);
      const session: StoredSession = {
        ...result,
        phone: payload.phone,
        fullName: payload.fullName,
      };
      await saveSession(session);
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
