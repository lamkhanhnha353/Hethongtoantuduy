import { useEffect, useRef, useState, type RefObject } from 'react';
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
import { validateProfileForm, type ProfileFieldErrors } from '@/validations';

function PencilButton({ inputRef }: { inputRef: RefObject<TextInput | null> }) {
  return (
    <Pressable
      hitSlop={8}
      onPress={() => inputRef.current?.focus()}
      className="flex-row items-center gap-1 rounded-lg bg-sky-50 px-2.5 py-1 active:bg-sky-100"
    >
      <Text className="text-xs font-semibold text-sky-600">Sửa</Text>
    </Pressable>
  );
}

export default function ScreenProfile() {
  const router = useRouter();
  const { isHydrated: isSessionHydrated } = useSession();
  const logout = useLogout();
  const { data: profile, isPending, isError } = useProfileQuery();

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

  if (!isSessionHydrated || isPending || isError || !profile) {
    return (
      <SafeAreaView className="flex-1 items-center justify-center bg-slate-50" edges={[]}>
        <View className="items-center rounded-3xl border border-slate-100 bg-white p-6 shadow-sm">
          <ActivityIndicator size="large" color="#0284c7" />
          <Text className="mt-3.5 text-sm font-medium text-slate-500">Đang tải hồ sơ...</Text>
        </View>
      </SafeAreaView>
    );
  }

  return <ProfileForm profile={profile} />;
}

/**
 * Form chỉ mount sau khi tải được profile nên state khởi tạo thẳng từ props
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
  const [fieldErrors, setFieldErrors] = useState<ProfileFieldErrors>({});

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

    const nextErrors = validateProfileForm(
      { fullName, email },
      { requireEmail: emailEditable },
    );

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
    <SafeAreaView className="flex-1 bg-slate-50" edges={[]}>
      <ScrollView
        showsVerticalScrollIndicator={false}
        contentContainerClassName="px-5 py-5 pb-36"
      >
        {/* Header Title */}


        {/* Hero Profile Card */}
        <View className="rounded-3xl border border-slate-200/80 bg-white p-5 shadow-sm shadow-slate-100">
          <View className="flex-row items-center gap-4">
            <View className="relative">
              <Avatar name={profile.fullName} size={68} uri={profile.avatar} />
              <View className="absolute bottom-0 right-0 h-4 w-4 rounded-full border-2 border-white bg-emerald-500" />
            </View>

            <View className="flex-1">
              <Text className="text-lg font-bold text-slate-900" numberOfLines={1}>
                {profile.fullName}
              </Text>
              <Text className="mt-0.5 text-sm font-medium text-slate-500">
                {profile.phone}
              </Text>

              <View className="mt-2.5 flex-row flex-wrap items-center gap-2">
                <View className="flex-row items-center rounded-full border border-amber-200/80 bg-amber-50 px-2.5 py-0.5">
                  <View className="mr-1.5 h-1.5 w-1.5 rounded-full bg-amber-500" />
                  <Text className="text-xs font-semibold text-amber-800">
                    {ROLE_LABELS[profile.role]}
                  </Text>
                </View>

                {/* <View className="rounded-full bg-slate-100 px-2.5 py-0.5">
                  <Text className="font-mono text-xs font-medium text-slate-600">
                    #{profile.accountId}
                  </Text> */}
              </View>
            </View>
          </View>
        </View>


        {/* Notice Message */}
        {notice ? (
          <View className="mt-4">
            <AlertBanner message={notice} tone="success" />
          </View>
        ) : null}

        {/* Thông tin cá nhân */}
        <View className="mt-5 rounded-3xl border border-slate-200/80 bg-white p-5 shadow-sm shadow-slate-100">


          {serverError ? (
            <View className="mb-4">
              <AlertBanner message={serverError} tone="error" />
            </View>
          ) : null}

          <View className="mt-2">
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
                className={profile.emailConfirm ? 'bg-slate-50 text-slate-500' : undefined}
                containerClassName="mb-1"
                editable={!profile.emailConfirm}
                error={fieldErrors.email}
                hint={profile.emailConfirm ? 'Email đã được xác nhận bảo mật' : undefined}
                keyboardType="email-address"
                label="Email"
                onChangeText={setEmail}
                placeholder="ten@gmail.com"
                returnKeyType="done"
                rightAccessory={
                  profile.emailConfirm ? (
                    <View className="rounded-md border border-emerald-200 bg-emerald-50 px-2 py-0.5">
                      <Text className="text-[11px] font-semibold text-emerald-700">
                        ✓ Đã xác thực
                      </Text>
                    </View>
                  ) : (
                    <PencilButton inputRef={emailInputRef} />
                  )
                }
                value={profile.emailConfirm ? (profile.email ?? '') : email}
              />
            ) : null}
          </View>
        </View>

        {/* Thông tin tài khoản */}
        <View className="mt-5 rounded-3xl border border-slate-200/80 bg-white p-5 shadow-sm shadow-slate-100">
          <SectionHeader title="Chi tiết tài khoản" />

          <View className="mt-1">
            <AccountRow label="Số điện thoại" value={profile.phone} />
            <AccountRow label="Email" value={profile.email ?? 'Chưa cập nhật'} />
            <AccountRow label="Mã hồ sơ" value={`#${profile.accountId}`} isMonospace />
            <AccountRow label="Vai trò hệ thống" value={ROLE_LABELS[profile.role]} isLast />
          </View>
        </View>

        {/* Bảo mật */}
        <View className="mt-5 rounded-3xl border border-slate-200/80 bg-white p-5 shadow-sm shadow-slate-100">
          <SectionHeader
            title="Bảo mật & Xác thực"
            subtitle="Quản lý mật khẩu và các phương thức xác minh"
          />

          <View className="mt-2 gap-3">
            <SecurityMenuTile
              title="Đổi mật khẩu"
              subtitle="Cập nhật mật khẩu định kỳ để an toàn hơn"
              onPress={() => router.push('/change-password')}
            />

            {isParent ? (
              <SecurityMenuTile
                title="Mã PIN giao dịch"
                subtitle={
                  profile.hasPin
                    ? 'Đã kích hoạt mã PIN bảo mật giao dịch'
                    : 'Chưa thiết lập mã PIN bảo mật'
                }
                badgeText={profile.hasPin ? 'Đã bật' : 'Chưa tạo'}
                badgeTone={profile.hasPin ? 'success' : 'neutral'}
                onPress={() => router.push('/profile-pin')}
              />
            ) : null}
          </View>
        </View>

        {/* Nút Đăng xuất */}
        <View className="mt-6">
          <Pressable
            disabled={isLoggingOut}
            onPress={() => {
              void handleLogout();
            }}
            className="flex-row items-center justify-center gap-2 rounded-2xl border border-rose-200 bg-rose-50/70 py-3.5 active:bg-rose-100"
          >
            {isLoggingOut ? (
              <ActivityIndicator size="small" color="#e11d48" />
            ) : (
              <Text className="text-sm font-semibold text-rose-600">
                Đăng xuất khỏi thiết bị
              </Text>
            )}
          </Pressable>
        </View>
      </ScrollView>

      {/* Thanh lưu cố định phía dưới */}
      <View
        className="absolute inset-x-0 bottom-0 border-t border-slate-200/80 bg-white/95 px-5 pt-3 shadow-lg shadow-slate-900/5 backdrop-blur-md"
        style={{ paddingBottom: Math.max(insets.bottom, 14) }}
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

