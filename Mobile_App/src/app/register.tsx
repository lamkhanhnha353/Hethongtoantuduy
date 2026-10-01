import { useState } from 'react';
import { Link, router } from 'expo-router';
import { KeyboardAvoidingView, Platform, ScrollView, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { AlertBanner } from '@/components/AlertBanner';
import { DebugNotice } from '@/components/DebugNotice';
import { FormField } from '@/components/FormField';
import { PrimaryButton } from '@/components/PrimaryButton';
import { RoleSelector } from '@/components/RoleSelector';
import { toApiError, useRegisterMutation } from '@/hooks/useAuthMutations';
import type { UserRole } from '@/types/auth';

type FieldName = 'phone' | 'password' | 'fullName' | 'email' | 'role';
type FieldErrors = Partial<Record<FieldName, string>>;

export default function ScreenRegister() {
  const [fullName, setFullName] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [email, setEmail] = useState('');
  const [role, setRole] = useState<UserRole>('Parent');

  const registerMutation = useRegisterMutation();

  const needsEmail = role === 'Teacher';

  const apiError = registerMutation.isError ? toApiError(registerMutation.error) : null;

  const fieldErrors: FieldErrors = apiError
    ? {
        phone: apiError.fieldErrors.Phone?.[0],
        password: apiError.fieldErrors.Password?.[0],
        fullName: apiError.fieldErrors.FullName?.[0],
        email: apiError.fieldErrors.Email?.[0],
        role: apiError.fieldErrors.Role?.[0],
      }
    : {};

  function handleSubmit() {
    registerMutation.mutate(
      {
        phone: phone.trim(),
        password,
        role,
        fullName: fullName.trim(),
        email: needsEmail ? email.trim() : undefined,
      },
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
            <Text className="text-3xl font-bold text-slate-900">Đăng ký</Text>
            <Text className="mt-2 text-sm text-slate-600">
              Tạo tài khoản để bắt đầu sử dụng hệ thống.
            </Text>
          </View>

          {apiError ? <AlertBanner tone="error" message={apiError.message} /> : null}

          <RoleSelector error={fieldErrors.role} onChange={setRole} value={role} />

          <FormField
            autoComplete="name"
            error={fieldErrors.fullName}
            label="Họ và tên"
            onChangeText={setFullName}
            placeholder="Nguyễn Văn A"
            value={fullName}
          />

          <FormField
            autoComplete="tel"
            error={fieldErrors.phone}
            keyboardType="phone-pad"
            label="Số điện thoại"
            onChangeText={setPhone}
            placeholder="0987654321"
            value={phone}
          />

          <FormField
            autoComplete="password"
            error={fieldErrors.password}
            label="Mật khẩu"
            onChangeText={setPassword}
            placeholder="Tối thiểu 6 ký tự"
            secureTextEntry
            value={password}
          />

          {needsEmail ? (
            <FormField
              autoCapitalize="none"
              autoComplete="email"
              error={fieldErrors.email}
              keyboardType="email-address"
              label="Email"
              onChangeText={setEmail}
              placeholder="giangvien@example.com"
              value={email}
            />
          ) : null}

          <PrimaryButton
            label="Tạo tài khoản"
            loading={registerMutation.isPending}
            onPress={handleSubmit}
          />

          <Text className="mt-6 text-center text-sm text-slate-600">
            Đã có tài khoản?{' '}
            <Link className="font-semibold text-sky-700" href="/login">
              Đăng nhập
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
