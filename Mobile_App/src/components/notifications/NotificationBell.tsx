import { useCallback, useState } from 'react';
import { Animated, Text, TouchableOpacity, View } from 'react-native';
import { Feather } from '@expo/vector-icons';
import { useQueryClient } from '@tanstack/react-query';
import { useFocusEffect, useRouter } from 'expo-router';

import { useNotificationRealtime } from '@/hooks/useNotificationRealtime';
import { useNotificationUnreadCountQuery } from '@/hooks/useNotifications';
import { useSession } from '@/hooks/useSession';
import {
  notificationsRootQueryKey,
  notificationUnreadCountQueryKey,
} from '@/lib/queryKeys';
import type {
  NotificationUnreadCountResponseDto,
  RealtimeNotificationDto,
} from '@/types/notification';

type NotificationBellProps = {
  className?: string;
  iconColor?: string;
  iconSize?: number;
  badgeBgColor?: string;
};

export function NotificationBell({
  className = 'relative h-10 w-10 items-center justify-center rounded-full bg-white/20',
  iconColor = 'white',
  iconSize = 18,
  badgeBgColor = 'bg-rose-500',
}: NotificationBellProps = {}) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const { session } = useSession();
  const isParent = session?.role === 'Parent';
  const { data, refetch } = useNotificationUnreadCountQuery(isParent);

  // Hiệu ứng nảy chuông khi nhận thông báo realtime
  const [scaleAnim] = useState(() => new Animated.Value(1));

  const triggerRing = useCallback(() => {
    Animated.sequence([
      Animated.timing(scaleAnim, { toValue: 1.25, duration: 150, useNativeDriver: true }),
      Animated.timing(scaleAnim, { toValue: 0.9, duration: 150, useNativeDriver: true }),
      Animated.timing(scaleAnim, { toValue: 1.1, duration: 150, useNativeDriver: true }),
      Animated.timing(scaleAnim, { toValue: 1, duration: 150, useNativeDriver: true }),
    ]).start();
  }, [scaleAnim]);

  // Lắng nghe thông báo realtime từ SignalR
  const handleNewNotification = useCallback(
    (_notification: RealtimeNotificationDto) => {
      // 1. Hiệu ứng rung chuông
      triggerRing();

      // 2. Tăng ngay số lượng chưa đọc trong cache (optimistic update)
      queryClient.setQueryData<NotificationUnreadCountResponseDto>(
        notificationUnreadCountQueryKey,
        (old) => ({
          unreadCount: (old?.unreadCount ?? 0) + 1,
        }),
      );

      // 3. Đồng bộ lại toàn bộ dữ liệu thông báo từ server
      void queryClient.invalidateQueries({ queryKey: notificationsRootQueryKey });
    },
    [queryClient, triggerRing],
  );

  useNotificationRealtime(handleNewNotification, {
    enabled: isParent,
  });

  // Đồng bộ lại khi focus vào màn hình
  useFocusEffect(
    useCallback(() => {
      if (isParent) {
        void refetch();
      }
    }, [isParent, refetch]),
  );

  if (!isParent) {
    return null;
  }

  const unreadCount = data?.unreadCount ?? 0;

  return (
    <TouchableOpacity
      accessibilityLabel={
        unreadCount > 0 ? `${unreadCount} thông báo chưa đọc` : 'Thông báo'
      }
      accessibilityRole="button"
      activeOpacity={0.7}
      className={className}
      onPress={() => router.push('/notifications')}
    >
      <Animated.View style={{ transform: [{ scale: scaleAnim }] }}>
        <Feather color={iconColor} name="bell" size={iconSize} />
      </Animated.View>

      {unreadCount > 0 ? (
        <View
          className={`absolute -right-1 -top-1 min-w-[18px] items-center justify-center rounded-full border-2 border-indigo-500 ${badgeBgColor} px-1`}
        >
          <Text className="text-[9px] font-bold leading-[14px] text-white">
            {unreadCount > 99 ? '99+' : unreadCount}
          </Text>
        </View>
      ) : null}
    </TouchableOpacity>
  );
}
