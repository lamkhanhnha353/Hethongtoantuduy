import { Redirect } from 'expo-router';
import { ActivityIndicator, Text, View } from 'react-native';

import { useSession } from '@/hooks/useSession';

export default function ScreenIndex() {
  const { session, isHydrated } = useSession();

  if (!isHydrated) {
    return (
      <View className="flex-1 items-center justify-center bg-slate-50">
        <ActivityIndicator size="large" color="#0284c7" />
        <Text className="mt-3 text-sm text-slate-500">Đang kiểm tra phiên đăng nhập...</Text>
      </View>
    );
  }

  return <Redirect href={session ? '/home' : '/login'} />;
}