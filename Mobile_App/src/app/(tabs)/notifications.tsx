import { useCallback, useMemo, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  FlatList,
  RefreshControl,
  ScrollView,
  Text,
  TouchableOpacity,
  View,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useFocusEffect, useRouter } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';

import { AlertBanner } from '@/components/AlertBanner';
import { EmptyState } from '@/components/EmptyState';
import { NotificationCard } from '@/components/notifications/NotificationCard';
import { useNotificationRealtime } from '@/hooks/useNotificationRealtime';
import {
  useMarkAllNotificationsReadMutation,
  useMarkNotificationReadMutation,
  useNotificationsQuery,
  useNotificationUnreadCountQuery,
  type NotificationFilters,
} from '@/hooks/useNotifications';
import { useSession } from '@/hooks/useSession';
import { toApiError } from '@/services/apiError';
import type { NotificationResponseDto, NotificationType } from '@/types/notification';

type NotificationFilterValue = 'ALL' | 'UNREAD' | NotificationType;

const FILTERS: { value: NotificationFilterValue; label: string }[] = [
  { value: 'ALL', label: 'Tất cả' },
  { value: 'UNREAD', label: 'Chưa đọc' },
  { value: 'TUITION', label: 'Học phí' },
  { value: 'ATTENDANCE', label: 'Điểm danh' },
  { value: 'ACADEMIC', label: 'Học tập' },
  { value: 'SYSTEM', label: 'Hệ thống' },
  { value: 'ANNOUNCEMENT', label: 'Chung' },
];

export default function NotificationsScreen() {
  const { session, isHydrated } = useSession();

  if (!isHydrated) {
    return (
      <SafeAreaView className="flex-1 items-center justify-center bg-slate-50" edges={['top']}>
        <ActivityIndicator color="#0284c7" size="large" />
      </SafeAreaView>
    );
  }

  if (session?.role !== 'Parent') {
    return (
      <SafeAreaView className="flex-1 bg-slate-50 px-6 py-6" edges={['top']}>
        <Text className="text-2xl font-bold text-slate-900">Thông báo</Text>
        <View className="mt-4">
          <AlertBanner
            message="Tính năng thông báo chỉ dành cho tài khoản phụ huynh."
            tone="info"
          />
        </View>
      </SafeAreaView>
    );
  }

  return <ParentNotifications />;
}

