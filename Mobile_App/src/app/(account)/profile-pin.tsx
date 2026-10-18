import { useState } from 'react';
import { ScrollView, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { AlertBanner } from '@/components/AlertBanner';
import { FormField } from '@/components/FormField';
import { PrimaryButton } from '@/components/PrimaryButton';
import { ScreenHeader } from '@/components/ScreenHeader';
import { toApiError } from '@/services/apiError';
import { useSetPinMutation } from '@/hooks/usePin';
import { useProfileQuery } from '@/hooks/useProfile';

const PIN_PATTERN = /^\d{4,6}$/;
const PIN_FORMAT_MESSAGE = 'PIN phải gồm 4 đến 6 chữ số.';
const PIN_MISMATCH_MESSAGE = 'PIN hiện tại không đúng. Vui lòng kiểm tra lại.';

export default function ScreenProfilePin() {
  const { data: profile, isPending, isError, error, refetch } = useProfileQuery();
  const setPinMutation = useSetPinMutation();

  const [currentPin, setCurrentPin] = useState('');
  const [newPin, setNewPin] = useState('');
  const [confirmPin, setConfirmPin] = useState('');
  const [notice, setNotice] = useState<string | null>(null);
  const [localError, setLocalError] = useState<string | null>(null);

  const hasPin = profile?.hasPin ?? false;

  const loadError = isError ? toApiError(error) : null;
  const saveError = setPinMutation.isError ? toApiError(setPinMutation.error) : null;

  const apiErrorMessage = saveError
    ? saveError.status === 403
      ? PIN_MISMATCH_MESSAGE
      : saveError.message
    : null;

  const fieldErrors = saveError
    ? {
        currentPin: saveError.fieldErrors.currentPin?.[0],
        newPin: saveError.fieldErrors.newPin?.[0],
      }
    : {};

  if (isPending) {
    return (
      <SafeAreaView className="flex-1 bg-slate-50" edges={['top', 'bottom']}>
        <ScrollView contentContainerClassName="px-6 py-6">
          <ScreenHeader title="Đặt / đổi PIN" />
          <AlertBanner message="Đang tải thông tin..." tone="info" />
        </ScrollView>
      </SafeAreaView>
    );
  }

  if (loadError || !profile) {
    return (
      <SafeAreaView className="flex-1 bg-slate-50" edges={['top', 'bottom']}>
        <ScrollView contentContainerClassName="px-6 py-6">
          <ScreenHeader title="Đặt / đổi PIN" />
          <AlertBanner
            message={loadError?.message ?? 'Không tải được hồ sơ.'}
            tone="error"
          />
          <PrimaryButton
            label="Thử lại"
            onPress={() => {
              void refetch();
            }}
            variant="ghost"
          />
        </ScrollView>
      </SafeAreaView>
    );
  }

  function handleSubmit() {
    setNotice(null);
    setLocalError(null);

    if (!PIN_PATTERN.test(newPin)) {
      setLocalError(PIN_FORMAT_MESSAGE);
      return;
    }

    if (newPin !== confirmPin) {
      setLocalError('Mã PIN xác nhận không khớp.');
      return;
    }

    const payload = hasPin ? { newPin, currentPin } : { newPin };

    setPinMutation.mutate(payload, {
      onSuccess: () => {
        setNotice('Đã lưu PIN mới.');
        setCurrentPin('');
        setNewPin('');
        setConfirmPin('');
      },
    });
  }

  return (
    <SafeAreaView className="flex-1 bg-slate-50" edges={['top', 'bottom']}>
      <ScrollView contentContainerClassName="px-6 py-6" keyboardShouldPersistTaps="handled">
        <ScreenHeader title="Đặt / đổi PIN" />

        <View className="rounded-2xl border border-slate-200 bg-white p-5">
          <Text className="text-sm text-slate-500">Trạng thái hiện tại</Text>
          <Text className="mt-1 text-base font-semibold text-slate-900">
            {hasPin ? 'Đã thiết lập PIN giao dịch' : 'Chưa thiết lập PIN giao dịch'}
          </Text>
          <Text className="mt-2 text-xs leading-4 text-slate-500">
            PIN được dùng để xác nhận các thao tác quan trọng như xóa học sinh.
          </Text>
        </View>

        {apiErrorMessage ? <AlertBanner message={apiErrorMessage} tone="error" /> : null}
        {localError ? <AlertBanner message={localError} tone="error" /> : null}
        {notice ? <AlertBanner message={notice} tone="success" /> : null}

        <View className="mt-5 rounded-2xl border border-slate-200 bg-white p-5">
          <Text className="text-base font-semibold text-slate-900">
            {hasPin ? 'Thay đổi PIN' : 'Thiết lập PIN'}
          </Text>

          <View className="mt-4">
            {hasPin ? (
              <FormField
                containerClassName="mb-4"
                error={fieldErrors.currentPin}
                keyboardType="number-pad"
                label="PIN hiện tại"
                maxLength={6}
                onChangeText={(value) => setCurrentPin(value.replace(/\D/gu, ''))}
                placeholder="••••"
                secureTextEntry
                value={currentPin}
              />
            ) : null}

            <FormField
              containerClassName="mb-4"
              error={fieldErrors.newPin}
              hint={PIN_FORMAT_MESSAGE}
              keyboardType="number-pad"
              label="PIN mới"
              maxLength={6}
              onChangeText={(value) => setNewPin(value.replace(/\D/gu, ''))}
              placeholder="••••"
              secureTextEntry
              value={newPin}
            />

            <FormField
              containerClassName="mb-4"
              keyboardType="number-pad"
              label="Nhập lại PIN mới"
              maxLength={6}
              onChangeText={(value) => setConfirmPin(value.replace(/\D/gu, ''))}
              placeholder="••••"
              secureTextEntry
              value={confirmPin}
            />
          </View>

          <PrimaryButton label="Lưu PIN" loading={setPinMutation.isPending} onPress={handleSubmit} />
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
