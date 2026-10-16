import { Text, TouchableOpacity, View } from 'react-native';

import { ROLE_FEATURES, ROLE_LABELS, ROLE_SUMMARY, type HomeFeature } from '@/content/homeContent';
import type { StoredSession } from '@/types/auth';

type HomeOverviewProps = {
  session: StoredSession;
};

export function HomeOverview({ session }: HomeOverviewProps) {
  return (
    <View className="rounded-2xl border border-slate-200 bg-white p-5">
      <Text className="text-xs font-semibold uppercase tracking-wide text-slate-500">
        Vai trò của bạn
      </Text>

      <Text className="mt-2 text-lg font-semibold text-slate-900">
        {ROLE_LABELS[session.role]}
      </Text>

      <Text className="mt-1 text-sm leading-5 text-slate-600">{ROLE_SUMMARY[session.role]}</Text>

      <View className="mt-4 gap-2 border-t border-slate-100 pt-4">
        <Row label="Số điện thoại" value={session.phone} />
        <Row label="Mã hồ sơ" value={`#${session.profileId}`} />
      </View>
    </View>
  );
}

type RowProps = {
  label: string;
  value: string;
};

function Row({ label, value }: RowProps) {
  return (
    <View className="flex-row items-center justify-between">
      <Text className="text-sm text-slate-500">{label}</Text>
      <Text className="text-sm font-medium text-slate-900">{value}</Text>
    </View>
  );
}

type FeatureGridProps = {
  role: StoredSession['role'];
  onSelect: (feature: HomeFeature) => void;
};

export function FeatureGrid({ role, onSelect }: FeatureGridProps) {
  return (
    <View className="mt-6">
      <Text className="text-base font-semibold text-slate-900">Chức năng</Text>
      <Text className="mt-1 text-sm text-slate-500">
        Chọn một mục để bắt đầu sử dụng.
      </Text>

      <View className="mt-3 flex-row flex-wrap gap-3">
        {ROLE_FEATURES[role].map((feature) => (
          <TouchableOpacity
            key={feature.id}
            accessibilityRole="button"
            activeOpacity={0.85}
            className="min-w-[45%] flex-1 rounded-xl border border-slate-200 bg-white p-4"
            onPress={() => onSelect(feature)}
          >
            <Text className="text-sm font-semibold text-slate-900">{feature.title}</Text>
            <Text className="mt-1 text-xs leading-4 text-slate-500">{feature.description}</Text>
          </TouchableOpacity>
        ))}
      </View>
    </View>
  );
}