import { router, useLocalSearchParams } from 'expo-router';
import type React from 'react';
import { ActivityIndicator, Text, TouchableOpacity, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Feather, MaterialCommunityIcons } from '@expo/vector-icons';

import { AlertBanner } from '@/components/AlertBanner';
import { Avatar } from '@/components/Avatar';
import { PrimaryButton } from '@/components/PrimaryButton';
import { useStudentQuery } from '@/hooks/useStudents';
import { toApiError } from '@/services/apiError';

type StatIconName = React.ComponentProps<typeof MaterialCommunityIcons>['name'];

const STATS: { key: 'totalPoints' | 'stars' | 'streakDays'; label: string; icon: StatIconName }[] = [
  { key: 'totalPoints', label: 'Điểm', icon: 'star' },
  { key: 'stars', label: 'Ngôi sao', icon: 'trophy' },
  { key: 'streakDays', label: 'Chuỗi ngày', icon: 'fire' },
];

export default function StudentSpaceScreen() {
  const { id } = useLocalSearchParams<{ id?: string }>();
  const studentId = Number(id);
  const { data: student, isPending, isError, error, refetch } = useStudentQuery(studentId);

  const loadError = isError ? toApiError(error) : null;

  return (
    <SafeAreaView className="flex-1 bg-slate-50" edges={['bottom']}>
      <View className="bg-indigo-600 pt-14 pb-7 px-5 relative overflow-hidden">
        <View className="absolute -top-12 -right-12 w-40 h-40 rounded-full bg-cyan-400/20 opacity-50" />
        <View className="absolute bottom-0 left-4 w-24 h-24 rounded-full bg-indigo-300/20 opacity-50" />

        <View className="flex-row items-center gap-3 relative z-10">
          <TouchableOpacity
            activeOpacity={0.5}
            onPress={() => router.back()}
            className="w-10 h-10 rounded-full bg-white/20 flex items-center justify-center border border-white/30"
          >
            <Feather name="chevron-left" size={24} color="white" />
          </TouchableOpacity>
          <View>
            <Text className="text-sm text-indigo-100">Không gian học tập</Text>
            <Text className="text-lg font-bold text-white leading-tight">Học sinh</Text>
          </View>
        </View>
      </View>

      <View className="flex-1 px-5 pt-6">
        {isPending ? (
          <View className="items-center pt-16">
            <ActivityIndicator size="large" color="#4f46e5" />
            <Text className="mt-3 text-sm text-slate-500">Đang tải thông tin học sinh...</Text>
          </View>
        ) : null}

        {!isPending && loadError ? (
          <View className="gap-3 pt-4">
            <AlertBanner tone="error" message={loadError.message} />
            <PrimaryButton
              label="Thử lại"
              onPress={() => {
                void refetch();
              }}
              variant="ghost"
            />
          </View>
        ) : null}

        {!isPending && !loadError && student ? (
          <View>
            <View className="items-center bg-white rounded-2xl border border-slate-200 p-6 shadow-sm">
              <Avatar name={student.fullName} uri={student.avatar} size={88} />
              <Text className="mt-3 text-lg font-bold text-slate-900" numberOfLines={1}>
                {student.fullName}
              </Text>
              <Text className="mt-0.5 text-sm text-slate-500">Học viên Toán Tư Duy</Text>
            </View>

            <View className="mt-4 flex-row gap-3">
              {STATS.map((stat) => (
                <View
                  key={stat.key}
                  className="flex-1 items-center bg-white rounded-2xl border border-slate-200 py-4 shadow-sm"
                >
                  <View className="w-9 h-9 rounded-full bg-indigo-50 items-center justify-center mb-2">
                    <MaterialCommunityIcons name={stat.icon} size={16} color="#4f46e5" />
                  </View>
                  <Text className="text-lg font-bold text-slate-900">{student[stat.key]}</Text>
                  <Text className="text-xs text-slate-500 mt-0.5">{stat.label}</Text>
                </View>
              ))}
            </View>

            <Text className="mt-5 text-xs leading-4 text-slate-400 text-center">
              Tiến độ chi tiết, bảng xếp hạng và bài tập sẽ được cập nhật trong bản tới.
            </Text>
          </View>
        ) : null}
      </View>
    </SafeAreaView>
  );
}
