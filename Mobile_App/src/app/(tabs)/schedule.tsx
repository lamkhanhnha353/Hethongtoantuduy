import { useMemo, useRef, useState } from 'react';
import { Ionicons } from '@expo/vector-icons';
import { Redirect, router } from 'expo-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ActivityIndicator, Alert, FlatList, Modal, ScrollView, Text, TextInput, TouchableOpacity, useWindowDimensions, View } from 'react-native';

import { LessonCard } from '@/components/schedule/LessonCard';
import { useParentLessons } from '@/hooks/useParentLessons';
import { useSession } from '@/hooks/useSession';
import { leaveRequestsQueryKey } from '@/lib/queryKeys';
import { createLeaveRequest, getStudentLeaveRequests } from '@/services/lessonService';
import type { LeaveRequestDto, StudentScheduleLesson } from '@/types/lesson';

type CalendarStatus = 'completed' | 'upcoming' | 'none';

const WEEKDAY_LABELS = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];
const EMPTY_LEAVE_REQUESTS: LeaveRequestDto[] = [];
const DAY_ITEM_WIDTH = 60;
const DAYS_BEFORE_TODAY = 365;
const MONTHS_BEFORE_SELECTED = 12;

function addDays(date: Date, amount: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + amount);
}

function getMonthDates(month: Date) {
  const firstDayOffset = (new Date(month.getFullYear(), month.getMonth(), 1).getDay() + 6) % 7;
  return Array.from({ length: 42 }, (_, index) => {
    const date = new Date(month.getFullYear(), month.getMonth(), index - firstDayOffset + 1);
    return { date, dateKey: toDateKey(date), isCurrentMonth: date.getMonth() === month.getMonth() };
  });
}

function lessonKey(lesson: StudentScheduleLesson): string {
  return `${lesson.studentId}-${lesson.lessonId}`;
}

function canRequestLeave(lesson: StudentScheduleLesson, now: number): boolean {
  if (lesson.status.toUpperCase() !== 'SCHEDULED' || !lesson.startTime) {
    return false;
  }

  const startTime = new Date(lesson.startTime).getTime();
  return Number.isFinite(startTime) && startTime - now >= 2 * 60 * 60 * 1000;
}

function getLeaveRequestMessage(request: LeaveRequestDto): string {
  switch (request.status.toUpperCase()) {
    case 'PENDING':
      return 'Bạn đã gửi đơn xin nghỉ buổi này rồi. Đơn đang chờ duyệt.';
    case 'APPROVED':
      return 'Đơn xin nghỉ buổi này đã được duyệt.';
    case 'REJECTED':
      return 'Đơn xin nghỉ buổi này đã bị từ chối.';
    default:
      return 'Đơn xin nghỉ buổi này đã được xử lý.';
  }
}

