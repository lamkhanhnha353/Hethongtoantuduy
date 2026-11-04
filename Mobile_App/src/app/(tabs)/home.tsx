import { useState } from 'react';
import { Redirect, useRouter } from 'expo-router';
import { ActivityIndicator, Alert, ScrollView, Text, View, TouchableOpacity } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Feather } from '@expo/vector-icons';

import { WelcomeHeader } from '@/components/parent/WelcomeHeader';
import { useParentLessons } from '@/hooks/useParentLessons';
import { useSession } from '@/hooks/useSession';

export default function ScreenHome() {
  const router = useRouter();
  const { session, isHydrated } = useSession();
  const { lessons, studentsQuery, lessonsQuery } = useParentLessons(session?.role === 'Parent');
  const [now] = useState(() => Date.now());

  if (!isHydrated) {
    return (
      <SafeAreaView
        className="flex-1 items-center justify-center bg-slate-50"
        edges={['bottom']}
      >
        <ActivityIndicator size="large" color="#0284c7" />
        <Text className="mt-3 text-sm text-slate-500">Đang tải thông tin tài khoản...</Text>
      </SafeAreaView>
    );
  }

  if (!session) {
    return <Redirect href="/login" />;
  }

  const today = new Date();
  const todayKey = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`;
  const upcomingLesson = lessons
    .filter((lesson) => {
      if (lesson.status.toUpperCase() !== 'SCHEDULED') {
        return false;
      }

      return lesson.startTime
        ? new Date(lesson.startTime).getTime() >= now
        : lesson.lessonDate.slice(0, 10) >= todayKey;
    })
    .sort((left, right) => {
      const leftTime = new Date(left.startTime ?? left.lessonDate).getTime();
      const rightTime = new Date(right.startTime ?? right.lessonDate).getTime();
      return leftTime - rightTime;
    })[0];
  const showComingSoonAlert = () => {
    Alert.alert('Thông báo', 'Tính năng này đang được phát triển và sẽ sớm ra mắt.');
  };

  return (
    <View className="flex-1 bg-[#f1f5f9]">
      <WelcomeHeader />

      <ScrollView 
        className="flex-1 px-4 pt-4" 
        contentContainerStyle={{ paddingBottom: 30 }} 
        showsVerticalScrollIndicator={false}
      >
        {/* BUỔI HỌC SẮP TỚI */}
        <View className="bg-orange-50 rounded-2xl p-4 border border-amber-200 mb-4 relative overflow-hidden">
          <View className="absolute right-0 top-0 translate-x-4 -translate-y-4 w-28 h-28 bg-amber-400/10 rounded-full" />
          
          <View className="flex-row items-center justify-between mb-3 z-10">
            <View className="flex-row items-center gap-2">
              <View className="px-2 py-0.5 bg-amber-500 rounded-md">
                <Text className="text-white text-[10px] font-bold uppercase tracking-wider">
                  {upcomingLesson
                    ? upcomingLesson.lessonDate.slice(0, 10) === todayKey
                      ? 'Hôm nay'
                      : new Intl.DateTimeFormat('vi-VN', { day: 'numeric', month: 'numeric' }).format(
                          new Date(upcomingLesson.lessonDate),
                        )
                    : 'Lịch học'}
                </Text>
              </View>
              <Text className="text-xs font-bold text-amber-900 uppercase tracking-wide">Buổi học sắp tới</Text>
            </View>
            <Text className="text-xs font-semibold text-amber-700">
              {upcomingLesson
                ? `${upcomingLesson.startLabel} - ${upcomingLesson.endLabel}`
                : session?.role !== 'Parent'
                  ? ''
                  : studentsQuery.isError || lessonsQuery.isError
                  ? 'Không tải được lịch'
                  : ''}
            </Text>
          </View>

          <View className="flex-row items-start gap-3 bg-white/90 rounded-xl p-3 border border-amber-100 z-10">
            <View className="w-10 h-10 rounded-xl bg-indigo-100 items-center justify-center">
              <Text className="text-indigo-700 font-bold text-sm">
                {upcomingLesson?.studentName.slice(0, 2).toUpperCase() ?? '...'}
              </Text>
            </View>
            <View className="flex-1">
              <Text className="font-bold text-slate-900 text-sm" numberOfLines={1}>
                {upcomingLesson
                  ? `${upcomingLesson.className} · ${upcomingLesson.studentName}`
                  : session?.role !== 'Parent'
                    ? 'Lịch học dành cho tài khoản phụ huynh.'
                    : studentsQuery.isPending || lessonsQuery.isPending
                    ? 'Đang tải lịch học...'
                    : studentsQuery.isError || lessonsQuery.isError
                      ? 'Chưa thể tải lịch học'
                      : 'Chưa có buổi học sắp tới'}
              </Text>
              <Text className="text-xs text-slate-600 mt-0.5" numberOfLines={1}>
                {upcomingLesson
                  ? [
                      upcomingLesson.title,
                      new Intl.DateTimeFormat('vi-VN').format(new Date(upcomingLesson.lessonDate)),
                      upcomingLesson.room,
                      upcomingLesson.teacherName ? `GV: ${upcomingLesson.teacherName}` : null,
                    ]
                      .filter(Boolean)
                      .join(' · ')
                  : session?.role === 'Parent'
                    ? 'Lịch học của các con sẽ hiển thị tại đây.'
                    : ''}
              </Text>
            </View>
          </View>

          <View className="mt-3 flex-row gap-2 z-10">
            <TouchableOpacity
              activeOpacity={0.7}
              className="flex-1 bg-white items-center justify-center py-2.5 rounded-xl border border-amber-200"
              onPress={() => router.push('/(tabs)/schedule')}
            >
              <Text className="text-amber-900 text-xs font-semibold">Xem chi tiết</Text>
            </TouchableOpacity>
            <TouchableOpacity
              activeOpacity={0.7}
              className="flex-1 bg-amber-500 items-center justify-center py-2.5 rounded-xl shadow-sm"
              onPress={() => {
                if (upcomingLesson) {
                  router.push({
                    pathname: '/leave-requests',
                    params: {
                      studentId: String(upcomingLesson.studentId),
                      lessonId: String(upcomingLesson.lessonId),
                    },
                  });
                  return;
                }

                router.push('/leave-requests');
              }}
            >
              <Text className="text-white text-xs font-semibold">Xin nghỉ phép</Text>
            </TouchableOpacity>
          </View>
        </View>

        {/* BÀI TẬP CẦN LÀM */}
        <View className="bg-white rounded-2xl p-3.5 border border-slate-200 shadow-sm flex-row items-center justify-between mb-4">
          <View className="flex-row items-center gap-3">
            <View className="w-10 h-10 rounded-xl bg-rose-50 items-center justify-center">
              <Feather name="clock" size={20} color="#f43f5e" />
            </View>
            <View>
              <Text className="text-xs font-bold text-slate-800">2 Bài tập sắp hết hạn nộp</Text>
              <Text className="text-[11px] text-slate-500 mt-0.5">Minh An • Hạn chót 21:00 tối nay</Text>
            </View>
          </View>
          <TouchableOpacity activeOpacity={0.7} className="bg-indigo-50 px-3 py-1.5 rounded-lg">
            <Text className="text-xs font-bold text-indigo-600">Xem ngay</Text>
          </TouchableOpacity>
        </View>

        {/* TIỆN ÍCH QUẢN LÝ */}
        <View className="bg-white rounded-2xl p-4 border border-slate-200 shadow-sm mb-4">
          <Text className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-4">Tiện ích quản lý</Text>
          
          <View className="flex-row justify-between px-1">
            <TouchableOpacity
              activeOpacity={0.7}
              className="items-center gap-2"
              onPress={() => router.push('/leave-requests')}
            >
              <View className="w-14 h-14 rounded-2xl bg-rose-500 items-center justify-center shadow-sm">
                <Feather name="calendar" size={24} color="white" />
              </View>
              <Text className="text-[11px] font-semibold text-slate-700">Xin nghỉ</Text>
            </TouchableOpacity>
            
            <TouchableOpacity
              activeOpacity={0.7}
              className="items-center gap-2"
              onPress={() => router.push('/invoices')}
            >
              <View className="w-14 h-14 rounded-2xl bg-amber-500 items-center justify-center shadow-sm">
                <Feather name="file-text" size={24} color="white" />
              </View>
              <Text className="text-[11px] font-semibold text-slate-700">Hóa đơn</Text>
            </TouchableOpacity>
            
            <TouchableOpacity 
              activeOpacity={0.7} 
              className="items-center gap-2"
              onPress={() => router.push('/students')}
            >
              <View className="w-14 h-14 rounded-2xl bg-emerald-500 items-center justify-center shadow-sm">
                <Feather name="users" size={24} color="white" />
              </View>
              <Text className="text-[11px] font-semibold text-slate-700">Hồ sơ con</Text>
            </TouchableOpacity>
            
          <TouchableOpacity
              activeOpacity={0.7}
              className="items-center gap-2"
              onPress={() => router.push('/chat')}
            >
              <View className="w-14 h-14 rounded-2xl bg-fuchsia-500 items-center justify-center shadow-sm">
                <Feather name="message-circle" size={24} color="white" />
              </View>
              <Text className="text-[11px] font-semibold text-slate-700">Nhắn tin</Text>
          </TouchableOpacity>

          </View>
        </View>

        {/* KHÓA HỌC ĐANG MỞ */}
        <View className="mb-6">
          <View className="flex-row items-center justify-between mb-3">
            <Text className="font-bold text-slate-800 text-[15px]">Khóa học đang mở</Text>
            
            <TouchableOpacity 
              activeOpacity={0.7} 
              className="flex-row items-center gap-1"
              onPress={() => router.push('/courses')} 
            >
              <Text className="text-xs text-indigo-600 font-semibold">Xem tất cả</Text>
              <Feather name="chevron-right" size={14} color="#4f46e5" />
            </TouchableOpacity>

          </View>
          
          <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={{ gap: 12 }}>
            <View className="bg-white rounded-2xl overflow-hidden shadow-sm border border-slate-200 w-[260px]">
              <View className="h-28 bg-indigo-500 items-center justify-center relative">
                <Text className="text-white text-3xl font-bold opacity-90">FM</Text>
                <View className="absolute top-2.5 left-2.5 bg-white px-2 py-0.5 rounded-full">
                  <Text className="text-[10px] font-bold text-indigo-700">Đang chiêu sinh</Text>
                </View>
              </View>
              <View className="p-3.5">
                <Text className="font-bold text-slate-900 text-sm">Finger Math Cơ Bản</Text>
                <Text className="text-xs text-slate-500 mt-1">Độ tuổi 5-7 • 3 tháng</Text>
                <View className="flex-row items-center justify-between mt-3">
                  <Text className="text-sm font-bold text-indigo-600">1.500.000đ</Text>
                  <TouchableOpacity 
                    activeOpacity={0.7} 
                    className="bg-indigo-50 px-3 py-1.5 rounded-lg"
                    onPress={() => router.push('/courses')}
                  >
                    <Text className="text-xs font-semibold text-indigo-600">Xem chi tiết</Text>
                  </TouchableOpacity>
                </View>
              </View>
            </View>

            <View className="bg-white rounded-2xl overflow-hidden shadow-sm border border-slate-200 w-[260px]">
              <View className="h-28 bg-teal-500 items-center justify-center relative">
                <Text className="text-white text-3xl font-bold opacity-90">SB</Text>
                <View className="absolute top-2.5 left-2.5 bg-white px-2 py-0.5 rounded-full">
                  <Text className="text-[10px] font-bold text-teal-700">Đang chiêu sinh</Text>
                </View>
              </View>
              <View className="p-3.5">
                <Text className="font-bold text-slate-900 text-sm">Soroban Nâng Cao</Text>
                <Text className="text-xs text-slate-500 mt-1">Độ tuổi 7-10 • 4 tháng</Text>
                <View className="flex-row items-center justify-between mt-3">
                  <Text className="text-sm font-bold text-indigo-600">1.800.000đ</Text>
                  <TouchableOpacity 
                    activeOpacity={0.7} 
                    className="bg-indigo-50 px-3 py-1.5 rounded-lg"
                    onPress={() => router.push('/courses')} 
                  >
                    <Text className="text-xs font-semibold text-indigo-600">Xem chi tiết</Text>
                  </TouchableOpacity>
                </View>
              </View>
            </View>
          </ScrollView>
        </View>
      </ScrollView>
    </View>
  );
}