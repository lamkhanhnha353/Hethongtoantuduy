import { useQuery } from '@tanstack/react-query';

import { studentLessonsQueryKey } from '@/lib/queryKeys';
import { getStudentLessons } from '@/services/lessonService';
import { useStudentsQuery } from '@/hooks/useStudents';
import type { StudentLessonDto, StudentScheduleLesson } from '@/types/lesson';
import type { StudentResponseDto } from '@/types/student';

const EMPTY_STUDENTS: StudentResponseDto[] = [];
const EMPTY_LESSONS: StudentScheduleLesson[] = [];

function formatTime(value: string | null): string {
  if (!value) {
    return '--:--';
  }

  return value.match(/(?:T|^)(\d{2}:\d{2})/)?.[1] ?? '--:--';
}

export function useParentLessons(enabled = true) {
  const studentsQuery = useStudentsQuery(enabled);
  const students = studentsQuery.data ?? EMPTY_STUDENTS;
  const studentIds = students.map((student) => student.id);
  const lessonsQuery = useQuery<StudentScheduleLesson[]>({
    queryKey: studentLessonsQueryKey(studentIds),
    queryFn: async () => {
      const lessonsByStudent = await Promise.all(
        students.map(async (student) => {
          const lessons: StudentLessonDto[] = await getStudentLessons(student.id);
          return lessons.map((lesson) => ({
            ...lesson,
            studentId: student.id,
            studentName: student.fullName,
            startLabel: formatTime(lesson.startTime),
            endLabel: formatTime(lesson.endTime),
          }));
        }),
      );

      return lessonsByStudent.flat();
    },
    enabled: enabled && studentsQuery.isSuccess,
    staleTime: 30_000,
  });

  return {
    students,
    studentIds,
    lessons: lessonsQuery.data ?? EMPTY_LESSONS,
    studentsQuery,
    lessonsQuery,
  };
}
