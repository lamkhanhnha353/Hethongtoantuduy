import { useState } from 'react';
import { useRouter } from 'expo-router';
import { ActivityIndicator, ScrollView, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { AlertBanner } from '@/components/AlertBanner';
import { Avatar } from '@/components/Avatar';
import { FormField } from '@/components/FormField';
import { PrimaryButton } from '@/components/PrimaryButton';
import { ROLE_LABELS } from '@/content/homeContent';
import { toApiError } from '@/services/apiError';
import { useClearSessionMutation, useSessionQuery } from '@/hooks/useSession';
import { useProfileQuery, useUpdateProfileMutation } from '@/hooks/useProfile';

export default function ScreenProfile() {
  const router = useRouter();
  const { isPending: isSessionPending } = useSessionQuery();
  const { data: profile, isPending, isError, error, refetch } = useProfileQuery();
  const updateMutation = useUpdateProfileMutation();
  const clearSession = useClearSessionMutation();

  const [notice, setNotice] = useState<string | null>(null);
  const [isLoggingOut, setIsLoggingOut] = useState(false);

  const apiError = isError ? toApiError(error) : null;
  const saveError = updateMutation.isError ? toApiError(updateMutation.error) : null;
  const fieldErrors = saveError ? { fullName: saveError.fieldErrors.fullName?.[0] } : {};

  if (isSessionPending || isPending) {
    return (
      <SafeAreaView className="flex-1 items-center justify-center bg-slate-50" edges={['top']}>
        <ActivityIndicator size="large" color="#0284c7" />
        <Text className="mt-3 text-sm text-slate-500">Đang tải hồ sơ...</Text>
      </SafeAreaView>
    );
  }

  if (apiError || !profile) {
    return (
      <SafeAreaView className="flex-1 bg-slate-50 px-6 py-6" edges={['top']}>
        <Text className="text-2xl font-bold text-slate-900">Cá nhân</Text>
        <View className="mt-4">
          <AlertBanner message={apiError?.message ?? 'Không tải được hồ sơ.'} tone="error" />
        </View>
        <PrimaryButton
          label="Thử lại"
          onPress={() => {
            void refetch();
          }}
        />
      </SafeAreaView>
    );
  }

  const isParent = profile.role === 'Parent';

  function handleSave(fullName: string) {
    setNotice(null);
    updateMutation.mutate({ fullName }, { onSuccess: () => setNotice('Đã lưu thay đổi.') });
  }

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
    <SafeAreaView className="flex-1 bg-slate-50" edges={['top']}>
      <ScrollView contentContainerClassName="px-6 py-6">
        <Text className="mb-5 text-2xl font-bold text-slate-900">Cá nhân</Text>

        <View className="flex-row items-center gap-4 rounded-2xl border border-slate-200 bg-white p-5">
          <Avatar name={profile.fullName} size={64} uri={profile.avatar} />

          <View className="flex-1">
            <Text className="text-lg font-semibold text-slate-900" numberOfLines={1}>
              {profile.fullName}
            </Text>
            <Text className="mt-0.5 text-sm text-slate-500">{profile.phone}</Text>
            <View className="mt-1.5 self-start rounded-full bg-amber-100 px-2.5 py-1">
              <Text className="text-xs font-semibold text-amber-800">
                {ROLE_LABELS[profile.role]}
              </Text>
            </View>
          </View>
        </View>

        {notice ? <AlertBanner message={notice} tone="success" /> : null}

        <View className="mt-5 rounded-2xl border border-slate-200 bg-white p-5">
          <Text className="text-base font-semibold text-slate-900">Thông tin cá nhân</Text>

          <NameForm
            key={profile.fullName}
            error={fieldErrors.fullName}
            initialFullName={profile.fullName}
            loading={updateMutation.isPending}
            onSave={handleSave}
          />
        </View>

        <View className="mt-5 rounded-2xl border border-slate-200 bg-white p-5">
          <Text className="text-base font-semibold text-slate-900">Tài khoản</Text>

          <View className="mt-3 gap-2 border-t border-slate-100 pt-4">
            <Row label="Số điện thoại" value={profile.phone} />
            <Row label="Email" value={profile.email ?? 'Chưa cập nhật'} />
            <Row label="Mã hồ sơ" value={`#${profile.accountId}`} />
            <Row label="Vai trò" value={ROLE_LABELS[profile.role]} />
          </View>
        </View>

        {isParent ? (
          <View className="mt-5 rounded-2xl border border-slate-200 bg-white p-5">
            <Text className="text-base font-semibold text-slate-900">Bảo mật</Text>

            <View className="mt-3 flex-row items-center justify-between border-t border-slate-100 pt-4">
              <Text className="text-sm text-slate-500">Mã PIN giao dịch</Text>
              <Text className="text-sm font-medium text-slate-900">
                {profile.hasPin ? 'Đã thiết lập' : 'Chưa thiết lập'}
              </Text>
            </View>

            <View className="mt-4">
              <PrimaryButton
                label="Đặt / đổi PIN"
                onPress={() => router.push('/profile-pin')}
                variant="ghost"
              />
            </View>
          </View>
        ) : null}

        <View className="mt-6">
          <PrimaryButton
            label="Đăng xuất"
            loading={isLoggingOut}
            onPress={() => {
              void handleLogout();
            }}
            variant="ghost"
          />
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}

type NameFormProps = {
  initialFullName: string;
  loading: boolean;
  error?: string;
  onSave: (fullName: string) => void;
};

/**
 * `key` theo giá trị trên máy chủ để tự khởi tạo lại state mỗi khi tên được
 * cập nhật thành công, tránh set state trong useEffect.
 */
function NameForm({ initialFullName, loading, error, onSave }: NameFormProps) {
  const [fullName, setFullName] = useState(initialFullName);

  return (
    <View className="mt-4">
      <FormField
        containerClassName="mb-4"
        error={error}
        label="Họ và tên"
        onChangeText={setFullName}
        placeholder="Nguyễn Văn A"
        returnKeyType="done"
        textContentType="name"
        value={fullName}
      />

      <PrimaryButton label="Lưu thay đổi" loading={loading} onPress={() => onSave(fullName.trim())} />
    </View>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <View className="flex-row items-center justify-between">
      <Text className="text-sm text-slate-500">{label}</Text>
      <Text className="text-sm font-medium text-slate-900" numberOfLines={1}>
        {value}
      </Text>
    </View>
  );
}
