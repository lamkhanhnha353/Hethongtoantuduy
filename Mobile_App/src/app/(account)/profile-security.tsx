import { ActivityIndicator, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useRouter } from 'expo-router';

import { AlertBanner } from '@/components/AlertBanner';
import { ProfileMenuRow } from '@/components/profile/ProfileMenuRow';
import { ProfileSubpageHeader } from '@/components/profile/ProfileSubpageHeader';
import { useProfileQuery } from '@/hooks/useProfile';
import { toApiError } from '@/services/apiError';

export default function ProfileSecurityScreen() {
  const router = useRouter();
  const { data: profile, isPending, isError, error } = useProfileQuery();

  if (isPending) {
    return (
      <SafeAreaView className="flex-1 bg-slate-50" edges={['top']}>
        <ProfileSubpageHeader title="Bảo mật" onBack={() => router.back()} />
        <View className="flex-1 items-center justify-center">
          <ActivityIndicator color="#0284c7" size="large" />
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView className="flex-1 bg-slate-50" edges={['top']}>
      <ProfileSubpageHeader title="Bảo mật" onBack={() => router.back()} />
      <View className="px-4 py-5">
        <Text className="mb-5 text-sm text-slate-500">
          Quản lý mật khẩu và các phương thức xác minh tài khoản.
        </Text>
        {isError ? (
          <AlertBanner message={toApiError(error).message} tone="error" />
        ) : (
          <View className="overflow-hidden rounded-2xl bg-white">
            <ProfileMenuRow
              description="Cập nhật mật khẩu định kỳ để an toàn hơn"
              icon="lock"
              onPress={() => router.push('/change-password')}
              title="Đổi mật khẩu"
            />
            {profile?.role === 'Parent' ? (
              <ProfileMenuRow
                badge={profile.hasPin ? 'Đã bật' : 'Chưa tạo'}
                description={
                  profile.hasPin
                    ? 'Mã PIN giao dịch đã được kích hoạt'
                    : 'Thiết lập mã PIN bảo mật giao dịch'
                }
                hasTopBorder
                icon="key"
                onPress={() => router.push('/profile-pin')}
                title="Mã PIN giao dịch"
              />
            ) : null}
          </View>
        )}
      </View>
    </SafeAreaView>
  );
}
