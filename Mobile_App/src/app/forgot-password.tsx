import { useState } from 'react';
import { router } from 'expo-router';
import { Text, TouchableOpacity, View } from 'react-native';
import { KeyboardAwareScrollView } from 'react-native-keyboard-aware-scroll-view';
import { SafeAreaView } from 'react-native-safe-area-context';

import { AlertBanner } from '@/components/AlertBanner';
import { FormField } from '@/components/FormField';
import { PrimaryButton } from '@/components/PrimaryButton';

const NOT_READY_MESSAGE =
  'Tính năng khôi phục mật khẩu đang được phát triển. Khi hoàn tất, bạn sẽ nhận được mã xác thực qua số điện thoại đã đăng ký.';

export default function ScreenForgotPassword() {
  const [phone, setPhone] = useState('');
  const [notice, setNotice] = useState<string | null>(null);

  function handleSubmit() {
    setNotice(NOT_READY_MESSAGE);
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
          {/* <Text className="mt-2 text-sm leading-5 text-slate-600">
            Nhập số điện thoại đã đăng ký, chúng tôi sẽ gửi mã đặt lại mật khẩu.
          </Text> */}
        </View>

        {notice ? <AlertBanner tone="info" message={notice} /> : null}

        <FormField
          autoCapitalize="none"
          autoComplete="tel"
          keyboardType="phone-pad"
          label="Số điện thoại"
          onChangeText={setPhone}
          onSubmitEditing={handleSubmit}
          placeholder="0987654321"
          returnKeyType="go"
          textContentType="telephoneNumber"
          value={phone}
        />

        <PrimaryButton label="Gửi mã khôi phục" onPress={handleSubmit} />

        <Text className="mt-6 text-center text-xs leading-4 text-slate-400">
          Nếu bạn nhớ mật khẩu, hãy đăng nhập bằng số điện thoại và mật khẩu hiện tại.
        </Text>
      </KeyboardAwareScrollView>
    </SafeAreaView>
  );
}