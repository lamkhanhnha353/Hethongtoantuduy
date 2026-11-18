import { Feather, Ionicons } from '@expo/vector-icons';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback, useEffect, useRef, useState } from 'react';
import type { ComponentRef } from 'react';
import {
  ActivityIndicator,
  FlatList,
  Keyboard,
  Platform,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from 'react-native';
import { SafeAreaView, useSafeAreaInsets } from 'react-native-safe-area-context';

import { AIMessageItem } from '@/components/ai/AIMessageItem';
import { useAIChatRoom } from '@/hooks/useAIChatRoom';
import type { AIChatMessage } from '@/types/aiChat';

const SUGGESTIONS = [
  'Toán Tư Duy là gì?',
  'Khóa học nào phù hợp cho bé 5 tuổi?',
  'Chính sách bảo hành khóa học?',
];

export default function AIChatRoomScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const { id, title: titleParam } = useLocalSearchParams<{
    id: string;
    title?: string;
  }>();
  const conversationId = Number(id);

  const listRef = useRef<FlatList<AIChatMessage>>(null);
  const rootRef = useRef<ComponentRef<typeof View> | null>(null);
  const insetsBottomRef = useRef(insets.bottom);
  // Chiều cao bàn phím: đẩy cả khung tin nhắn + ô nhập lên trên bàn phím
  const [keyboardHeight, setKeyboardHeight] = useState(0);

  const scrollToBottom = useCallback((animated = true) => {
    requestAnimationFrame(() => {
      listRef.current?.scrollToEnd({ animated });
    });
  }, []);

  useEffect(() => {
    insetsBottomRef.current = insets.bottom;
  }, [insets.bottom]);

  useEffect(() => {
    let scrollTimeout: ReturnType<typeof setTimeout> | undefined;

    const scrollToBottomSoon = () => {
      if (scrollTimeout) clearTimeout(scrollTimeout);
      scrollTimeout = setTimeout(() => scrollToBottom(true), 60);
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

      // Đo đáy thật của màn hình rồi trừ đi vị trí đỉnh bàn phím để khung
      // nhập luôn nằm sát ngay trên bàn phím trên cả iOS lẫn Android.
      const applyOverlap = (rootBottom: number | null) => {
        const measured =
          rootBottom !== null && keyboardTop > 0 ? rootBottom - keyboardTop : height;
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
  }, [scrollToBottom]);

  const {
    messages,
    title,
    loading,
    loadError,
    inputText,
    setInputText,
    streaming,
    statusText,
    sendMessage,
    stopStreaming,
  } = useAIChatRoom({
    conversationId,
    initialTitle: titleParam,
    onScrollToBottom: scrollToBottom,
  });

  const renderMessage = ({ item }: { item: AIChatMessage }) => (
    <AIMessageItem
      message={item}
      statusText={item.streaming ? statusText : null}
    />
  );

  const keyboardOpen = keyboardHeight > 0;

  return (
    <SafeAreaView ref={rootRef} className="flex-1 bg-slate-100" edges={['top']}>
      {/* Header */}
      <View className="flex-row items-center border-b border-slate-200 bg-white px-4 py-3">
        <TouchableOpacity
          accessibilityLabel="Quay lại"
          className="mr-3 p-1"
          onPress={() => router.back()}
        >
          <Feather color="#1e293b" name="arrow-left" size={23} />
        </TouchableOpacity>

        <View className="mr-3 h-10 w-10 items-center justify-center rounded-full bg-indigo-100">
          <Ionicons color="#4f46e5" name="sparkles" size={20} />
        </View>

        <View className="flex-1">
          <Text className="text-base font-bold text-slate-800" numberOfLines={1}>
            {title}
          </Text>
          <Text className="text-[11px] text-slate-500" numberOfLines={1}>
            {streaming ? statusText ?? 'Đang trả lời...' : 'Trợ lý AI · Sẵn sàng hỗ trợ'}
          </Text>
        </View>
      </View>

      {/* Body: đẩy lên trên bàn phím trên cả iOS lẫn Android */}
      <View className="flex-1" style={{ paddingBottom: keyboardHeight }}>
        {loading ? (
          <View className="flex-1 items-center justify-center">
            <ActivityIndicator color="#4f46e5" size="large" />
            <Text className="mt-3 text-sm text-slate-500">Đang tải hội thoại...</Text>
          </View>
        ) : loadError ? (
          <View className="flex-1 items-center justify-center px-8">
            <Feather color="#ef4444" name="alert-circle" size={34} />
            <Text className="mt-3 text-center text-sm leading-6 text-slate-600">
              {loadError}
            </Text>
            <TouchableOpacity
              className="mt-4 rounded-xl bg-indigo-600 px-5 py-2.5"
              onPress={() => router.back()}
            >
              <Text className="text-sm font-bold text-white">Quay lại</Text>
            </TouchableOpacity>
          </View>
        ) : (
          <FlatList
            ref={listRef}
            contentContainerStyle={{
              flexGrow: 1,
              justifyContent: 'flex-end',
              paddingHorizontal: 16,
              paddingVertical: 18,
            }}
            data={messages}
            keyboardDismissMode="interactive"
            keyboardShouldPersistTaps="handled"
            keyExtractor={(item, index) => `${item.id}-${index}`}
            ListEmptyComponent={
              <View className="flex-1 items-center justify-center px-2">
                <View className="h-16 w-16 items-center justify-center rounded-full bg-indigo-100">
                  <Ionicons color="#4f46e5" name="sparkles" size={30} />
                </View>
                <Text className="mt-4 text-center text-base font-bold text-slate-800">
                  Xin chào, mình là trợ lý AI
                </Text>
                <Text className="mt-2 text-center text-sm leading-6 text-slate-500">
                  Mình có thể giải đáp về khóa học, lộ trình, học phí và chính sách
                  của Toán Tư Duy.
                </Text>

                <View className="mt-5 w-full gap-2">
                  {SUGGESTIONS.map((suggestion) => (
                    <TouchableOpacity
                      key={suggestion}
                      activeOpacity={0.75}
                      className="rounded-2xl border border-slate-200 bg-white px-4 py-3"
                      onPress={() => setInputText(suggestion)}
                    >
                      <Text className="text-sm font-medium text-slate-700">
                        {suggestion}
                      </Text>
                    </TouchableOpacity>
                  ))}
                </View>
              </View>
            }
            onContentSizeChange={() => scrollToBottom(false)}
            renderItem={renderMessage}
            showsVerticalScrollIndicator={false}
          />
        )}

        {/* Input bar — luôn nằm sát trên bàn phím */}
        <View
          className="flex-row items-end border-t border-slate-200 bg-white px-4 pt-2"
          style={{ paddingBottom: keyboardOpen ? 10 : 10 + insets.bottom }}
        >
          <TextInput
            className="min-h-10 flex-1 rounded-3xl bg-slate-100 px-4 py-2 text-[15px] text-slate-800"
            editable={!loading && !loadError}
            maxLength={2000}
            multiline
            onChangeText={setInputText}
            onFocus={() => scrollToBottom(true)}
            placeholder="Hỏi trợ lý AI..."
            placeholderTextColor="#94a3b8"
            style={{ maxHeight: 120 }}
            textAlignVertical="center"
            value={inputText}
          />

          <TouchableOpacity
            accessibilityLabel={streaming ? 'Dừng trả lời' : 'Gửi câu hỏi'}
            className={`ml-2 h-10 w-10 items-center justify-center rounded-full ${
              streaming ? 'bg-rose-500' : inputText.trim() ? 'bg-indigo-600' : 'bg-slate-200'
            }`}
            disabled={!streaming && !inputText.trim()}
            onPress={streaming ? stopStreaming : () => void sendMessage()}
          >
            <Feather
              color={streaming || inputText.trim() ? '#fff' : '#94a3b8'}
              name={streaming ? 'square' : 'send'}
              size={17}
            />
          </TouchableOpacity>
        </View>
      </View>
    </SafeAreaView>
  );
}
