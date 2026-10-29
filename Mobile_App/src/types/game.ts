export type MathQuestion = {
  expression: string;
  options: number[];
  correctIndex: number;
};

export type QuizStatus = 'idle' | 'playing' | 'correct' | 'wrong';

export type SessionBonus = {
  points: number;
  stars: number;
};
