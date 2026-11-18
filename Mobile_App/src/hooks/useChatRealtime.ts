import { useEffect } from 'react';
import { AppState } from 'react-native';
import { useQuery } from '@tanstack/react-query';

import { API_URL } from '@/config/env';
import { chatConversationsQueryKey } from '@/lib/queryKeys';
import { chatService, type ChatConversation, type ChatMessage } from '@/services/chat.service';
import { chatHubService } from '@/services/chatHub';
import { getAccessToken } from '@/store/authStore';

export function useChatConversationsQuery(enabled = true) {
  return useQuery<ChatConversation[]>({
    queryKey: chatConversationsQueryKey,
    queryFn: async () => {
      const { data } = await chatService.getConversations();
      return Array.isArray(data?.data) ? data.data : [];
    },
    enabled,
    staleTime: 15_000,
  });
}

export function useChatUnreadCount(enabled = true) {
  const query = useChatConversationsQuery(enabled);
  const unreadCount = (query.data ?? []).reduce(
    (total, conversation) => total + Math.max(0, conversation.unreadCount ?? 0),
    0,
  );

  return {
    unreadCount,
    conversations: query.data ?? [],
    refetch: query.refetch,
  };
}

export function useChatRealtime(
  onNewMessage: (message: ChatMessage) => void,
  options?: {
    enabled?: boolean;
  },
) {
  const enabled = options?.enabled ?? true;

  useEffect(() => {
    if (!enabled) {
      return;
    }

    const getToken = async () => getAccessToken();
    let isMounted = true;

    const unsubscribe = chatHubService.onReceiveNewMessage((message) => {
      if (isMounted) {
        onNewMessage(message);
      }
    });

    void chatHubService.startConnection(API_URL, getToken);

    const subscription = AppState.addEventListener('change', (nextAppState) => {
      if (nextAppState === 'active') {
        void chatHubService.startConnection(API_URL, getToken);
      }
    });

    return () => {
      isMounted = false;
      subscription.remove();
      unsubscribe();
    };
  }, [enabled, onNewMessage]);
}
