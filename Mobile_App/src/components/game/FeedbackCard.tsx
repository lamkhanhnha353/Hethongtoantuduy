import { Text, View } from 'react-native';

import { PrimaryButton } from '@/components/PrimaryButton';

type FeedbackCardProps = {
  variant: 'correct' | 'wrong';
  correctAnswer?: number;
  points?: number;
  stars?: number;
  onAction: () => void;
};

export function FeedbackCard({
  variant,
  correctAnswer,
  points = 0,
  stars = 0,
  onAction,
}: FeedbackCardProps) {
  const isCorrect = variant === 'correct';

  return (
    <View
      className={`mt-4 rounded-2xl border p-4 ${
        isCorrect ? 'border-emerald-200 bg-emerald-50' : 'border-rose-200 bg-rose-50'
      }`}
    >
      <Text
        className={`text-center text-base font-semibold ${
          isCorrect ? 'text-emerald-700' : 'text-rose-700'
        }`}
      >
        {isCorrect
          ? `Chính xác! +${points} điểm và +${stars} sao`
          : `Chưa đúng rồi. Đáp án đúng là ${correctAnswer}.`}
      </Text>

      {!isCorrect ? (
        <Text className="mt-1 text-center text-xs text-rose-500">
          Cố gắng lên, bé làm lại được mà!
        </Text>
      ) : null}

      <View className="mt-4">
        <PrimaryButton
          label={isCorrect ? 'Câu hỏi tiếp theo' : 'Thử lại'}
          onPress={onAction}
        />
      </View>
    </View>
  );
}
