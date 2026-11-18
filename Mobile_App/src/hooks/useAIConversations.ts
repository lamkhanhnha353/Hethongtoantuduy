import { useFocusEffect } from 'expo-router';
import { useCallback, useRef, useState } from 'react';

import { aiChatService } from '@/services/aiChat.service';
import { toApiError } from '@/services/apiError';
import type { AIConversationDto, AIConversationSummaryDto } from '@/types/aiChat';

export function useAIConversations() {
  const [conversations, setConversations] = useState<AIConversationSummaryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [creating, setCreating] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const creatingRef = useRef(false);

  const loadConversations = useCallback(async () => {
    try {
      const data = await aiChatService.listConversations();
      setConversations(data.items);
      setErrorMessage(null);
    } catch (error) {
      setErrorMessage(toApiError(error).message);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  const refreshConversations = useCallback(() => {
    setRefreshing(true);
    void loadConversations();
  }, [loadConversations]);

  useFocusEffect(
    useCallback(() => {
      void loadConversations();
    }, [loadConversations]),
  );

  const createConversation = useCallback(async (): Promise<AIConversationDto | null> => {
    if (creatingRef.current) {
      return null;
    }

    creatingRef.current = true;
    setCreating(true);
    try {
      return await aiChatService.createConversation();
    } finally {
      creatingRef.current = false;
      setCreating(false);
    }
  }, []);

  const deleteConversation = useCallback(async (conversationId: number) => {
    await aiChatService.deleteConversation(conversationId);
    setConversations((current) => current.filter((item) => item.id !== conversationId));
  }, []);

  return {
    conversations,
    loading,
    refreshing,
    creating,
    errorMessage,
    loadConversations,
    refreshConversations,
    createConversation,
    deleteConversation,
  };
}
