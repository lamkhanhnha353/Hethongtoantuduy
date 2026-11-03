import { useCallback } from 'react';
import { Text, TouchableOpacity, View } from 'react-native';
import { Feather } from '@expo/vector-icons';
import { useFocusEffect, useRouter } from 'expo-router';

import { useNotificationUnreadCountQuery } from '@/hooks/useNotifications';
import { useSession } from '@/hooks/useSession';

export function NotificationBell() {
  const router = useRouter();
  const { session } = useSession();
  const isParent = session?.role === 'Parent';
  const { data, refetch } = useNotificationUnreadCountQuery(isParent);

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
      className="relative h-10 w-10 items-center justify-center rounded-full bg-white/20"
      onPress={() => router.push('/notifications')}
    >
      <Feather color="white" name="bell" size={18} />

      {unreadCount > 0 ? (
        <View className="absolute -right-1 -top-1 min-w-[18px] items-center justify-center rounded-full border-2 border-indigo-500 bg-rose-500 px-1">
          <Text className="text-[9px] font-bold leading-[14px] text-white">
            {unreadCount > 99 ? '99+' : unreadCount}
          </Text>
        </View>
      ) : null}
    </TouchableOpacity>
  );
}
