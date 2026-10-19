import { useRef, useState, type RefObject } from 'react';
import { useRouter } from 'expo-router';
import { ActivityIndicator, Pressable, ScrollView, Text, TextInput, View } from 'react-native';
import { SafeAreaView, useSafeAreaInsets } from 'react-native-safe-area-context';

import { AlertBanner } from '@/components/AlertBanner';
import { Avatar } from '@/components/Avatar';
import { FormField } from '@/components/FormField';
import { PrimaryButton } from '@/components/PrimaryButton';
import { ROLE_LABELS } from '@/content/homeContent';
import { toApiError } from '@/services/apiError';
import { useLogout, useSession } from '@/hooks/useSession';
import {
  useProfileQuery,
  useUpdateEmailMutation,
  useUpdateProfileMutation,
} from '@/hooks/useProfile';
import type { ProfileResponseDto } from '@/types/profile';

type FieldErrors = { fullName?: string; email?: string };

function PencilButton({ inputRef }: { inputRef: RefObject<TextInput | null> }) {
  return (
    <Pressable hitSlop={8} onPress={() => inputRef.current?.focus()}>
      <Text className="text-base">✏️</Text>
    </Pressable>
  );
}

export default function ScreenProfile() {
  const { isHydrated: isSessionHydrated } = useSession();
  const { data: profile, isPending, isError, error, refetch } = useProfileQuery();

  const apiError = isError ? toApiError(error) : null;

  if (!isSessionHydrated || isPending) {
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

  return <ProfileForm profile={profile} />;
}

/**
 * Form chỉ mount sau khi tải được profile nên state khởi tạo thẳng từ props
 * (không cần effect đồng bộ).
 */
function ProfileForm({ profile }: { profile: ProfileResponseDto }) {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const updateMutation = useUpdateProfileMutation();
  const emailMutation = useUpdateEmailMutation();
  const logout = useLogout();

  const nameInputRef = useRef<TextInput>(null);
  const emailInputRef = useRef<TextInput>(null);

  const [notice, setNotice] = useState<string | null>(null);
  const [isLoggingOut, setIsLoggingOut] = useState(false);
  const [fullName, setFullName] = useState(profile.fullName);
  const [email, setEmail] = useState(profile.email ?? '');
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  const saveError = updateMutation.isError ? toApiError(updateMutation.error) : null;
  const emailError = emailMutation.isError ? toApiError(emailMutation.error) : null;
  const serverError = emailError?.message ?? saveError?.message ?? null;

  const isParent = profile.role === 'Parent';

  function handleSave() {
    const trimmedName = fullName.trim();
    const trimmedEmail = email.trim();
    const emailEditable = isParent && !profile.emailConfirm;
    const nameChanged = trimmedName !== profile.fullName;
    const emailChanged = emailEditable && trimmedEmail !== (profile.email ?? '');

    const nextErrors: FieldErrors = {};
    if (!trimmedName) nextErrors.fullName = 'Họ và tên không được để trống.';
    if (emailEditable && !trimmedEmail) nextErrors.email = 'Email không được để trống.';

    setFieldErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) return;

    if (!nameChanged && !emailChanged) {
      setNotice('Không có thay đổi nào để lưu.');
      return;
    }

    setNotice(null);

    const tasks: Promise<unknown>[] = [];
    if (nameChanged) tasks.push(updateMutation.mutateAsync({ fullName: trimmedName }));
    if (emailChanged) tasks.push(emailMutation.mutateAsync({ email: trimmedEmail }));

    void Promise.all(tasks)
      .then(() => setNotice('Đã lưu thay đổi.'))
      .catch(() => {
        // Lỗi hiển thị qua banner serverError từ mutation.
      });
  }

  async function handleLogout() {
    setIsLoggingOut(true);

    try {
      await logout();
      router.replace('/login');
    } finally {
      setIsLoggingOut(false);
    }
  }

  return (
    <SafeAreaView className="flex-1 bg-slate-50" edges={['top']}>
      <ScrollView contentContainerClassName="px-6 py-6 pb-32">
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

          {serverError ? (
            <View className="mt-4">
              <AlertBanner message={serverError} tone="error" />
            </View>
          ) : null}

          <View className="mt-4">
            <FormField
              ref={nameInputRef}
              containerClassName="mb-4"
              error={fieldErrors.fullName}
              label="Họ và tên"
              onChangeText={setFullName}
              placeholder="Nguyễn Văn A"
              returnKeyType="done"
              rightAccessory={<PencilButton inputRef={nameInputRef} />}
              textContentType="name"
              value={fullName}
            />

            {isParent ? (
              <FormField
                ref={emailInputRef}
                autoCapitalize="none"
                autoComplete="email"
                className={
                  profile.emailConfirm ? 'bg-slate-100 text-slate-500' : undefined
                }
                containerClassName="mb-4"
                editable={!profile.emailConfirm}
                error={fieldErrors.email}
                hint={profile.emailConfirm ? 'Email đã xác nhận, không thể thay đổi' : undefined}
                keyboardType="email-address"
                label="Email"
                onChangeText={setEmail}
                placeholder="ten@gmail.com"
                returnKeyType="done"
                rightAccessory={
                  profile.emailConfirm ? undefined : <PencilButton inputRef={emailInputRef} />
                }
                value={profile.emailConfirm ? (profile.email ?? '') : email}
              />
            ) : null}
          </View>
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

      <View
        className="absolute inset-x-0 bottom-0 border-t border-slate-200 bg-white px-6 pt-3"
        style={{ paddingBottom: Math.max(insets.bottom, 12) }}
      >
        <PrimaryButton
          label="Lưu thay đổi"
          loading={updateMutation.isPending || emailMutation.isPending}
          onPress={handleSave}
        />
      </View>
    </SafeAreaView>
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
