import { Text, TouchableOpacity, View } from 'react-native';

import type { UserRole } from '@/types/auth';

const ROLES: { value: UserRole; label: string; hint: string }[] = [
  { value: 'Parent', label: 'Phụ huynh', hint: 'Theo dõi con và thanh toán học phí' },
  { value: 'Teacher', label: 'Giáo viên', hint: 'Quản lý lớp, buổi học và bài tập' },
];

type RoleSelectorProps = {
  value: UserRole;
  onChange: (value: UserRole) => void;
  error?: string;
};

export function RoleSelector({ value, onChange, error }: RoleSelectorProps) {
  return (
    <View className="mb-4">
      <Text className="mb-1.5 text-sm font-medium text-slate-700">Vai trò</Text>

      <View className="flex-row gap-3">
        {ROLES.map((role) => {
          const isSelected = role.value === value;

          return (
            <TouchableOpacity
              key={role.value}
              accessibilityRole="radio"
              accessibilityState={{ selected: isSelected }}
              activeOpacity={0.9}
              onPress={() => onChange(role.value)}
              className={`flex-1 rounded-xl border px-3 py-3 ${
                isSelected ? 'border-sky-600 bg-sky-50' : 'border-slate-300 bg-white'
              }`}
            >
              <Text
                className={`text-sm font-semibold ${isSelected ? 'text-sky-700' : 'text-slate-800'}`}
              >
                {role.label}
              </Text>
              {/* <Text className="mt-0.5 text-xs text-slate-500">{role.hint}</Text> */}
            </TouchableOpacity>
          );
        })}
      </View>

      {error ? <Text className="mt-1 text-xs text-red-600">{error}</Text> : null}
    </View>
  );
}
