export type NotificationType =
  | 'SYSTEM'
  | 'TUITION'
  | 'ACADEMIC'
  | 'ATTENDANCE'
  | 'ANNOUNCEMENT'
  | 'SPECIAL'
  | 'LEAVE_REQUEST'
  | 'ENROLLMENT'
  | 'PAYMENT'
  | 'ACCOUNT';

export type RealtimeNotificationDto = {
  id: number;
  type: string;
  title: string;
  body?: string | null;
  createdAt: string;
};

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

export type NotificationSettingDto = {
  type: string;
  enabled: boolean;
};

export type UpdateNotificationSettingsRequestDto = {
  settings: NotificationSettingDto[];
};

export const NOTIFICATION_TYPE_LABELS: Record<string, string> = {
  SYSTEM: 'Hệ thống',
  TUITION: 'Học phí',
  ACADEMIC: 'Học tập',
  ATTENDANCE: 'Điểm danh',
  ANNOUNCEMENT: 'Thông báo chung',
  SPECIAL: 'Đặc biệt',
  LEAVE_REQUEST: 'Đơn xin nghỉ',
  ENROLLMENT: 'Ghi danh & Xếp lớp',
  PAYMENT: 'Thanh toán',
  ACCOUNT: 'Tài khoản',
};

export const NOTIFICATION_TYPE_ICONS: Record<string, string> = {
  SYSTEM: 'bell',
  TUITION: 'credit-card',
  ACADEMIC: 'book-open',
  ATTENDANCE: 'calendar',
  ANNOUNCEMENT: 'volume-2',
  SPECIAL: 'alert-circle',
  LEAVE_REQUEST: 'file-text',
  ENROLLMENT: 'user-check',
  PAYMENT: 'check-circle',
  ACCOUNT: 'shield',
};

export const NOTIFICATION_TYPE_COLORS: Record<string, { bg: string; icon: string }> = {
  SYSTEM: { bg: 'bg-slate-100', icon: '#475569' },
  TUITION: { bg: 'bg-amber-100', icon: '#b45309' },
  ACADEMIC: { bg: 'bg-indigo-100', icon: '#4338ca' },
  ATTENDANCE: { bg: 'bg-rose-100', icon: '#e11d48' },
  ANNOUNCEMENT: { bg: 'bg-sky-100', icon: '#0284c7' },
  SPECIAL: { bg: 'bg-red-100', icon: '#dc2626' },
  LEAVE_REQUEST: { bg: 'bg-orange-100', icon: '#ea580c' },
  ENROLLMENT: { bg: 'bg-emerald-100', icon: '#059669' },
  PAYMENT: { bg: 'bg-emerald-100', icon: '#059669' },
  ACCOUNT: { bg: 'bg-violet-100', icon: '#7c3aed' },
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
