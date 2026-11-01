import { useState } from 'react';
import { ActivityIndicator, ScrollView, Text, TouchableOpacity, View } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { Redirect, router } from 'expo-router';

import { useSession } from '@/hooks/useSession';
import { useStudentRoadmaps } from '@/hooks/useStudentRoadmaps';
import type { StudentRoadmapDto } from '@/types/roadmap';

function RoadmapCard({
  roadmap,
  showStudent,
}: {
  roadmap: StudentRoadmapDto;
  showStudent: boolean;
}) {
  const progress =
    roadmap.totalSessions > 0
      ? Math.min(100, (roadmap.completedSessions / roadmap.totalSessions) * 100)
      : 0;

  return (
    <TouchableOpacity
      activeOpacity={0.75}
      onPress={() =>
        router.push({
          pathname: '/roadmap/detail',
          params: {
            studentId: String(roadmap.studentId),
            classId: String(roadmap.classId),
          },
        })
      }
      className="mb-4 rounded-3xl border border-slate-100 bg-white p-5 shadow-sm"
    >
      {showStudent ? (
        <View className="mb-3 self-start rounded-md bg-indigo-50 px-2.5 py-1">
          <Text className="text-[11px] font-black uppercase tracking-wide text-[#5a64f1]">
            {roadmap.studentName}
          </Text>
        </View>
      ) : null}
      <Text className="mb-1 text-xl font-black text-slate-800">{roadmap.courseName}</Text>
      <Text className="mb-4 text-sm text-slate-500">
        {roadmap.courseLevelName} · {roadmap.className}
      </Text>
      <View className="mb-2 flex-row items-center justify-between">
        <Text className="text-xs font-medium text-slate-500">Tiến độ buổi học</Text>
        <Text className="text-xs font-bold text-[#5a64f1]">
          {roadmap.completedSessions}/{roadmap.totalSessions}
        </Text>
      </View>
      <View className="h-2 overflow-hidden rounded-full bg-slate-100">
        <View className="h-full rounded-full bg-[#5a64f1]" style={{ width: `${progress}%` }} />
      </View>
    </TouchableOpacity>
  );
}

export default function RoadmapListScreen() {
  const { session, isHydrated } = useSession();
  const { students, roadmaps, studentsQuery, roadmapsQuery } = useStudentRoadmaps(
    session?.role === 'Parent',
  );
  const [activeStudentId, setActiveStudentId] = useState<number | 'all'>('all');
  const visibleRoadmaps =
    activeStudentId === 'all'
      ? roadmaps
      : roadmaps.filter((roadmap) => roadmap.studentId === activeStudentId);
  const isPending = studentsQuery.isPending || roadmapsQuery.isPending;
  const error = studentsQuery.error ?? roadmapsQuery.error;

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
        <View className="mb-6 flex-row items-center">
          <TouchableOpacity
            className="mr-4 h-10 w-10 items-center justify-center rounded-full bg-white/20"
            onPress={() => router.back()}
          >
            <Ionicons name="chevron-back" size={24} color="white" />
          </TouchableOpacity>
          <Text className="flex-1 text-2xl font-black text-white">Lộ trình học</Text>
        </View>
        <ScrollView horizontal showsHorizontalScrollIndicator={false}>
          <TouchableOpacity
            onPress={() => setActiveStudentId('all')}
            activeOpacity={0.8}
            className={`mr-3 rounded-full border px-4 py-2 ${
              activeStudentId === 'all' ? 'border-white bg-white' : 'border-white/30 bg-white/20'
            }`}
          >
            <Text className={`font-black ${activeStudentId === 'all' ? 'text-[#5a64f1]' : 'text-white'}`}>
              Tất cả học sinh
            </Text>
          </TouchableOpacity>
          {students.map((student) => (
            <TouchableOpacity
              key={student.id}
              onPress={() => setActiveStudentId(student.id)}
              activeOpacity={0.8}
              className={`mr-3 rounded-full border px-4 py-2 ${
                activeStudentId === student.id
                  ? 'border-white bg-white'
                  : 'border-white/30 bg-white/20'
              }`}
            >
              <Text
                className={`font-black ${
                  activeStudentId === student.id ? 'text-[#5a64f1]' : 'text-white'
                }`}
              >
                {student.fullName}
              </Text>
            </TouchableOpacity>
          ))}
        </ScrollView>
      </View>

      <ScrollView
        className="flex-1 px-5 pt-6"
        contentContainerStyle={{ paddingBottom: 32 }}
        showsVerticalScrollIndicator={false}
      >
        {isPending ? (
          <View className="items-center py-12">
            <ActivityIndicator size="large" color="#5a64f1" />
            <Text className="mt-3 text-slate-500">Đang tải lộ trình học...</Text>
          </View>
        ) : error ? (
          <View className="items-center rounded-2xl bg-white px-5 py-10">
            <Text className="text-center text-slate-600">
              {error instanceof Error ? error.message : 'Không thể tải lộ trình học.'}
            </Text>
            <TouchableOpacity onPress={retry} className="mt-4 rounded-xl bg-indigo-600 px-5 py-3">
              <Text className="font-bold text-white">Thử tải lại</Text>
            </TouchableOpacity>
          </View>
        ) : visibleRoadmaps.length === 0 ? (
          <View className="items-center rounded-2xl bg-white px-5 py-12">
            <Ionicons name="map-outline" size={40} color="#94a3b8" />
            <Text className="mt-3 text-center font-medium text-slate-500">
              Học sinh chưa tham gia lớp học nào.
            </Text>
          </View>
        ) : (
          visibleRoadmaps.map((roadmap) => (
            <RoadmapCard
              key={`${roadmap.studentId}-${roadmap.classId}`}
              roadmap={roadmap}
              showStudent={activeStudentId === 'all'}
            />
          ))
        )}
      </ScrollView>
    </View>
  );
}
