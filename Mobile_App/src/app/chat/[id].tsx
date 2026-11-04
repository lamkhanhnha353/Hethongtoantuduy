import { Feather } from '@expo/vector-icons';
import { useLocalSearchParams, useRouter } from 'expo-router';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { useCallback, useEffect, useRef, useState } from 'react';
import type { ComponentRef } from 'react';
import {
  ActivityIndicator,
  AppState,
  Alert,
  FlatList,
  Keyboard,
  Platform,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from 'react-native';
import { SafeAreaView, useSafeAreaInsets } from 'react-native-safe-area-context';

import { API_URL } from '@/config/env';
import { useSession } from '@/hooks/useSession';
import { chatService, type ChatAttachment, type ChatMessage } from '@/services/chat.service';
import { toApiError } from '@/services/apiError';

import { getChatTimestamp } from './chatDateTime';
import { ChatAttachmentPicker } from './components/ChatAttachmentPicker';
import { ChatImageViewer } from './components/ChatImageViewer';
import { ChatMessageItem } from './components/ChatMessageItem';

const CHAT_PAGE_SIZE = 30;

function mergeMessage(messages: ChatMessage[], message: ChatMessage): ChatMessage[] {
  const updated = messages.filter((item) => item.id !== message.id);
  updated.push(message);
  return updated.sort((left, right) => {
    const timeDifference = getChatTimestamp(left.createdAt) - getChatTimestamp(right.createdAt);
    if (timeDifference) return timeDifference;
    if (left.id < 0 && right.id < 0) return right.id - left.id;
    return left.id - right.id;
  });
}

function belongsToSameMessageGroup(current: ChatMessage, next?: ChatMessage) {
  if (!next || current.senderType !== next.senderType || current.senderId !== next.senderId) {
    return false;
  }

  const elapsed = getChatTimestamp(next.createdAt) - getChatTimestamp(current.createdAt);
  return elapsed >= 0 && elapsed <= 5 * 60 * 1000;
}

export default function ChatRoomScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const { id, partnerId, partnerName } = useLocalSearchParams<{
    id: string;
    partnerId: string;
    partnerName?: string;
  }>();
  const { session } = useSession();
  const conversationId = Number(id);
  const receiverId = Number(partnerId);
  const listRef = useRef<FlatList<ChatMessage>>(null);
  const inputRef = useRef<ComponentRef<typeof TextInput> | null>(null);
  const rootRef = useRef<ComponentRef<typeof View> | null>(null);
  const insetsBottomRef = useRef(insets.bottom);
  const nextPageRef = useRef(2);
  const hasMoreMessagesRef = useRef(false);
  const loadingOlderMessagesRef = useRef(false);
  const messagesLoadedRef = useRef(false);
  const historyGenerationRef = useRef(0);
  const pendingHubMessagesRef = useRef<ChatMessage[]>([]);
  const pendingDeletedMessageIdsRef = useRef(new Set<number>());
  const optimisticIdRef = useRef(0);
  const lastOptimisticTimestampRef = useRef(0);
  const textSendQueueRef = useRef<Promise<void>>(Promise.resolve());

  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadedConversationId, setLoadedConversationId] = useState<number | null>(null);
  const [loadingOlderMessages, setLoadingOlderMessages] = useState(false);
  const [inputText, setInputText] = useState('');
  const inputTextRef = useRef(inputText);
  const [inputHeight, setInputHeight] = useState(40);
  const [uploadingAttachment, setUploadingAttachment] = useState(false);
  const [hubConnected, setHubConnected] = useState(false);
  const [selectedImageUrl, setSelectedImageUrl] = useState<string | null>(null);
  // Chiều cao bàn phím: đẩy cả khung nhập lên trên bàn phím (giống Zalo)
  const [keyboardHeight, setKeyboardHeight] = useState(0);
  const isNearBottomRef = useRef(true);
  const hasInitialScrollRef = useRef(false);

  useEffect(() => {
    insetsBottomRef.current = insets.bottom;
  }, [insets.bottom]);

  useEffect(() => {
    const focusTimer = setTimeout(() => inputRef.current?.focus(), 250);
    return () => clearTimeout(focusTimer);
  }, []);

  const updateInputText = useCallback((value: string) => {
    inputTextRef.current = value;
    setInputText(value);
  }, []);

  const loadOlderMessages = useCallback(async () => {
    if (
      !messagesLoadedRef.current ||
      !hasMoreMessagesRef.current ||
      loadingOlderMessagesRef.current
    ) {
      return;
    }

    const generation = historyGenerationRef.current;
    loadingOlderMessagesRef.current = true;
    setLoadingOlderMessages(true);
    try {
      const { data } = await chatService.getMessages(conversationId, nextPageRef.current);
      if (generation !== historyGenerationRef.current) return;
      nextPageRef.current += 1;
      hasMoreMessagesRef.current = data.data.length === CHAT_PAGE_SIZE;
      setMessages((current) => data.data.reduce(mergeMessage, current));
    } catch (error) {
      if (generation === historyGenerationRef.current) {
        Alert.alert('Lỗi tải tin nhắn cũ', toApiError(error).message);
      }
    } finally {
      if (generation === historyGenerationRef.current) {
        loadingOlderMessagesRef.current = false;
        setLoadingOlderMessages(false);
      }
    }
  }, [conversationId]);

  const createOptimisticMessage = useCallback(
    (message: Omit<ChatMessage, 'id' | 'createdAt' | 'pending'>): ChatMessage => {
      const timestamp = Math.max(Date.now(), lastOptimisticTimestampRef.current + 1);
      lastOptimisticTimestampRef.current = timestamp;
      optimisticIdRef.current -= 1;
      return {
        ...message,
        id: optimisticIdRef.current,
        createdAt: new Date(timestamp).toISOString(),
        pending: true,
      };
    },
    [],
  );

  useEffect(() => {
    let scrollTimeout: ReturnType<typeof setTimeout> | undefined;

    const scrollToBottomSoon = () => {
      isNearBottomRef.current = true;
      if (scrollTimeout) clearTimeout(scrollTimeout);
      scrollTimeout = setTimeout(() => {
        listRef.current?.scrollToEnd({ animated: true });
      }, 60);
    };

    const showEvent = Platform.OS === 'ios' ? 'keyboardWillChangeFrame' : 'keyboardDidShow';
    const hideEvent = Platform.OS === 'ios' ? 'keyboardWillHide' : 'keyboardDidHide';

    const showSub = Keyboard.addListener(showEvent, (event) => {
      const height = event.endCoordinates?.height ?? 0;
      const keyboardTop = event.endCoordinates?.screenY ?? 0;

      if (height <= 0) {
        setKeyboardHeight(0);
        return;
      }

      // Đo đáy thật của màn hình rồi trừ đi vị trí đỉnh bàn phím:
      // khung nhập luôn nằm sát ngay trên bàn phím, không phụ thuộc
      // việc Android báo chiều cao thiếu/thừa phần thanh điều hướng.
      const applyOverlap = (rootBottom: number | null) => {
        const measured =
          rootBottom !== null && keyboardTop > 0 ? rootBottom - keyboardTop : height;
        // Android (edge-to-edge) báo chiều cao bàn phím thiếu phần thanh điều hướng,
        // nên lấy giá trị lớn hơn giữa số đo thật và (chiều cao + inset đáy) cho chắc.
        const androidSafe = height + insetsBottomRef.current;
        const overlap = Platform.OS === 'android' ? Math.max(measured, androidSafe) : measured;
        setKeyboardHeight(Math.max(0, Math.round(overlap)));
        scrollToBottomSoon();
      };

      if (rootRef.current) {
        rootRef.current.measureInWindow((_x, y, _w, h) => applyOverlap(y + h));
      } else {
        applyOverlap(null);
      }
    });
    const hideSub = Keyboard.addListener(hideEvent, () => {
      setKeyboardHeight(0);
    });

    return () => {
      showSub.remove();
      hideSub.remove();
      if (scrollTimeout) clearTimeout(scrollTimeout);
    };
  }, []);

  useEffect(() => {
    let active = true;
    historyGenerationRef.current += 1;
    nextPageRef.current = 2;
    hasMoreMessagesRef.current = false;
    loadingOlderMessagesRef.current = false;
    messagesLoadedRef.current = false;
    pendingHubMessagesRef.current = [];
    pendingDeletedMessageIdsRef.current.clear();
    hasInitialScrollRef.current = false;
    isNearBottomRef.current = true;

    chatService
      .getMessages(conversationId)
      .then(({ data }) => {
        if (active) {
          nextPageRef.current = 2;
          hasMoreMessagesRef.current = data.data.length === CHAT_PAGE_SIZE;
          const visibleMessages = data.data
            .filter((message) => !pendingDeletedMessageIdsRef.current.has(message.id))
            .reduce(mergeMessage, pendingHubMessagesRef.current);
          pendingHubMessagesRef.current = [];
          pendingDeletedMessageIdsRef.current.clear();
          messagesLoadedRef.current = true;
          setLoadedConversationId(conversationId);
          setMessages(visibleMessages);
          requestAnimationFrame(() => {
            requestAnimationFrame(() => {
              listRef.current?.scrollToEnd({ animated: false });
              isNearBottomRef.current = true;
              hasInitialScrollRef.current = true;
            });
          });
        }
      })
      .catch((error: unknown) => {
        if (active) {
          setMessages([]);
          setLoadedConversationId(conversationId);
          Alert.alert('Lỗi tải tin nhắn', toApiError(error).message);
        }
      })
      .finally(() => {
        if (active) {
          setLoading(false);
          setLoadingOlderMessages(false);
        }
      });

    return () => {
      active = false;
    };
  }, [conversationId]);

  useEffect(() => {
    const token = session?.accessToken;
    if (!token || !Number.isInteger(conversationId)) return;

    let disposed = false;
    let connection: HubConnection | undefined;
    let startPromise: Promise<void> | null = null;

    const startConnection = async () => {
      if (
        disposed ||
        AppState.currentState !== 'active' ||
        !connection ||
        connection.state !== HubConnectionState.Disconnected ||
        startPromise
      ) {
        return;
      }

      startPromise = connection.start()
        .then(() => {
          if (!disposed) setHubConnected(true);
        })
        .catch((error: unknown) => {
          if (!disposed && AppState.currentState === 'active') {
            console.warn('Không thể kết nối SignalR:', error);
          }
          if (!disposed) setHubConnected(false);
        })
        .finally(() => {
          startPromise = null;
        });

      await startPromise;
    };

    const createConnection = () => {
      connection = new HubConnectionBuilder()
        .withUrl(`${API_URL}/chathub`, { accessTokenFactory: () => token })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.None)
        .build();

      connection.on('ReceiveNewMessage', (message: ChatMessage) => {
        if (message.conversationId === conversationId) {
          if (messagesLoadedRef.current) {
            setMessages((current) => mergeMessage(current, message));
          } else {
            pendingHubMessagesRef.current = mergeMessage(pendingHubMessagesRef.current, message);
          }
        }
      });
      connection.on('MessageDeleted', (deletedConversationId: number, messageId: number) => {
        if (deletedConversationId === conversationId) {
          if (messagesLoadedRef.current) {
            setMessages((current) => current.filter((message) => message.id !== messageId));
          } else {
            pendingDeletedMessageIdsRef.current.add(messageId);
          }
        }
      });
      connection.onreconnected(() => {
        if (!disposed) setHubConnected(true);
      });
      connection.onreconnecting(() => {
        if (!disposed) setHubConnected(false);
      });
      connection.onclose(() => {
        if (!disposed) setHubConnected(false);
      });

      void startConnection();
    };

    createConnection();
    const appStateSubscription = AppState.addEventListener('change', (state) => {
      if (state === 'active') void startConnection();
      else setHubConnected(false);
    });

    return () => {
      disposed = true;
      appStateSubscription.remove();
      if (connection) {
        connection.off('ReceiveNewMessage');
        connection.off('MessageDeleted');
        void connection.stop();
      }
    };
  }, [conversationId, session?.accessToken]);

  const handleSendMessage = useCallback(() => {
    const content = inputTextRef.current.trim();
    if (!content || !Number.isInteger(receiverId) || receiverId < 1) return;

    isNearBottomRef.current = true;
    const optimisticMessage = createOptimisticMessage({
      conversationId,
      senderType: session?.role ?? 'Parent',
      senderId: session?.profileId ?? null,
      messageType: 'TEXT',
      content,
      attachmentUrl: null,
      isRead: false,
    });
    const localId = optimisticMessage.id;
    updateInputText('');
    setMessages((current) => mergeMessage(current, optimisticMessage));
    requestAnimationFrame(() => {
      requestAnimationFrame(() => listRef.current?.scrollToEnd({ animated: false }));
    });
    textSendQueueRef.current = textSendQueueRef.current.then(async () => {
      try {
        const { data } = await chatService.sendMessage({
          receiverId,
          receiverType: 'TEACHER',
          messageType: 'TEXT',
          content,
        });
        setMessages((current) => {
          const withoutOptimistic = current.filter((message) => message.id !== localId);
          return mergeMessage(withoutOptimistic, {
            ...data.data,
            createdAt: optimisticMessage.createdAt,
          });
        });
      } catch (error) {
        setMessages((current) => current.filter((message) => message.id !== localId));
        if (!inputTextRef.current && optimisticIdRef.current === localId) {
          updateInputText(content);
        }
        Alert.alert('Không gửi được tin nhắn', toApiError(error).message);
      }
    });
  }, [
    conversationId,
    createOptimisticMessage,
    receiverId,
    session?.profileId,
    session?.role,
    updateInputText,
  ]);

  const handleDeleteMessage = useCallback(async (messageId: number) => {
    try {
      await chatService.deleteMessage(conversationId, messageId);
      setMessages((current) => current.filter((message) => message.id !== messageId));
    } catch (error) {
      Alert.alert('Không thể xóa tin nhắn', toApiError(error).message);
    }
  }, [conversationId]);

  const sendAttachment = useCallback(async (attachment: ChatAttachment) => {
    if (uploadingAttachment || !Number.isInteger(receiverId) || receiverId < 1) return;

    isNearBottomRef.current = true;
    const optimisticMessage = createOptimisticMessage({
      conversationId,
      senderType: session?.role ?? 'Parent',
      senderId: session?.profileId ?? null,
      messageType: attachment.type.startsWith('image/') ? 'IMAGE' : 'FILE',
      content: attachment.name,
      attachmentUrl: attachment.uri,
      isRead: false,
    });
    const localId = optimisticMessage.id;
    setUploadingAttachment(true);
    setMessages((current) => mergeMessage(current, optimisticMessage));
    requestAnimationFrame(() => {
      requestAnimationFrame(() => listRef.current?.scrollToEnd({ animated: false }));
    });
    try {
      const { data } = await chatService.sendAttachment(receiverId, 'TEACHER', attachment);
      setMessages((current) => {
        const withoutOptimistic = current.filter((message) => message.id !== localId);
        return mergeMessage(withoutOptimistic, data.data);
      });
    } catch (error) {
      setMessages((current) => current.filter((message) => message.id !== localId));
      Alert.alert('Không gửi được tệp', toApiError(error).message);
    } finally {
      setUploadingAttachment(false);
    }
  }, [
    conversationId,
    createOptimisticMessage,
    receiverId,
    session?.profileId,
    session?.role,
    uploadingAttachment,
  ]);

  const keyboardOpen = keyboardHeight > 0;
  const isLoadingConversation = loading || loadedConversationId !== conversationId;

  return (
    // Chỉ chừa safe-area phía trên; phía dưới tự xử lý theo bàn phím
    <SafeAreaView ref={rootRef} className="flex-1 bg-slate-100" edges={['top']}>
      <View className="z-10 flex-row items-center border-b border-slate-200 bg-white px-4 py-3">
        <TouchableOpacity
          onPress={() => router.back()}
          className="mr-3 p-1"
          accessibilityLabel="Quay lại"
        >
          <Feather name="arrow-left" size={23} color="#1e293b" />
        </TouchableOpacity>
        <View className="mr-3 h-10 w-10 items-center justify-center rounded-full bg-indigo-100">
          <Text className="text-sm font-bold text-indigo-700">
            {partnerName?.slice(0, 2).toUpperCase() || 'GV'}
          </Text>
        </View>
        <View className="flex-1">
          <Text className="text-base font-bold text-slate-800" numberOfLines={1}>
            {partnerName || 'Giáo viên'}
          </Text>
          <Text className={`text-xs ${hubConnected ? 'text-emerald-600' : 'text-slate-400'}`}>
            {hubConnected ? 'Đang kết nối' : 'Đang kết nối lại...'}
          </Text>
        </View>
        <TouchableOpacity
          onPress={() =>
            router.push({
              pathname: '/chat/info',
              params: {
                conversationId: String(conversationId),
                partnerName: partnerName || 'Giáo viên',
              },
            })
          }
          className="ml-2 h-10 w-10 items-center justify-center rounded-full bg-indigo-50"
          accessibilityLabel="Thông tin hội thoại và tệp đã chia sẻ"
        >
          <Text className="text-xl font-bold text-indigo-600">!</Text>
        </TouchableOpacity>
      </View>

      {/* paddingBottom = chiều cao bàn phím → danh sách + khung nhập luôn nằm trên bàn phím */}
      <View className="flex-1" style={{ paddingBottom: keyboardHeight }}>
        {isLoadingConversation ? (
          <View className="flex-1 items-center justify-center">
            <ActivityIndicator size="large" color="#4f46e5" />
          </View>
        ) : (
          <FlatList
            ref={listRef}
            data={messages}
            keyExtractor={(item) => item.id.toString()}
            renderItem={({ item, index }) => (
              <ChatMessageItem
                item={item}
                isMine={item.senderType.toUpperCase() === session?.role.toUpperCase()}
                showTime={!belongsToSameMessageGroup(item, messages[index + 1])}
                onOpenImage={setSelectedImageUrl}
                onDelete={(messageId) => void handleDeleteMessage(messageId)}
              />
            )}
            ListHeaderComponent={
              loadingOlderMessages ? (
                <View className="items-center py-3">
                  <ActivityIndicator size="small" color="#4f46e5" />
                </View>
              ) : undefined
            }
            contentContainerStyle={{
              flexGrow: 1,
              justifyContent: 'flex-end',
              paddingHorizontal: 16,
              paddingVertical: 18,
            }}
            onScroll={(event) => {
              const { contentOffset, contentSize, layoutMeasurement } = event.nativeEvent;
              isNearBottomRef.current =
                contentSize.height - contentOffset.y - layoutMeasurement.height < 100;
              if (
                contentOffset.y <= 60 &&
                contentSize.height > layoutMeasurement.height + 40
              ) {
                void loadOlderMessages();
              }
            }}
            scrollEventThrottle={100}
            maintainVisibleContentPosition={{ minIndexForVisible: 0 }}
            onContentSizeChange={() => {
              if (!hasInitialScrollRef.current || isNearBottomRef.current) {
                listRef.current?.scrollToEnd({ animated: hasInitialScrollRef.current });
              }
              hasInitialScrollRef.current = true;
            }}
            onLayout={() => {
              if (isNearBottomRef.current) {
                listRef.current?.scrollToEnd({ animated: false });
              }
            }}
            keyboardShouldPersistTaps="handled"
            showsVerticalScrollIndicator={false}
            ListEmptyComponent={
              <View className="flex-1 items-center justify-center">
                <Feather name="message-circle" size={34} color="#94a3b8" />
                <Text className="mt-2 text-sm text-slate-500">Bắt đầu cuộc trò chuyện</Text>
              </View>
            }
          />
        )}

        <View
          className="flex-row items-end border-t border-slate-200 bg-white px-5 pt-2"
          style={{ paddingBottom: keyboardOpen ? 10 : 10 + insets.bottom }}
        >
          <ChatAttachmentPicker disabled={uploadingAttachment} onSelect={sendAttachment} />
          <TextInput
            ref={inputRef}
            className="min-h-10 flex-1 rounded-3xl bg-slate-100 px-4 py-2 text-[15px] text-slate-800"
            style={{ height: inputHeight, maxHeight: 128 }}
            placeholder="Tin nhắn"
            placeholderTextColor="#94a3b8"
            value={inputText}
            onChangeText={updateInputText}
            onFocus={() => {
              isNearBottomRef.current = true;
              requestAnimationFrame(() => listRef.current?.scrollToEnd({ animated: true }));
            }}
            onContentSizeChange={({ nativeEvent }) => {
              // contentSize đã gồm padding nên không cộng thêm, tránh ô nhập bị cao thừa
              const nextHeight = Math.min(128, Math.max(40, nativeEvent.contentSize.height));
              setInputHeight((current) =>
                Math.abs(current - nextHeight) > 2 ? nextHeight : current,
              );
              if (isNearBottomRef.current) {
                requestAnimationFrame(() => listRef.current?.scrollToEnd({ animated: false }));
              }
            }}
            multiline
            scrollEnabled={inputHeight >= 128}
            textAlignVertical={inputHeight > 48 ? 'top' : 'center'}
            maxLength={5000}
            editable
            returnKeyType="default"
          />
          <TouchableOpacity
            onPress={() => void handleSendMessage()}
            disabled={!inputText.trim()}
            className={`ml-2 h-10 w-10 items-center justify-center rounded-full ${
              inputText.trim() ? 'bg-indigo-600' : 'bg-slate-200'
            }`}
            accessibilityLabel="Gửi tin nhắn"
          >
            <Feather
              name="send"
              size={17}
              color={inputText.trim() ? '#fff' : '#94a3b8'}
            />
          </TouchableOpacity>
        </View>
      </View>
      <ChatImageViewer
        imageUrl={selectedImageUrl}
        onClose={() => setSelectedImageUrl(null)}
      />
    </SafeAreaView>
  );
}