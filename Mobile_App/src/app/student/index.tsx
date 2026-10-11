
import { Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

export default function StudentDashboard() {
  return (
    <SafeAreaView className="flex-1 items-center justify-center bg-sky-100">
      <Text className="text-2xl font-bold text-sky-900">Không gian Học Sinh</Text>
    </SafeAreaView>
  );
}