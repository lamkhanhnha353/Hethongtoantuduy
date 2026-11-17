import React from 'react';
import { Text, View } from 'react-native';

import { EmojiGroup } from '@/components/game/EmojiGroup';

export type MathOperandProps = {
  count: number;
  emoji: string;
  size: number;
  dimmedFrom?: number;
  subLabel?: string;
  badgeNumber?: number | string;
  className?: string;
};

export function MathOperand({
  count,
  emoji,
  size,
  dimmedFrom,
  subLabel,
  badgeNumber,
  className = '',
}: MathOperandProps) {
  return (
    <View
      className={`min-h-[110px] min-w-[90px] items-center justify-center rounded-2xl border border-white/5 p-2 ${className}`}
    >
      {count <= 0 ? (
        <Text className="px-2 text-2xl font-black text-emerald-100/40">0</Text>
      ) : (
        <EmojiGroup
          className="max-w-full"
          count={count}
          dimmedFrom={dimmedFrom}
          emoji={emoji}
          size={size}
        />
      )}

      {badgeNumber !== undefined && (
        <View className="mt-1 rounded-full bg-emerald-950/60 px-2 py-0.5">
          <Text className="text-xs font-bold text-emerald-200">{badgeNumber}</Text>
        </View>
      )}

      {Boolean(subLabel) && (
        <Text className="mt-1.5 text-xs font-bold text-amber-300">{subLabel}</Text>
      )}
    </View>
  );
}
