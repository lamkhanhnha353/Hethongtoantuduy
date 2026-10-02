import type { UserRole } from '@/types/auth';

export const ROLE_LABELS: Record<UserRole, string> = {
  Parent: 'Phụ huynh',
  Teacher: 'Giáo viên',
};

export const ROLE_BADGE_CLASS: Record<UserRole, string> = {
  Parent: 'bg-amber-100 text-amber-800',
  Teacher: 'bg-emerald-100 text-emerald-800',
};

export const ROLE_AVATAR_CLASS: Record<UserRole, string> = {
  Parent: 'bg-amber-500',
  Teacher: 'bg-emerald-600',
};

export const ROLE_SUMMARY: Record<UserRole, string> = {
  Parent: 'Theo dõi học tập, lịch học và học phí của con bạn.',
  Teacher: 'Quản lý lớp phụ trách, buổi học và bài tập của học sinh.',
};

export type HomeFeature = {
  id: string;
  title: string;
  description: string;
};

export const ROLE_FEATURES: Record<UserRole, HomeFeature[]> = {
  Parent: [
    { id: 'students', title: 'Học sinh của tôi', description: 'Tiến độ và kết quả học tập' },
    { id: 'schedule', title: 'Lịch học', description: 'Buổi học sắp tới của con' },
    { id: 'invoices', title: 'Học phí', description: 'Hóa đơn và thanh toán' },
    { id: 'assignments', title: 'Bài tập', description: 'Bài tập về nhà cần hoàn thành' },
  ],
  Teacher: [
    { id: 'classes', title: 'Lớp phụ trách', description: 'Danh sách lớp và học sinh' },
    { id: 'lessons', title: 'Buổi học', description: 'Lịch dạy và tiến độ giảng bài' },
    { id: 'assignments', title: 'Bài tập', description: 'Giao và chấm bài tập' },
    { id: 'attendance', title: 'Điểm danh', description: 'Điểm danh học sinh theo lớp' },
  ],
};

export function getGreeting(date: Date = new Date()): string {
  const hour = date.getHours();

  if (hour < 11) {
    return 'Chào buổi sáng';
  }

  if (hour < 18) {
    return 'Chào buổi chiều';
  }

  return 'Chào buổi tối';
}