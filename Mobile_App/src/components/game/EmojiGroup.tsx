import { Text, View } from 'react-native';

type EmojiGroupProps = {
  count: number;
  emoji: string;
  size?: number;
  dimmedFrom?: number;
  className?: string;
};

export function EmojiGroup({
  count,
  emoji,
  size = 22,
  dimmedFrom = count,
  className = '',
}: EmojiGroupProps) {
  if (count <= 0) {
    return null;
  }

  return (
    <View className={`flex-row flex-wrap items-center justify-center gap-0.5 ${className}`}>
      {Array.from({ length: count }, (_, index) => (
        <Text
          key={index}
          className={index >= dimmedFrom ? 'opacity-30' : ''}
          style={{ fontSize: size, lineHeight: size * 1.3 }}
        >
          {emoji}
        </Text>
      ))}
    </View>
  );
}
