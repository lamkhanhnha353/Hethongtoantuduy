import type { ComponentProps } from 'react';
import { Pressable, Text, View } from 'react-native';
import { Feather } from '@expo/vector-icons';
import { formatDistanceToNow } from 'date-fns';
import { vi } from 'date-fns/locale';

import type { NotificationResponseDto } from '@/types/notification';
import {
  getNotificationTypeLabel,
  getNotificationTypeStyle,
  NOTIFICATION_TYPE_ICONS,
} from '@/types/notification';

type FeatherName = ComponentProps<typeof Feather>['name'];

type NotificationCardProps = {
  notification: NotificationResponseDto;
  onPress: (notification: NotificationResponseDto) => void;
};

export function NotificationCard({ notification, onPress }: NotificationCardProps) {
  const type = notification.type?.toUpperCase() ?? 'SYSTEM';
  const typeStyle = getNotificationTypeStyle(type);
  const icon = (NOTIFICATION_TYPE_ICONS[type] ?? 'bell') as FeatherName;
  const createdAt = new Date(notification.createdAt);
  const timeLabel = Number.isNaN(createdAt.getTime())
    ? null
    : formatDistanceToNow(createdAt, { addSuffix: true, locale: vi });

  return (
    <Pressable
      accessibilityLabel={notification.title}
      accessibilityRole="button"
      className={`rounded-2xl border p-4 ${
        notification.isRead
          ? 'border-slate-200 bg-white'
          : 'border-sky-200 bg-sky-50/70'
      } active:opacity-80`}
      onPress={() => onPress(notification)}
    >
      <View className="flex-row gap-3">
        <View
          className={`h-10 w-10 items-center justify-center rounded-xl ${typeStyle.bg}`}
        >
          <Feather color={typeStyle.icon} name={icon} size={19} />
        </View>

        <View className="flex-1">
          <View className="flex-row items-start gap-2">
            <Text
              className={`flex-1 text-sm ${
                notification.isRead
                  ? 'font-semibold text-slate-800'
                  : 'font-bold text-slate-900'
              }`}
              numberOfLines={2}
            >
              {notification.title}
            </Text>

            {!notification.isRead ? (
              <View className="mt-1.5 h-2 w-2 rounded-full bg-sky-500" />
            ) : null}
          </View>

          {notification.body ? (
            <Text
              className="mt-1 text-[13px] leading-5 text-slate-600"
              numberOfLines={3}
            >
              {notification.body}
            </Text>
          ) : null}

          <View className="mt-2 flex-row flex-wrap items-center gap-2">
            <Text className="text-[11px] font-semibold text-slate-400">
              {getNotificationTypeLabel(type)}
            </Text>

            {timeLabel ? (
              <>
                <View className="h-1 w-1 rounded-full bg-slate-300" />
                <Text className="text-[11px] text-slate-400">{timeLabel}</Text>
              </>
            ) : null}
          </View>
        </View>
      </View>
    </Pressable>
  );
}
