import { useEffect, useState } from 'react';
import { Text, TextInput, TouchableOpacity, View } from 'react-native';

import type { StudentPracticeSequenceTermDto } from '@/types/assignment';

type TimedSequenceAnswerProps = {
  terms: StudentPracticeSequenceTermDto[];
  value: string;
  onChange: (answer: string) => void;
};

export function TimedSequenceAnswer({ terms, value, onChange }: TimedSequenceAnswerProps) {
  const [hasStarted, setHasStarted] = useState(false);
  const [shownTermCount, setShownTermCount] = useState(0);

  useEffect(() => {
    if (!hasStarted || shownTermCount < 1 || shownTermCount > terms.length) return undefined;
    const timer = setTimeout(() => setShownTermCount((count) => count + 1), 1000);
    return () => clearTimeout(timer);
  }, [hasStarted, shownTermCount, terms.length]);

  const isShowingSequence = hasStarted && shownTermCount <= terms.length;
  const currentTerm = terms[shownTermCount - 1];

  return (
    <View className="mt-4 items-center rounded-2xl bg-indigo-50 p-4">
      {!hasStarted ? (
        <TouchableOpacity
          accessibilityRole="button"
          className="rounded-full bg-indigo-600 px-6 py-3"
          onPress={() => {
            setHasStarted(true);
            setShownTermCount(1);
          }}
        >
          <Text className="font-bold text-white">Bắt đầu xem dãy số</Text>
        </TouchableOpacity>
      ) : isShowingSequence ? (
        <>
          <Text className="text-sm font-semibold text-indigo-700">
            Số {shownTermCount} / {terms.length}
          </Text>
          <Text className="mt-3 text-5xl font-extrabold text-indigo-950">
            {currentTerm?.operator ? `${currentTerm.operator} ` : ''}
            {currentTerm?.value}
          </Text>
        </>
      ) : (
        <>
          <Text className="text-sm font-semibold text-indigo-700">
            Kết quả của dãy số là bao nhiêu?
          </Text>
          <TextInput
            accessibilityLabel="Kết quả phép tính theo nhịp"
            className="mt-3 w-full rounded-xl border border-indigo-200 bg-white px-4 py-3 text-center text-xl text-slate-900"
            keyboardType="numeric"
            onChangeText={onChange}
            placeholder="Nhập kết quả"
            value={value}
          />
        </>
      )}
    </View>
  );
}
