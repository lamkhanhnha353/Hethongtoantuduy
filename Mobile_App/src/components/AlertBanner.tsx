import { Text, View } from 'react-native';

type AlertBannerProps = {
  tone: 'error' | 'info' | 'success';
  message: string;
};

const TONE_STYLES = {
  error: 'border-red-200 bg-red-50',
  info: 'border-sky-200 bg-sky-50',
  success: 'border-emerald-200 bg-emerald-50',
} as const;

const TEXT_STYLES = {
  error: 'text-red-700',
  info: 'text-sky-700',
  success: 'text-emerald-700',
} as const;

export function AlertBanner({ tone, message }: AlertBannerProps) {
  return (
    <View className={`mb-4 rounded-xl border px-4 py-3 ${TONE_STYLES[tone]}`}>
      <Text className={`text-sm ${TEXT_STYLES[tone]}`}>{message}</Text>
    </View>
  );
}
