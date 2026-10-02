import { ActivityIndicator, Text, TouchableOpacity } from 'react-native';

type PrimaryButtonProps = {
  label: string;
  onPress: () => void;
  loading?: boolean;
  disabled?: boolean;
  variant?: 'primary' | 'ghost';
};

export function PrimaryButton({
  label,
  onPress,
  loading = false,
  disabled = false,
  variant = 'primary',
}: PrimaryButtonProps) {
  const isPrimary = variant === 'primary';
  const isInactive = disabled || loading;

  return (
    <TouchableOpacity
      accessibilityRole="button"
      accessibilityState={{ disabled: isInactive, busy: loading }}
      activeOpacity={0.85}
      className={`flex-row items-center justify-center rounded-xl px-4 py-3.5 ${
        isPrimary ? 'bg-sky-600' : 'border border-slate-300 bg-white'
      } ${isInactive ? 'opacity-50' : ''}`}
      disabled={isInactive}
      onPress={onPress}
    >
      {loading ? (
        <ActivityIndicator color={isPrimary ? '#ffffff' : '#0f172a'} />
      ) : (
        <Text
          className={`text-base font-semibold ${isPrimary ? 'text-white' : 'text-slate-800'}`}
        >
          {label}
        </Text>
      )}
    </TouchableOpacity>
  );
}
