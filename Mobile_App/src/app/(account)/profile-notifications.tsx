import { useMemo } from 'react';
import { ActivityIndicator, ScrollView, Switch, Text, TouchableOpacity, View } from 'react-native';
import { useRouter } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';

import { AlertBanner } from '@/components/AlertBanner';
import { ProfileSubpageHeader } from '@/components/profile/ProfileSubpageHeader';
import {
  useNotificationSettingsQuery,
  useUpdateNotificationSettingsMutation,
} from '@/hooks/useNotifications';
import { useSession } from '@/hooks/useSession';
import { toApiError } from '@/services/apiError';

type PreferenceToggleProps = {
  title: string;
  description: string;
  value: boolean;
  disabled?: boolean;
  onValueChange: (value: boolean) => void;
};

function PreferenceToggle({
  title,
  description,
  value,
  disabled = false,
  onValueChange,
}: PreferenceToggleProps) {
  return (
    <View className="flex-row items-center justify-between border-b border-slate-100 px-4 py-4 last:border-b-0">
      <View className="mr-3 flex-1">
        <Text className={`text-sm font-semibold ${disabled ? 'text-slate-400' : 'text-slate-900'}`}>
          {title}
        </Text>
        <Text className="mt-1 text-xs text-slate-500">{description}</Text>
      </View>
      <Switch
        accessibilityLabel={title}
        disabled={disabled}
        ios_backgroundColor="#cbd5e1"
        onValueChange={onValueChange}
        thumbColor={value && !disabled ? '#0284c7' : '#f8fafc'}
        trackColor={{ false: '#cbd5e1', true: '#7dd3fc' }}
        value={value && !disabled}
      />
    </View>
  );
}

const SETTING_KEYS = {
  master: 'IN_APP_NOTIFICATION',
  tuition: 'TUITION',
  attendance: 'ATTENDANCE',
  academic: 'ACADEMIC',
  announcement: 'ANNOUNCEMENT',
  account: 'ACCOUNT',
  chat: 'CHAT',
} as const;

export default function ProfileNotificationsScreen() {
  const router = useRouter();
  const { session } = useSession();
  const isLoggedIn = Boolean(session?.accessToken);

  const settingsQuery = useNotificationSettingsQuery(isLoggedIn);
  const updateMutation = useUpdateNotificationSettingsMutation();

  const settingsMap = useMemo(() => {
    const map = new Map<string, boolean>();
    (settingsQuery.data ?? []).forEach((item) => {
      map.set(item.type.toUpperCase(), item.enabled);
    });
    return map;
  }, [settingsQuery.data]);

  const isEnabled = (key: string) => settingsMap.get(key) ?? true;

  const notificationsEnabled = isEnabled(SETTING_KEYS.master);
  const tuitionEnabled = isEnabled(SETTING_KEYS.tuition);
  const attendanceEnabled = isEnabled(SETTING_KEYS.attendance);
  const academicEnabled = isEnabled(SETTING_KEYS.academic);
  const announcementEnabled = isEnabled(SETTING_KEYS.announcement);
  const accountEnabled = isEnabled(SETTING_KEYS.account);
  const chatEnabled = isEnabled(SETTING_KEYS.chat);

  const handleToggle = (type: string, enabled: boolean) => {
    updateMutation.mutate([{ type, enabled }]);
  };

  const loadError = settingsQuery.isError ? toApiError(settingsQuery.error) : null;
  const saveError = updateMutation.isError ? toApiError(updateMutation.error) : null;

  return (
    <SafeAreaView className="flex-1 bg-slate-50" edges={['top']}>
      <ProfileSubpageHeader
        title="Cài đặt thông báo"
        onBack={() => router.back()}
      />

      {settingsQuery.isPending ? (
        <View className="flex-1 items-center justify-center">
          <ActivityIndicator color="#0284c7" size="large" />
          <Text className="mt-3 text-sm text-slate-500">Đang tải cài đặt thông báo...</Text>
        </View>
      ) : loadError ? (
        <View className="px-4 py-6">
          <AlertBanner message={loadError.message} tone="error" />
          <TouchableOpacity
            className="mt-4 self-start rounded-xl bg-sky-600 px-4 py-2.5"
            onPress={() => {
              void settingsQuery.refetch();
            }}
          >
            <Text className="text-xs font-bold text-white">Thử lại</Text>
          </TouchableOpacity>
        </View>
      ) : (
        <ScrollView className="flex-1 px-4 py-5" showsVerticalScrollIndicator={false}>
          <Text className="mb-4 text-sm text-slate-500">
            Tùy chỉnh các loại thông báo bạn muốn nhận trong ứng dụng. Thay đổi sẽ được lưu tự động.
          </Text>

          {saveError ? (
            <View className="mb-4">
              <AlertBanner message={saveError.message} tone="error" />
            </View>
          ) : null}

          <View className="overflow-hidden rounded-2xl bg-white">
            <PreferenceToggle
              description="Bật hoặc tắt toàn bộ thông báo trong ứng dụng"
              onValueChange={(value) => handleToggle(SETTING_KEYS.master, value)}
              title="Cho phép thông báo"
              value={notificationsEnabled}
            />
          </View>

          <Text className="mb-2 ml-1 mt-6 text-xs font-bold uppercase tracking-wide text-slate-500">
            Danh mục thông báo
          </Text>
          <View className="overflow-hidden rounded-2xl bg-white">
            <PreferenceToggle
              description="Hóa đơn mới, nhắc đóng học phí, hết hạn và xếp lớp"
              disabled={!notificationsEnabled}
              onValueChange={(value) => handleToggle(SETTING_KEYS.tuition, value)}
              title="Học phí & Ghi danh"
              value={tuitionEnabled}
            />
            <PreferenceToggle
              description="Điểm danh vắng/trễ, nhắc lịch học và duyệt đơn xin nghỉ"
              disabled={!notificationsEnabled}
              onValueChange={(value) => handleToggle(SETTING_KEYS.attendance, value)}
              title="Điểm danh & Xin nghỉ"
              value={attendanceEnabled}
            />
            <PreferenceToggle
              description="Bài tập mới từ giáo viên, nộp bài và kết quả chấm điểm"
              disabled={!notificationsEnabled}
              onValueChange={(value) => handleToggle(SETTING_KEYS.academic, value)}
              title="Bài tập & Học tập"
              value={academicEnabled}
            />
            <PreferenceToggle
              description="Thông báo chung và cập nhật hệ thống từ trung tâm"
              disabled={!notificationsEnabled}
              onValueChange={(value) => handleToggle(SETTING_KEYS.announcement, value)}
              title="Thông báo trung tâm"
              value={announcementEnabled}
            />
            <PreferenceToggle
              description="Đăng ký tài khoản, đổi mật khẩu và tạo hồ sơ học viên"
              disabled={!notificationsEnabled}
              onValueChange={(value) => handleToggle(SETTING_KEYS.account, value)}
              title="Tài khoản & Bảo mật"
              value={accountEnabled}
            />
            <PreferenceToggle
              description="Hiện banner thông báo nổi khi giáo viên gửi tin nhắn mới"
              disabled={!notificationsEnabled}
              onValueChange={(value) => handleToggle(SETTING_KEYS.chat, value)}
              title="Tin nhắn từ giáo viên"
              value={chatEnabled}
            />
          </View>

          <Text className="mt-3 px-1 pb-8 text-xs leading-5 text-slate-400">
            Các thông báo đặc biệt hoặc buổi học quan trọng vẫn được ưu tiên hiển thị khi bạn bật Cho phép thông báo.
          </Text>
        </ScrollView>
      )}
    </SafeAreaView>
  );
}
