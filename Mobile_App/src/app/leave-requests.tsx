import { useMemo, useState } from 'react';
import { useLocalSearchParams, Redirect, router } from 'expo-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ActivityIndicator, Alert, Modal, ScrollView, Text, TextInput, TouchableOpacity, View } from 'react-native';
import { Ionicons } from '@expo/vector-icons';

import { useParentLessons } from '@/hooks/useParentLessons';
import { useSession } from '@/hooks/useSession';
import { leaveRequestsQueryKey } from '@/lib/queryKeys';
import { cancelLeaveRequest, createLeaveRequest, getStudentLeaveRequests } from '@/services/lessonService';
import type { LeaveRequestDto, StudentScheduleLesson } from '@/types/lesson';

const EMPTY_REQUESTS: LeaveRequestDto[] = [];

function formatDate(value: string | null): string {
  if (!value) {
    return 'Chưa có thông tin';
  }

  return new Intl.DateTimeFormat('vi-VN', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  }).format(new Date(value));
}

function statusPresentation(status: string) {
  switch (status.toUpperCase()) {
    case 'APPROVED':
      return { label: 'Đã duyệt', style: 'bg-emerald-100 text-emerald-700' };
    case 'REJECTED':
      return { label: 'Từ chối', style: 'bg-rose-100 text-rose-700' };
    case 'CANCELLED':
      return { label: 'Đã hủy', style: 'bg-slate-100 text-slate-600' };
    default:
      return { label: 'Chờ duyệt', style: 'bg-amber-100 text-amber-700' };
  }
}

function canRequestLeave(lesson: StudentScheduleLesson): boolean {
  if (lesson.status.toUpperCase() !== 'SCHEDULED') {
    return false;
  }

  if (!lesson.startTime) {
    return false;
  }

  const startsAt = new Date(lesson.startTime).getTime();
  return Number.isFinite(startsAt) && startsAt - Date.now() >= 2 * 60 * 60 * 1000;
}

