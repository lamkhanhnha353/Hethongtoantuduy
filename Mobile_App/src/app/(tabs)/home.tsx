import { useState } from 'react';
import { Redirect, useRouter } from 'expo-router';
import { ActivityIndicator, ScrollView, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';



import { AlertBanner } from '@/components/AlertBanner';
import { WelcomeHeader } from '@/components/parent/WelcomeHeader';
import type { HomeFeature } from '@/content/homeContent';
import { useProfileQuery } from '@/hooks/useProfile';
import { useSession } from '@/hooks/useSession';

const PARENT_FEATURE_ROUTES: Record<string, '/students'> = {
  students: '/students',
};

export default function ScreenHome() {
  const router = useRouter();
  const { session, isHydrated } = useSession();
  const { data: profile } = useProfileQuery();
  const [notice, setNotice] = useState<string | null>(null);

  if (!isHydrated) {
    return (
      <SafeAreaView
        className="flex-1 items-center justify-center bg-slate-50"
        edges={['bottom']}
      >
        <ActivityIndicator size="large" color="#0284c7" />
        <Text className="mt-3 text-sm text-slate-500">Đang tải thông tin tài khoản...</Text>
      </SafeAreaView>
    );
  }

  if (!session) {
    return <Redirect href="/login" />;
  }

  const displayName = profile?.fullName.trim() || session.fullName.trim() || session.phone;

  function handleSelectFeature(feature: HomeFeature) {
    if (session?.role === 'Parent') {
      const route = PARENT_FEATURE_ROUTES[feature.id];

      if (route) {
        router.push(route);
        return;
      }
    }

    setNotice(`${feature.title} đang được phát triển.`);
  }

  return (
    <SafeAreaView className="flex-1 bg-slate-50" edges={[]}>
      <ScrollView className="flex-1" contentContainerClassName="px-6 py-6">
        {notice ? <AlertBanner tone="info" message={notice} /> : null}

        <WelcomeHeader />

      </ScrollView>
    </SafeAreaView>
  );
}
