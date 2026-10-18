import { Redirect } from 'expo-router';
import { ActivityIndicator, Text } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { SelectProfile } from '@/components/SelectProfile';
import { useSession } from '@/hooks/useSession';

export default function ScreenSelectProfile() {
  const { session, isHydrated } = useSession();

  if (!isHydrated) {
    return (
      <SafeAreaView
        className="flex-1 items-center justify-center bg-slate-50"
        edges={['bottom']}
      >
        <ActivityIndicator size="large" color="#4f46e5" />
        <Text className="mt-3 text-sm text-slate-500">Đang kiểm tra phiên đăng nhập...</Text>
      </SafeAreaView>
    );
  }

  if (!session) {
    return <Redirect href="/login" />;
  }

  return (
    <SafeAreaView className="flex-1 bg-slate-50" edges={['bottom']}>
      <SelectProfile />
    </SafeAreaView>
  );
}
