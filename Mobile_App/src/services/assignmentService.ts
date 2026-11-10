import { apiClient } from '@/services/apiClient';
import type {
  StudentPracticeAssignmentDetailDto,
  StudentPracticeAssignmentDto,
  StudentPracticeSubmissionResponseDto,
  SubmitStudentPracticeRequest,
} from '@/types/assignment';

export async function getStudentPracticeAssignments(
  studentId: number,
): Promise<StudentPracticeAssignmentDto[]> {
  const { data } = await apiClient.get<StudentPracticeAssignmentDto[]>(
    `/api/assignments/student/${studentId}/practice`,
  );
  return data;
}

export async function getStudentPracticeAssignment(
  assignmentId: number,
  studentId: number,
): Promise<StudentPracticeAssignmentDetailDto> {
  const { data } = await apiClient.get<StudentPracticeAssignmentDetailDto>(
    `/api/assignments/${assignmentId}/student/${studentId}/practice`,
  );
  return data;
}

export async function submitStudentPracticeAssignment(
  assignmentId: number,
  studentId: number,
  request: SubmitStudentPracticeRequest,
): Promise<StudentPracticeSubmissionResponseDto> {
  const { data } = await apiClient.post<StudentPracticeSubmissionResponseDto>(
    `/api/assignments/${assignmentId}/student/${studentId}/practice/submit`,
    request,
  );
  return data;
}
