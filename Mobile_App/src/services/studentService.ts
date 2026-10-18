import { toAvatarFormData, type PickedImage } from '@/lib/imageUpload';
import { apiClient } from '@/services/apiClient';
import type {
  CreateStudentRequestDto,
  StudentResponseDto,
  UpdateStudentRequestDto,
} from '@/types/student';

export async function getStudents(): Promise<StudentResponseDto[]> {
  const { data } = await apiClient.get<StudentResponseDto[]>('/api/students');
  return data;
}

export async function getStudent(id: number): Promise<StudentResponseDto> {
  const { data } = await apiClient.get<StudentResponseDto>(`/api/students/${id}`);
  return data;
}

export async function createStudent(payload: CreateStudentRequestDto): Promise<StudentResponseDto> {
  const { data } = await apiClient.post<StudentResponseDto>('/api/students', payload);
  return data;
}

export async function updateStudent(
  id: number,
  payload: UpdateStudentRequestDto,
): Promise<StudentResponseDto> {
  const { data } = await apiClient.put<StudentResponseDto>(`/api/students/${id}`, payload);
  return data;
}

export async function deleteStudent(id: number, pin: string): Promise<void> {
  await apiClient.delete(`/api/students/${id}`, {
    headers: { 'X-Pin': pin },
  });
}

export async function uploadStudentAvatar(
  id: number,
  image: PickedImage,
): Promise<StudentResponseDto> {
  const { data } = await apiClient.post<StudentResponseDto>(
    `/api/students/${id}/avatar`,
    toAvatarFormData(image),
  );
  return data;
}

export async function deleteStudentAvatar(id: number): Promise<StudentResponseDto> {
  const { data } = await apiClient.delete<StudentResponseDto>(`/api/students/${id}/avatar`);
  return data;
}
