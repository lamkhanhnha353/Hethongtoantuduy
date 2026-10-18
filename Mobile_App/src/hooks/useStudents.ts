import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import type { PickedImage } from '@/lib/imageUpload';
import { studentQueryKey, studentsQueryKey } from '@/lib/queryKeys';
import {
  createStudent,
  deleteStudent,
  deleteStudentAvatar,
  getStudent,
  getStudents,
  updateStudent,
  uploadStudentAvatar,
} from '@/services/studentService';
import type {
  CreateStudentRequestDto,
  StudentResponseDto,
  UpdateStudentRequestDto,
} from '@/types/student';

export function useStudentsQuery(enabled = true) {
  return useQuery<StudentResponseDto[]>({
    queryKey: studentsQueryKey,
    queryFn: getStudents,
    enabled,
  });
}

export function useStudentQuery(id: number) {
  return useQuery<StudentResponseDto>({
    queryKey: studentQueryKey(id),
    queryFn: () => getStudent(id),
    enabled: Number.isFinite(id),
  });
}

export function useCreateStudentMutation() {
  const queryClient = useQueryClient();

  return useMutation<StudentResponseDto, unknown, CreateStudentRequestDto>({
    mutationFn: createStudent,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: studentsQueryKey });
    },
  });
}

export function useUpdateStudentMutation(id: number) {
  const queryClient = useQueryClient();

  return useMutation<StudentResponseDto, unknown, UpdateStudentRequestDto>({
    mutationFn: (payload) => updateStudent(id, payload),
    onSuccess: (student) => {
      queryClient.setQueryData(studentQueryKey(id), student);
      void queryClient.invalidateQueries({ queryKey: studentsQueryKey });
    },
  });
}

export function useDeleteStudentMutation() {
  const queryClient = useQueryClient();

  return useMutation<void, unknown, { id: number; pin: string }>({
    mutationFn: ({ id, pin }) => deleteStudent(id, pin),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: studentsQueryKey });
    },
  });
}

export function useUploadStudentAvatarMutation(id: number) {
  const queryClient = useQueryClient();

  return useMutation<StudentResponseDto, unknown, PickedImage>({
    mutationFn: (image) => uploadStudentAvatar(id, image),
    onSuccess: (student) => {
      queryClient.setQueryData(studentQueryKey(id), student);
      void queryClient.invalidateQueries({ queryKey: studentsQueryKey });
    },
  });
}

export function useDeleteStudentAvatarMutation(id: number) {
  const queryClient = useQueryClient();

  return useMutation<StudentResponseDto, unknown, void>({
    mutationFn: () => deleteStudentAvatar(id),
    onSuccess: (student) => {
      queryClient.setQueryData(studentQueryKey(id), student);
      void queryClient.invalidateQueries({ queryKey: studentsQueryKey });
    },
  });
}
