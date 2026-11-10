import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import {
  studentPracticeAssignmentQueryKey,
  studentPracticeAssignmentsQueryKey,
} from '@/lib/queryKeys';
import {
  getStudentPracticeAssignment,
  getStudentPracticeAssignments,
  submitStudentPracticeAssignment,
} from '@/services/assignmentService';
import type {
  StudentPracticeAssignmentDetailDto,
  StudentPracticeAssignmentDto,
  StudentPracticeSubmissionResponseDto,
  SubmitStudentPracticeRequest,
} from '@/types/assignment';

export function useStudentPracticeAssignmentsQuery(studentId: number) {
  return useQuery<StudentPracticeAssignmentDto[]>({
    queryKey: studentPracticeAssignmentsQueryKey(studentId),
    queryFn: () => getStudentPracticeAssignments(studentId),
    enabled: Number.isFinite(studentId) && studentId > 0,
  });
}

export function useStudentPracticeAssignmentQuery(studentId: number, assignmentId: number) {
  return useQuery<StudentPracticeAssignmentDetailDto>({
    queryKey: studentPracticeAssignmentQueryKey(studentId, assignmentId),
    queryFn: () => getStudentPracticeAssignment(assignmentId, studentId),
    enabled: Number.isFinite(studentId) && studentId > 0
      && Number.isFinite(assignmentId) && assignmentId > 0,
  });
}

export function useSubmitStudentPracticeAssignmentMutation(
  studentId: number,
  assignmentId: number,
) {
  const queryClient = useQueryClient();

  return useMutation<
    StudentPracticeSubmissionResponseDto,
    unknown,
    SubmitStudentPracticeRequest
  >({
    mutationFn: (request) => submitStudentPracticeAssignment(assignmentId, studentId, request),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: studentPracticeAssignmentsQueryKey(studentId),
      });
    },
  });
}
