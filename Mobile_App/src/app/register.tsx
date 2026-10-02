import { useRef, useState } from 'react';
import { Link, router } from 'expo-router';
import { Text, TextInput, View } from 'react-native';
import { KeyboardAwareScrollView } from 'react-native-keyboard-aware-scroll-view';
import { SafeAreaView } from 'react-native-safe-area-context';

import { AlertBanner } from '@/components/AlertBanner';
import { ConsentCheckbox } from '@/components/ConsentCheckbox';
import { FormField } from '@/components/FormField';
import { PasswordField } from '@/components/PasswordField';
import { PrimaryButton } from '@/components/PrimaryButton';
import { RoleSelector } from '@/components/RoleSelector';
import { Divider, SocialAuthButtons } from '@/components/SocialAuthButtons';
import { toApiError, useRegisterMutation } from '@/hooks/useAuthMutations';
import type { UserRole } from '@/types/auth';

type FieldName = 'phone' | 'password' | 'fullName' | 'email' | 'role';
type FieldErrors = Partial<Record<FieldName, string>>;

const SOCIAL_NOT_READY_MESSAGE =
  'Đăng ký bằng Google hoặc Facebook đang được phát triển. Bạn vui lòng điền thông tin bên dưới.';
const CONSENT_REQUIRED_MESSAGE = 'Bạn cần đồng ý với Điều khoản sử dụng và Chính sách bảo mật.';
const TERMS_LABEL =
  'Tôi đồng ý với Điều khoản sử dụng và Chính sách bảo mật của Toán Tư Duy.';

export default function ScreenRegister() {
  const [fullName, setFullName] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [email, setEmail] = useState('');
  const [role, setRole] = useState<UserRole>('Parent');
  const [hasConsent, setHasConsent] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);

  const fullNameRef = useRef<TextInput>(null);
  const phoneRef = useRef<TextInput>(null);
  const passwordRef = useRef<TextInput>(null);
  const emailRef = useRef<TextInput>(null);

  const registerMutation = useRegisterMutation();

  const needsEmail = role === 'Teacher';

  const apiError = registerMutation.isError ? toApiError(registerMutation.error) : null;

  const fieldErrors: FieldErrors = apiError
    ? {
      phone: apiError.fieldErrors.phone?.[0],
      password: apiError.fieldErrors.password?.[0],
      fullName: apiError.fieldErrors.fullName?.[0],
      email: apiError.fieldErrors.email?.[0],
      role: apiError.fieldErrors.role?.[0],
    }
    : {};

  function handleSubmit() {
    if (!hasConsent) {
      setNotice(CONSENT_REQUIRED_MESSAGE);
      return;
    }

    setNotice(null);
    registerMutation.mutate(
      {
        phone: phone.trim(),
        password,
        role,
        fullName: fullName.trim(),
        email: needsEmail ? email.trim() : undefined,
      },
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
        <View className="mb-6">
          <Text className="text-3xl font-bold text-slate-900">Đăng ký</Text>
          {/* <Text className="mt-2 text-sm leading-5 text-slate-600">
            Chỉ mất một phút để bắt đầu học tập cùng Toán Tư Duy.
          </Text> */}
        </View>

        {apiError ? <AlertBanner tone="error" message={apiError.message} /> : null}
        {notice ? <AlertBanner tone="info" message={notice} /> : null}

        <RoleSelector error={fieldErrors.role} onChange={setRole} value={role} />

        <FormField
          autoComplete="name"
          containerClassName="mb-4"
          error={fieldErrors.fullName}
          label="Họ và tên"
          onChangeText={setFullName}
          onSubmitEditing={() => phoneRef.current?.focus()}
          placeholder="Nguyễn Văn A"
          returnKeyType="next"
          textContentType="name"
          value={fullName}
          ref={fullNameRef}
        />

        <FormField
          autoCapitalize="none"
          autoComplete="tel"
          containerClassName="mb-4"
          error={fieldErrors.phone}
          keyboardType="phone-pad"
          label="Số điện thoại"
          onChangeText={setPhone}
          onSubmitEditing={() => passwordRef.current?.focus()}
          placeholder="0987654321"
          returnKeyType="next"
          textContentType="telephoneNumber"
          value={phone}
          ref={phoneRef}
        />

        <PasswordField
          autoComplete="password-new"
          error={fieldErrors.password}
          hint="Tối thiểu 6 ký tự."
          label="Mật khẩu"
          onChangeText={setPassword}
          onSubmitEditing={() => {
            if (needsEmail) {
              emailRef.current?.focus();
            }
          }}
          placeholder="Nhập mật khẩu"
          returnKeyType="next"
          textContentType="newPassword"
          value={password}
          ref={passwordRef}
        />

        {needsEmail ? (
          <FormField
            autoCapitalize="none"
            autoComplete="email"
            containerClassName="mb-4"
            error={fieldErrors.email}
            // hint="Bắt buộc với tài khoản giáo viên."
            keyboardType="email-address"
            label="Email"
            onChangeText={setEmail}
            onSubmitEditing={handleSubmit}
            placeholder="giangvien@example.com"
            returnKeyType="go"
            textContentType="emailAddress"
            value={email}
            ref={emailRef}
          />
        ) : null}

        <ConsentCheckbox checked={hasConsent} label={TERMS_LABEL} onChange={setHasConsent} />

        <PrimaryButton
          label="Đăng ký"
          loading={registerMutation.isPending}
          onPress={handleSubmit}
        />

        <Divider label="hoặc" />

        <SocialAuthButtons
          disabled={registerMutation.isPending}
          onPressProvider={() => setNotice(SOCIAL_NOT_READY_MESSAGE)}
        />

        <Text className="mt-8 text-center text-sm text-slate-600">
          Đã có tài khoản?{' '}
          <Link className="font-semibold text-sky-700" href="/login">
            Đăng nhập
          </Link>
        </Text>
      </KeyboardAwareScrollView>
    </SafeAreaView>
  );
}