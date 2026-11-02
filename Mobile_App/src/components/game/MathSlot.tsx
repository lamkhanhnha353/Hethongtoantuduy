import React, { forwardRef } from 'react';
import { Text, View, type ViewProps } from 'react-native';

import type { QuizStatus } from '@/types/game';

export type MathSlotProps = ViewProps & {
  status: QuizStatus;
  value: number | string | null;
  placeholder?: string;
};

export const MathSlot = forwardRef<View, MathSlotProps>(function MathSlot(
  { status, value, placeholder = '?', className = '', ...viewProps },
  ref,
) {
  const isPlaying = status === 'playing';

  const slotBorderAndBg =
    status === 'correct'
      ? 'border-emerald-300 bg-emerald-400/20'
      : status === 'wrong'
        ? 'border-rose-300 bg-rose-400/20'
        : 'border-emerald-100/50 bg-emerald-950/30';

  const textColor = status === 'wrong' ? 'text-rose-100' : 'text-emerald-50';

  return (
    <View
      ref={ref}
      className={`h-20 w-20 shrink-0 items-center justify-center rounded-2xl border-2 ${
        isPlaying ? 'border-dashed' : 'border-solid'
      } ${slotBorderAndBg} ${className}`}
      {...viewProps}
    >
      <Text className={`text-2xl font-black ${textColor}`}>
        {isPlaying || value === null ? placeholder : value}
      </Text>
    </View>
  );
});
