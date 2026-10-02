import { Redirect } from 'expo-router';
import { ActivityIndicator, Text, View } from 'react-native';

import { useSessionQuery } from '@/hooks/useSession';

export default function ScreenIndex() {
  const { data: session, isPending } = useSessionQuery();

  if (isPending) {
    return (
      <View className="flex-1 items-center justify-center bg-slate-50">
        <ActivityIndicator size="large" color="#0284c7" />
        <Text className="mt-3 text-sm text-slate-500">Đang kiểm tra phiên đăng nhập...</Text>
      </View>
    );
  }

  return <Redirect href={session ? '/home' : '/login'} />;
}