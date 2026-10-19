import { useRef, useState } from 'react';
import { router } from 'expo-router';
import { Text, TextInput, TouchableOpacity, View } from 'react-native';
import { KeyboardAwareScrollView } from 'react-native-keyboard-aware-scroll-view';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Feather } from '@expo/vector-icons';

import { AlertBanner } from '@/components/AlertBanner';
import { FormField } from '@/components/FormField';
import { PrimaryButton } from '@/components/PrimaryButton';
import { useChangePasswordMutation } from '@/hooks/useAuthMutations';
import { toApiError } from '@/services/apiError';
import { validateChangePassword, type ChangePasswordFieldErrors } from '@/validations';

type FieldErrors = ChangePasswordFieldErrors;

export default function ScreenChangePassword() {
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmNewPassword, setConfirmNewPassword] = useState('');
  const [showCurrent, setShowCurrent] = useState(false);
  const [showNew, setShowNew] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [localErrors, setLocalErrors] = useState<FieldErrors>({});
  const [notice, setNotice] = useState<string | null>(null);

  const newRef = useRef<TextInput>(null);
  const confirmRef = useRef<TextInput>(null);

  const changePasswordMutation = useChangePasswordMutation();

  const apiError = changePasswordMutation.isError
    ? toApiError(changePasswordMutation.error)
    : null;

  const serverErrors: FieldErrors = apiError
    ? {
        currentPassword: apiError.fieldErrors.currentPassword?.[0],
        newPassword: apiError.fieldErrors.newPassword?.[0],
        confirmNewPassword: apiError.fieldErrors.confirmNewPassword?.[0],
      }
    : {};

  const errors: FieldErrors = { ...localErrors, ...serverErrors };

  function handleSubmit() {
    setNotice(null);

    const validation = validateChangePassword({ currentPassword, newPassword, confirmNewPassword });
    setLocalErrors(validation);

    if (Object.keys(validation).length > 0) {
      return;
    }

    changePasswordMutation.mutate(
      { currentPassword, newPassword, confirmNewPassword },
      {
        onSuccess: (response) => {
          setCurrentPassword('');
          setNewPassword('');
          setConfirmNewPassword('');
          setLocalErrors({});
          setNotice(response.message);
        },
      },
    );
  }

  function toggleEye(current: boolean, setter: (value: boolean) => void) {
    return (
      <TouchableOpacity
        accessibilityLabel={current ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
        accessibilityRole="button"
        hitSlop={8}
        onPress={() => setter(!current)}
        className="p-1"
      >
        <Feather name={current ? 'eye-off' : 'eye'} size={18} color="#94a3b8" />
      </TouchableOpacity>
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
          accessibilityLabel="Quay lại trang cá nhân"
          accessibilityRole="button"
          className="mb-6 self-start"
          hitSlop={8}
          onPress={() => router.back()}
        >
          <Text className="text-sm font-semibold text-sky-700">← Quay lại</Text>
        </TouchableOpacity>

        <View className="mb-6">
          <Text className="text-3xl font-bold text-slate-900">Đổi mật khẩu</Text>
          <Text className="mt-2 text-sm leading-5 text-slate-600">
            Nhập mật khẩu hiện tại để xác minh, sau đó đặt mật khẩu mới cho tài khoản.
          </Text>
        </View>

        {apiError ? <AlertBanner tone="error" message={apiError.message} /> : null}
        {notice ? <AlertBanner tone="success" message={notice} /> : null}

        <FormField
          autoCapitalize="none"
          autoComplete="password"
          error={errors.currentPassword}
          label="Mật khẩu hiện tại"
          onChangeText={setCurrentPassword}
          onSubmitEditing={() => newRef.current?.focus()}
          placeholder="••••••••"
          returnKeyType="next"
          rightAccessory={toggleEye(showCurrent, setShowCurrent)}
          secureTextEntry={!showCurrent}
          value={currentPassword}
        />

        <FormField
          autoCapitalize="none"
          autoComplete="new-password"
          error={errors.newPassword}
          label="Mật khẩu mới"
          onChangeText={setNewPassword}
          onSubmitEditing={() => confirmRef.current?.focus()}
          placeholder="••••••••"
          ref={newRef}
          returnKeyType="next"
          rightAccessory={toggleEye(showNew, setShowNew)}
          secureTextEntry={!showNew}
          value={newPassword}
        />

        <FormField
          autoCapitalize="none"
          autoComplete="new-password"
          error={errors.confirmNewPassword}
          label="Xác nhận mật khẩu mới"
          onChangeText={setConfirmNewPassword}
          onSubmitEditing={handleSubmit}
          placeholder="••••••••"
          ref={confirmRef}
          returnKeyType="go"
          rightAccessory={toggleEye(showConfirm, setShowConfirm)}
          secureTextEntry={!showConfirm}
          value={confirmNewPassword}
        />

        <PrimaryButton
          label="Đổi mật khẩu"
          loading={changePasswordMutation.isPending}
          onPress={handleSubmit}
        />

        <Text className="mt-6 text-center text-xs leading-4 text-slate-400">
          Bạn vẫn đăng nhập bình thường sau khi đổi mật khẩu.
        </Text>
      </KeyboardAwareScrollView>
    </SafeAreaView>
  );
}
