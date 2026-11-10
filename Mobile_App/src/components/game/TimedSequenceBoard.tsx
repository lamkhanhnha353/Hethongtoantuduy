import { useEffect, useState } from 'react';
import { Text, TouchableOpacity, View } from 'react-native';

import { audio } from '@/audio';
import type { TimedSequenceQuestion } from '@/types/game';

type TimedSequenceBoardProps = {
  question: TimedSequenceQuestion;
  onNext: () => void;
  onCorrect: () => void;
};

export function TimedSequenceBoard({ question, onNext, onCorrect }: TimedSequenceBoardProps) {
  const [shownTermCount, setShownTermCount] = useState(1);
  const [selectedIndex, setSelectedIndex] = useState<number | null>(null);
  const [isCorrect, setIsCorrect] = useState(false);

  useEffect(() => {
    if (shownTermCount > question.terms.length) return undefined;
    const timer = setTimeout(() => setShownTermCount((count) => count + 1), 1000);
    return () => clearTimeout(timer);
  }, [question.id, question.terms.length, shownTermCount]);

  const isShowingSequence = shownTermCount <= question.terms.length;
  const currentTerm = question.terms[shownTermCount - 1];

  function handleSelect(index: number) {
    if (isCorrect) return;
    audio.pop();
    setSelectedIndex(index);
    if (index === question.correctIndex) {
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
          Tính nhẩm theo nhịp
        </Text>
        {isShowingSequence ? (
          <>
            <Text className="mt-3 text-sm text-slate-500">
              Số {shownTermCount} / {question.terms.length}
            </Text>
            <View className="mt-4 h-36 w-full items-center justify-center rounded-3xl bg-indigo-50">
              <Text className="text-6xl font-extrabold text-indigo-900">
                {currentTerm?.operator ? `${currentTerm.operator} ` : ''}
                {currentTerm?.value}
              </Text>
            </View>
            <Text className="mt-4 text-sm text-slate-500">
              Nhìn và ghi nhớ nhé!
            </Text>
          </>
        ) : (
          <>
            <Text className="mt-4 text-center text-base font-semibold text-slate-700">
              Kết quả của dãy số là bao nhiêu?
            </Text>
            <View className="mt-5 w-full flex-row justify-center gap-3">
              {question.options.map((option, index) => {
                const selectedWrong = selectedIndex === index && !isCorrect;
                return (
                  <TouchableOpacity
                    accessibilityRole="button"
                    className={`min-w-20 items-center rounded-2xl border px-5 py-4 ${
                      selectedWrong
                        ? 'border-rose-300 bg-rose-50'
                        : 'border-indigo-100 bg-indigo-50'
                    }`}
                    key={`${question.id}-${index}`}
                    onPress={() => handleSelect(index)}
                  >
                    <Text
                      className={`text-2xl font-extrabold ${
                        selectedWrong ? 'text-rose-700' : 'text-indigo-900'
                      }`}
                    >
                      {option}
                    </Text>
                  </TouchableOpacity>
                );
              })}
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
              ) : selectedIndex !== null ? (
                <Text className="font-semibold text-rose-600">Chưa đúng, bé thử lại nhé!</Text>
              ) : null}
            </View>
          </>
        )}
      </View>
    </View>
  );
}
