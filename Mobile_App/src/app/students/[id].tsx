import { Ionicons } from '@expo/vector-icons';
import { useState } from 'react';
import * as ImagePicker from 'expo-image-picker';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { ActivityIndicator, Alert, Pressable, ScrollView, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { AlertBanner } from '@/components/AlertBanner';
import { Avatar } from '@/components/Avatar';
import { FormField } from '@/components/FormField';
import { PinModal } from '@/components/PinModal';
import { PrimaryButton } from '@/components/PrimaryButton';
import { ScreenHeader } from '@/components/ScreenHeader';
import { prepareAvatarUpload } from '@/lib/imageUpload';
import { toApiError } from '@/services/apiError';
import {
  useDeleteStudentAvatarMutation,
  useDeleteStudentMutation,
  useStudentQuery,
  useUpdateStudentMutation,
  useUploadStudentAvatarMutation,
} from '@/hooks/useStudents';

const PIN_MISMATCH_MESSAGE = 'PIN không đúng. Vui lòng nhập lại.';

type AvatarNotice = { tone: 'error' | 'success'; message: string };

export default function ScreenStudentDetail() {
  const router = useRouter();
  const { id } = useLocalSearchParams<{ id: string }>();
  const studentId = Number(id);

  const { data: student, isPending, isError, error, refetch } = useStudentQuery(studentId);
  const updateMutation = useUpdateStudentMutation(studentId);
  const deleteMutation = useDeleteStudentMutation();
  const uploadAvatarMutation = useUploadStudentAvatarMutation(studentId);
  const deleteAvatarMutation = useDeleteStudentAvatarMutation(studentId);

  const [notice, setNotice] = useState<string | null>(null);
  const [pinVisible, setPinVisible] = useState(false);
  const [avatarNotice, setAvatarNotice] = useState<AvatarNotice | null>(null);

  const loadError = isError ? toApiError(error) : null;
  const saveError = updateMutation.isError ? toApiError(updateMutation.error) : null;
  const deleteError = deleteMutation.isError ? toApiError(deleteMutation.error) : null;

  const pinErrorMessage = deleteError
    ? deleteError.status === 403
      ? PIN_MISMATCH_MESSAGE
      : deleteError.message
    : null;

  const invalidId = !Number.isFinite(studentId);

  if (invalidId || loadError || (isPending && !student)) {
    return (
      <SafeAreaView className="flex-1 bg-slate-50" edges={['top', 'bottom']}>
        <ScrollView contentContainerClassName="px-6 py-6">
          <ScreenHeader title="Học sinh" />

          {isPending && !invalidId ? (
            <View className="items-center py-10">
              <ActivityIndicator size="large" color="#0284c7" />
            </View>
          ) : (
            <>
              <AlertBanner
                message={
                  invalidId
                    ? 'Mã học sinh không hợp lệ.'
                    : (loadError?.message ?? 'Không tải được thông tin học sinh.')
                }
                tone="error"
              />
              <PrimaryButton
                label="Thử lại"
                onPress={() => {
                  void refetch();
                }}
                variant="ghost"
              />
            </>
          )}
        </ScrollView>
      </SafeAreaView>
    );
  }

  if (!student) {
    return null;
  }

  function handleSave(fullName: string) {
    setNotice(null);
    updateMutation.mutate({ fullName }, { onSuccess: () => setNotice('Đã lưu thay đổi.') });
  }

  function handlePinSubmit(pin: string) {
    deleteMutation.mutate(
      { id: studentId, pin },
      {
        onSuccess: () => {
          setPinVisible(false);
          router.back();
        },
      },
    );
  }

  function handleAvatarPress() {
    if (uploadAvatarMutation.isPending || deleteAvatarMutation.isPending) {
      return;
    }

    const options: { text: string; style?: 'cancel' | 'destructive'; onPress?: () => void }[] = [
      { text: 'Chọn ảnh từ thư viện', onPress: () => void handlePickAvatar() },
    ];

    if (student?.avatar) {
      options.push({ text: 'Xóa ảnh', style: 'destructive', onPress: handleRemoveAvatar });
    }

    options.push({ text: 'Hủy', style: 'cancel' });

    Alert.alert('Ảnh đại diện', 'Chọn ảnh mới cho bé hoặc xóa ảnh hiện tại.', options);
  }

  async function handlePickAvatar() {
    setAvatarNotice(null);

    let result: ImagePicker.ImagePickerResult;

    try {
      result = await ImagePicker.launchImageLibraryAsync({
        mediaTypes: ['images'],
        allowsEditing: true,
        aspect: [1, 1],
        quality: 0.7,
      });
    } catch {
      setAvatarNotice({ tone: 'error', message: 'Không mở được thư viện ảnh. Vui lòng thử lại.' });
      return;
    }

    const asset = result.canceled ? undefined : result.assets[0];

    if (!asset) {
      return;
    }

    const prepared = prepareAvatarUpload(asset);

    if ('error' in prepared) {
      setAvatarNotice({ tone: 'error', message: prepared.error });
      return;
    }

    uploadAvatarMutation.mutate(prepared.image, {
      onSuccess: () => setAvatarNotice({ tone: 'success', message: 'Đã cập nhật ảnh đại diện.' }),
      onError: (uploadError) =>
        setAvatarNotice({ tone: 'error', message: toApiError(uploadError).message }),
    });
  }

  function handleRemoveAvatar() {
    setAvatarNotice(null);

    deleteAvatarMutation.mutate(undefined, {
      onSuccess: () => setAvatarNotice({ tone: 'success', message: 'Đã xóa ảnh đại diện.' }),
      onError: (deleteError) =>
        setAvatarNotice({ tone: 'error', message: toApiError(deleteError).message }),
    });
  }

  return (
    <SafeAreaView className="flex-1 bg-slate-50" edges={['top', 'bottom']}>
      <ScrollView contentContainerClassName="px-6 py-6" keyboardShouldPersistTaps="handled">
        <ScreenHeader title="Chi tiết học sinh" />

        <View className="rounded-2xl border border-slate-200 bg-white p-5">
          <View className="flex-row items-center gap-4">
            <Pressable
              accessibilityLabel="Cập nhật ảnh đại diện"
              accessibilityRole="button"
              className="relative"
              onPress={handleAvatarPress}
            >
              <Avatar name={student.fullName} size={64} uri={student.avatar} />
              <View className="absolute -bottom-1 -right-1 h-7 w-7 items-center justify-center rounded-full border-2 border-white bg-sky-600">
                <Ionicons color="#ffffff" name="camera" size={13} />
              </View>
            </Pressable>

            <View className="flex-1">
              <Text className="text-lg font-semibold text-slate-900" numberOfLines={1}>
                {student.fullName}
              </Text>
              <Text className="mt-1 text-xs text-slate-500">Mã học sinh: #{student.id}</Text>
              <Text className="mt-1 text-xs text-slate-400">Nhấn vào ảnh để thay đổi</Text>
            </View>
          </View>

          {uploadAvatarMutation.isPending ? (
            <View className="mt-3 flex-row items-center gap-2">
              <ActivityIndicator color="#0284c7" size="small" />
              <Text className="text-xs text-slate-500">Đang tải ảnh lên...</Text>
            </View>
          ) : null}

          {avatarNotice ? (
            <View className="mt-3 -mb-1">
              <AlertBanner message={avatarNotice.message} tone={avatarNotice.tone} />
            </View>
          ) : null}
        </View>

        <View className="mt-4 rounded-2xl border border-slate-200 bg-white p-5">
          <Text className="text-base font-semibold text-slate-900">Thống kê</Text>
          <View className="mt-3 gap-2 border-t border-slate-100 pt-4">
            <Row label="Tổng điểm" value={String(student.totalPoints)} />
            <Row label="Sao" value={String(student.stars)} />
            <Row label="Chuỗi ngày học" value={String(student.streakDays)} />
          </View>
        </View>

        {notice ? <AlertBanner message={notice} tone="success" /> : null}

        <View className="mt-5 rounded-2xl border border-slate-200 bg-white p-5">
          <Text className="text-base font-semibold text-slate-900">Chỉnh sửa</Text>

          <StudentEditForm
            key={student.fullName}
            error={saveError?.fieldErrors.fullName?.[0]}
            initialFullName={student.fullName}
            loading={updateMutation.isPending}
            onSave={handleSave}
          />
        </View>

        <View className="mt-5">
          <PrimaryButton label="Xóa học sinh" onPress={() => setPinVisible(true)} variant="ghost" />
          <Text className="mt-2 text-center text-xs leading-4 text-slate-500">
            Thao tác này yêu cầu PIN giao dịch và không thể hoàn tác.
          </Text>
        </View>
      </ScrollView>

      <PinModal
        confirmLabel="Xóa"
        description="Nhập PIN giao dịch để xác nhận xóa học sinh. Dữ liệu không thể khôi phục."
        errorMessage={pinErrorMessage}
        loading={deleteMutation.isPending}
        onCancel={() => setPinVisible(false)}
        onSubmit={handlePinSubmit}
        title="Xác nhận xóa"
        visible={pinVisible}
      />
    </SafeAreaView>
  );
}

type StudentEditFormProps = {
  initialFullName: string;
  loading: boolean;
  error?: string;
  onSave: (fullName: string) => void;
};

/**
 * `key` theo tên trên máy chủ để tự khởi tạo lại state mỗi khi tên được cập
 * nhật thành công, tránh set state trong useEffect.
 */
function StudentEditForm({ initialFullName, loading, error, onSave }: StudentEditFormProps) {
  const [fullName, setFullName] = useState(initialFullName);

  return (
    <View className="mt-4">
      <FormField
        containerClassName="mb-4"
        error={error}
        label="Họ và tên"
        onChangeText={setFullName}
        placeholder="Nguyễn Văn A"
        returnKeyType="done"
        textContentType="name"
        value={fullName}
      />

      <PrimaryButton label="Lưu thay đổi" loading={loading} onPress={() => onSave(fullName.trim())} />
    </View>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <View className="flex-row items-center justify-between">
      <Text className="text-sm text-slate-500">{label}</Text>
      <Text className="text-sm font-medium text-slate-900">{value}</Text>
    </View>
  );
}
