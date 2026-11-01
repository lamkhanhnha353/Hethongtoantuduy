import { apiClient } from '@/services/apiClient';
import type { StudentRoadmapDto } from '@/types/roadmap';

export async function getStudentRoadmaps(studentId: number): Promise<StudentRoadmapDto[]> {
  const { data } = await apiClient.get<StudentRoadmapDto[]>(
    `/api/lessons/student/${studentId}/roadmap`,
  );
  return data;
}
