import { router } from 'expo-router';

import { queryClient } from '@/lib/queryClient';
import { useAuthStore } from '@/store/authStore';

export const SESSION_EXPIRED_MESSAGE = 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.';

let redirecting = false;

/**
 * Dọn phiên khi máy chủ trả 401 vì token hết hạn: xoá store + storage, xoá cache
 * dữ liệu máy chủ và đưa người dùng về màn đăng nhập kèm lý do. Có cờ chống trùng
 * để nhiều request 401 cùng lúc không đá người dùng nhiều lần.
 */
export async function handleSessionExpired() {
  if (redirecting) {
    return;
  }

  redirecting = true;

  try {
    await useAuthStore.getState().logout();
    queryClient.clear();
    router.replace({ pathname: '/login', params: { reason: 'expired' } });
  } finally {
    redirecting = false;
  }
}