function ParentNotifications() {
  const router = useRouter();
  const [filter, setFilter] = useState<NotificationFilterValue>('ALL');
  const [isRefreshing, setIsRefreshing] = useState(false);

  const filters = useMemo<NotificationFilters>(
    () => ({
      unreadOnly: filter === 'UNREAD',
      type: filter === 'ALL' || filter === 'UNREAD' ? null : filter,
    }),
    [filter],
  );

  const notificationsQuery = useNotificationsQuery(filters);
  const unreadCountQuery = useNotificationUnreadCountQuery();
  const markReadMutation = useMarkNotificationReadMutation();
  const markAllMutation = useMarkAllNotificationsReadMutation();

  const refetchNotifications = notificationsQuery.refetch;
  const refetchUnreadCount = unreadCountQuery.refetch;

  useFocusEffect(
    useCallback(() => {
      void refetchNotifications();
      void refetchUnreadCount();
    }, [refetchNotifications, refetchUnreadCount]),
  );

  useNotificationRealtime(
    useCallback(() => {
      void refetchNotifications();
      void refetchUnreadCount();
    }, [refetchNotifications, refetchUnreadCount]),
  );

  const notifications = useMemo(
    () => notificationsQuery.data?.pages.flatMap((page) => page.items) ?? [],
    [notificationsQuery.data],
  );

  const unreadCount =
    unreadCountQuery.data?.unreadCount ??
    notificationsQuery.data?.pages[0]?.unreadCount ??
    0;

  const queryError = notificationsQuery.isError
    ? toApiError(notificationsQuery.error)
    : null;
  const markReadError = markReadMutation.isError
    ? toApiError(markReadMutation.error)
    : null;
  const markAllError = markAllMutation.isError
    ? toApiError(markAllMutation.error)
    : null;
  const actionError = markAllError ?? markReadError;

  const handleRefresh = useCallback(async () => {
    setIsRefreshing(true);
    try {
      await Promise.all([refetchNotifications(), refetchUnreadCount()]);
    } finally {
      setIsRefreshing(false);
    }
  }, [refetchNotifications, refetchUnreadCount]);

  const hasNextPage = notificationsQuery.hasNextPage;
  const isFetchingNextPage = notificationsQuery.isFetchingNextPage;
  const fetchNextPage = notificationsQuery.fetchNextPage;

  const handleLoadMore = useCallback(() => {
    if (hasNextPage && !isFetchingNextPage) {
      void fetchNextPage();
    }
  }, [fetchNextPage, hasNextPage, isFetchingNextPage]);

  const handleMarkAllRead = useCallback(() => {
    if (unreadCount === 0 || markAllMutation.isPending) {
      return;
    }

    Alert.alert(
      'Đọc tất cả thông báo',
      'Đánh dấu tất cả thông báo là đã đọc?',
      [
        { text: 'Hủy', style: 'cancel' },
        { text: 'Đọc tất cả', onPress: () => markAllMutation.mutate() },
      ],
    );
  }, [markAllMutation, unreadCount]);

  const handleNotificationPress = useCallback(
    (notification: NotificationResponseDto) => {
      if (!notification.isRead) {
        markReadMutation.mutate(notification.id);
      }

      const type = notification.type?.toUpperCase() ?? '';
      const refId = notification.refId ?? '';

      if (type === 'TUITION' || refId.startsWith('INVOICE:')) {
        router.push('/invoices');
        return;
      }

      if (type === 'ATTENDANCE' && refId.startsWith('LEAVE_REQUEST:')) {
        router.push('/leave-requests');
        return;
      }

      if (
        (type === 'ATTENDANCE' && refId.startsWith('LESSON:')) ||
        type === 'ACADEMIC'
      ) {
        router.push('/(tabs)/schedule');
      }
    },
    [markReadMutation, router],
  );

  const emptyTitle =
    filter === 'UNREAD'
      ? 'Không có thông báo chưa đọc'
      : filter === 'ALL'
        ? 'Chưa có thông báo nào'
        : 'Không có thông báo phù hợp';
  const emptyDescription =
    filter === 'ALL'
      ? 'Thông báo về học phí, điểm danh và học tập sẽ xuất hiện tại đây.'
      : 'Thử chọn bộ lọc khác để xem thông báo.';

  return (
    <SafeAreaView className="flex-1 bg-slate-50" edges={['top']}>
      <View className="flex-row items-center border-b border-slate-200 bg-white px-4 py-4">
        <View className="h-10 w-10 items-center justify-center rounded-full bg-sky-50">
          <Ionicons color="#0284c7" name="notifications-outline" size={22} />
        </View>

        <View className="ml-3 flex-1">
          <Text className="text-lg font-black text-slate-800">Thông báo</Text>
          <Text className="text-[11px] text-slate-500">
            {unreadCount > 0 ? `${unreadCount} thông báo chưa đọc` : 'Bạn đã xem hết thông báo'}
          </Text>
        </View>

        <TouchableOpacity
          accessibilityLabel="Đọc tất cả thông báo"
          accessibilityRole="button"
          className={`rounded-xl px-3 py-2 ${
            unreadCount > 0 ? 'bg-sky-50' : 'bg-slate-100'
          }`}
          disabled={unreadCount === 0 || markAllMutation.isPending}
          onPress={handleMarkAllRead}
        >
          {markAllMutation.isPending ? (
            <ActivityIndicator color="#0284c7" size="small" />
          ) : (
            <Text
              className={`text-[11px] font-bold ${
                unreadCount > 0 ? 'text-sky-700' : 'text-slate-400'
              }`}
            >
              Đọc tất cả
            </Text>
          )}
        </TouchableOpacity>
      </View>

      <View className="px-4 pt-4">
        <ScrollView
          contentContainerStyle={{ gap: 8 }}
          horizontal
          showsHorizontalScrollIndicator={false}
        >
          {FILTERS.map((item) => {
            const isSelected = filter === item.value;

            return (
              <TouchableOpacity
                key={item.value}
                accessibilityRole="button"
                className={`rounded-full border px-4 py-2 ${
                  isSelected
                    ? 'border-sky-600 bg-sky-600'
                    : 'border-slate-200 bg-white'
                }`}
                onPress={() => setFilter(item.value)}
              >
                <Text
                  className={`text-[12px] font-bold ${
                    isSelected ? 'text-white' : 'text-slate-600'
                  }`}
                >
                  {item.label}
                </Text>
              </TouchableOpacity>
            );
          })}
        </ScrollView>
      </View>

      {actionError ? (
        <View className="px-4 pt-3">
          <AlertBanner message={actionError.message} tone="error" />
        </View>
      ) : null}

      {queryError ? (
        <View className="mx-4 mt-4 rounded-2xl border border-rose-200 bg-rose-50 p-4">
          <Text className="mb-3 text-[13px] font-medium leading-5 text-rose-800">
            {queryError.message}
          </Text>
          <TouchableOpacity
            className="self-start rounded-xl bg-rose-600 px-4 py-2"
            onPress={() => {
              void refetchNotifications();
            }}
          >
            <Text className="text-[12px] font-bold text-white">Thử lại</Text>
          </TouchableOpacity>
        </View>
      ) : notificationsQuery.isPending ? (
        <View className="flex-1 items-center justify-center">
          <ActivityIndicator color="#0284c7" size="large" />
          <Text className="mt-3 text-sm text-slate-500">Đang tải thông báo...</Text>
        </View>
      ) : (
        <FlatList
          contentContainerStyle={{
            paddingBottom: 32,
            paddingHorizontal: 16,
            paddingTop: 16,
          }}
          data={notifications}
          ItemSeparatorComponent={() => <View className="h-3" />}
          keyExtractor={(item) => String(item.id)}
          ListEmptyComponent={
            <EmptyState description={emptyDescription} title={emptyTitle} />
          }
          ListFooterComponent={
            notificationsQuery.isFetchingNextPage ? (
              <View className="items-center py-4">
                <ActivityIndicator color="#0284c7" size="small" />
              </View>
            ) : undefined
          }
          onEndReached={handleLoadMore}
          onEndReachedThreshold={0.4}
          refreshControl={
            <RefreshControl
              colors={['#0284c7']}
              onRefresh={() => {
                void handleRefresh();
              }}
              refreshing={isRefreshing}
              tintColor="#0284c7"
            />
          }
          renderItem={({ item }) => (
            <NotificationCard notification={item} onPress={handleNotificationPress} />
          )}
          showsVerticalScrollIndicator={false}
        />
      )}
    </SafeAreaView>
  );
}
