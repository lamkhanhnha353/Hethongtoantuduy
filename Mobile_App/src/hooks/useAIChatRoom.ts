import { useCallback, useEffect, useRef, useState } from 'react';
import { aiChatService } from '@/services/aiChat.service';
import { toApiError } from '@/services/apiError';
import type { AIChatMessage, AIChatStreamEvent } from '@/types/aiChat';

type UseAIChatRoomParams = {
  conversationId: number;
  initialTitle?: string;
  onScrollToBottom?: (animated?: boolean) => void;
};

export function useAIChatRoom({
  conversationId,
  initialTitle,
  onScrollToBottom,
}: UseAIChatRoomParams) {
  const invalidConversationId =!Number.isInteger(conversationId) || conversationId < 1;
  const abortRef = useRef<AbortController | null>(null);
  const nextOptimisticIdRef = useRef(-1);
  const mountedRef = useRef(true);
  const streamingRef = useRef(false);

  const [messages, setMessages] = useState<AIChatMessage[]>([]);
  const [title, setTitle] = useState(initialTitle || 'Trợ lý AI');
  const [loading, setLoading] = useState(!invalidConversationId);
  const [loadError, setLoadError] = useState<string | null>(  invalidConversationId ? 'Mã hội thoại không hợp lệ.' : null, );
  
  const [inputText, setInputText] = useState('');
  const [streaming, setStreaming] = useState(false);
  const [statusText, setStatusText] = useState<string | null>(null);

  const scrollToBottom = useCallback(
    (animated = true) => {
      onScrollToBottom?.(animated);
    },
    [onScrollToBottom],
  );

  useEffect(() => {
    mountedRef.current = true;

    return () => {
      mountedRef.current = false;
      abortRef.current?.abort();
    };
  }, []);

  useEffect(() => {
    let active = true;

    if (invalidConversationId) {
      return () => {
        active = false;
      };
    }

    Promise.all([
      aiChatService.getConversation(conversationId),
      aiChatService.getMessages(conversationId),
    ])
      .then(([conversation, messageList]) => {
        if (!active) {
          return;
        }

        setTitle(conversation.title);
        setMessages(messageList.items);
        setLoadError(null);
        scrollToBottom(false);
      })
      .catch((error) => {
        if (active) {
          setLoadError(toApiError(error).message);
        }
      })
      .finally(() => {
        if (active) {
          setLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [conversationId, invalidConversationId, scrollToBottom]);

  const sendMessage = useCallback(async () => {
    const prompt = inputText.trim();

    if (
      !prompt ||
      streaming ||
      streamingRef.current ||
      !Number.isInteger(conversationId) ||
      conversationId < 1
    ) {
      return;
    }

    streamingRef.current = true;

    const createdAt = new Date().toISOString();
    nextOptimisticIdRef.current -= 1;
    const userMessageId = nextOptimisticIdRef.current;
    nextOptimisticIdRef.current -= 1;
    const assistantMessageId = nextOptimisticIdRef.current;

    const userMessage: AIChatMessage = {
      id: userMessageId,
      conversationId,
      role: 'user',
      content: prompt,
      tokenCount: 0,
      sources: [],
      status: 'done',
      createdAt,
    };
    const assistantMessage: AIChatMessage = {
      id: assistantMessageId,
      conversationId,
      role: 'assistant',
      content: '',
      tokenCount: 0,
      sources: [],
      status: 'done',
      createdAt,
      pending: true,
      streaming: true,
    };

    setInputText('');
    setMessages((current) => [...current, userMessage, assistantMessage]);
    setStreaming(true);
    setStatusText('Đang tìm tài liệu...');
    scrollToBottom(false);

    const controller = new AbortController();
    abortRef.current = controller;
    let answer = '';
    let terminalEvent = false;

    const patchAssistant = (patch: Partial<AIChatMessage>) => {
      if (!mountedRef.current) {
        return;
      }

      setMessages((current) =>
        current.map((message) =>
          message.id === assistantMessageId ? { ...message, ...patch } : message,
        ),
      );
    };

    const handleEvent = (event: AIChatStreamEvent) => {
      if (!mountedRef.current) {
        return;
      }

      if (event.event === 'status') {
        setStatusText(
          event.data.stage === 'summarizing'
            ? 'Đang tóm tắt hội thoại...'
            : 'Đang tìm tài liệu...',
        );
        return;
      }

      if (event.event === 'sources') {
        patchAssistant({ sources: event.data.sources });
        return;
      }

      if (event.event === 'delta') {
        answer += event.data.content;
        patchAssistant({ content: answer });
        return;
      }

      if (event.event === 'done') {
        terminalEvent = true;
        answer = event.data.content || answer;
        patchAssistant({
          // Không thay đổi ID để FlatList không unmount/remount item (tránh blank flash)
          content: answer || 'Không nhận được phản hồi từ trợ lý AI.',
          pending: false,
          streaming: false,
          error: !answer,
        });

        if (event.data.title) {
          setTitle(event.data.title);
        }

        setStatusText(null);
        return;
      }

      terminalEvent = true;
      patchAssistant({
        content: answer ? `${answer}\n\n${event.data.message}` : event.data.message,
        pending: false,
        streaming: false,
        error: true,
      });
      setStatusText(null);
    };

    try {
      await aiChatService.streamChat({
        prompt,
        conversationId,
        signal: controller.signal,
        onEvent: handleEvent,
      });

      if (!terminalEvent) {
        patchAssistant({
          content: answer || 'Không nhận được phản hồi từ trợ lý AI.',
          pending: false,
          streaming: false,
          error: !answer,
          status: answer ? 'partial' : 'done',
        });
      }
    } catch (error) {
      if (controller.signal.aborted) {
        patchAssistant({
          content: answer || 'Đã dừng trả lời.',
          pending: false,
          streaming: false,
          status: answer ? 'partial' : 'done',
        });
      } else {
        const errorMessage = toApiError(error).message;
        patchAssistant({
          content: answer ? `${answer}\n\n${errorMessage}` : errorMessage,
          pending: false,
          streaming: false,
          error: true,
          status: answer ? 'partial' : 'done',
        });
      }
    } finally {
      streamingRef.current = false;
      abortRef.current = null;
      if (mountedRef.current) {
        setStreaming(false);
        setStatusText(null);
        scrollToBottom(true);
      }
    }
  }, [conversationId, inputText, scrollToBottom, streaming]);

  const stopStreaming = useCallback(() => {
    abortRef.current?.abort();
  }, []);

  return {
    messages,
    title,
    loading,
    loadError,
    invalidConversationId,
    inputText,
    setInputText,
    streaming,
    statusText,
    sendMessage,
    stopStreaming,
  };
}