export default function LeaveRequestsScreen() {
  const { session, isHydrated } = useSession();
  const params = useLocalSearchParams<{ studentId?: string; lessonId?: string }>();
  const { students, studentIds, lessons, studentsQuery, lessonsQuery } = useParentLessons(
    session?.role === 'Parent',
  );
  const queryClient = useQueryClient();
  const studentIdParam = Array.isArray(params.studentId) ? params.studentId[0] : params.studentId;
  const lessonIdParam = Array.isArray(params.lessonId) ? params.lessonId[0] : params.lessonId;
  const routeStudentId = Number(studentIdParam);
  const routeLessonId = Number(lessonIdParam);
  const hasRequestedLesson =
    Number.isSafeInteger(routeStudentId) &&
    routeStudentId > 0 &&
    Number.isSafeInteger(routeLessonId) &&
    routeLessonId > 0;
  const [selectedStudentId, setSelectedStudentId] = useState<number | null>(() =>
    hasRequestedLesson ? routeStudentId : null,
  );
  const [selectedLessonId, setSelectedLessonId] = useState<number | null>(() =>
    hasRequestedLesson ? routeLessonId : null,
  );
  const [reason, setReason] = useState('');
  const [isRequestModalOpen, setIsRequestModalOpen] = useState(hasRequestedLesson);
  const [notice, setNotice] = useState<string | null>(null);
  const requestStudentId = selectedStudentId ?? students[0]?.id ?? null;

  const leaveRequestsQuery = useQuery<LeaveRequestDto[]>({
    queryKey: leaveRequestsQueryKey(studentIds),
    queryFn: async () => {
      const requestsByStudent = await Promise.all(
        students.map((student) => getStudentLeaveRequests(student.id)),
      );
      return requestsByStudent.flat();
    },
    enabled: studentsQuery.isSuccess,
  });

  const createRequestMutation = useMutation({
    mutationFn: createLeaveRequest,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['leaveRequests'] });
      setIsRequestModalOpen(false);
      setSelectedLessonId(null);
      setReason('');
      setNotice('Đơn xin nghỉ đã được gửi. Bạn có thể theo dõi trạng thái tại đây.');
    },
  });
  const cancelRequestMutation = useMutation({
    mutationFn: cancelLeaveRequest,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['leaveRequests'] });
      setNotice('Đơn xin nghỉ đã được hủy.');
    },
    onError: (error) => {
      Alert.alert(
        'Không thể hủy đơn',
        error instanceof Error ? error.message : 'Vui lòng thử lại sau.',
      );
    },
  });

  const leaveRequests = leaveRequestsQuery.data ?? EMPTY_REQUESTS;
  const visibleRequests = useMemo(
    () =>
      selectedStudentId === null
        ? leaveRequests
        : leaveRequests.filter((request) => request.studentId === selectedStudentId),
    [leaveRequests, selectedStudentId],
  );
  const existingLessonIds = new Set(
    leaveRequests
      .filter(
        (request) =>
          request.studentId === requestStudentId &&
          request.status.toUpperCase() !== 'CANCELLED',
      )
      .map((request) => request.lessonId),
  );
  const requestableLessons = lessons.filter(
    (lesson) =>
      lesson.studentId === requestStudentId &&
      canRequestLeave(lesson) &&
      !existingLessonIds.has(lesson.lessonId),
  );
  const selectedLesson = requestableLessons.find(
    (lesson) =>
      lesson.studentId === requestStudentId && lesson.lessonId === selectedLessonId,
  );

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

  const submitRequest = () => {
    if (requestStudentId === null || selectedLessonId === null) {
      return;
    }

    createRequestMutation.mutate({
      studentId: requestStudentId,
      lessonId: selectedLessonId,
      ...(reason.trim() ? { reason: reason.trim() } : {}),
    });
  };

  const confirmCancelRequest = (request: LeaveRequestDto) => {
    Alert.alert(
      'Hủy đơn xin nghỉ?',
      `Đơn xin nghỉ buổi "${request.lessonTitle}" sẽ được hủy.`,
      [
        { text: 'Giữ lại', style: 'cancel' },
        {
          text: 'Hủy đơn',
          style: 'destructive',
          onPress: () => cancelRequestMutation.mutate(request.id),
        },
      ],
    );
  };

  const retry = () => {
    if (studentsQuery.isError) {
      void studentsQuery.refetch();
    } else if (lessonsQuery.isError) {
      void lessonsQuery.refetch();
    } else {
      void leaveRequestsQuery.refetch();
    }
  };

  const isPending = studentsQuery.isPending || lessonsQuery.isPending || leaveRequestsQuery.isPending;
  const loadError = studentsQuery.error ?? lessonsQuery.error ?? leaveRequestsQuery.error;

  return (
    <View className="flex-1 bg-slate-50">
      <View className="rounded-b-[30px] bg-[#5a64f1] px-5 pb-6 pt-14">
        <View className="mb-5 flex-row items-center">
          <TouchableOpacity onPress={() => router.back()} className="mr-3 h-10 w-10 items-center justify-center rounded-full bg-white/20">
            <Ionicons name="arrow-back" size={20} color="white" />
          </TouchableOpacity>
          <View className="flex-1">
            <Text className="text-2xl font-black text-white">Xin nghỉ</Text>
            <Text className="mt-1 text-sm text-indigo-100">Lịch sử và trạng thái đơn xin nghỉ</Text>
          </View>
        </View>
        <ScrollView horizontal showsHorizontalScrollIndicator={false}>
          <TouchableOpacity
            onPress={() => setSelectedStudentId(null)}
            className={`mr-2 rounded-full border px-4 py-2 ${
              selectedStudentId === null ? 'border-white bg-white' : 'border-white/30 bg-white/20'
            }`}
          >
            <Text className={`font-bold ${selectedStudentId === null ? 'text-[#5a64f1]' : 'text-white'}`}>
              Tất cả học sinh
            </Text>
          </TouchableOpacity>
          {students.map((student) => (
            <TouchableOpacity
              key={student.id}
              onPress={() => setSelectedStudentId(student.id)}
              className={`mr-2 rounded-full border px-4 py-2 ${
                selectedStudentId === student.id
                  ? 'border-white bg-white'
                  : 'border-white/30 bg-white/20'
              }`}
            >
              <Text
                className={`font-bold ${
                  selectedStudentId === student.id ? 'text-[#5a64f1]' : 'text-white'
                }`}
              >
                {student.fullName}
              </Text>
            </TouchableOpacity>
          ))}
        </ScrollView>
      </View>

      <ScrollView className="flex-1 px-5 pt-5" contentContainerStyle={{ paddingBottom: 32 }}>
        <TouchableOpacity
          onPress={() => {
            setNotice(null);
            setSelectedLessonId(null);
            setReason('');
            setIsRequestModalOpen(true);
          }}
          disabled={students.length === 0}
          className="mb-5 flex-row items-center justify-center rounded-2xl bg-orange-500 py-4"
        >
          <Ionicons name="calendar-outline" size={19} color="white" />
          <Text className="ml-2 font-bold text-white">Tạo đơn xin nghỉ</Text>
        </TouchableOpacity>

        {notice ? (
          <View className="mb-4 rounded-xl border border-emerald-200 bg-emerald-50 p-4">
            <Text className="font-medium text-emerald-800">{notice}</Text>
          </View>
        ) : null}

        {isPending ? (
          <View className="items-center py-12">
            <ActivityIndicator size="large" color="#5a64f1" />
            <Text className="mt-3 text-slate-500">Đang tải dữ liệu...</Text>
          </View>
        ) : loadError ? (
          <View className="items-center rounded-2xl bg-white px-5 py-10">
            <Text className="text-center text-slate-600">
              {loadError instanceof Error ? loadError.message : 'Không thể tải dữ liệu xin nghỉ.'}
            </Text>
            <TouchableOpacity onPress={retry} className="mt-4 rounded-xl bg-indigo-600 px-5 py-3">
              <Text className="font-bold text-white">Thử tải lại</Text>
            </TouchableOpacity>
          </View>
        ) : students.length === 0 ? (
          <View className="items-center py-12">
            <Text className="text-center font-medium text-slate-500">
              Chưa có hồ sơ học sinh để gửi đơn xin nghỉ.
            </Text>
          </View>
        ) : visibleRequests.length === 0 ? (
          <View className="items-center rounded-2xl bg-white px-5 py-12">
            <Ionicons name="document-text-outline" size={40} color="#94a3b8" />
            <Text className="mt-3 text-center font-medium text-slate-500">
              Chưa có lịch sử xin nghỉ.
            </Text>
          </View>
        ) : (
          visibleRequests.map((request) => {
            const status = statusPresentation(request.status);
            return (
              <View
                key={request.id}
                className="mb-3 rounded-2xl border border-slate-100 bg-white p-4 shadow-sm"
              >
                <View className="mb-2 flex-row items-start justify-between">
                  <Text className="mr-3 flex-1 text-base font-black text-slate-800">
                    {request.lessonTitle}
                  </Text>
                  <Text className={`rounded-full px-3 py-1 text-xs font-bold ${status.style}`}>
                    {status.label}
                  </Text>
                </View>
                <Text className="text-sm text-slate-500">
                  {request.studentName} · Buổi học {formatDate(request.lessonDate)}
                </Text>
                {request.reason ? (
                  <Text className="mt-2 text-sm text-slate-600">Lý do: {request.reason}</Text>
                ) : null}
                {request.createdAt ? (
                  <Text className="mt-2 text-xs text-slate-400">
                    Gửi ngày {formatDate(request.createdAt)}
                  </Text>
                ) : null}
                {request.status.toUpperCase() === 'PENDING' ? (
                  <TouchableOpacity
                    disabled={cancelRequestMutation.isPending}
                    onPress={() => confirmCancelRequest(request)}
                    className="mt-3 self-start rounded-xl border border-rose-200 bg-rose-50 px-4 py-2"
                  >
                    <Text className="text-sm font-bold text-rose-700">
                      {cancelRequestMutation.isPending ? 'Đang hủy...' : 'Hủy đơn xin nghỉ'}
                    </Text>
                  </TouchableOpacity>
                ) : null}
              </View>
            );
          })
        )}
      </ScrollView>

      <Modal
        transparent
        animationType="slide"
        visible={isRequestModalOpen}
        onRequestClose={() => setIsRequestModalOpen(false)}
      >
        <View className="flex-1 justify-end bg-black/40">
          <View className="max-h-[88%] rounded-t-[30px] bg-white px-5 pb-8 pt-4">
            <View className="mb-5 flex-row items-center justify-between">
              <Text className="text-xl font-black text-slate-800">Tạo đơn xin nghỉ</Text>
              <TouchableOpacity onPress={() => setIsRequestModalOpen(false)} className="p-2">
                <Ionicons name="close" size={24} color="#64748b" />
              </TouchableOpacity>
            </View>

            {students.length > 1 ? (
              <ScrollView horizontal className="mb-4" showsHorizontalScrollIndicator={false}>
                {students.map((student) => (
                  <TouchableOpacity
                    key={student.id}
                    onPress={() => {
                      setSelectedStudentId(student.id);
                      setSelectedLessonId(null);
                    }}
                    className={`mr-2 rounded-full border px-4 py-2 ${
                      requestStudentId === student.id
                        ? 'border-indigo-600 bg-indigo-50'
                        : 'border-slate-200 bg-white'
                    }`}
                  >
                    <Text
                      className={`font-bold ${
                        requestStudentId === student.id ? 'text-indigo-700' : 'text-slate-600'
                      }`}
                    >
                      {student.fullName}
                    </Text>
                  </TouchableOpacity>
                ))}
              </ScrollView>
            ) : null}

            <Text className="mb-2 font-bold text-slate-700">Chọn buổi học</Text>
            <ScrollView className="max-h-56">
              {requestableLessons.length === 0 ? (
                <Text className="rounded-xl bg-slate-50 p-4 text-sm text-slate-500">
                  Không có buổi học phù hợp để xin nghỉ. Đơn cần được gửi trước giờ học ít nhất 2 tiếng.
                </Text>
              ) : (
                requestableLessons.map((lesson) => (
                  <TouchableOpacity
                    key={`${lesson.studentId}-${lesson.lessonId}`}
                    onPress={() => setSelectedLessonId(lesson.lessonId)}
                    className={`mb-2 rounded-xl border p-3 ${
                      selectedLessonId === lesson.lessonId
                        ? 'border-indigo-500 bg-indigo-50'
                        : 'border-slate-200 bg-white'
                    }`}
                  >
                    <Text className="font-bold text-slate-800">{lesson.title}</Text>
                    <Text className="mt-1 text-sm text-slate-500">
                      {lesson.className} · {formatDate(lesson.lessonDate)} · {lesson.startLabel}
                    </Text>
                  </TouchableOpacity>
                ))
              )}
            </ScrollView>

            <Text className="mb-2 mt-4 font-bold text-slate-700">Lý do (không bắt buộc)</Text>
            <TextInput
              value={reason}
              onChangeText={setReason}
              placeholder="Nhập lý do xin nghỉ..."
              multiline
              textAlignVertical="top"
              className="mb-3 min-h-20 rounded-xl border border-slate-200 bg-white p-3 text-slate-800"
            />
            {createRequestMutation.error ? (
              <Text className="mb-3 text-sm text-rose-600">
                {createRequestMutation.error instanceof Error
                  ? createRequestMutation.error.message
                  : 'Không thể gửi đơn xin nghỉ.'}
              </Text>
            ) : null}
            <TouchableOpacity
              onPress={submitRequest}
              disabled={
                selectedLessonId === null ||
                !selectedLesson ||
                createRequestMutation.isPending
              }
              className={`items-center rounded-xl py-4 ${
                selectedLessonId === null || !selectedLesson || createRequestMutation.isPending
                  ? 'bg-slate-300'
                  : 'bg-orange-500'
              }`}
            >
              <Text className="font-bold text-white">
                {createRequestMutation.isPending ? 'Đang gửi...' : 'Gửi đơn xin nghỉ'}
              </Text>
            </TouchableOpacity>
          </View>
        </View>
      </Modal>
    </View>
  );
}
