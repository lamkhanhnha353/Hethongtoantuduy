import { Text, TouchableOpacity, View } from 'react-native';

type PracticeModePickerProps = {
  onSelectClassic: () => void;
  onSelectTimed: () => void;
  onSelectTrueFalse: () => void;
};

const MODES = [
  {
    title: 'Toán vui',
    description: 'Chọn đáp án cho phép cộng, trừ quen thuộc.',
    emoji: '🧩',
    onPressKey: 'classic',
  },
  {
    title: 'Tính nhẩm theo nhịp',
    description: 'Mỗi số hiện lần lượt trong 1 giây.',
    emoji: '⚡',
    onPressKey: 'timed',
  },
  {
    title: 'Đúng hay sai?',
    description: 'Đọc phép tính và chọn nhận định chính xác.',
    emoji: '✅',
    onPressKey: 'trueFalse',
  },
] as const;

export function PracticeModePicker({
  onSelectClassic,
  onSelectTimed,
  onSelectTrueFalse,
}: PracticeModePickerProps) {
  const handlers = {
    classic: onSelectClassic,
    timed: onSelectTimed,
    trueFalse: onSelectTrueFalse,
  };

  return (
    <View className="w-full max-w-xl self-center gap-3">
      <View className="mb-1 items-center">
        <Text className="text-xl font-extrabold text-slate-900">Bé muốn luyện dạng nào?</Text>
        <Text className="mt-1 text-center text-sm text-slate-500">
          Chọn một trò chơi để bắt đầu nhé!
        </Text>
      </View>
      {MODES.map((mode) => (
        <TouchableOpacity
          accessibilityRole="button"
          activeOpacity={0.82}
          className="flex-row items-center rounded-2xl border border-indigo-100 bg-white p-4 shadow-sm"
          key={mode.onPressKey}
          onPress={handlers[mode.onPressKey]}
        >
          <Text className="mr-3 text-3xl">{mode.emoji}</Text>
          <View className="flex-1">
            <Text className="text-base font-bold text-slate-900">{mode.title}</Text>
            <Text className="mt-1 text-sm leading-5 text-slate-500">{mode.description}</Text>
          </View>
          <Text className="ml-2 text-xl font-bold text-indigo-500">›</Text>
        </TouchableOpacity>
      ))}
    </View>
  );
}
