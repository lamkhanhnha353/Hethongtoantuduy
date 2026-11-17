import { apiClient } from '@/services/apiClient';
import type {
  MarkAllNotificationsReadResponse,
  NotificationListParams,
  NotificationListResponseDto,
  NotificationUnreadCountResponseDto,
} from '@/types/notification';

const DEFAULT_PAGE_SIZE = 20;

export async function getNotifications(
  params: NotificationListParams = {},
): Promise<NotificationListResponseDto> {
  const { data } = await apiClient.get<NotificationListResponseDto>('/api/notifications', {
    params: {
      page: params.page ?? 1,
      pageSize: params.pageSize ?? DEFAULT_PAGE_SIZE,
      unreadOnly: params.unreadOnly || undefined,
      type: params.type || undefined,
    },
  });

  return data;
}

export async function getUnreadCount(): Promise<NotificationUnreadCountResponseDto> {
  const { data } = await apiClient.get<NotificationUnreadCountResponseDto>('/api/notifications/unread-count',
  );

  return data;
}

export async function markNotificationRead(notificationId: number): Promise<void> {
  await apiClient.put(`/api/notifications/${notificationId}/read`);
}

export async function markAllNotificationsRead(): Promise<MarkAllNotificationsReadResponse> {
  const { data } = await apiClient.put<MarkAllNotificationsReadResponse>('/api/notifications/read-all',
  );

  return data;
}
