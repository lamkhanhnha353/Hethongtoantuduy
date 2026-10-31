import { useQuery } from '@tanstack/react-query';

import { studentRoadmapsQueryKey } from '@/lib/queryKeys';
import { getStudentRoadmaps } from '@/services/roadmapService';
import { useStudentsQuery } from '@/hooks/useStudents';
import type { StudentResponseDto } from '@/types/student';
import type { StudentRoadmapDto } from '@/types/roadmap';

const EMPTY_STUDENTS: StudentResponseDto[] = [];
const EMPTY_ROADMAPS: StudentRoadmapDto[] = [];

export function useStudentRoadmaps(enabled = true) {
  const studentsQuery = useStudentsQuery(enabled);
  const students = studentsQuery.data ?? EMPTY_STUDENTS;
  const studentIds = students.map((student) => student.id);
  const roadmapsQuery = useQuery<StudentRoadmapDto[]>({
    queryKey: studentRoadmapsQueryKey(studentIds),
    queryFn: async () => {
      const roadmapsByStudent = await Promise.all(
        students.map((student) => getStudentRoadmaps(student.id)),
      );
      return roadmapsByStudent.flat();
    },
    enabled: enabled && studentsQuery.isSuccess,
  });

  return {
    students,
    roadmaps: roadmapsQuery.data ?? EMPTY_ROADMAPS,
    studentsQuery,
    roadmapsQuery,
  };
}
