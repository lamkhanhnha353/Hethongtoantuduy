import { useState } from 'react';
import { Link, router } from 'expo-router';
import { KeyboardAvoidingView, Platform, ScrollView, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { AlertBanner } from '@/components/AlertBanner';
import { DebugNotice } from '@/components/DebugNotice';
import { FormField } from '@/components/FormField';
import { PrimaryButton } from '@/components/PrimaryButton';
import { toApiError, useLoginMutation } from '@/hooks/useAuthMutations';

type FieldErrors = Partial<Record<'phone' | 'password', string>>;

export default function ScreenLogin() {
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');

  const loginMutation = useLoginMutation();

  const apiError = loginMutation.isError ? toApiError(loginMutation.error) : null;

  const fieldErrors: FieldErrors = apiError
    ? {
        phone: apiError.fieldErrors.Phone?.[0],
        password: apiError.fieldErrors.Password?.[0],
      }
    : {};

  function handleSubmit() {
    loginMutation.mutate(
      { phone: phone.trim(), password },
      { onSuccess: () => router.replace('/') },
    );
  }

  return (
    <SafeAreaView className="flex-1 bg-slate-50" edges={['top', 'bottom']}>
      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        className="flex-1"
      >
        <ScrollView
          className="flex-1"
          contentContainerClassName="flex-grow justify-center px-6 py-10"
          keyboardShouldPersistTaps="handled"
        >
          <View className="mb-8">
            <Text className="text-3xl font-bold text-slate-900">Đăng nhập</Text>
            <Text className="mt-2 text-sm text-slate-600">
              Hệ thống Toán Tư Duy dành cho phụ huynh, học sinh và giáo viên.
            </Text>
          </View>

          {apiError ? <AlertBanner tone="error" message={apiError.message} /> : null}

          <FormField
            autoComplete="tel"
            error={fieldErrors.phone}
            keyboardType="phone-pad"
            label="Số điện thoại"
            onChangeText={setPhone}
            placeholder="0987654321"
            returnKeyType="next"
            value={phone}
          />

          <FormField
            autoComplete="password"
            error={fieldErrors.password}
            label="Mật khẩu"
            onChangeText={setPassword}
            placeholder="Nhập mật khẩu"
            returnKeyType="go"
            secureTextEntry
            value={password}
          />

          <PrimaryButton
            label="Đăng nhập"
            loading={loginMutation.isPending}
            onPress={handleSubmit}
          />

          <Text className="mt-6 text-center text-sm text-slate-600">
            Chưa có tài khoản?{' '}
            <Link className="font-semibold text-sky-700" href="/register">
              Đăng ký ngay
            </Link>
          </Text>

          <View className="mt-8">
            <DebugNotice />
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}
