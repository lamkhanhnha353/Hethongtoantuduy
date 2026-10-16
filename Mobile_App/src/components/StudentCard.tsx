import { Ionicons } from '@expo/vector-icons';
import { Text, TouchableOpacity, View } from 'react-native';

import { Avatar } from '@/components/Avatar';
import type { StudentResponseDto } from '@/types/student';

type StudentCardProps = {
  student: StudentResponseDto;
  onPress: () => void;
};

export function StudentCard({ student, onPress }: StudentCardProps) {
  return (
    <TouchableOpacity
      accessibilityRole="button"
      activeOpacity={0.85}
      className="flex-row items-center gap-4 rounded-2xl border border-slate-200 bg-white p-4"
      onPress={onPress}
    >
      <Avatar name={student.fullName} size={48} uri={student.avatar} />

      <View className="flex-1">
        <Text className="text-base font-semibold text-slate-900" numberOfLines={1}>
          {student.fullName}
        </Text>

        <View className="mt-1 flex-row flex-wrap gap-x-4 gap-y-1">
          <Stat label="Điểm" value={student.totalPoints} />
          <Stat label="Sao" value={student.stars} />
          <Stat label="Chuỗi ngày" value={student.streakDays} />
        </View>
      </View>

      <Ionicons color="#94a3b8" name="chevron-forward" size={20} />
    </TouchableOpacity>
  );
}

function Stat({ label, value }: { label: string; value: number }) {
  return (
    <Text className="text-xs text-slate-500">
      {label}: <Text className="font-semibold text-slate-700">{value}</Text>
    </Text>
  );
}
