import { Feather } from '@expo/vector-icons';
import { Pressable, Text, View } from 'react-native';

type ProfileMenuRowProps = {
  title: string;
  description: string;
  icon: keyof typeof Feather.glyphMap;
  onPress: () => void;
  badge?: string;
  hasTopBorder?: boolean;
};

export function ProfileMenuRow({
  title,
  description,
  icon,
  onPress,
  badge,
  hasTopBorder = false,
}: ProfileMenuRowProps) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={`${title}, ${description}`}
      className={`flex-row items-center px-4 py-4 active:bg-slate-50 ${
        hasTopBorder ? 'border-t border-slate-100' : ''
      }`}
      onPress={onPress}
    >
      <View className="h-10 w-10 shrink-0 items-center justify-center rounded-full bg-sky-50">
        <Feather color="#0284c7" name={icon} size={20} />
      </View>
      <View className="ml-3 min-w-0 flex-1">
        <Text className="text-[15px] font-semibold text-slate-900">{title}</Text>
        <Text className="mt-0.5 text-xs text-slate-500" numberOfLines={1}>
          {description}
        </Text>
      </View>
      {badge ? (
        <View className="mr-2 min-w-[22px] items-center justify-center rounded-full bg-rose-500 px-1.5 py-0.5">
          <Text className="text-[10px] font-bold text-white">{badge}</Text>
        </View>
      ) : null}
      <Feather color="#94a3b8" name="chevron-right" size={19} />
    </Pressable>
  );
}
