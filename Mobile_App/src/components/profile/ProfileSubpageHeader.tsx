import type { ReactNode } from 'react';
import { Ionicons } from '@expo/vector-icons';
import { Pressable, Text, View } from 'react-native';

type ProfileSubpageHeaderProps = {
  title: string;
  onBack: () => void;
  rightAccessory?: ReactNode;
};

export function ProfileSubpageHeader({
  title,
  onBack,
  rightAccessory,
}: ProfileSubpageHeaderProps) {
  return (
    <View className="flex-row items-center border-b border-slate-200 bg-white px-4 py-3">
      <Pressable
        accessibilityLabel="Quay lại"
        accessibilityRole="button"
        className="h-10 w-10 items-center justify-center rounded-full bg-slate-50"
        onPress={onBack}
      >
        <Ionicons color="#334155" name="arrow-back" size={22} />
      </Pressable>
      <Text className="ml-3 flex-1 text-lg font-bold text-slate-900">{title}</Text>
      {rightAccessory}
    </View>
  );
}
