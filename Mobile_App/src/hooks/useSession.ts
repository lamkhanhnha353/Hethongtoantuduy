import { useQuery, useQueryClient } from '@tanstack/react-query';

import { loadSession, clearSession } from '@/services/tokenStorage';
import type { StoredSession } from '@/types/auth';

export const sessionQueryKey = ['session'] as const;

/**
 * Đọc phiên đăng nhập từ SecureStore. Đây là dữ liệu cục bộ nên
 * `staleTime: Infinity` + `gcTime: Infinity` để không refetch không cần thiết.
 */
export function useSessionQuery() {
  return useQuery<StoredSession | null>({
    queryKey: sessionQueryKey,
    queryFn: loadSession,
    staleTime: Infinity,
    gcTime: Infinity,
  });
}

/**
 * Xóa session cả trong storage lẫn cache của TanStack Query
 * để các màn hình khác tự động phản ánh trạng thái đã đăng xuất.
 */
export function useClearSessionMutation() {
  const queryClient = useQueryClient();

  return async function clear() {
    await clearSession();
    queryClient.setQueryData(sessionQueryKey, null);
  };
}
