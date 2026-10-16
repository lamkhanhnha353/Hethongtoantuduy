import { Text, View } from 'react-native';

import { PrimaryButton } from '@/components/PrimaryButton';

type EmptyStateProps = {
  title: string;
  description?: string;
  actionLabel?: string;
  onAction?: () => void;
};

export function EmptyState({ title, description, actionLabel, onAction }: EmptyStateProps) {
  return (
    <View className="items-center rounded-2xl border border-dashed border-slate-300 bg-white px-6 py-10">
      <Text className="text-base font-semibold text-slate-900">{title}</Text>

      {description ? (
        <Text className="mt-2 text-center text-sm leading-5 text-slate-500">{description}</Text>
      ) : null}

      {actionLabel && onAction ? (
        <View className="mt-5 w-full">
          <PrimaryButton label={actionLabel} onPress={onAction} />
        </View>
      ) : null}
    </View>
  );
}
