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

export type TimedSequenceTerm = {
  operator: '+' | '−' | null;
  value: number;
};

export type TimedSequenceQuestion = {
  id: string;
  terms: TimedSequenceTerm[];
  answer: number;
  options: number[];
  correctIndex: number;
};

export type TrueFalseQuestion = {
  id: string;
  expression: string;
  isTrue: boolean;
};

export type QuizStatus = 'idle' | 'playing' | 'correct' | 'wrong';

export type SessionBonus = {
  points: number;
  stars: number;
};
