import { useEffect } from 'react';
import { AppState } from 'react-native';

import { API_URL } from '@/config/env';
import { notificationHubService } from '@/services/notificationHub';
import { getAccessToken } from '@/store/authStore';
import type { RealtimeNotificationDto } from '@/types/notification';

export const useNotificationRealtime = (
  onNewNotification: (notification: RealtimeNotificationDto) => void,
  options?: {
    apiUrl?: string;
    getToken?: () => Promise<string | null>;
    enabled?: boolean;
  },
) => {
  const apiUrl = options?.apiUrl ?? API_URL;
  const customGetToken = options?.getToken;
  const enabled = options?.enabled ?? true;

  useEffect(() => {
    if (!enabled) {
      return;
    }

    const getToken = customGetToken ?? (async () => getAccessToken());
    let isMounted = true;

    // Đăng ký listener nhận thông báo
    const unsubscribe = notificationHubService.onReceiveNotification((notification) => {
      if (isMounted) {
        onNewNotification(notification);
      }
    });

    // Bắt đầu kết nối
    void notificationHubService.startConnection(apiUrl, getToken);

    // Lắng nghe thay đổi trạng thái App: khi active thì đảm bảo kết nối vẫn duy trì
    const subscription = AppState.addEventListener('change', (nextAppState) => {
      if (nextAppState === 'active') {
        void notificationHubService.startConnection(apiUrl, getToken);
      }
    });

    return () => {
      isMounted = false;
      subscription.remove();
      unsubscribe();
    };
  }, [apiUrl, customGetToken, enabled, onNewNotification]);
};
