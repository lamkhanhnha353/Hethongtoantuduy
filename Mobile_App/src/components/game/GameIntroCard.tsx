import { Text, View } from 'react-native';

import { PrimaryButton } from '@/components/PrimaryButton';

type GameIntroCardProps = {
  title: string;
  description: string;
  actionLabel?: string;
  onStart: () => void;
};

export function GameIntroCard({
  title,
  description,
  actionLabel = 'Bắt đầu',
  onStart,
}: GameIntroCardProps) {
  return (
    <View className="items-center rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
      <Text className="text-lg font-bold text-slate-900">{title}</Text>
      <Text className="mt-2 text-center text-sm leading-5 text-slate-500">{description}</Text>
      <View className="mt-5 w-full">
        <PrimaryButton label={actionLabel} onPress={onStart} />
      </View>
    </View>
  );
}
