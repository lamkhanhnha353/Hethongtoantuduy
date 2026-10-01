import { Text, TouchableOpacity, View } from 'react-native';
import { router } from 'expo-router';

export default function ScreenHome() {
  return (
    <View className="flex-1 items-center justify-center bg-white">
      <Text className="mb-5 text-2xl font-bold text-[#333333]">
        Hello world
      </Text>

      <TouchableOpacity
        className="rounded-lg bg-[#ff0000] px-5 py-3"
        onPress={() => router.push('/login')}
      >
        <Text className="text-base font-bold text-white">Bấm vào đây hãy</Text>
      </TouchableOpacity>
    </View>
  );
}
