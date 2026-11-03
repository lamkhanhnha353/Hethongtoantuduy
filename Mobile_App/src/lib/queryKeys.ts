import type { NotificationType } from '@/types/notification';

export const profileQueryKey = ['profile'] as const;

export const notificationsRootQueryKey = ['notifications'] as const;

export const notificationListRootQueryKey = ['notifications', 'list'] as const;

export const notificationUnreadCountQueryKey = ['notifications', 'unreadCount'] as const;

export function notificationListQueryKey(filters: {
  unreadOnly: boolean;
  type: NotificationType | null;
}) {
  return [...notificationListRootQueryKey, filters.unreadOnly, filters.type] as const;
}

export const studentsQueryKey = ['students'] as const;

export function studentQueryKey(id: number) {
  return ['students', id] as const;
}

export function studentLessonsQueryKey(studentIds: number[]) {
  return ['studentLessons', studentIds] as const;
}

export function leaveRequestsQueryKey(studentIds: number[]) {
  return ['leaveRequests', studentIds] as const;
}

export function studentRoadmapsQueryKey(studentIds: number[]) {
  return ['studentRoadmaps', studentIds] as const;
}