function SectionHeader({ title, subtitle }: { title: string; subtitle?: string }) {
  return (
    <View className="mb-3.5">
      <Text className="text-base font-bold text-slate-900">{title}</Text>
      {subtitle ? <Text className="mt-0.5 text-xs text-slate-500">{subtitle}</Text> : null}
    </View>
  );
}

function AccountRow({
  label,
  value,
  isMonospace = false,
  isLast = false,
}: {
  label: string;
  value: string;
  isMonospace?: boolean;
  isLast?: boolean;
}) {
  return (
    <View
      className={`flex-row items-center justify-between py-3 ${!isLast ? 'border-b border-slate-100' : ''
        }`}
    >
      <Text className="text-sm font-medium text-slate-500">{label}</Text>
      <Text
        className={`text-sm font-semibold text-slate-800 ${isMonospace ? 'font-mono text-slate-600' : ''
          }`}
        numberOfLines={1}
      >
        {value}
      </Text>
    </View>
  );
}

/**
 * Tile hành động bảo mật chuẩn Navigation Row
 */
function SecurityMenuTile({
  title,
  subtitle,
  badgeText,
  badgeTone = 'neutral',
  onPress,
}: {
  title: string;
  subtitle: string;
  badgeText?: string;
  badgeTone?: 'neutral' | 'success';
  onPress: () => void;
}) {
  return (
    <Pressable
      onPress={onPress}
      className="flex-row items-center justify-between rounded-2xl border border-slate-100 bg-slate-50/70 p-3.5 active:bg-slate-100"
    >
      <View className="mr-2 flex-1">
        <Text className="text-sm font-semibold text-slate-900">{title}</Text>
        <Text className="mt-0.5 text-xs text-slate-500" numberOfLines={1}>
          {subtitle}
        </Text>
      </View>

      <View className="flex-row items-center gap-1.5">
        {badgeText ? (
          <View
            className={`rounded-full px-2 py-0.5 ${badgeTone === 'success'
              ? 'border border-emerald-200 bg-emerald-50'
              : 'bg-slate-200/60'
              }`}
          >
            <Text
              className={`text-[11px] font-semibold ${badgeTone === 'success' ? 'text-emerald-700' : 'text-slate-600'
                }`}
            >
              {badgeText}
            </Text>
          </View>
        ) : null}
        <Text className="text-base font-light text-slate-400">›</Text>
      </View>
    </Pressable>
  );
}
