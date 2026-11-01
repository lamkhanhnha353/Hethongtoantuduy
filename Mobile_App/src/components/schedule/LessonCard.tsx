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
  const leaveLabel = leaveRequest
    ? leaveRequest.status.toUpperCase() === 'PENDING'
      ? 'Đã xin nghỉ · Chờ duyệt'
      : leaveRequest.status.toUpperCase() === 'APPROVED'
        ? 'Đã xin nghỉ · Đã duyệt'
        : 'Đã xin nghỉ · Từ chối'
    : null;

  return (
    <TouchableOpacity
      activeOpacity={0.7}
      onPress={() => onPress(lesson)}
      className="bg-white rounded-3xl p-5 mb-4 shadow-sm border border-slate-100 flex-row"
    >
      {/* Cột Thời Gian */}
      <View className="items-center justify-center pr-4 border-r border-slate-100 mr-4">
        <Text className="text-slate-800 font-black text-lg">{lesson.startLabel}</Text>
        <Text className="text-slate-400 font-medium text-xs mb-1">đến</Text>
        <Text className="text-slate-500 font-bold text-sm">{lesson.endLabel}</Text>
      </View>

      {/* Cột Nội Dung */}
      <View className="flex-1 justify-center">
        <View className="flex-row justify-between items-center mb-1">
          <Text className="text-slate-400 text-xs font-bold uppercase tracking-wider" numberOfLines={1}>
            {lesson.className}
          </Text>
        </View>
        
        <Text className="text-slate-800 font-bold text-base mb-2 leading-5">{lesson.title}</Text>
        
        <View className="flex-row items-center justify-between">
          <Text className="flex-1 text-xs text-slate-500" numberOfLines={1}>
            {[leaveLabel ?? statusLabel, lesson.teacherName ? `GV: ${lesson.teacherName}` : null, lesson.room]
              .filter(Boolean)
              .join(' · ')}
          </Text>
          
          {/* NỔI BẬT TÊN CON nếu đang chọn Tất cả */}
          {activeChildId === 'all' && (
            <Text className="text-[#5a64f1] bg-indigo-50 px-2 py-0.5 rounded text-[11px] font-black uppercase tracking-wide">
              {lesson.studentName}
            </Text>
          )}
        </View>
      </View>
    </TouchableOpacity>
  );
};