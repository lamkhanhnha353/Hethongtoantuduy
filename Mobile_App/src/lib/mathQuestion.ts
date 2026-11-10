import type {
  MathQuestion,
  MathTheme,
  TimedSequenceQuestion,
  TimedSequenceTerm,
  TrueFalseQuestion,
} from '@/types/game';

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

function buildNumericOptions(answer: number): { options: number[]; correctIndex: number } {
  const candidates = [answer + 1, answer - 1, answer + 2, answer - 2, answer + 3, answer + 4]
    .filter((value) => value >= 0 && value <= 30 && value !== answer);
  const distractors = [...new Set(candidates)].slice(0, OPTION_COUNT - 1);
  let next = answer + 5;
  while (distractors.length < OPTION_COUNT - 1) {
    if (next !== answer && !distractors.includes(next)) distractors.push(next);
    next += 1;
  }

  const options = [...distractors, answer];
  for (let index = options.length - 1; index > 0; index -= 1) {
    const swapIndex = Math.floor(Math.random() * (index + 1));
    [options[index], options[swapIndex]] = [options[swapIndex], options[index]];
  }
  return { options, correctIndex: options.indexOf(answer) };
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

export function generateTimedSequenceQuestion(): TimedSequenceQuestion {
  const terms: TimedSequenceTerm[] = [];
  let total = randomInt(1, 5);
  terms.push({ operator: null, value: total });

  for (let index = 0; index < 3; index += 1) {
    const canSubtract = total > 0;
    const subtract = canSubtract && Math.random() < 0.4;
    const operator = subtract ? '−' : '+';
    const maxValue = subtract ? Math.min(5, total) : 5;
    const value = randomInt(1, maxValue);
    total = subtract ? total - value : total + value;
    terms.push({ operator, value });
  }

  const { options, correctIndex } = buildNumericOptions(total);
  return {
    id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
    terms,
    answer: total,
    options,
    correctIndex,
  };
}

export function generateTrueFalseQuestion(): TrueFalseQuestion {
  const isAddition = Math.random() < 0.5;
  const first = randomInt(1, isAddition ? RANGE - 1 : RANGE);
  const second = isAddition ? randomInt(1, RANGE - first) : randomInt(1, first);
  const answer = isAddition ? first + second : first - second;
  const isTrue = Math.random() < 0.5;
  const displayedAnswer = isTrue || answer === 0
    ? answer + (isTrue ? 0 : 1)
    : answer + (Math.random() < 0.5 ? 1 : -1);
  const operator = isAddition ? '+' : '−';

  return {
    id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
    expression: `${first} ${operator} ${second} = ${displayedAnswer}`,
    isTrue,
  };
}
