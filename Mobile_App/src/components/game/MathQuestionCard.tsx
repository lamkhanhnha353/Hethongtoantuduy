import { Text, View } from 'react-native';

type MathQuestionCardProps = {
  expression: string;
};

export function MathQuestionCard({ expression }: MathQuestionCardProps) {
  return (
    <View className="items-center rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
      <Text className="text-4xl font-black tracking-tight text-slate-900">
        {expression} = ?
      </Text>
    </View>
  );
}
