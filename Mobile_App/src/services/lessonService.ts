import { apiClient } from '@/services/apiClient';
import type {
  CreateLeaveRequestDto,
  LeaveRequestDto,
  StudentLessonDto,
} from '@/types/lesson';

export async function getStudentLessons(studentId: number): Promise<StudentLessonDto[]> {
  const { data } = await apiClient.get<StudentLessonDto[]>(
    `/api/lessons/student/${studentId}`,
  );
  return data;
}

export async function getStudentLeaveRequests(studentId: number): Promise<LeaveRequestDto[]> {
  const { data } = await apiClient.get<LeaveRequestDto[]>(
    `/api/LeaveRequests/student/${studentId}`,
  );
  return data;
}

export async function createLeaveRequest(
  payload: CreateLeaveRequestDto,
): Promise<LeaveRequestDto> {
  const { data } = await apiClient.post<LeaveRequestDto>('/api/LeaveRequests', payload);
  return data;
}

export async function cancelLeaveRequest(requestId: number): Promise<void> {
  await apiClient.delete(`/api/LeaveRequests/${requestId}`);
}