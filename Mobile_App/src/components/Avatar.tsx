import { Image } from 'expo-image';
import { Text, View } from 'react-native';

import { initialsFrom } from '@/lib/name';

type AvatarProps = {
  name: string;
  uri?: string | null;
  size?: number;
};

export function Avatar({ name, uri, size = 48 }: AvatarProps) {
  const dimension = { width: size, height: size, borderRadius: size / 2 };

  if (uri) {
    return (
      <Image
        contentFit="cover"
        source={{ uri }}
        style={dimension}
        accessibilityLabel={`Ảnh của ${name}`}
      />
    );
  }

  return (
    <View className="items-center justify-center bg-sky-600" style={dimension}>
      <Text className="font-bold text-white" style={{ fontSize: size * 0.4 }}>
        {initialsFrom(name)}
      </Text>
    </View>
  );
}
