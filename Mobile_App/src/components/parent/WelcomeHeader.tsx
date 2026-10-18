import { useState } from 'react';
import { ScrollView, Text, TouchableOpacity, View } from 'react-native';
import { Feather } from '@expo/vector-icons';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { Avatar } from '@/components/Avatar';
import { useProfileQuery } from '@/hooks/useProfile';
import { useSession } from '@/hooks/useSession';
import { useStudentsQuery } from '@/hooks/useStudents';

function shortName(fullName: string): string {
  const parts = fullName.trim().split(/\s+/u).filter(Boolean);
  if (parts.length <= 1) return fullName.trim();
  return parts.slice(1).join(' ');
}

export function WelcomeHeader() {
  const insets = useSafeAreaInsets();
  const { session } = useSession();
  const { data: profile } = useProfileQuery();
  const { data: students } = useStudentsQuery(session?.role === 'Parent');
  const [selectedStudentId, setSelectedStudentId] = useState<number | null>(null);

  const parentName =
    profile?.fullName.trim() || session?.fullName.trim() || session?.phone || '';
  const parentAvatar = profile?.avatar ?? null;

  return (
    <View
      className="bg-indigo-600 relative overflow-hidden"
      style={{ paddingTop: insets.top + 10, paddingBottom: 15 }}
    >
      <View className="absolute -top-16 -right-10 w-48 h-48 rounded-full bg-cyan-300/20 opacity-50" />
      <View className="absolute bottom-0 left-0 w-32 h-32 rounded-full bg-indigo-300/20 opacity-50" />

      {/* Thông tin Phụ huynh & Chuông thông báo */}
      <View className="px-5 flex-row items-center justify-between z-10">
        <View className="flex-row items-center gap-3">
          <Avatar name={parentName || 'PH'} uri={parentAvatar} size={48} />
          <View>
            <Text className="text-xs text-indigo-100 font-medium">Xin chào Phụ huynh,</Text>
            <Text className="text-lg font-bold text-white tracking-tight" numberOfLines={1}>
              {parentName || '...'}
            </Text>
          </View>
        </View>

        <TouchableOpacity
          activeOpacity={0.7}
          className="relative w-10 h-10 rounded-full bg-white/20 items-center justify-center border border-white/30"
        >
          <Feather name="bell" size={20} color="white" />
        </TouchableOpacity>
      </View>

      {/* Thanh trượt chọn con */}
      <ScrollView
        horizontal
        showsHorizontalScrollIndicator={false}
        className="mt-5 z-10"
        contentContainerStyle={{ paddingHorizontal: 20, gap: 10 }}
      >
        <TouchableOpacity
          activeOpacity={0.7}
          className="flex-row items-center gap-2 px-3.5 py-1.5 rounded-full"
          style={
            selectedStudentId === null
              ? {
                  backgroundColor: '#ffffff',
                  shadowColor: '#000000',
                  shadowOffset: { width: 0, height: 1 },
                  shadowOpacity: 0.06,
                  shadowRadius: 2,
                  elevation: 2,
                }
              : {
                  backgroundColor: 'rgba(255,255,255,0.2)',
                  borderWidth: 1,
                  borderColor: 'rgba(255,255,255,0.3)',
                }
          }
          onPress={() => setSelectedStudentId(null)}
        >
          <View
            className="w-5 h-5 rounded-full items-center justify-center"
            style={{
              backgroundColor: selectedStudentId === null ? '#4f46e5' : 'rgba(255,255,255,0.3)',
            }}
          >
            <Text className="text-white text-[10px] font-bold">All</Text>
          </View>
          <Text
            className={`text-xs font-bold ${
              selectedStudentId === null ? 'text-indigo-900' : 'text-white font-semibold'
            }`}
          >
            Tất cả con
          </Text>
        </TouchableOpacity>

        {(students ?? []).map((student) => {
          const isSelected = selectedStudentId === student.id;
          return (
            <TouchableOpacity
              key={student.id}
              activeOpacity={0.7}
              className="flex-row items-center gap-2 px-3.5 py-1.5 rounded-full"
              style={
                isSelected
                  ? {
                      backgroundColor: '#ffffff',
                      shadowColor: '#000000',
                      shadowOffset: { width: 0, height: 1 },
                      shadowOpacity: 0.06,
                      shadowRadius: 2,
                      elevation: 2,
                    }
                  : {
                      backgroundColor: 'rgba(255,255,255,0.2)',
                      borderWidth: 1,
                      borderColor: 'rgba(255,255,255,0.3)',
                    }
              }
              onPress={() => setSelectedStudentId(student.id)}
            >
              <Avatar name={student.fullName} uri={student.avatar} size={20} />
              <Text
                className={`text-xs ${
                  isSelected ? 'text-indigo-900 font-bold' : 'text-white font-semibold'
                }`}
                numberOfLines={1}
              >
                {shortName(student.fullName)}
              </Text>
            </TouchableOpacity>
          );
        })}
      </ScrollView>
    </View>
  );
}
