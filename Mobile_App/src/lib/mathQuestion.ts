import type { MathQuestion, MathTheme } from '@/types/game';

export const RANGE = 10;
export const OPTION_COUNT = 3;

export const REWARD = {
  points: 10,
  stars: 1,
} as const;

export const THEMES: MathTheme[] = [
  { id: 'apple', emoji: '🍎', label: 'quả táo' },
  { id: 'star', emoji: '⭐', label: 'ngôi sao' },
  { id: 'balloon', emoji: '🎈', label: 'quả bóng bay' },
  { id: 'cookie', emoji: '🍪', label: 'chiếc bánh quy' },
  { id: 'flower', emoji: '🌸', label: 'bông hoa' },
  { id: 'orange', emoji: '🍊', label: 'quả cam' },
  { id: 'duck', emoji: '🐤', label: 'chú vịt con' },
  { id: 'candy', emoji: '🍬', label: 'viên kẹo' },
];

function randomInt(min: number, max: number): number {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}

function pickTheme(): MathTheme {
  return THEMES[Math.floor(Math.random() * THEMES.length)];
}

function buildStory(a: number, b: number, isAddition: boolean, theme: MathTheme): string {
  const { label } = theme;

  if (isAddition) {
    if (b === 0) {
      return `Bé có ${a} ${label}, không thêm ${label} nào nữa. Hỏi bé có tất cả bao nhiêu ${label}?`;
    }
    if (a === 0) {
      return `Bé chưa có ${label} nào, mẹ cho thêm ${b} ${label}. Hỏi bé có tất cả bao nhiêu ${label}?`;
    }
    return `Bé có ${a} ${label}, mẹ cho thêm ${b} ${label} nữa. Hỏi bé có tất cả bao nhiêu ${label}?`;
  }

  if (b === 0) {
    return `Bé có ${a} ${label}, không cho bạn ${label} nào. Hỏi bé còn lại bao nhiêu ${label}?`;
  }
  return `Bé có ${a} ${label}, bé cho bạn ${b} ${label}. Hỏi bé còn lại bao nhiêu ${label}?`;
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
  const operator = isAddition ? '+' : '−';
  const theme = pickTheme();

  const { options, correctIndex } = buildOptions(a, b, answer, isAddition);

  return {
    id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
    a,
    b,
    operator,
    answer,
    expression: `${a} ${operator} ${b}`,
    options,
    correctIndex,
    theme,
    story: buildStory(a, b, isAddition, theme),
  };
}
