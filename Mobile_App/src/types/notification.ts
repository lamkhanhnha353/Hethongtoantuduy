export type NotificationType = 'SYSTEM' | 'TUITION' | 'ACADEMIC' | 'ATTENDANCE' | 'ANNOUNCEMENT';

export type NotificationResponseDto = {
  id: number;
  title: string;
  body?: string | null;
  type?: string | null;
  refId?: string | null;
  isRead: boolean;
  createdAt: string;
};

export type NotificationListResponseDto = {
  items: NotificationResponseDto[];
  page: number;
  pageSize: number;
  total: number;
  unreadCount: number;
};

export type NotificationUnreadCountResponseDto = {
  unreadCount: number;
};

export type NotificationListParams = {
  page?: number;
  pageSize?: number;
  unreadOnly?: boolean;
  type?: string | null;
};

export type MarkAllNotificationsReadResponse = {
  updated: number;
};

export const NOTIFICATION_TYPE_LABELS: Record<string, string> = {
  SYSTEM: 'Hệ thống',
  TUITION: 'Học phí',
  ACADEMIC: 'Học tập',
  ATTENDANCE: 'Điểm danh',
  ANNOUNCEMENT: 'Thông báo chung',
};

export const NOTIFICATION_TYPE_ICONS: Record<string, string> = {
  SYSTEM: 'bell',
  TUITION: 'file-text',
  ACADEMIC: 'book-open',
  ATTENDANCE: 'calendar',
  ANNOUNCEMENT: 'megaphone',
};

export const NOTIFICATION_TYPE_COLORS: Record<string, { bg: string; icon: string }> = {
  SYSTEM: { bg: 'bg-slate-100', icon: '#475569' },
  TUITION: { bg: 'bg-amber-100', icon: '#b45309' },
  ACADEMIC: { bg: 'bg-indigo-100', icon: '#4338ca' },
  ATTENDANCE: { bg: 'bg-rose-100', icon: '#e11d48' },
  ANNOUNCEMENT: { bg: 'bg-sky-100', icon: '#0284c7' },
};

export function getNotificationTypeLabel(type?: string | null): string {
  if (!type) return 'Thông báo';
  const key = type.toUpperCase();
  return NOTIFICATION_TYPE_LABELS[key] ?? 'Thông báo';
}

export function getNotificationTypeStyle(type?: string | null) {
  if (!type) return NOTIFICATION_TYPE_COLORS.SYSTEM;
  const key = type.toUpperCase();
  return NOTIFICATION_TYPE_COLORS[key] ?? NOTIFICATION_TYPE_COLORS.SYSTEM;
}
