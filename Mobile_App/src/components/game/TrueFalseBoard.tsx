import { useState } from 'react';
import { Text, TouchableOpacity, View } from 'react-native';

import { audio } from '@/audio';
import type { TrueFalseQuestion } from '@/types/game';

type TrueFalseBoardProps = {
  question: TrueFalseQuestion;
  onNext: () => void;
  onCorrect: () => void;
};

export function TrueFalseBoard({ question, onNext, onCorrect }: TrueFalseBoardProps) {
  const [selected, setSelected] = useState<boolean | null>(null);
  const [isCorrect, setIsCorrect] = useState(false);

  function handleSelect(value: boolean) {
    if (isCorrect) return;
    audio.pop();
    setSelected(value);
    if (value === question.isTrue) {
      setIsCorrect(true);
      audio.correct();
      onCorrect();
      return;
    }
    audio.wrong();
  }

  return (
    <View className="flex-1 items-center justify-center rounded-[30px] bg-indigo-950 p-5">
      <View className="w-full max-w-xl items-center rounded-[24px] bg-white px-5 py-7">
        <Text className="text-sm font-bold uppercase tracking-wide text-indigo-500">
          Đúng hay sai?
        </Text>
        <Text className="mt-6 rounded-3xl bg-indigo-50 px-6 py-8 text-center text-4xl font-extrabold text-indigo-950">
          {question.expression}
        </Text>
        <View className="mt-6 w-full flex-row justify-center gap-4">
          {[
            { label: 'Đúng', value: true, color: 'bg-emerald-600' },
            { label: 'Sai', value: false, color: 'bg-rose-600' },
          ].map((option) => (
            <TouchableOpacity
              accessibilityRole="button"
              className={`min-w-32 items-center rounded-2xl px-6 py-4 ${option.color}`}
              key={option.label}
              onPress={() => handleSelect(option.value)}
            >
              <Text className="text-xl font-extrabold text-white">{option.label}</Text>
            </TouchableOpacity>
          ))}
        </View>
        <View className="mt-5 min-h-12 items-center justify-center">
          {isCorrect ? (
            <View className="items-center gap-3">
              <Text className="text-base font-bold text-emerald-700">
                Chính xác! Giỏi lắm! 🌟
              </Text>
              <TouchableOpacity
                accessibilityRole="button"
                className="rounded-full bg-emerald-600 px-6 py-3"
                onPress={onNext}
              >
                <Text className="font-bold text-white">Câu tiếp theo</Text>
              </TouchableOpacity>
            </View>
          ) : selected !== null ? (
            <Text className="font-semibold text-rose-600">Chưa đúng, bé thử lại nhé!</Text>
          ) : null}
        </View>
      </View>
    </View>
  );
}
