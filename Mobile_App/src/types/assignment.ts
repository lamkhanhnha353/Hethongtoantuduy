export type StudentPracticeAssignmentDto = {
  assignmentId: number;
  title: string;
  lessonTitle: string | null;
  startTime: string;
  deadline: string;
  timeLimitMinutes: number;
  totalQuestions: number;
  totalPoints: number;
  studentStatus: string;
  score: number | null;
  completedAt: string | null;
  isAvailable: boolean;
};

export type StudentPracticeQuestionOptionDto = {
  key: string;
  text: string;
};

export type StudentPracticeQuestionDto = {
  questionAssignmentId: number;
  orderIndex: number;
  pointValue: number;
  type: string;
  content: string;
  imageUrl: string | null;
  difficulty: string;
  options: StudentPracticeQuestionOptionDto[];
  sequenceTerms: StudentPracticeSequenceTermDto[];
};

export type StudentPracticeSequenceTermDto = {
  operator: '+' | '-' | null;
  value: number;
};

export type StudentPracticeAssignmentDetailDto = {
  assignmentId: number;
  title: string;
  lessonTitle: string | null;
  deadline: string;
  timeLimitMinutes: number;
  totalPoints: number;
  questions: StudentPracticeQuestionDto[];
};

export type SubmitStudentPracticeRequest = {
  answers: {
    questionAssignmentId: number;
    answer: string;
  }[];
};

export type StudentPracticeSubmissionResponseDto = {
  assignmentId: number;
  status: string;
  score: number;
  totalPoints: number;
  correctCount: number;
  totalQuestions: number;
  submittedAt: string;
};
