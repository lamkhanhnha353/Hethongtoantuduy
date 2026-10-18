import { View, Text } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Feather } from '@expo/vector-icons';

export default function ScheduleScreen() {
  return (
    <SafeAreaView className="flex-1 items-center justify-center bg-[#f1f5f9]">
      <View className="w-16 h-16 rounded-full bg-indigo-100 items-center justify-center mb-4">
        <Feather name="calendar" size={28} color="#4f46e5" />
      </View>
      <Text className="text-lg font-bold text-slate-800">Lịch học</Text>
      <Text className="text-sm text-slate-500 mt-2">Tính năng đang được phát triển</Text>
    </SafeAreaView>
  );
}