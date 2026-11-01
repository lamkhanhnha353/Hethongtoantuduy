import React from 'react';
import { Text, View } from 'react-native';

import { MathOperand } from '@/components/game/MathOperand';
import type { MathQuestion } from '@/types/game';

export type MathDisplayMode = 'standard' | 'take-away';
export type SlotPosition = 'a' | 'b' | 'answer';

export type MathExpressionProps = {
  question: MathQuestion;
  slot: React.ReactNode;
  displayMode?: MathDisplayMode;
  slotPosition?: SlotPosition;
  emojiScale?: number;
  showNumbers?: boolean;
};

export function calculateEmojiSize(count: number, scale: number = 1): number {
  const base = count <= 6 ? 26 : count <= 12 ? 20 : 16;
  return Math.max(12, Math.round(base * scale));
}

export function MathExpression({
  question,
  slot,
  displayMode = 'standard',
  slotPosition = 'answer',
  emojiScale = 1,
  showNumbers = false,
}: MathExpressionProps) {
  const { a, b, operator, theme, answer } = question;
  const maxOperand = Math.max(a, b);
  const size = calculateEmojiSize(maxOperand, emojiScale);

  const renderOperator = (symbol: string) => (
    <Text className="text-3xl font-black text-emerald-100/70">{symbol}</Text>
  );

  // Chế độ 'take-away' sư phạm: gộp phép trừ vào 1 tập hợp A có các phần tử bị mờ
  if (displayMode === 'take-away' && operator === '−' && slotPosition === 'answer') {
    return (
      <View className="flex-1 flex-row items-center justify-center gap-2 px-2 sm:gap-4">
        <MathOperand
          badgeNumber={showNumbers ? a : undefined}
          count={a}
          dimmedFrom={a - b}
          emoji={theme.emoji}
          size={size}
          subLabel={b > 0 ? `− ${b}` : undefined}
        />
        {renderOperator('=')}
        {slot}
      </View>
    );
  }

  // Chế độ chuẩn (Standard): [Toán hạng 1] [Toán tử] [Toán hạng 2] = [Kết quả]
  const renderItem = (pos: SlotPosition, val: number) => {
    if (slotPosition === pos) {
      return slot;
    }
    return (
      <MathOperand
        badgeNumber={showNumbers ? val : undefined}
        count={val}
        emoji={theme.emoji}
        size={size}
      />
    );
  };

  return (
    <View className="flex-1 flex-row items-center justify-center gap-2 px-2 sm:gap-4">
      {renderItem('a', a)}
      {renderOperator(operator)}
      {renderItem('b', b)}
      {renderOperator('=')}
      {renderItem('answer', answer)}
    </View>
  );
}
