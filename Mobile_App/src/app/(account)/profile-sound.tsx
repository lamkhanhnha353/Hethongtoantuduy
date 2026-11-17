import { Switch, Text, View } from 'react-native';
import { useRouter } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ProfileSubpageHeader } from '@/components/profile/ProfileSubpageHeader';
import { useSoundEnabled } from '@/hooks/useAudio';

export default function ProfileSoundScreen() {
  const router = useRouter();
  const { enabled, setEnabled } = useSoundEnabled();

  return (
    <SafeAreaView className="flex-1 bg-slate-50" edges={['top']}>
      <ProfileSubpageHeader title="Âm thanh" onBack={() => router.back()} />
      <View className="px-4 py-5">
        <Text className="mb-5 text-sm text-slate-500">
          Tùy chỉnh hiệu ứng âm thanh trong quá trình học.
        </Text>
        <View className="flex-row items-center justify-between rounded-2xl bg-white p-4">
          <View className="mr-3 flex-1">
            <Text className="text-sm font-semibold text-slate-900">Hiệu ứng âm thanh</Text>
            <Text className="mt-1 text-xs text-slate-500">Phát khi trả lời đúng hoặc sai</Text>
          </View>
          <Switch
            accessibilityLabel="Bật hiệu ứng âm thanh"
            ios_backgroundColor="#cbd5e1"
            onValueChange={(value) => {
              void setEnabled(value);
            }}
            thumbColor={enabled ? '#0284c7' : '#f8fafc'}
            trackColor={{ false: '#cbd5e1', true: '#7dd3fc' }}
            value={enabled}
          />
        </View>
      </View>
    </SafeAreaView>
  );
}
