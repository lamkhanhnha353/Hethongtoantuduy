import { View, Text, TouchableOpacity } from 'react-native';
import { Feather } from '@expo/vector-icons';
import { useRouter } from 'expo-router';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useProfileQuery } from '@/hooks/useProfile';
import { useSession } from '@/hooks/useSession';
import { Avatar } from '@/components/Avatar';
import { useStudentsQuery } from '@/hooks/useStudents';

import { NotificationBell } from '@/components/notifications/NotificationBell';

export function WelcomeHeader() {
  const { session } = useSession();
  const { data: profile } = useProfileQuery(!!session);
  const displayName = profile?.fullName.trim() || session?.fullName.trim() || session?.phone || '';
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const isParent = session?.role === 'Parent';
  const { data: students, isPending: studentsPending } = useStudentsQuery(isParent);

  function handleSelectStudent(id: number) {
    router.push({ pathname: '/students/space', params: { id: String(id) } });
  }


  return (

    <View
      // Giữ lại nền màu cũ 
      className="bg-indigo-500 pb-6 px-4 rounded-b-3xl"
      style={{ paddingTop: insets.top + 16 }}
    >
      {/* Hàng ngang duy nhất chứa: Nút Back + Avatar + Info + Chuông */}
      <View className="flex-row items-center justify-between">

        {/* Cụm bên trái: Back + Avatar + Text */}
        <View className="flex-row items-center gap-3">
          {/* Nút Trở về */}
          <TouchableOpacity
            activeOpacity={0.7}
            className="w-10 h-10 items-center justify-center rounded-full bg-white/20"
            onPress={() => router.replace('/select-profile')}
          >
            <Feather name="chevron-left" size={24} color="white" />
          </TouchableOpacity>
          {/* <Avatar  /> */}
          <Avatar name={displayName || 'PH'} uri={profile?.avatar ?? null} size={56} />


          {/* Thông tin phụ huynh ( */}
          <View>
            <Text className="text-indigo-100 text-[11px] mb-0.5">Xin chào Phụ huynh,</Text>
            <Text className="text-white font-bold text-[15px]">{displayName || 'PH'}</Text>
          </View>
        </View>

        <NotificationBell />
      </View>

      {/* Bộ lọc hồ sơ con */}
      <View className="flex-row mt-6 gap-2">
        <TouchableOpacity className="bg-white px-3 py-1.5 rounded-full flex-row items-center gap-1.5">
          <View className="w-5 h-5 rounded-full bg-indigo-600 items-center justify-center">
            <Text className="text-white text-[10px] font-bold">All</Text>
          </View>
          <Text className="text-indigo-600 text-xs font-bold">Tất cả con</Text>
        </TouchableOpacity>

        {(students ?? []).map((student) => (
          <TouchableOpacity
            key={student.id}
            activeOpacity={0.7}
            className="bg-indigo-400/50 px-3 py-1.5 rounded-full border border-indigo-300 flex-row items-center gap-1.5"
            onPress={() => handleSelectStudent(student.id)}
          >
            <View className="w-5 h-5 rounded-full bg-teal-500 items-center justify-center">
              <Avatar name={student.fullName || 'student'} uri={student?.avatar ?? null} size={24} />
            </View>
            <Text className="text-white text-xs font-semibold">{student.fullName}</Text>
          </TouchableOpacity>
        ))}
      </View>
    </View>
  );
}