import type { ComponentProps } from 'react';
import { useCallback, useEffect, useRef, useState } from 'react';
import { Animated, Pressable, Text, View } from 'react-native';
import { Feather } from '@expo/vector-icons';
import { useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'expo-router';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { useNotificationRealtime } from '@/hooks/useNotificationRealtime';
import { useSession } from '@/hooks/useSession';
import {
  notificationsRootQueryKey,
  notificationUnreadCountQueryKey,
} from '@/lib/queryKeys';
import type {
  NotificationUnreadCountResponseDto,
  RealtimeNotificationDto,
} from '@/types/notification';
import {
  getNotificationTypeLabel,
  getNotificationTypeStyle,
  NOTIFICATION_TYPE_ICONS,
} from '@/types/notification';

type FeatherName = ComponentProps<typeof Feather>['name'];

const AUTO_DISMISS_MS = 4500;

export function RealtimeNotificationBanner() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const queryClient = useQueryClient();
  const { session } = useSession();
  const isParent = session?.role === 'Parent';

  const [banner, setBanner] = useState<RealtimeNotificationDto | null>(null);
  const translateY = useRef(new Animated.Value(-160)).current;
  const opacity = useRef(new Animated.Value(0)).current;
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const dismissBanner = useCallback(() => {
    if (timerRef.current) {
      clearTimeout(timerRef.current);
      timerRef.current = null;
    }

    Animated.parallel([
      Animated.timing(translateY, {
        toValue: -160,
        duration: 220,
        useNativeDriver: true,
      }),
      Animated.timing(opacity, {
        toValue: 0,
        duration: 200,
        useNativeDriver: true,
      }),
    ]).start(() => {
      setBanner(null);
    });
  }, [opacity, translateY]);

  const handleNewNotification = useCallback(
    (notification: RealtimeNotificationDto) => {
      // Cập nhật đồng bộ số chưa đọc và danh sách thông báo trên toàn ứng dụng
      queryClient.setQueryData<NotificationUnreadCountResponseDto>(
        notificationUnreadCountQueryKey,
        (old) => ({
          unreadCount: (old?.unreadCount ?? 0) + 1,
        }),
      );
      void queryClient.invalidateQueries({ queryKey: notificationsRootQueryKey });

      if (timerRef.current) {
        clearTimeout(timerRef.current);
      }

      setBanner(notification);
      translateY.setValue(-160);
      opacity.setValue(0);

      Animated.parallel([
        Animated.spring(translateY, {
          toValue: 0,
          useNativeDriver: true,
          bounciness: 6,
        }),
        Animated.timing(opacity, {
          toValue: 1,
          duration: 180,
          useNativeDriver: true,
        }),
      ]).start();

      timerRef.current = setTimeout(() => {
        dismissBanner();
      }, AUTO_DISMISS_MS);
    },
    [dismissBanner, opacity, queryClient, translateY],
  );

  useNotificationRealtime(handleNewNotification, {
    enabled: isParent,
  });

  useEffect(() => {
    return () => {
      if (timerRef.current) {
        clearTimeout(timerRef.current);
      }
    };
  }, []);

  if (!isParent || !banner) {
    return null;
  }

  const normalizedType = banner.type?.toUpperCase() ?? 'SYSTEM';
  const typeStyle = getNotificationTypeStyle(normalizedType);
  const iconName = (NOTIFICATION_TYPE_ICONS[normalizedType] ?? 'bell') as FeatherName;

  const handlePress = () => {
    const currentType = normalizedType;
    const lowerTitle = (banner.title ?? '').toLowerCase();
    dismissBanner();

    if (currentType === 'TUITION' || currentType === 'PAYMENT') {
      router.push('/invoices');
      return;
    }

    if (
      currentType === 'LEAVE_REQUEST' ||
      (currentType === 'ATTENDANCE' && lowerTitle.includes('đơn xin nghỉ'))
    ) {
      router.push('/leave-requests');
      return;
    }

    if (currentType === 'ENROLLMENT' || currentType === 'ATTENDANCE') {
      router.push('/(tabs)/schedule');
      return;
    }

    router.push('/(tabs)/notifications');
  };

  return (
    <Animated.View
      pointerEvents="box-none"
      style={{
        position: 'absolute',
        top: Math.max(insets.top, 12) + 6,
        left: 14,
        right: 14,
        zIndex: 9999,
        elevation: 20,
        opacity,
        transform: [{ translateY }],
      }}
    >
      <Pressable
        onPress={handlePress}
        className="flex-row items-start gap-3 rounded-2xl border border-slate-200 bg-white p-3.5"
        style={{
          shadowColor: '#0f172a',
          shadowOffset: { width: 0, height: 8 },
          shadowOpacity: 0.16,
          shadowRadius: 16,
          elevation: 12,
        }}
      >
        <View
          className={`h-10 w-10 items-center justify-center rounded-xl ${typeStyle.bg}`}
        >
          <Feather color={typeStyle.icon} name={iconName} size={19} />
        </View>

        <View className="flex-1">
          <View className="flex-row items-center justify-between gap-2">
            <Text className="text-[11px] font-bold uppercase tracking-wide text-sky-600">
              {getNotificationTypeLabel(normalizedType)}
            </Text>
            <Pressable
              hitSlop={10}
              onPress={(event) => {
                event.stopPropagation();
                dismissBanner();
              }}
            >
              <Feather color="#94a3b8" name="x" size={16} />
            </Pressable>
          </View>

          <Text
            className="mt-0.5 text-[14px] font-bold text-slate-900"
            numberOfLines={1}
          >
            {banner.title}
          </Text>

          {banner.body ? (
            <Text
              className="mt-0.5 text-[12px] leading-4 text-slate-600"
              numberOfLines={2}
            >
              {banner.body}
            </Text>
          ) : null}
        </View>
      </Pressable>
    </Animated.View>
  );
}
