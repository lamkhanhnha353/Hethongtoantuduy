import { Ionicons } from '@expo/vector-icons';
import { useRouter } from 'expo-router';
import { ActivityIndicator, FlatList, RefreshControl, TouchableOpacity, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { AlertBanner } from '@/components/AlertBanner';
import { EmptyState } from '@/components/EmptyState';
import { ProfileSubpageHeader } from '@/components/profile/ProfileSubpageHeader';
import { StudentCard } from '@/components/StudentCard';
import { toApiError } from '@/services/apiError';
import { useStudentsQuery } from '@/hooks/useStudents';
import { useSession } from '@/hooks/useSession';

export default function ScreenStudents() {
  const router = useRouter();
  const { session, isHydrated } = useSession();

  if (!isHydrated) {
    return (
      <SafeAreaView className="flex-1 bg-slate-50" edges={['top']}>
        <ProfileSubpageHeader title="Học sinh" onBack={() => router.back()} />
        <View className="flex-1 items-center justify-center">
          <ActivityIndicator size="large" color="#0284c7" />
        </View>
      </SafeAreaView>
    );
  }

  if (session?.role !== 'Parent') {
    return (
      <SafeAreaView className="flex-1 bg-slate-50" edges={['top']}>
        <ProfileSubpageHeader title="Học sinh" onBack={() => router.back()} />
        <View className="px-6 py-6">
          <AlertBanner
            message="Tính năng quản lý học sinh chỉ dành cho tài khoản phụ huynh."
            tone="info"
          />
        </View>
      </SafeAreaView>
    );
  }

  return <ParentStudents />;
}

function ParentStudents() {
  const router = useRouter();
  const { data: students, isPending, isError, error, refetch, isRefetching } = useStudentsQuery();

  const apiError = isError ? toApiError(error) : null;

  return (
    <SafeAreaView className="flex-1 bg-slate-50" edges={['top']}>
      <ProfileSubpageHeader
        onBack={() => router.back()}
        rightAccessory={
          <TouchableOpacity
            accessibilityLabel="Thêm học sinh"
            accessibilityRole="button"
            className="h-10 w-10 items-center justify-center rounded-full bg-sky-600"
            onPress={() => router.push('/students/new')}
          >
            <Ionicons color="#ffffff" name="add" size={22} />
          </TouchableOpacity>
        }
        title="Học sinh"
      />

      <View className="flex-1 px-6">
        {isPending ? (
          <View className="flex-1 items-center justify-center">
            <ActivityIndicator size="large" color="#0284c7" />
          </View>
        ) : apiError ? (
          <AlertBanner message={apiError.message} tone="error" />
        ) : (
          <FlatList
            contentContainerClassName="pb-6"
            data={students}
            keyExtractor={(item) => String(item.id)}
            ItemSeparatorComponent={() => <View className="h-3" />}
            ListEmptyComponent={
              <EmptyState
                actionLabel="Thêm học sinh"
                description="Thêm hồ sơ học sinh để theo dõi tiến độ học tập."
                onAction={() => router.push('/students/new')}
                title="Chưa có học sinh nào"
              />
            }
            refreshControl={
              <RefreshControl
                colors={['#0284c7']}
                onRefresh={() => {
                  void refetch();
                }}
                refreshing={isRefetching}
                tintColor="#0284c7"
              />
            }
            renderItem={({ item }) => (
              <StudentCard
                onPress={() =>
                  router.push({
                    pathname: '/(account)/students/[id]',
                    params: { id: String(item.id) },
                  })
                }
                student={item}
              />
            )}
          />
        )}
      </View>
    </SafeAreaView>
  );
}
