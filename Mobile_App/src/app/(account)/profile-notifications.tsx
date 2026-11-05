import { useState } from 'react';
import { Switch, Text, View } from 'react-native';
import { useRouter } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ProfileSubpageHeader } from '@/components/profile/ProfileSubpageHeader';

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

export default function ProfileNotificationsScreen() {
  const router = useRouter();
  const [notificationsEnabled, setNotificationsEnabled] = useState(true);
  const [tuitionEnabled, setTuitionEnabled] = useState(true);
  const [attendanceEnabled, setAttendanceEnabled] = useState(true);
  const [learningEnabled, setLearningEnabled] = useState(true);
  const [announcementsEnabled, setAnnouncementsEnabled] = useState(true);

  return (
    <SafeAreaView className="flex-1 bg-slate-50" edges={['top']}>
      <ProfileSubpageHeader
        title="Cài đặt thông báo"
        onBack={() => router.back()}
      />
      <View className="px-4 py-5">
        <Text className="mb-5 text-sm text-slate-500">
          Chọn các loại thông báo bạn muốn nhận.
        </Text>

        <View className="overflow-hidden rounded-2xl bg-white">
          <PreferenceToggle
            description="Bật hoặc tắt toàn bộ thông báo trong ứng dụng"
            onValueChange={setNotificationsEnabled}
            title="Cho phép thông báo"
            value={notificationsEnabled}
          />
        </View>

        <Text className="mb-2 ml-1 mt-6 text-xs font-bold uppercase tracking-wide text-slate-500">
          Loại thông báo
        </Text>
        <View className="overflow-hidden rounded-2xl bg-white">
          <PreferenceToggle
            description="Thông báo về học phí và thanh toán"
            disabled={!notificationsEnabled}
            onValueChange={setTuitionEnabled}
            title="Học phí"
            value={tuitionEnabled}
          />
          <PreferenceToggle
            description="Thông báo điểm danh và xin nghỉ học"
            disabled={!notificationsEnabled}
            onValueChange={setAttendanceEnabled}
            title="Điểm danh"
            value={attendanceEnabled}
          />
          <PreferenceToggle
            description="Cập nhật tình hình và kết quả học tập"
            disabled={!notificationsEnabled}
            onValueChange={setLearningEnabled}
            title="Học tập"
            value={learningEnabled}
          />
          <PreferenceToggle
            description="Thông báo chung từ trung tâm"
            disabled={!notificationsEnabled}
            onValueChange={setAnnouncementsEnabled}
            title="Thông báo chung"
            value={announcementsEnabled}
          />
        </View>
        <Text className="mt-3 px-1 text-xs leading-5 text-slate-400">
          Thiết lập hiện chỉ áp dụng trên giao diện; chức năng lưu và gửi thông báo sẽ được kết nối sau.
        </Text>
      </View>
    </SafeAreaView>
  );
}