function toDateKey(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function toLessonDateKey(date: string): string {
  return date.slice(0, 10);
}

function formatMonthLabel(date: Date): string {
  return new Intl.DateTimeFormat('vi-VN', { month: 'long', year: 'numeric' }).format(date);
}

function formatLessonStatus(status: string): string {
  switch (status.toUpperCase()) {
    case 'COMPLETED':
      return 'Đã học';
    case 'SCHEDULED':
      return 'Sắp học';
    case 'HOLIDAY':
      return 'Ngày nghỉ';
    case 'CANCELED':
    case 'CANCELLED':
      return 'Đã hủy';
    case 'MAKE_UP':
      return 'Học bù';
    default:
      return status;
  }
}

function renderStatusDot(status: CalendarStatus, isActive: boolean) {
  if (status === 'none') {
    return null;
  }

  const color = isActive ? 'bg-white' : status === 'completed' ? 'bg-emerald-500' : 'bg-orange-500';
  return <View className={`mt-1.5 h-1.5 w-1.5 rounded-full ${color}`} />;
}

export default function ScheduleScreen() {
  const { session, isHydrated } = useSession();
  const isParent = session?.role === 'Parent';
  const { students, studentIds, lessons, studentsQuery, lessonsQuery } = useParentLessons(isParent);
  const queryClient = useQueryClient();
  const { height: windowHeight } = useWindowDimensions();

  const [activeChildId, setActiveChildId] = useState<number | 'all'>('all');
  const [selectedDate, setSelectedDate] = useState(() => new Date());
  const [now] = useState(() => Date.now());
  const [selectedLesson, setSelectedLesson] = useState<StudentScheduleLesson | null>(null);
  const [showFullCalendar, setShowFullCalendar] = useState(false);
  const [selectedLeaveKeys, setSelectedLeaveKeys] = useState<string[]>([]);
  const [showLeaveReasonModal, setShowLeaveReasonModal] = useState(false);
  const [leaveReason, setLeaveReason] = useState('');
  const [leaveNotice, setLeaveNotice] = useState<string | null>(null);
  const [calendarAnchor] = useState(
    () => new Date(new Date().getFullYear(), new Date().getMonth(), new Date().getDate()),
  );
  const monthPageHeight = Math.max(300, windowHeight * 0.82 - 140);
  const weekListRef = useRef<FlatList<Date>>(null);

  const leaveRequestsQuery = useQuery<LeaveRequestDto[]>({
    queryKey: leaveRequestsQueryKey(studentIds),
    queryFn: async () => {
      const requestsByStudent = await Promise.all(
        students.map((student) => getStudentLeaveRequests(student.id)),
      );
      return requestsByStudent.flat();
    },
    enabled: isParent && studentsQuery.isSuccess,
  });
  const leaveRequests = leaveRequestsQuery.data ?? EMPTY_LEAVE_REQUESTS;
  const activeLeaveRequests = useMemo(
    () => leaveRequests.filter((request) => request.status.toUpperCase() !== 'CANCELLED'),
    [leaveRequests],
  );
  const leaveRequestsByLesson = useMemo(
    () =>
      new Map(
        activeLeaveRequests.map((request) => [
          `${request.studentId}-${request.lessonId}`,
          request,
        ]),
      ),
    [activeLeaveRequests],
  );
  const sendLeaveRequestsMutation = useMutation({
    mutationFn: async ({
      selectedLessons,
      reason,
    }: {
      selectedLessons: StudentScheduleLesson[];
      reason: string;
    }) => {
      const results = await Promise.all(
        selectedLessons.map(async (lesson) => {
          try {
            await createLeaveRequest({
              studentId: lesson.studentId,
              lessonId: lesson.lessonId,
              ...(reason.trim() ? { reason: reason.trim() } : {}),
            });
            return { key: lessonKey(lesson), studentName: lesson.studentName, error: null };
          } catch (error) {
            return {
              key: lessonKey(lesson),
              studentName: lesson.studentName,
              error: error instanceof Error ? error.message : 'Không thể gửi đơn xin nghỉ.',
            };
          }
        }),
      );

      return results;
    },
    onSettled: async () => {
      await queryClient.invalidateQueries({ queryKey: ['leaveRequests'] });
    },
    onSuccess: (results) => {
      const successes = results.filter((result) => result.error === null);
      const failures = results.filter((result) => result.error !== null);
      setSelectedLeaveKeys(failures.map((failure) => failure.key));
      setLeaveNotice(
        failures.length === 0
          ? `Đã gửi ${successes.length} đơn xin nghỉ.`
          : `Đã gửi ${successes.length}/${results.length} đơn. ${failures
              .map((failure) => `${failure.studentName}: ${failure.error}`)
              .join(' ')}`,
      );
      if (failures.length === 0) {
        setShowLeaveReasonModal(false);
        setLeaveReason('');
      }
    },
  });

  const visibleLessons = useMemo(
    () =>
      activeChildId === 'all'
        ? lessons
        : lessons.filter((lesson) => lesson.studentId === activeChildId),
    [activeChildId, lessons],
  );
  const calendarStatuses = useMemo(() => {
    const statuses = new Map<string, CalendarStatus>();
    for (const lesson of visibleLessons) {
      const dateKey = toLessonDateKey(lesson.lessonDate);
      const status = lesson.status.toUpperCase();
      if (status === 'COMPLETED') {
        statuses.set(dateKey, 'completed');
      } else if (status === 'SCHEDULED' && statuses.get(dateKey) !== 'completed') {
        statuses.set(dateKey, 'upcoming');
      }
    }
    return statuses;
  }, [visibleLessons]);
  const getDayStatus = (dateKey: string): CalendarStatus => calendarStatuses.get(dateKey) ?? 'none';
  const selectedDateKey = toDateKey(selectedDate);
  const currentLessons = useMemo(
    () => visibleLessons.filter((lesson) => toLessonDateKey(lesson.lessonDate) === selectedDateKey),
    [selectedDateKey, visibleLessons],
  );
  const selectedLeaveLessons = useMemo(
    () => currentLessons.filter((lesson) => selectedLeaveKeys.includes(lessonKey(lesson))),
    [currentLessons, selectedLeaveKeys],
  );

  const calendarDays = useMemo(
    () => Array.from({ length: 1096 }, (_, index) => addDays(calendarAnchor, index - DAYS_BEFORE_TODAY)),
    [calendarAnchor],
  );
  const calendarMonths = useMemo(
    () =>
      Array.from({ length: 37 }, (_, index) =>
        new Date(
          selectedDate.getFullYear(),
          selectedDate.getMonth() + index - MONTHS_BEFORE_SELECTED,
          1,
        ),
      ),
    [selectedDate],
  );

  const dateTitle = new Intl.DateTimeFormat('vi-VN', {
    weekday: 'long',
    day: 'numeric',
    month: 'numeric',
    year: 'numeric',
  }).format(selectedDate);

  const isPending =
    studentsQuery.isPending || lessonsQuery.isPending || leaveRequestsQuery.isPending;
  const error = studentsQuery.error ?? lessonsQuery.error ?? leaveRequestsQuery.error;

  const retryLoading = () => {
    if (studentsQuery.isError) {
      void studentsQuery.refetch();
      return;
    }

    if (lessonsQuery.isError) {
      void lessonsQuery.refetch();
      return;
    }

    void leaveRequestsQuery.refetch();
  };

  const selectDate = (date: Date) => {
    setSelectedLeaveKeys([]);
    setSelectedDate(date);
    setShowFullCalendar(false);
    const index =
      Math.round((date.getTime() - calendarAnchor.getTime()) / (24 * 60 * 60 * 1000)) +
      DAYS_BEFORE_TODAY;
    if (index >= 0 && index < calendarDays.length) {
      weekListRef.current?.scrollToIndex({ index, animated: true, viewPosition: 0.5 });
    }
  };

  const openMonthCalendar = () => {
    setShowFullCalendar(true);
  };

  const findLeaveRequest = (lesson: StudentScheduleLesson) =>
    leaveRequestsByLesson.get(lessonKey(lesson));

  const openLeaveReasonModal = (lessonsToRequest: StudentScheduleLesson[]) => {
    if (lessonsToRequest.length === 0) {
      return;
    }

    setLeaveNotice(null);
    setSelectedLeaveKeys(lessonsToRequest.map(lessonKey));
    setSelectedLesson(null);
    setLeaveReason('');
    setShowLeaveReasonModal(true);
  };

  const handleLessonPress = (lesson: StudentScheduleLesson) => {
    const request = findLeaveRequest(lesson);
    if (request) {
      Alert.alert('Đơn xin nghỉ', getLeaveRequestMessage(request));
      return;
    }

    setSelectedLesson(lesson);
  };

  const toggleLeaveSelection = (lesson: StudentScheduleLesson) => {
    const request = findLeaveRequest(lesson);
    if (request) {
      Alert.alert('Đơn xin nghỉ', getLeaveRequestMessage(request));
      return;
    }

    if (!canRequestLeave(lesson, now)) {
      Alert.alert(
        'Không thể xin nghỉ',
        'Đơn xin nghỉ cần được gửi trước giờ học ít nhất 2 tiếng.',
      );
      return;
    }

    const key = lessonKey(lesson);
    setSelectedLeaveKeys((selected) =>
      selected.includes(key) ? selected.filter((item) => item !== key) : [...selected, key],
    );
  };

  const submitLeaveRequests = () => {
    const eligibleLessons = selectedLeaveLessons.filter(
      (lesson) => !findLeaveRequest(lesson) && canRequestLeave(lesson, now),
    );
    if (eligibleLessons.length === 0) {
      return;
    }

    sendLeaveRequestsMutation.mutate({
      selectedLessons: eligibleLessons,
      reason: leaveReason,
    });
  };

  if (isHydrated && !session) {
    return <Redirect href="/login" />;
  }

  if (isHydrated && !isParent) {
    return (
      <View className="flex-1 items-center justify-center bg-slate-50 px-6">
        <Ionicons name="calendar-outline" size={40} color="#94a3b8" />
        <Text className="mt-3 text-center font-medium text-slate-600">
          Lịch học chỉ dành cho tài khoản phụ huynh.
        </Text>
      </View>
    );
  }

  return (
    <View className="flex-1 bg-slate-50">
      <View className="z-10 rounded-b-[32px] bg-[#5a64f1] px-5 pb-6 pt-14 shadow-sm">
        <View className="mb-6 flex-row items-center justify-between">
          <View className="flex-1">
            <Text className="text-2xl font-black text-white">Lịch học</Text>
            <Text className="mt-1 text-sm font-medium capitalize text-indigo-100">
              {formatMonthLabel(selectedDate)}
            </Text>
          </View>
          <TouchableOpacity
            accessibilityRole="button"
            accessibilityLabel="Mở lịch tháng"
            activeOpacity={0.75}
            onPress={openMonthCalendar}
            className="h-11 w-11 items-center justify-center rounded-full bg-white/20"
          >
            <Ionicons name="calendar-outline" size={20} color="white" />
          </TouchableOpacity>
        </View>

        <ScrollView horizontal showsHorizontalScrollIndicator={false}>
          <TouchableOpacity
            onPress={() => {
              setActiveChildId('all');
              setSelectedLeaveKeys([]);
            }}
            activeOpacity={0.8}
            className={`mr-3 rounded-full border px-4 py-2 ${
              activeChildId === 'all' ? 'border-white bg-white' : 'border-white/30 bg-white/20'
            }`}
          >
            <Text
              className={`text-sm font-black uppercase tracking-wide ${
                activeChildId === 'all' ? 'text-[#5a64f1]' : 'text-white'
              }`}
            >
              Tất cả học sinh
            </Text>
          </TouchableOpacity>
          {students.map((student) => {
            const isActive = activeChildId === student.id;
            return (
              <TouchableOpacity
                key={student.id}
                onPress={() => {
                  setActiveChildId(student.id);
                  setSelectedLeaveKeys([]);
                }}
                activeOpacity={0.8}
                className={`mr-3 rounded-full border px-4 py-2 ${
                  isActive ? 'border-white bg-white' : 'border-white/30 bg-white/20'
                }`}
              >
                <Text
                  className={`text-sm font-black uppercase tracking-wide ${
                    isActive ? 'text-[#5a64f1]' : 'text-white'
                  }`}
                >
                  {student.fullName}
                </Text>
              </TouchableOpacity>
            );
          })}
        </ScrollView>
      </View>

      <ScrollView className="flex-1 px-5 pt-5" showsVerticalScrollIndicator={false}>
        <View className="mb-6 rounded-2xl border border-slate-100 bg-white p-2 shadow-sm">
          <FlatList
            ref={weekListRef}
            className="h-[68px]"
            horizontal
            data={calendarDays}
            initialScrollIndex={DAYS_BEFORE_TODAY}
            getItemLayout={(_, index) => ({
              length: DAY_ITEM_WIDTH,
              offset: 2 + DAY_ITEM_WIDTH * index,
              index,
            })}
            keyExtractor={toDateKey}
            showsHorizontalScrollIndicator={false}
            decelerationRate={0.9}
            snapToInterval={DAY_ITEM_WIDTH}
            snapToAlignment="start"
            disableIntervalMomentum
            contentContainerStyle={{ paddingHorizontal: 2 }}
            initialNumToRender={15}
            windowSize={5}
            renderItem={({ item: date }) => {
              const dateKey = toDateKey(date);
              const isActive = selectedDateKey === dateKey;
              const dayName = WEEKDAY_LABELS[(date.getDay() + 6) % 7];
              return (
                <TouchableOpacity
                  key={dateKey}
                  onPress={() => selectDate(date)}
                  className={`mr-2 h-[68px] w-[52px] items-center justify-center rounded-xl ${
                    isActive ? 'bg-[#5a64f1]' : 'bg-transparent'
                  }`}
                >
                  <Text className={`mb-1 text-[11px] font-semibold ${isActive ? 'text-indigo-100' : 'text-slate-400'}`}>
                    {dayName}
                  </Text>
                  <Text className={`text-base font-black ${isActive ? 'text-white' : 'text-slate-700'}`}>
                    {date.getDate()}
                  </Text>
                  {renderStatusDot(getDayStatus(dateKey), isActive)}
                </TouchableOpacity>
              );
            }}
          />
        </View>

        <View className="mb-4 flex-row items-center justify-between">
          <Text className="flex-1 text-lg font-black capitalize text-slate-800">{dateTitle}</Text>
          <TouchableOpacity
            className="ml-2 flex-row items-center rounded-full border border-indigo-100 bg-indigo-50 px-3 py-1.5"
            activeOpacity={0.7}
            onPress={() => router.push('/roadmap')}
          >
            <Ionicons name="map-outline" size={16} color="#5a64f1" style={{ marginRight: 4 }} />
            <Text className="text-xs font-bold text-[#5a64f1]">Xem lộ trình</Text>
          </TouchableOpacity>
        </View>

        {leaveNotice ? (
          <View className="mb-4 rounded-xl border border-indigo-100 bg-indigo-50 p-3">
            <Text className="text-sm font-medium text-indigo-800">{leaveNotice}</Text>
          </View>
        ) : null}

        {isPending ? (
          <View className="items-center justify-center py-12">
            <ActivityIndicator size="large" color="#5a64f1" />
            <Text className="mt-3 font-medium text-slate-500">Đang tải lịch học...</Text>
          </View>
        ) : error ? (
          <View className="items-center justify-center rounded-2xl bg-white px-5 py-10">
            <Ionicons name="cloud-offline-outline" size={36} color="#94a3b8" />
            <Text className="mt-3 text-center font-medium text-slate-600">
              {error instanceof Error ? error.message : 'Không thể tải lịch học.'}
            </Text>
            <TouchableOpacity
              className="mt-4 rounded-xl bg-indigo-600 px-5 py-3"
              onPress={retryLoading}
              activeOpacity={0.8}
            >
              <Text className="font-bold text-white">Thử tải lại</Text>
            </TouchableOpacity>
          </View>
        ) : students.length === 0 ? (
          <View className="items-center justify-center py-12">
            <Ionicons name="people-outline" size={38} color="#94a3b8" />
            <Text className="mt-3 text-center font-medium text-slate-500">
              Chưa có học sinh để hiển thị lịch học.
            </Text>
          </View>
        ) : currentLessons.length === 0 ? (
          <View className="items-center justify-center py-12">
            <Text className="mb-3 text-4xl">🏖️</Text>
            <Text className="font-medium text-slate-500">Không có lịch học trong ngày này.</Text>
          </View>
        ) : (
          currentLessons.map((lesson) => {
            const leaveRequest = findLeaveRequest(lesson);

            return (
              <LessonCard
                key={`${lesson.studentId}-${lesson.lessonId}`}
                lesson={lesson}
                activeChildId={activeChildId}
                leaveRequest={leaveRequest}
                onPress={handleLessonPress}
              />
            );
          })
        )}
        <View className="h-24" />
      </ScrollView>

      <Modal
        animationType="slide"
        transparent
        visible={selectedLesson !== null}
        onRequestClose={() => setSelectedLesson(null)}
      >
        <View className="flex-1 justify-end bg-black/40">
          <View className="rounded-t-[32px] bg-white px-6 pb-10 pt-3 shadow-xl">
            <View className="mb-6 h-1.5 w-12 self-center rounded-full bg-slate-300" />
            {selectedLesson ? (
              <View>
                <Text className="mb-5 text-2xl font-black text-slate-800">Chi tiết buổi học</Text>
                <View className="mb-6 rounded-2xl border border-slate-100 bg-slate-50 p-5">
                  <Text className="mb-1 text-xs font-bold uppercase tracking-wider text-slate-400">Chủ đề</Text>
                  <Text className="mb-5 text-lg font-black text-slate-800">{selectedLesson.title}</Text>
                  <View className="mb-4 flex-row">
                    <View className="flex-1">
                      <Text className="mb-1 text-xs font-bold uppercase tracking-wider text-slate-400">Học sinh</Text>
                      <Text className="font-black text-[#5a64f1]">{selectedLesson.studentName}</Text>
                    </View>
                    <View className="flex-1">
                      <Text className="mb-1 text-xs font-bold uppercase tracking-wider text-slate-400">Lớp học</Text>
                      <Text className="font-bold text-slate-800">{selectedLesson.className}</Text>
                    </View>
                  </View>
                  <View className="flex-row">
                    <View className="flex-1">
                      <Text className="mb-1 text-xs font-bold uppercase tracking-wider text-slate-400">Thời gian</Text>
                      <Text className="font-bold text-slate-800">
                        {selectedLesson.startLabel} - {selectedLesson.endLabel}
                      </Text>
                    </View>
                    <View className="flex-1">
                      <Text className="mb-1 text-xs font-bold uppercase tracking-wider text-slate-400">Trạng thái</Text>
                      <Text className="font-bold text-slate-800">
                        {formatLessonStatus(selectedLesson.status)}
                      </Text>
                    </View>
                  </View>
                  <View className="mt-4 flex-row">
                    <View className="flex-1">
                      <Text className="mb-1 text-xs font-bold uppercase tracking-wider text-slate-400">Giáo viên</Text>
                      <Text className="font-bold text-slate-800">{selectedLesson.teacherName || 'Chưa cập nhật'}</Text>
                    </View>
                    <View className="flex-1">
                      <Text className="mb-1 text-xs font-bold uppercase tracking-wider text-slate-400">Phòng học</Text>
                      <Text className="font-bold text-slate-800">{selectedLesson.room || 'Chưa cập nhật'}</Text>
                    </View>
                  </View>
                </View>
                {activeChildId === 'all' && selectedLesson.status.toUpperCase() === 'SCHEDULED' ? (
                  <TouchableOpacity
                    accessibilityRole="checkbox"
                    accessibilityState={{
                      checked: selectedLeaveKeys.includes(lessonKey(selectedLesson)),
                    }}
                    onPress={() => toggleLeaveSelection(selectedLesson)}
                    className="mb-4 flex-row items-center rounded-2xl border border-slate-200 bg-white p-4"
                  >
                    <View
                      className={`mr-3 h-6 w-6 items-center justify-center rounded-md border ${
                        selectedLeaveKeys.includes(lessonKey(selectedLesson))
                          ? 'border-orange-500 bg-orange-500'
                          : 'border-slate-300 bg-white'
                      }`}
                    >
                      {selectedLeaveKeys.includes(lessonKey(selectedLesson)) ? (
                        <Ionicons name="checkmark" size={17} color="white" />
                      ) : null}
                    </View>
                    <View className="flex-1">
                      <Text className="font-bold text-slate-800">Chọn buổi này để xin nghỉ</Text>
                      <Text className="mt-1 text-xs text-slate-500">
                        {canRequestLeave(selectedLesson, now)
                          ? 'Có thể chọn hoặc bỏ chọn buổi học này.'
                          : 'Cần gửi đơn trước giờ học ít nhất 2 tiếng.'}
                      </Text>
                    </View>
                  </TouchableOpacity>
                ) : null}
                <View className="flex-row">
                  <TouchableOpacity
                    className={`items-center rounded-2xl bg-slate-100 py-4 ${
                      activeChildId === 'all' && selectedLesson.status.toUpperCase() === 'SCHEDULED'
                        ? 'flex-1'
                        : 'mr-2 flex-1'
                    }`}
                    onPress={() => setSelectedLesson(null)}
                  >
                    <Text className="text-base font-bold text-slate-700">Đóng lại</Text>
                  </TouchableOpacity>
                  {activeChildId !== 'all' && selectedLesson.status.toUpperCase() === 'SCHEDULED' ? (
                    <TouchableOpacity
                      disabled={!canRequestLeave(selectedLesson, now)}
                      className={`ml-2 flex-1 items-center rounded-2xl py-4 ${
                        canRequestLeave(selectedLesson, now) ? 'bg-orange-500' : 'bg-slate-300'
                      }`}
                      onPress={() => {
                        openLeaveReasonModal([selectedLesson]);
                      }}
                    >
                      <Text className="text-base font-bold text-white">
                        {canRequestLeave(selectedLesson, now)
                          ? 'Xin nghỉ phép'
                          : 'Cần báo trước 2 tiếng'}
                      </Text>
                    </TouchableOpacity>
                  ) : null}
                </View>
                {activeChildId === 'all' && selectedLeaveLessons.length > 0 ? (
                  <TouchableOpacity
                    onPress={() => openLeaveReasonModal(selectedLeaveLessons)}
                    disabled={sendLeaveRequestsMutation.isPending}
                    className="mt-3 items-center rounded-2xl bg-orange-500 py-4"
                  >
                    <Text className="text-base font-bold text-white">
                      Tiếp tục xin nghỉ ({selectedLeaveLessons.length} buổi)
                    </Text>
                  </TouchableOpacity>
                ) : null}
              </View>
            ) : null}
          </View>
        </View>
      </Modal>

      <Modal
        animationType="slide"
        transparent
        visible={showLeaveReasonModal}
        onRequestClose={() => setShowLeaveReasonModal(false)}
      >
        <View className="flex-1 justify-end bg-black/40">
          <View className="max-h-[78%] rounded-t-[32px] bg-white px-6 pb-8 pt-4">
            <View className="mb-5 flex-row items-center justify-between">
              <View className="flex-1">
                <Text className="text-xl font-black text-slate-800">Xin nghỉ buổi học</Text>
                <Text className="mt-1 text-sm text-slate-500">
                  {selectedLeaveLessons.length} buổi đã chọn · {dateTitle}
                </Text>
              </View>
              <TouchableOpacity onPress={() => setShowLeaveReasonModal(false)} className="p-2">
                <Ionicons name="close" size={24} color="#64748b" />
              </TouchableOpacity>
            </View>
            <ScrollView className="mb-3 max-h-40">
              {selectedLeaveLessons.map((lesson) => (
                <Text
                  key={lessonKey(lesson)}
                  className="mb-1 rounded-lg bg-slate-50 p-2 text-sm text-slate-700"
                >
                  {lesson.studentName} · {lesson.title}
                </Text>
              ))}
            </ScrollView>
            <Text className="mb-2 font-bold text-slate-700">Lý do xin nghỉ</Text>
            <TextInput
              value={leaveReason}
              onChangeText={setLeaveReason}
              placeholder="Nhập lý do (không bắt buộc)..."
              multiline
              textAlignVertical="top"
              className="mb-4 min-h-24 rounded-xl border border-slate-200 bg-white p-3 text-slate-800"
            />
            <TouchableOpacity
              onPress={submitLeaveRequests}
              disabled={
                selectedLeaveLessons.length === 0 || sendLeaveRequestsMutation.isPending
              }
              className={`items-center rounded-xl py-4 ${
                selectedLeaveLessons.length === 0 || sendLeaveRequestsMutation.isPending
                  ? 'bg-slate-300'
                  : 'bg-orange-500'
              }`}
            >
              <Text className="font-bold text-white">
                {sendLeaveRequestsMutation.isPending
                  ? 'Đang gửi đơn...'
                  : `Gửi ${selectedLeaveLessons.length} đơn xin nghỉ`}
              </Text>
            </TouchableOpacity>
          </View>
        </View>
      </Modal>

      <Modal
        animationType="fade"
        transparent
        visible={showFullCalendar}
        onRequestClose={() => setShowFullCalendar(false)}
      >
        <View className="flex-1 items-center justify-center bg-black/50 p-5">
          <View className="h-[82%] w-full rounded-3xl bg-white p-5 shadow-2xl">
            <View className="mb-4 flex-row items-center justify-between">
              <Text className="text-xl font-black text-slate-800">Chọn ngày học</Text>
              <TouchableOpacity onPress={() => setShowFullCalendar(false)} className="p-2">
                <Ionicons name="close" size={23} color="#64748b" />
              </TouchableOpacity>
            </View>

            <View className="mb-4 flex-row justify-center">
              <View className="mr-5 flex-row items-center">
                <View className="mr-2 h-2 w-2 rounded-full bg-emerald-500" />
                <Text className="text-xs font-medium text-slate-500">Đã học</Text>
              </View>
              <View className="flex-row items-center">
                <View className="mr-2 h-2 w-2 rounded-full bg-orange-500" />
                <Text className="text-xs font-medium text-slate-500">Sắp học</Text>
              </View>
            </View>

            <View className="mb-2 flex-row justify-between px-1">
              {WEEKDAY_LABELS.map((day) => (
                <Text key={day} className="w-[13%] text-center text-xs font-bold text-slate-400">
                  {day}
                </Text>
              ))}
            </View>

            <FlatList
              key={`${selectedDate.getFullYear()}-${selectedDate.getMonth()}`}
              style={{ height: monthPageHeight, flexGrow: 0 }}
              data={calendarMonths}
              keyExtractor={(month) => `${month.getFullYear()}-${month.getMonth()}`}
              initialScrollIndex={12}
              getItemLayout={(_, index) => ({
                length: monthPageHeight,
                offset: monthPageHeight * index,
                index,
              })}
              showsVerticalScrollIndicator={false}
              snapToInterval={monthPageHeight}
              snapToAlignment="start"
              decelerationRate="fast"
              disableIntervalMomentum
              initialNumToRender={1}
              maxToRenderPerBatch={1}
              updateCellsBatchingPeriod={16}
              windowSize={3}
              renderItem={({ item: month }) => {
                const dates = getMonthDates(month);
                return (
                  <View style={{ height: monthPageHeight }} className="px-1">
                    <Text className="mb-3 text-lg font-black capitalize text-slate-800">
                      {formatMonthLabel(month)}
                    </Text>
                    <View className="flex-1 justify-around">
                      {Array.from({ length: 6 }, (_, weekIndex) => (
                      <View key={`${month.getMonth()}-${weekIndex}`} className="flex-row justify-between">
                        {dates.slice(weekIndex * 7, weekIndex * 7 + 7).map(({ date, dateKey, isCurrentMonth }) => {
                          const isActive = selectedDateKey === dateKey;
                          return (
                            <TouchableOpacity
                              key={dateKey}
                              onPress={() => selectDate(date)}
                              className={`h-11 w-[13%] items-center justify-center rounded-xl ${
                                isActive ? 'bg-[#5a64f1]' : 'bg-transparent'
                              }`}
                            >
                              <Text
                                className={`text-base font-black ${
                                  isActive
                                    ? 'text-white'
                                    : isCurrentMonth
                                      ? 'text-slate-700'
                                      : 'text-slate-300'
                                }`}
                              >
                                {date.getDate()}
                              </Text>
                              {renderStatusDot(getDayStatus(dateKey), isActive)}
                            </TouchableOpacity>
                          );
                        })}
                      </View>
                      ))}
                    </View>
                  </View>
                );
              }}
            />
          </View>
        </View>
      </Modal>
    </View>
  );
}
