import { useState } from 'react';
import { router } from 'expo-router';
import { Text, TouchableOpacity, View } from 'react-native';
import { KeyboardAwareScrollView } from 'react-native-keyboard-aware-scroll-view';
import { SafeAreaView } from 'react-native-safe-area-context';

import { AlertBanner } from '@/components/AlertBanner';
import { FormField } from '@/components/FormField';
import { PrimaryButton } from '@/components/PrimaryButton';
import { useForgotPasswordMutation } from '@/hooks/useAuthMutations';
import { toApiError } from '@/services/apiError';
import { validateForgotPassword, type ForgotPasswordFieldErrors } from '@/validations';

type FieldErrors = ForgotPasswordFieldErrors;

export default function ScreenForgotPassword() {
  const [email, setEmail] = useState('');
  const [localErrors, setLocalErrors] = useState<FieldErrors>({});

  const forgotPasswordMutation = useForgotPasswordMutation();

  const apiError = forgotPasswordMutation.isError
    ? toApiError(forgotPasswordMutation.error)
    : null;

  const serverErrors: FieldErrors = apiError ? { email: apiError.fieldErrors.email?.[0] } : {};
  const errors: FieldErrors = { ...localErrors, ...serverErrors };

  function handleSubmit() {
    const validation = validateForgotPassword({ email });
    setLocalErrors(validation);

    if (Object.keys(validation).length > 0) {
      return;
    }

    forgotPasswordMutation.mutate(
      { email: email.trim() },
      {
        onSuccess: (response) => {
          // Message chung (email không tồn tại) → tone info; gửi thật → tone success.
          const tone = response.message.startsWith('Nếu tài khoản') ? 'info' : 'success';

          router.replace({
            pathname: '/login',
            params: { resetNotice: response.message, resetTone: tone },
          });
        },
      },
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
        <TouchableOpacity
          accessibilityLabel="Quay lại trang đăng nhập"
          accessibilityRole="button"
          className="mb-6 self-start"
          hitSlop={8}
          onPress={() => router.back()}
        >
          <Text className="text-sm font-semibold text-sky-700">← Quay lại</Text>
        </TouchableOpacity>

        <View className="mb-6">
          <Text className="text-3xl font-bold text-slate-900">Quên mật khẩu?</Text>
          <Text className="mt-2 text-sm leading-5 text-slate-600">
            Nhập email đã đăng ký tài khoản, chúng tôi sẽ gửi mật khẩu tạm đến email đó.
          </Text>
        </View>

        {apiError ? <AlertBanner tone="error" message={apiError.message} /> : null}

        <FormField
          autoCapitalize="none"
          autoComplete="email"
          error={errors.email}
          keyboardType="email-address"
          label="Email"
          onChangeText={setEmail}
          onSubmitEditing={handleSubmit}
          placeholder="ten@gmail.com"
          returnKeyType="go"
          textContentType="emailAddress"
          value={email}
        />

        <PrimaryButton
          label="Gửi mật khẩu tạm"
          loading={forgotPasswordMutation.isPending}
          onPress={handleSubmit}
        />

        <Text className="mt-6 text-center text-xs leading-4 text-slate-400">
          Mật khẩu tạm chỉ được gửi nếu email đã xác minh. Sau khi đăng nhập, hãy đổi mật khẩu
          ngay tại tab Cá nhân → Bảo mật → Đổi mật khẩu.
        </Text>
      </KeyboardAwareScrollView>
    </SafeAreaView>
  );
}
