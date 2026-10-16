import { useState } from 'react';
import { Modal, Text, View } from 'react-native';

import { FormField } from '@/components/FormField';
import { PrimaryButton } from '@/components/PrimaryButton';

type PinModalProps = {
  visible: boolean;
  title?: string;
  description?: string;
  confirmLabel?: string;
  loading?: boolean;
  errorMessage?: string | null;
  onSubmit: (pin: string) => void;
  onCancel: () => void;
};

/**
 * Modal chỉ mount nội dung khi `visible` là true, nên ô PIN luôn được khởi tạo
 * rỗng mỗi lần mở lại (không cần effect reset state).
 */
export function PinModal({
  visible,
  title = 'Xác thực PIN',
  description,
  confirmLabel = 'Xác nhận',
  loading = false,
  errorMessage,
  onSubmit,
  onCancel,
}: PinModalProps) {
  return (
    <Modal animationType="fade" onRequestClose={onCancel} transparent visible={visible}>
      <View className="flex-1 items-center justify-center bg-slate-900/50 px-6">
        {visible ? (
          <PinForm
            confirmLabel={confirmLabel}
            description={description}
            errorMessage={errorMessage}
            loading={loading}
            onCancel={onCancel}
            onSubmit={onSubmit}
            title={title}
          />
        ) : null}
      </View>
    </Modal>
  );
}

type PinFormProps = Omit<PinModalProps, 'visible'>;

function PinForm({
  title = 'Xác thực PIN',
  description,
  confirmLabel = 'Xác nhận',
  loading = false,
  errorMessage,
  onSubmit,
  onCancel,
}: PinFormProps) {
  const [pin, setPin] = useState('');

  const isInvalid = pin.length < 4 || pin.length > 6;

  function handleSubmit() {
    if (!isInvalid) {
      onSubmit(pin);
    }
  }

  return (
    <View className="w-full rounded-2xl bg-white p-6">
      <Text className="text-lg font-bold text-slate-900">{title}</Text>
      {description ? (
        <Text className="mt-1 text-sm leading-5 text-slate-600">{description}</Text>
      ) : null}

      <View className="mt-4">
        <FormField
          autoFocus
          error={errorMessage ?? undefined}
          keyboardType="number-pad"
          label="PIN"
          maxLength={6}
          onChangeText={(value) => setPin(value.replace(/\D/gu, ''))}
          onSubmitEditing={handleSubmit}
          placeholder="••••"
          secureTextEntry
          value={pin}
        />
      </View>

      <View className="gap-3">
        <PrimaryButton
          disabled={isInvalid}
          label={confirmLabel}
          loading={loading}
          onPress={handleSubmit}
        />
        <PrimaryButton label="Hủy" onPress={onCancel} variant="ghost" />
      </View>
    </View>
  );
}
