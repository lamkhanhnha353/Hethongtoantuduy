import { useRef, useState } from 'react';
import { Link, router } from 'expo-router';
import { Text, TextInput, View } from 'react-native';
import { KeyboardAwareScrollView } from 'react-native-keyboard-aware-scroll-view';
import { SafeAreaView } from 'react-native-safe-area-context';

import { AlertBanner } from '@/components/AlertBanner';
import { FormField } from '@/components/FormField';
import { PasswordField } from '@/components/PasswordField';
import { PrimaryButton } from '@/components/PrimaryButton';
import { Divider, SocialAuthButtons } from '@/components/SocialAuthButtons';
import { toApiError, useLoginMutation } from '@/hooks/useAuthMutations';

type FieldErrors = Partial<Record<'phone' | 'password', string>>;

const SOCIAL_NOT_READY_MESSAGE =
  'Đăng nhập bằng Google hoặc Facebook đang được phát triển. Bạn vui lòng dùng số điện thoại.';

export default function ScreenLogin() {
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [notice, setNotice] = useState<string | null>(null);
  const passwordRef = useRef<TextInput>(null);

  const loginMutation = useLoginMutation();

  const apiError = loginMutation.isError ? toApiError(loginMutation.error) : null;

  const fieldErrors: FieldErrors = apiError
    ? {
        phone: apiError.fieldErrors.phone?.[0],
        password: apiError.fieldErrors.password?.[0],
      }
    : {};

  function handleSubmit() {
    setNotice(null);
    loginMutation.mutate(
      { phone: phone.trim(), password },
      { onSuccess: () => router.replace('/home') },
    );
  }

  return (
    <SafeAreaView className="flex-1 bg-slate-50" edges={['top', 'bottom']}>
      <KeyboardAwareScrollView
        className="flex-1"
        contentContainerClassName="flex-grow px-6 py-8"
        enableOnAndroid
        extraScrollHeight={24}
        keyboardShouldPersistTaps="handled"
      >
        <View className="mb-8">
          <Text className="text-3xl font-bold text-slate-900">Chào mừng trở lại</Text>
          <Text className="mt-2 text-sm leading-5 text-slate-600">
            Đăng nhập để tiếp tục theo dõi học tập cùng Toán Tư Duy.
          </Text>
        </View>

        {apiError ? <AlertBanner tone="error" message={apiError.message} /> : null}
        {notice ? <AlertBanner tone="info" message={notice} /> : null}

        <FormField
          autoCapitalize="none"
          autoComplete="tel"
          error={fieldErrors.phone}
          keyboardType="phone-pad"
          label="Số điện thoại"
          onChangeText={setPhone}
          onSubmitEditing={() => passwordRef.current?.focus()}
          placeholder="0987654321"
          returnKeyType="next"
          textContentType="telephoneNumber"
          value={phone}
        />

        <PasswordField
          autoComplete="password"
          containerClassName="mb-2"
          error={fieldErrors.password}
          label="Mật khẩu"
          onChangeText={setPassword}
          onSubmitEditing={handleSubmit}
          placeholder="Nhập mật khẩu"
          returnKeyType="go"
          textContentType="password"
          value={password}
        />

        <View className="mb-6 items-end">
          <Link
            className="text-sm font-semibold text-sky-700"
            href="/forgot-password"
            onPress={() => setNotice(null)}
          >
            Quên mật khẩu?
          </Link>
        </View>

        <PrimaryButton
          label="Đăng nhập"
          loading={loginMutation.isPending}
          onPress={handleSubmit}
        />

        <Divider label="hoặc" />

        <SocialAuthButtons
          disabled={loginMutation.isPending}
          onPressProvider={() => setNotice(SOCIAL_NOT_READY_MESSAGE)}
        />

        <Text className="mt-8 text-center text-sm text-slate-600">
          Chưa có tài khoản?{' '}
          <Link className="font-semibold text-sky-700" href="/register">
            Đăng ký ngay
          </Link>
        </Text>
      </KeyboardAwareScrollView>
    </SafeAreaView>
  );
}