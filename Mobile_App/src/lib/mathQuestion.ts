import type { MathQuestion } from '@/types/game';

export const RANGE = 20;
export const OPTION_COUNT = 4;

export const REWARD = {
  points: 10,
  stars: 1,
} as const;

function randomInt(min: number, max: number): number {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}

function buildOptions(
  a: number,
  b: number,
  answer: number,
  isAddition: boolean,
): { options: number[]; correctIndex: number } {
  const preferred = isAddition
    ? [answer + 1, answer - 1, Math.abs(a - b), answer + 2, answer - 2]
    : [a + b, answer + 1, answer - 1, answer + 2, answer - 2];

  const distractors: number[] = [];
  for (const candidate of preferred) {
    if (candidate < 0 || candidate === answer || distractors.includes(candidate)) {
      continue;
    }
    distractors.push(candidate);
    if (distractors.length === OPTION_COUNT - 1) {
      break;
    }
  }

  let fallback = answer + 3;
  while (distractors.length < OPTION_COUNT - 1) {
    if (fallback !== answer && !distractors.includes(fallback)) {
      distractors.push(fallback);
    }
    fallback += 1;
  }

  const options = [...distractors, answer];
  for (let i = options.length - 1; i > 0; i -= 1) {
    const j = Math.floor(Math.random() * (i + 1));
    [options[i], options[j]] = [options[j], options[i]];
  }

  return { options, correctIndex: options.indexOf(answer) };
}

export function generateQuestion(): MathQuestion {
  const isAddition = Math.random() < 0.5;

  const a = randomInt(0, RANGE);
  const b = isAddition ? randomInt(0, RANGE - a) : randomInt(0, a);
  const answer = isAddition ? a + b : a - b;
  const expression = isAddition ? `${a} + ${b}` : `${a} − ${b}`;

  const { options, correctIndex } = buildOptions(a, b, answer, isAddition);

  return { expression, options, correctIndex };
}
