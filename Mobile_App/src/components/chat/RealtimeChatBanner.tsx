import { useCallback, useEffect, useRef, useState } from 'react';
import { Animated, Pressable, Text, View } from 'react-native';
import { Feather } from '@expo/vector-icons';
import { useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'expo-router';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { useChatRealtime } from '@/hooks/useChatRealtime';
import { useNotificationSettingsQuery } from '@/hooks/useNotifications';
import { useSession } from '@/hooks/useSession';
import { chatConversationsQueryKey, notificationSettingsQueryKey } from '@/lib/queryKeys';
import type { ChatConversation, ChatMessage } from '@/services/chat.service';
import { chatHubService } from '@/services/chatHub';
import type { NotificationSettingDto } from '@/types/notification';

type ChatBannerPayload = {
  conversationId: number;
  partnerId?: number;
  partnerName: string;
  preview: string;
};

const AUTO_DISMISS_MS = 4500;

function formatMessagePreview(message: ChatMessage): string {
  const type = message.messageType?.toUpperCase();
  if (type === 'IMAGE') {
    return '📷 Đã gửi một hình ảnh';
  }
  if (type === 'FILE') {
    return '📎 Đã gửi một tệp đính kèm';
  }
  if (type === 'RECALLED') {
    return 'Tin nhắn đã được thu hồi';
  }
  return message.content?.trim() || 'Bạn có tin nhắn mới';
}

export function RealtimeChatBanner() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const queryClient = useQueryClient();
  const { session } = useSession();
  const isParent = session?.role === 'Parent';

  // Tải sẵn cài đặt thông báo để kiểm tra bật/tắt banner tin nhắn
  useNotificationSettingsQuery(isParent);

  const [banner, setBanner] = useState<ChatBannerPayload | null>(null);
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

  const handleNewMessage = useCallback(
    (message: ChatMessage) => {
      const isViewingRoom =
        chatHubService.getActiveConversationId() === message.conversationId;
      const preview = formatMessagePreview(message);

      // Cập nhật lạc quan danh sách hội thoại: đẩy lên đầu & tăng số chưa đọc ngay lập tức
      const cachedConversations = queryClient.getQueryData<ChatConversation[]>(
        chatConversationsQueryKey,
      );
      const matchedConversation = cachedConversations?.find(
        (conv) => conv.id === message.conversationId,
      );

      if (cachedConversations && matchedConversation) {
        const updatedConversation: ChatConversation = {
          ...matchedConversation,
          lastMessage: preview,
          lastMessageAt: message.createdAt || new Date().toISOString(),
          unreadCount: isViewingRoom
            ? 0
            : Math.max(0, matchedConversation.unreadCount ?? 0) + 1,
        };
        const others = cachedConversations.filter(
          (conv) => conv.id !== message.conversationId,
        );
        queryClient.setQueryData<ChatConversation[]>(chatConversationsQueryKey, [
          updatedConversation,
          ...others,
        ]);
      }

      // Đồng bộ lại từ server để đảm bảo chính xác tuyệt đối (nhất là với hội thoại mới)
      void queryClient.invalidateQueries({ queryKey: chatConversationsQueryKey });

      // Không hiện banner nếu phụ huynh đang mở sẵn đúng phòng chat này
      if (isViewingRoom) {
        return;
      }

      // Tôn trọng cài đặt bật/tắt thông báo chung và thông báo tin nhắn giáo viên
      const cachedSettings = queryClient.getQueryData<NotificationSettingDto[]>(
        notificationSettingsQueryKey,
      );
      if (cachedSettings) {
        const masterSetting = cachedSettings.find(
          (item) => item.type.toUpperCase() === 'IN_APP_NOTIFICATION',
        );
        const chatSetting = cachedSettings.find(
          (item) => item.type.toUpperCase() === 'CHAT',
        );
        if (masterSetting?.enabled === false || chatSetting?.enabled === false) {
          return;
        }
      }

      if (timerRef.current) {
        clearTimeout(timerRef.current);
      }

      setBanner({
        conversationId: message.conversationId,
        partnerId: matchedConversation?.partnerId,
        partnerName: matchedConversation?.partnerName || 'Giáo viên',
        preview,
      });

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

  useChatRealtime(handleNewMessage, {
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

  const handlePress = () => {
    const target = banner;
    dismissBanner();

    if (target.partnerId) {
      router.push({
        pathname: '/chat/[id]',
        params: {
          id: String(target.conversationId),
          partnerId: String(target.partnerId),
          partnerName: target.partnerName,
        },
      });
      return;
    }

    router.push('/chat');
  };

  return (
    <Animated.View
      pointerEvents="box-none"
      style={{
        position: 'absolute',
        top: Math.max(insets.top, 12) + 6,
        left: 14,
        right: 14,
        zIndex: 9998,
        elevation: 19,
        opacity,
        transform: [{ translateY }],
      }}
    >
      <Pressable
        onPress={handlePress}
        className="flex-row items-start gap-3 rounded-2xl border border-fuchsia-200 bg-white p-3.5"
        style={{
          shadowColor: '#0f172a',
          shadowOffset: { width: 0, height: 8 },
          shadowOpacity: 0.16,
          shadowRadius: 16,
          elevation: 12,
        }}
      >
        <View className="h-10 w-10 items-center justify-center rounded-xl bg-fuchsia-100">
          <Feather color="#c026d3" name="message-circle" size={20} />
        </View>

        <View className="flex-1">
          <View className="flex-row items-center justify-between gap-2">
            <Text className="text-[11px] font-bold uppercase tracking-wide text-fuchsia-600">
              Tin nhắn mới
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
            {banner.partnerName}
          </Text>

          <Text
            className="mt-0.5 text-[12px] leading-4 text-slate-600"
            numberOfLines={2}
          >
            {banner.preview}
          </Text>
        </View>
      </Pressable>
    </Animated.View>
  );
}
