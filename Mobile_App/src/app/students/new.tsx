import { useState } from 'react';
import { useRouter } from 'expo-router';
import { ScrollView, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { AlertBanner } from '@/components/AlertBanner';
import { FormField } from '@/components/FormField';
import { PrimaryButton } from '@/components/PrimaryButton';
import { ScreenHeader } from '@/components/ScreenHeader';
import { toApiError } from '@/services/apiError';
import { useCreateStudentMutation } from '@/hooks/useStudents';

export default function ScreenNewStudent() {
  const router = useRouter();
  const [fullName, setFullName] = useState('');

  const createMutation = useCreateStudentMutation();

  const apiError = createMutation.isError ? toApiError(createMutation.error) : null;
  const fieldErrors = { fullName: apiError?.fieldErrors.fullName?.[0] };

  function handleSubmit() {
    createMutation.mutate(
      { fullName: fullName.trim() },
      { onSuccess: () => router.back() },
    );
  }

  return (
    <SafeAreaView className="flex-1 bg-slate-50" edges={['top', 'bottom']}>
      <ScrollView
        contentContainerClassName="px-6 py-6"
        keyboardShouldPersistTaps="handled"
      >
        <ScreenHeader title="Thêm học sinh" />

        {apiError ? <AlertBanner message={apiError.message} tone="error" /> : null}

        <View className="mt-2">
          <FormField
            autoFocus
            containerClassName="mb-4"
            error={fieldErrors.fullName}
            label="Họ và tên"
            onChangeText={setFullName}
            onSubmitEditing={handleSubmit}
            placeholder="Nguyễn Văn A"
            returnKeyType="done"
            textContentType="name"
            value={fullName}
          />
          <Text className="text-xs leading-4 text-slate-500">
            Tối đa 100 ký tự. Bạn có thể thêm ảnh đại diện sau khi tạo học sinh.
          </Text>
        </View>

        <View className="mt-6">
          <PrimaryButton
            label="Lưu"
            loading={createMutation.isPending}
            onPress={handleSubmit}
          />
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
