export type MathTheme = {
  id: string;
  emoji: string;
  label: string;
};

export type MathQuestion = {
  id: string;
  a: number;
  b: number;
  operator: '+' | '−';
  answer: number;
  expression: string;
  options: number[];
  correctIndex: number;
  theme: MathTheme;
  story: string;
};

export type QuizStatus = 'idle' | 'playing' | 'correct' | 'wrong';

export type SessionBonus = {
  points: number;
  stars: number;
};
