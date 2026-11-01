import { ActivityIndicator, ScrollView, Text, TouchableOpacity, View } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { Redirect, router, useLocalSearchParams } from 'expo-router';

import { useSession } from '@/hooks/useSession';
import { useStudentRoadmaps } from '@/hooks/useStudentRoadmaps';
import type { StudentRoadmapLessonDto } from '@/types/roadmap';

function formatLessonDate(value: string | null): string | null {
  if (!value) {
    return null;
  }

  return new Intl.DateTimeFormat('vi-VN', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  }).format(new Date(value));
}

function getStatusLabel(status: string): string {
  switch (status.toUpperCase()) {
    case 'COMPLETED':
      return 'Đã hoàn thành';
    case 'IN_PROGRESS':
      return 'Đang học';
    case 'SCHEDULED':
      return 'Đã lên lịch';
    case 'CANCELED':
    case 'CANCELLED':
      return 'Đã hủy';
    case 'HOLIDAY':
      return 'Nghỉ học';
    default:
      return 'Chưa học';
  }
}

function RoadmapTimelineItem({
  item,
  isLast,
}: {
  item: StudentRoadmapLessonDto;
  isLast: boolean;
}) {
  const status = item.status.toUpperCase();
  const isCompleted = status === 'COMPLETED';
  const isInProgress = status === 'IN_PROGRESS' || status === 'SCHEDULED';
  const date = formatLessonDate(item.lessonDate);

  return (
    <View className="mb-2 flex-row">
      <View className="mr-4 items-center">
        <View
          className={`z-10 h-8 w-8 items-center justify-center rounded-full border-4 border-slate-50 ${
            isCompleted ? 'bg-emerald-500' : isInProgress ? 'bg-orange-500' : 'bg-slate-200'
          }`}
        >
          {isCompleted ? (
            <Ionicons name="checkmark" size={16} color="white" />
          ) : isInProgress ? (
            <View className="h-2.5 w-2.5 rounded-full bg-white" />
          ) : (
            <Ionicons name="lock-closed" size={12} color="#94a3b8" />
          )}
        </View>
        {!isLast ? (
          <View
            className={`absolute bottom-0 top-8 w-0.5 ${
              isCompleted ? 'bg-emerald-500' : 'bg-slate-200'
            }`}
          />
        ) : null}
      </View>

      <View className={`flex-1 pb-7 ${status === 'LOCKED' ? 'opacity-60' : ''}`}>
        <View className="mb-3 rounded-2xl border border-slate-100 bg-white p-4 shadow-sm">
          <View className="mb-2 flex-row items-start justify-between">
            <Text className={`mr-3 flex-1 font-black ${isInProgress ? 'text-orange-600' : 'text-slate-800'}`}>
              Buổi {item.sessionNumber}: {item.title}
            </Text>
            <Text
              className={`rounded-full px-2 py-1 text-[10px] font-bold ${
                isCompleted
                  ? 'bg-emerald-50 text-emerald-700'
                  : isInProgress
                    ? 'bg-orange-50 text-orange-700'
                    : 'bg-slate-100 text-slate-500'
              }`}
            >
              {getStatusLabel(item.status)}
            </Text>
          </View>
          {date ? <Text className="text-xs text-slate-500">{date}</Text> : null}
        </View>
      </View>
    </View>
  );
}

export default function RoadmapDetailScreen() {
  const { session, isHydrated } = useSession();
  const params = useLocalSearchParams<{ studentId?: string; classId?: string }>();
  const { roadmaps, studentsQuery, roadmapsQuery } = useStudentRoadmaps(
    session?.role === 'Parent',
  );
  const studentId = Number(Array.isArray(params.studentId) ? params.studentId[0] : params.studentId);
  const classId = Number(Array.isArray(params.classId) ? params.classId[0] : params.classId);
  const roadmap = roadmaps.find(
    (item) => item.studentId === studentId && item.classId === classId,
  );
  const error = studentsQuery.error ?? roadmapsQuery.error;
  const isPending = studentsQuery.isPending || roadmapsQuery.isPending;

  if (!isHydrated) {
    return (
      <View className="flex-1 items-center justify-center bg-slate-50">
        <ActivityIndicator size="large" color="#5a64f1" />
      </View>
    );
  }

  if (!session) {
    return <Redirect href="/login" />;
  }

  if (session.role !== 'Parent') {
    return <Redirect href="/(tabs)/home" />;
  }

  const retry = () => {
    if (studentsQuery.isError) {
      void studentsQuery.refetch();
    } else {
      void roadmapsQuery.refetch();
    }
  };

  return (
    <View className="flex-1 bg-slate-50">
      <View className="z-10 rounded-b-[32px] bg-[#5a64f1] px-5 pb-6 pt-14 shadow-sm">
        <View className="flex-row items-center">
          <TouchableOpacity
            className="mr-4 h-10 w-10 items-center justify-center rounded-full bg-white/20"
            onPress={() => router.back()}
          >
            <Ionicons name="chevron-back" size={24} color="white" />
          </TouchableOpacity>
          <View className="flex-1">
            <Text className="mb-1 text-xs font-bold uppercase tracking-wider text-indigo-100">
              Lộ trình của {roadmap?.studentName ?? 'học sinh'}
            </Text>
            <Text className="text-xl font-black text-white" numberOfLines={1}>
              {roadmap?.courseName ?? 'Chi tiết lộ trình'}
            </Text>
          </View>
        </View>
        {roadmap ? (
          <View className="mt-4 rounded-2xl bg-white/15 p-3">
            <Text className="font-bold text-white">{roadmap.courseLevelName}</Text>
            <Text className="mt-1 text-sm text-indigo-100">
              {roadmap.className} · Đã hoàn thành {roadmap.completedSessions}/{roadmap.totalSessions} buổi
            </Text>
          </View>
        ) : null}
      </View>

      <ScrollView
        className="flex-1 px-5 pt-8"
        contentContainerStyle={{ paddingBottom: 32 }}
        showsVerticalScrollIndicator={false}
      >
        {isPending ? (
          <View className="items-center py-12">
            <ActivityIndicator size="large" color="#5a64f1" />
            <Text className="mt-3 text-slate-500">Đang tải chi tiết lộ trình...</Text>
          </View>
        ) : error ? (
          <View className="items-center rounded-2xl bg-white px-5 py-10">
            <Text className="text-center text-slate-600">
              {error instanceof Error ? error.message : 'Không thể tải chi tiết lộ trình.'}
            </Text>
            <TouchableOpacity onPress={retry} className="mt-4 rounded-xl bg-indigo-600 px-5 py-3">
              <Text className="font-bold text-white">Thử tải lại</Text>
            </TouchableOpacity>
          </View>
        ) : !roadmap || roadmap.lessons.length === 0 ? (
          <View className="items-center rounded-2xl bg-white px-5 py-12">
            <Ionicons name="book-outline" size={40} color="#94a3b8" />
            <Text className="mt-3 text-center font-medium text-slate-500">
              Chưa có nội dung lộ trình cho lớp học này.
            </Text>
          </View>
        ) : (
          roadmap.lessons.map((item, index) => (
            <RoadmapTimelineItem
              key={`${roadmap.classId}-${item.sessionNumber}`}
              item={item}
              isLast={index === roadmap.lessons.length - 1}
            />
          ))
        )}
      </ScrollView>
    </View>
  );
}
