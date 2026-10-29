import { Text, TouchableOpacity } from 'react-native';

export type AnswerOptionState = 'default' | 'correct' | 'wrong';

type AnswerOptionProps = {
  value: number;
  state?: AnswerOptionState;
  disabled?: boolean;
  onPress: () => void;
};

export function AnswerOption({
  value,
  state = 'default',
  disabled = false,
  onPress,
}: AnswerOptionProps) {
  const stateClass = {
    default: 'border-slate-200 bg-white',
    correct: 'border-emerald-500 bg-emerald-50',
    wrong: 'border-rose-500 bg-rose-50',
  }[state];

  return (
    <TouchableOpacity
      accessibilityLabel={`Đáp án ${value}`}
      accessibilityRole="button"
      activeOpacity={0.8}
      className={`w-[47%] items-center justify-center rounded-2xl border-2 py-5 ${stateClass}`}
      disabled={disabled}
      onPress={onPress}
    >
      <Text className="text-2xl font-bold text-slate-900">{value}</Text>
    </TouchableOpacity>
  );
}
