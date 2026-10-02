import { PLATFORM_NOTE, SESSION_STORAGE_MODE } from '@/config/env';
import { Text, View } from 'react-native';

const STORAGE_NOTE =
  SESSION_STORAGE_MODE === 'local'
    ? 'Phiên đăng nhập đang lưu trong localStorage vì SecureStore không hỗ trợ web.'
    : null;

export function DebugNotice() {
  if (!PLATFORM_NOTE && !STORAGE_NOTE) {
    return null;
  }

  return (
    <View className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3">
      <Text className="text-xs font-semibold text-amber-800">Chế độ phát triển</Text>
      {PLATFORM_NOTE ? (
        <Text className="mt-1 text-xs text-amber-700">{PLATFORM_NOTE}</Text>
      ) : null}
      {STORAGE_NOTE ? (
        <Text className="mt-1 text-xs text-amber-700">{STORAGE_NOTE}</Text>
      ) : null}
    </View>
  );
}
