import { useEffect, useState } from 'react';
import { Feather } from '@expo/vector-icons';
import { useRouter } from 'expo-router';
import { ActivityIndicator, Pressable, ScrollView, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Avatar } from '@/components/Avatar';
import { ProfileMenuRow } from '@/components/profile/ProfileMenuRow';
import { ROLE_LABELS } from '@/content/homeContent';
import { useLogout, useSession } from '@/hooks/useSession';
import { useProfileQuery } from '@/hooks/useProfile';

export default function ProfileScreen() {
  const router = useRouter();
  const { isHydrated } = useSession();
  const logout = useLogout();
  const { data: profile, isPending, isError } = useProfileQuery();
  const [isLoggingOut, setIsLoggingOut] = useState(false);
  const isParent = profile?.role === 'Parent';

  useEffect(() => {
    if (!isError) return;

    let isCancelled = false;

    void logout().finally(() => {
      if (!isCancelled) {
        router.replace('/login');
      }
    });

    return () => {
      isCancelled = true;
    };
  }, [isError, logout, router]);

  async function handleLogout() {
    setIsLoggingOut(true);

    try {
      await logout();
      router.replace('/login');
    } finally {
      setIsLoggingOut(false);
    }
  }

  if (!isHydrated || isPending || isError || !profile) {
    return (
      <SafeAreaView className="flex-1 items-center justify-center bg-slate-50" edges={[]}>
        <ActivityIndicator color="#0284c7" size="large" />
        <Text className="mt-3 text-sm text-slate-500">Đang tải hồ sơ...</Text>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView className="flex-1 bg-indigo-500" edges={['top']}>
      <ScrollView
        className="flex-1 bg-slate-100"
        contentContainerClassName="pb-8"
        showsVerticalScrollIndicator={false}
      >
        <View className="h-12 bg-indigo-500" />

        <View className="-mt-5 mx-4 flex-row items-center rounded-2xl bg-white p-4 shadow-sm">
          <Avatar name={profile.fullName} size={58} uri={profile.avatar} />
          <View className="ml-3 flex-1">
            <Text className="text-base font-bold text-slate-900" numberOfLines={1}>
              {profile.fullName}
            </Text>
            <Text className="mt-1 text-sm text-slate-500">{profile.phone}</Text>
            <Text className="mt-1 text-xs font-medium text-sky-700">
              {ROLE_LABELS[profile.role]}
            </Text>
          </View>
        </View>

        <Text className="mb-2 ml-5 mt-6 text-xs font-bold uppercase tracking-wide text-slate-500">
          Tài khoản
        </Text>
        <View className="mx-4 overflow-hidden rounded-2xl bg-white">
          <ProfileMenuRow
            description="Cập nhật thông tin và email"
            icon="user"
            onPress={() => router.push('/profile-details')}
            title="Thông tin cá nhân"
          />
          {isParent ? (
            <ProfileMenuRow
              description="Quản lý hồ sơ và việc học"
              hasTopBorder
              icon="users"
              onPress={() => router.push('/(account)/students')}
              title="Học sinh"
            />
          ) : null}
        </View>

        <Text className="mb-2 ml-5 mt-6 text-xs font-bold uppercase tracking-wide text-slate-500">
          Cài đặt
        </Text>
        <View className="mx-4 overflow-hidden rounded-2xl bg-white">
          {isParent ? (
            <ProfileMenuRow
              description="Bật/tắt và cấu hình nội dung nhận"
              icon="bell"
              onPress={() => router.push('/profile-notifications')}
              title="Cài đặt thông báo"
            />
          ) : null}
          <ProfileMenuRow
            description="Mật khẩu và mã PIN giao dịch"
            hasTopBorder={isParent}
            icon="shield"
            onPress={() => router.push('/profile-security')}
            title="Bảo mật"
          />
          <ProfileMenuRow
            description="Hiệu ứng âm thanh khi học"
            hasTopBorder
            icon="volume-2"
            onPress={() => router.push('/profile-sound')}
            title="Âm thanh"
          />
        </View>

        <Pressable
          accessibilityRole="button"
          className="mx-4 mt-6 flex-row items-center justify-center gap-2 rounded-2xl border border-rose-200 bg-white py-4 active:bg-rose-50"
          disabled={isLoggingOut}
          onPress={() => {
            void handleLogout();
          }}
        >
          {isLoggingOut ? (
            <ActivityIndicator color="#e11d48" size="small" />
          ) : (
            <Feather color="#e11d48" name="log-out" size={17} />
          )}
          <Text className="text-sm font-semibold text-rose-600">Đăng xuất khỏi thiết bị</Text>
        </Pressable>
      </ScrollView>
    </SafeAreaView>
  );
}
