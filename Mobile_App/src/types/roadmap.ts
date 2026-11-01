export interface StudentRoadmapLessonDto {
  sessionNumber: number;
  lessonId: number | null;
  title: string;
  status: string;
  lessonDate: string | null;
}

export interface StudentRoadmapDto {
  studentId: number;
  studentName: string;
  classId: number;
  className: string;
  courseName: string;
  courseLevelName: string;
  totalSessions: number;
  completedSessions: number;
  lessons: StudentRoadmapLessonDto[];
}
