import { Redirect, router } from 'expo-router';
import { ActivityIndicator, Text, TouchableOpacity, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Feather } from '@expo/vector-icons';

import { AlertBanner } from '@/components/AlertBanner';
import { Avatar } from '@/components/Avatar';
import { PrimaryButton } from '@/components/PrimaryButton';
import { useProfileQuery } from '@/hooks/useProfile';
import { useLogout, useSession } from '@/hooks/useSession';
import { useStudentsQuery } from '@/hooks/useStudents';
import { toApiError } from '@/services/apiError';

export default function ScreenSelectProfile() {
  const { session, isHydrated } = useSession();
  const logout = useLogout();

  const isParent = session?.role === 'Parent';
  const isReady = isHydrated && session != null;
  const {
    data: profile,
    isPending,
    isError,
    error,
    refetch,
  } = useProfileQuery(isReady);
  const { data: students, isPending: studentsPending } = useStudentsQuery(isParent);

  const displayName =
    profile?.fullName.trim() || session?.fullName.trim() || session?.phone || '';
  const roleLabel = session?.role === 'Teacher' ? 'Giáo viên' : 'Phụ huynh';
  const loadError = isError ? toApiError(error) : null;

  function handleSelectAccount() {
    if (isParent) {
      router.push('/verify-pin');
      return;
    }
    router.replace('/(tabs)/home');
  }

  function handleSelectStudent(id: number) {
    router.push({ pathname: '/students/space', params: { id: String(id) } });
  }

  if (!isHydrated) {
    return (
      <SafeAreaView
        className="flex-1 items-center justify-center bg-slate-50"
        edges={['bottom']}
      >
        <ActivityIndicator size="large" color="#4f46e5" />
        <Text className="mt-3 text-sm text-slate-500">Đang kiểm tra phiên đăng nhập...</Text>
      </SafeAreaView>
    );
  }

  if (!session) {
    return <Redirect href="/login" />;
  }

  return (
    <SafeAreaView className="flex-1 bg-slate-50" edges={['bottom']}>
      <View className="flex-1 bg-slate-50">
        <View className="bg-indigo-600 pt-16 pb-7 px-6 items-center relative overflow-hidden">
          <View className="absolute -top-10 -right-10 w-36 h-36 rounded-full bg-cyan-400/20 opacity-50" />
          <View className="absolute bottom-0 left-0 w-24 h-24 rounded-full bg-indigo-300/20 opacity-50" />

          <View className="mb-3 relative z-10">
            <View className="w-16 h-16 rounded-2xl bg-indigo-500 items-center justify-center shadow-lg">
              <Text className="text-white text-2xl font-black tracking-tight">TT</Text>
            </View>
            <View className="absolute -top-1.5 -right-1.5 w-6 h-6 rounded-full bg-amber-400 items-center justify-center shadow-sm">
              <Text className="text-white text-xs font-bold">★</Text>
            </View>
          </View>
          <Text className="text-xl font-bold text-white tracking-tight z-10">Toán Tư Duy</Text>
          <Text className="text-indigo-100 text-sm mt-1.5 z-10">Chọn hồ sơ để tiếp tục</Text>
        </View>

        <View className="flex-1 px-5 pt-5 pb-8 justify-between z-10">
          <View>
            {loadError ? (
              <View className="mb-4 gap-2">
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

            <TouchableOpacity
              activeOpacity={0.7}
              className="w-full bg-white rounded-2xl p-4 flex-row items-center gap-4 shadow-sm border border-slate-100 mb-3"
              onPress={handleSelectAccount}
            >
              <Avatar name={displayName || 'PH'} uri={profile?.avatar ?? null} size={56} />

              <View className="flex-1">
                <Text className="font-bold text-slate-900 text-[15px]" numberOfLines={1}>
                  {isPending ? '...' : displayName}
                </Text>
                <Text className="text-xs text-slate-500 mt-0.5">{roleLabel}</Text>
              </View>

              {isParent && profile?.hasPin ? (
                <View className="flex-row items-center gap-1.5 bg-indigo-50 px-2.5 py-1.5 rounded-lg">
                  <Feather name="lock" size={12} color="#4f46e5" />
                  <Text className="text-[11px] font-bold text-indigo-600">PIN</Text>
                </View>
              ) : (
                <Feather name="chevron-right" size={20} color="#cbd5e1" />
              )}
            </TouchableOpacity>

            {isParent ? (
              <View>
                <Text className="text-xs font-semibold text-slate-400 uppercase tracking-wide mt-4 mb-2">
                  Học sinh của bạn
                </Text>

                {studentsPending ? (
                  <View className="flex-row items-center gap-2 py-3">
                    <ActivityIndicator size="small" color="#6366f1" />
                    <Text className="text-sm text-slate-500">Đang tải danh sách học sinh...</Text>
                  </View>
                ) : null}

                {!studentsPending && (students?.length ?? 0) === 0 ? (
                  <Text className="text-sm text-slate-500 py-2">
                    Chưa có học sinh nào. Thêm hồ sơ con bạn bên dưới.
                  </Text>
                ) : null}

                {(students ?? []).map((student) => (
                  <TouchableOpacity
                    key={student.id}
                    activeOpacity={0.7}
                    className="w-full bg-white rounded-2xl p-4 flex-row items-center gap-4 shadow-sm border border-slate-100 mb-3"
                    onPress={() => handleSelectStudent(student.id)}
                  >
                    <Avatar name={student.fullName} uri={student.avatar} size={56} />

                    <View className="flex-1">
                      <Text className="font-bold text-slate-900 text-[15px]" numberOfLines={1}>
                        {student.fullName}
                      </Text>
                      <Text className="text-xs text-slate-500 mt-0.5">
                        Học sinh • ⭐ {student.totalPoints} • 🔥 {student.streakDays} ngày
                      </Text>
                    </View>

                    <Feather name="chevron-right" size={20} color="#cbd5e1" />
                  </TouchableOpacity>
                ))}

                <TouchableOpacity
                  activeOpacity={0.7}
                  className="w-full bg-white rounded-2xl p-4 flex-row items-center gap-4 border-2 border-dashed border-indigo-200"
                  onPress={() => router.push('/students/new')}
                >
                  <View className="w-14 h-14 rounded-full bg-indigo-50 items-center justify-center">
                    <Feather name="plus" size={24} color="#6366f1" />
                  </View>
                  <View className="flex-1">
                    <Text className="font-semibold text-indigo-700 text-[15px]">
                      Thêm hồ sơ học sinh
                    </Text>
                    <Text className="text-xs text-slate-400 mt-0.5">Tạo hồ sơ mới cho con</Text>
                  </View>
                </TouchableOpacity>
              </View>
            ) : null}
          </View>

          <View className="mt-auto pt-4">
            <TouchableOpacity
              className="w-full flex-row items-center justify-center gap-2 bg-rose-500 py-3.5 rounded-2xl shadow-sm"
              activeOpacity={0.8}
              onPress={() => {
                void logout().then(() => router.replace('/login'));
              }}
            >
              <Feather name="log-out" size={18} color="white" />
              <Text className="text-white font-semibold text-sm">Đăng xuất</Text>
            </TouchableOpacity>
          </View>
        </View>
      </View>
    </SafeAreaView>
  );
}
