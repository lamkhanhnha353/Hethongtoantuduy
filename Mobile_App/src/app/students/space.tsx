import { router, useLocalSearchParams } from 'expo-router';
import type React from 'react';
import { ActivityIndicator, ScrollView, Text, TouchableOpacity, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Feather, MaterialCommunityIcons } from '@expo/vector-icons';

import { AlertBanner } from '@/components/AlertBanner';
import { Avatar } from '@/components/Avatar';
import { PrimaryButton } from '@/components/PrimaryButton';
import { useGameStore } from '@/store/gameStore';
import { useStudentQuery } from '@/hooks/useStudents';
import { useStudentPracticeAssignmentsQuery } from '@/hooks/useStudentPracticeAssignments';
import { toApiError } from '@/services/apiError';
import { audio } from '@/audio';

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
  const assignmentsQuery = useStudentPracticeAssignmentsQuery(studentId);
  const bonus = useGameStore((state) => state.bonusByStudentId[studentId]);

  const loadError = isError ? toApiError(error) : null;
  const assignmentsError = assignmentsQuery.error ? toApiError(assignmentsQuery.error) : null;

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

      <ScrollView className="flex-1" contentContainerClassName="px-5 pb-8 pt-6">
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
                  <Text className="text-lg font-bold text-slate-900">
                    {stat.key === 'totalPoints'
                      ? student.totalPoints + (bonus?.points ?? 0)
                      : stat.key === 'stars'
                        ? student.stars + (bonus?.stars ?? 0)
                        : student.streakDays}
                  </Text>
                  <Text className="text-xs text-slate-500 mt-0.5">{stat.label}</Text>
                </View>
              ))}
            </View>

            <View className="mt-5">
              <PrimaryButton
                label="Chơi Toán vui"
                onPress={() => {
                  audio.pop();
                  router.push({ pathname: '/students/quiz', params: { id: String(student.id) } });
                }}
              />
            </View>

            <View className="mt-7">
              <Text className="mb-3 text-lg font-bold text-slate-900">Bài tập từ giáo viên</Text>

              {assignmentsQuery.isPending ? (
                <View className="items-center rounded-2xl bg-white py-6">
                  <ActivityIndicator color="#4f46e5" />
                  <Text className="mt-2 text-sm text-slate-500">Đang tải bài tập...</Text>
                </View>
              ) : null}

              {assignmentsError ? (
                <View className="gap-3">
                  <AlertBanner tone="error" message={assignmentsError.message} />
                  <PrimaryButton
                    label="Tải lại bài tập"
                    onPress={() => {
                      void assignmentsQuery.refetch();
                    }}
                    variant="ghost"
                  />
                </View>
              ) : null}

              {!assignmentsQuery.isPending && !assignmentsError
                && (assignmentsQuery.data?.length ?? 0) === 0 ? (
                  <View className="rounded-2xl border border-slate-200 bg-white p-4">
                    <Text className="text-sm text-slate-500">Hiện chưa có bài tập được giao.</Text>
                  </View>
                ) : null}

              {assignmentsQuery.data?.map((assignment) => {
                const isCompleted = assignment.studentStatus === 'SUBMITTED'
                  || assignment.studentStatus === 'GRADED';

                return (
                  <TouchableOpacity
                    accessibilityRole="button"
                    className={`mb-3 rounded-2xl border border-slate-200 bg-white p-4 ${
                      assignment.isAvailable ? 'active:opacity-70' : 'opacity-75'
                    }`}
                    disabled={!assignment.isAvailable}
                    key={assignment.assignmentId}
                    onPress={() => {
                      audio.pop();
                      router.push({
                        pathname: '/students/assignment',
                        params: {
                          assignmentId: String(assignment.assignmentId),
                          studentId: String(student.id),
                        },
                      });
                    }}
                  >
                    <View className="flex-row items-start justify-between gap-3">
                      <View className="flex-1">
                        <Text className="font-semibold text-slate-900">{assignment.title}</Text>
                        {assignment.lessonTitle ? (
                          <Text className="mt-1 text-sm text-slate-500">
                            {assignment.lessonTitle}
                          </Text>
                        ) : null}
                      </View>
                      <Text className={`text-xs font-semibold ${
                        assignment.isAvailable ? 'text-indigo-600' : 'text-slate-500'
                      }`}>
                        {assignment.isAvailable
                          ? 'Làm bài'
                          : isCompleted
                            ? 'Đã nộp'
                            : 'Chưa mở / đã hết hạn'}
                      </Text>
                    </View>
                    <Text className="mt-3 text-xs text-slate-500">
                      {assignment.totalQuestions} câu · Hạn {new Date(assignment.deadline).toLocaleString()}
                    </Text>
                    {isCompleted && assignment.score !== null ? (
                      <Text className="mt-1 text-sm font-medium text-emerald-700">
                        Điểm: {assignment.score}/{assignment.totalPoints}
                      </Text>
                    ) : null}
                  </TouchableOpacity>
                );
              })}
            </View>
          </View>
        ) : null}
      </ScrollView>
    </SafeAreaView>
  );
}
