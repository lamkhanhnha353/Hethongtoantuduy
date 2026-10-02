import { useState } from 'react';
import { Redirect, useRouter } from 'expo-router';
import { ActivityIndicator, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { PrimaryButton } from '@/components/PrimaryButton';
import { FeatureGrid, HomeOverview } from '@/components/home/HomeOverview';
import { WelcomeHeader } from '@/components/home/WelcomeHeader';
import { useClearSessionMutation, useSessionQuery } from '@/hooks/useSession';

export default function ScreenHome() {
  const router = useRouter();
  const { data: session, isPending } = useSessionQuery();
  const clearSession = useClearSessionMutation();
  const [isLoggingOut, setIsLoggingOut] = useState(false);

  if (isPending) {
    return (
      <SafeAreaView
        className="flex-1 items-center justify-center bg-slate-50"
        edges={['top', 'bottom']}
      >
        <ActivityIndicator size="large" color="#0284c7" />
        <Text className="mt-3 text-sm text-slate-500">Đang tải thông tin tài khoản...</Text>
      </SafeAreaView>
    );
  }

  if (!session) {
    return <Redirect href="/login" />;
  }

  const displayName = session.fullName.trim() || session.phone;

  async function handleLogout() {
    setIsLoggingOut(true);

    try {
      await clearSession();
      router.replace('/login');
    } finally {
      setIsLoggingOut(false);
    }
  }

  return (
    <SafeAreaView className="flex-1 bg-slate-50" edges={['top', 'bottom']}>
      <View className="flex-1 px-6 py-6">
        <WelcomeHeader displayName={displayName} role={session.role} />

        {/* <View className="mt-6">
          <HomeOverview session={session} />
        </View> */}

        <FeatureGrid role={session.role} />

        <View className="mt-8">
          <PrimaryButton
            label="Đăng xuất"
            loading={isLoggingOut}
            onPress={handleLogout}
            variant="ghost"
          />
        </View>
      </View>
    </SafeAreaView>
  );
}