
// src/types/lesson.ts
export interface StudentLessonDto {
  lessonId: number;
  classId: number;
  className: string;
  lessonDate: string;
  startTime: string | null;
  endTime: string | null;
  title: string;
  status: string;
  isImportant: boolean;
  teacherName: string | null;
  room: string | null;
}

export interface StudentScheduleLesson extends StudentLessonDto {
  studentId: number;
  studentName: string;
  startLabel: string;
  endLabel: string;
}

export type LeaveRequestStatus = 'PENDING' | 'APPROVED' | 'REJECTED' | 'CANCELLED' | string;

export interface LeaveRequestDto {
  id: number;
  studentId: number;
  studentName: string;
  lessonId: number;
  lessonTitle: string;
  lessonDate: string;
  reason: string | null;
  status: LeaveRequestStatus;
  createdAt: string | null;
}

export interface CreateLeaveRequestDto {
  studentId: number;
  lessonId: number;
  reason?: string;
}