import { Ionicons } from '@expo/vector-icons';
import { router } from 'expo-router';
import { Text, TouchableOpacity, View } from 'react-native';

type ScreenHeaderProps = {
  title: string;
};

export function ScreenHeader({ title }: ScreenHeaderProps) {
  return (
    <View className="flex-row items-center gap-3 pb-4 pt-2">
      <TouchableOpacity
        accessibilityLabel="Quay lại"
        accessibilityRole="button"
        className="h-9 w-9 items-center justify-center rounded-full border border-slate-200 bg-white"
        hitSlop={8}
        onPress={() => router.back()}
      >
        <Ionicons color="#334155" name="arrow-back" size={18} />
      </TouchableOpacity>

      <Text className="flex-1 text-2xl font-bold text-slate-900" numberOfLines={1}>
        {title}
      </Text>
    </View>
  );
}
