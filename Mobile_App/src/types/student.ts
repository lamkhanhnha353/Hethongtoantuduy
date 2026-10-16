export type StudentResponseDto = {
  id: number;
  fullName: string;
  avatar: string | null;
  totalPoints: number;
  stars: number;
  streakDays: number;
};

export type CreateStudentRequestDto = {
  fullName: string;
};

export type UpdateStudentRequestDto = {
  fullName: string;
};
