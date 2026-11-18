import React from 'react';
import { View, Text, TouchableOpacity } from 'react-native';
import type { LeaveRequestDto, StudentScheduleLesson } from '@/types/lesson';

interface LessonCardProps {
  lesson: StudentScheduleLesson;
  activeChildId: string | number;
  leaveRequest?: LeaveRequestDto;
  onPress: (lesson: StudentScheduleLesson) => void;
}

export const LessonCard: React.FC<LessonCardProps> = ({
  lesson,
  activeChildId,
  leaveRequest,
  onPress,
}) => {
  const status = lesson.status.toUpperCase();
  const statusLabel =
    status === 'COMPLETED' ? 'Đã học' : status === 'SCHEDULED' ? 'Sắp học' : 'Buổi học';
  const leaveStatus = leaveRequest?.status.toUpperCase() ?? null;
  const isApprovedLeave = leaveStatus === 'APPROVED';
  const isPendingLeave = leaveStatus === 'PENDING';
  const isRejectedLeave = leaveStatus === 'REJECTED';

  const leaveBadge = isApprovedLeave
    ? {
        label: 'Nghỉ có phép',
        container: 'bg-rose-600',
        text: 'text-white',
      }
    : isPendingLeave
      ? {
          label: 'Chờ duyệt nghỉ',
          container: 'bg-amber-100 border border-amber-300',
          text: 'text-amber-800',
        }
      : isRejectedLeave
        ? {
            label: 'Từ chối nghỉ',
            container: 'bg-slate-200',
            text: 'text-slate-700',
          }
        : null;

  const cardSurfaceClass = isApprovedLeave
    ? 'bg-rose-50 border-rose-300'
    : isPendingLeave
      ? 'bg-amber-50/50 border-amber-200'
      : 'bg-white border-slate-100';

  const timeDividerClass = isApprovedLeave
    ? 'border-rose-200'
    : isPendingLeave
      ? 'border-amber-200'
      : 'border-slate-100';

  return (
    <TouchableOpacity
      activeOpacity={0.7}
      onPress={() => onPress(lesson)}
      className={`rounded-3xl p-5 mb-4 shadow-sm border flex-row ${cardSurfaceClass}`}
    >
      {/* Cột Thời Gian */}
      <View className={`items-center justify-center pr-4 border-r mr-4 ${timeDividerClass}`}>
        <Text
          className={`font-black text-lg ${
            isApprovedLeave ? 'text-rose-700' : 'text-slate-800'
          }`}
        >
          {lesson.startLabel}
        </Text>
        <Text
          className={`font-medium text-xs mb-1 ${
            isApprovedLeave ? 'text-rose-400' : 'text-slate-400'
          }`}
        >
          đến
        </Text>
        <Text
          className={`font-bold text-sm ${
            isApprovedLeave ? 'text-rose-600' : 'text-slate-500'
          }`}
        >
          {lesson.endLabel}
        </Text>
      </View>

      {/* Cột Nội Dung */}
      <View className="flex-1 justify-center">
        <View className="flex-row justify-between items-center gap-2 mb-1">
          <Text
            className={`flex-1 text-xs font-bold uppercase tracking-wider ${
              isApprovedLeave ? 'text-rose-500' : 'text-slate-400'
            }`}
            numberOfLines={1}
          >
            {lesson.className}
          </Text>

          {leaveBadge ? (
            <View className={`px-2.5 py-0.5 rounded-full ${leaveBadge.container}`}>
              <Text className={`text-[10px] font-bold ${leaveBadge.text}`}>
                {leaveBadge.label}
              </Text>
            </View>
          ) : null}
        </View>

        <Text
          className={`font-bold text-base mb-2 leading-5 ${
            isApprovedLeave ? 'text-rose-900' : 'text-slate-800'
          }`}
        >
          {lesson.title}
        </Text>

        <View className="flex-row items-center justify-between gap-2">
          <Text
            className={`flex-1 text-xs ${
              isApprovedLeave ? 'text-rose-700/80' : 'text-slate-500'
            }`}
            numberOfLines={1}
          >
            {[
              leaveBadge ? null : statusLabel,
              lesson.teacherName ? `GV: ${lesson.teacherName}` : null,
              lesson.room,
            ]
              .filter(Boolean)
              .join(' · ')}
          </Text>

          {/* NỔI BẬT TÊN CON nếu đang chọn Tất cả */}
          {activeChildId === 'all' && (
            <Text
              className={`px-2 py-0.5 rounded text-[11px] font-black uppercase tracking-wide ${
                isApprovedLeave
                  ? 'text-rose-700 bg-rose-100'
                  : 'text-[#5a64f1] bg-indigo-50'
              }`}
            >
              {lesson.studentName}
            </Text>
          )}
        </View>
      </View>
    </TouchableOpacity>
  );
};