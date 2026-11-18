import { Feather, Ionicons } from '@expo/vector-icons';
import * as ImagePicker from 'expo-image-picker';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback, useEffect, useRef, useState } from 'react';
import type { ComponentRef } from 'react';
import {
  ActivityIndicator,
  Alert,
  FlatList,
  Image,
  Keyboard,
  Platform,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from 'react-native';
import { SafeAreaView, useSafeAreaInsets } from 'react-native-safe-area-context';

import { AIMessageItem } from '@/components/ai/AIMessageItem';
import { ChatImageViewer } from '@/components/chat/ChatImageViewer';
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

  const isNearBottomRef = useRef(true);
  const hasInitialScrollRef = useRef(false);

  const scrollToBottom = useCallback((animated = true, force = false) => {
    if (force || isNearBottomRef.current) {
      requestAnimationFrame(() => {
        listRef.current?.scrollToEnd({ animated });
      });
    }
  }, []);

  useEffect(() => {
    insetsBottomRef.current = insets.bottom;
  }, [insets.bottom]);

  useEffect(() => {
    let scrollTimeout: ReturnType<typeof setTimeout> | undefined;

    const scrollToBottomSoon = () => {
      if (scrollTimeout) clearTimeout(scrollTimeout);
      scrollTimeout = setTimeout(() => scrollToBottom(true, false), 60);
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
    selectedImage,
    setSelectedImage,
    setImageBase64,
    streaming,
    statusText,
    sendMessage,
    stopStreaming,
    loadOlderMessages,
    loadingOlderMessages,
  } = useAIChatRoom({
    conversationId,
    initialTitle: titleParam,
    onScrollToBottom: scrollToBottom,
  });

  useEffect(() => {
    if (!loading && messages.length > 0) {
      hasInitialScrollRef.current = false;
      isNearBottomRef.current = true;
      requestAnimationFrame(() => {
        listRef.current?.scrollToEnd({ animated: false });
      });
      const timer = setTimeout(() => {
        listRef.current?.scrollToEnd({ animated: false });
      }, 60);
      return () => clearTimeout(timer);
    }
  }, [loading, conversationId]);

  const handleSelectImage = async () => {
    Alert.alert('Gửi ảnh bài toán', 'Chọn nguồn ảnh để AI giải đáp', [
      {
        text: 'Chụp ảnh mới',
        onPress: async () => {
          const perm = await ImagePicker.requestCameraPermissionsAsync();
          if (!perm.granted) {
            Alert.alert('Thông báo', 'Cần cấp quyền truy cập máy ảnh để chụp hình.');
            return;
          }
          const result = await ImagePicker.launchCameraAsync({
            allowsEditing: true,
            quality: 0.8,
            base64: true,
          });
          if (!result.canceled && result.assets && result.assets[0]) {
            const asset = result.assets[0];
            setSelectedImage(asset.uri);
            setImageBase64(asset.base64 ? `data:image/jpeg;base64,${asset.base64}` : null);
          }
        },
      },
      {
        text: 'Chọn từ thư viện',
        onPress: async () => {
          const perm = await ImagePicker.requestMediaLibraryPermissionsAsync();
          if (!perm.granted) {
            Alert.alert('Thông báo', 'Cần cấp quyền truy cập thư viện ảnh.');
            return;
          }
          const result = await ImagePicker.launchImageLibraryAsync({
            mediaTypes: ['images'],
            allowsEditing: true,
            quality: 0.8,
            base64: true,
          });
          if (!result.canceled && result.assets && result.assets[0]) {
            const asset = result.assets[0];
            setSelectedImage(asset.uri);
            setImageBase64(asset.base64 ? `data:image/jpeg;base64,${asset.base64}` : null);
          }
        },
      },
      { text: 'Hủy', style: 'cancel' },
    ]);
  };

  const [selectedImageUrl, setSelectedImageUrl] = useState<string | null>(null);

  const renderMessage = ({ item }: { item: AIChatMessage }) => (
    <AIMessageItem
      message={item}
      statusText={item.streaming ? statusText : null}
      onOpenImage={setSelectedImageUrl}
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
              justifyContent: messages.length === 0 ? 'center' : 'flex-end',
              paddingHorizontal: 16,
              paddingVertical: 18,
            }}
            data={messages}
            keyboardDismissMode="interactive"
            keyboardShouldPersistTaps="handled"
            keyExtractor={(item, index) => `${item.id}-${index}`}
            initialNumToRender={20}
            maintainVisibleContentPosition={{ minIndexForVisible: 0 }}
            ListHeaderComponent={
              loadingOlderMessages ? (
                <View className="items-center py-2">
                  <ActivityIndicator color="#4f46e5" size="small" />
                  <Text className="mt-1 text-xs text-slate-400">Đang tải tin nhắn cũ...</Text>
                </View>
              ) : undefined
            }
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
            renderItem={renderMessage}
            showsVerticalScrollIndicator={false}
          />
        )}

        {/* Khung xem trước ảnh đính kèm (nếu có) */}
        {selectedImage ? (
          <View className="flex-row items-center border-t border-slate-200 bg-white px-4 pt-2">
            <View className="relative">
              <Image
                source={{ uri: selectedImage }}
                className="h-16 w-16 rounded-xl border border-indigo-200 bg-slate-100"
              />
              <TouchableOpacity
                accessibilityLabel="Xóa ảnh đã chọn"
                className="absolute -right-2 -top-2 h-6 w-6 items-center justify-center rounded-full bg-rose-500 shadow-sm"
                onPress={() => {
                  setSelectedImage(null);
                  setImageBase64(null);
                }}
              >
                <Feather color="#fff" name="x" size={14} />
              </TouchableOpacity>
            </View>
            <Text className="ml-3 flex-1 text-xs leading-4 text-slate-500">
              Đã đính kèm ảnh bài toán. Nhập thêm câu hỏi hoặc nhấn Gửi để AI phân tích.
            </Text>
          </View>
        ) : null}

        {/* Input bar — luôn nằm sát trên bàn phím */}
        <View
          className="flex-row items-end border-t border-slate-200 bg-white px-3 pt-2"
          style={{ paddingBottom: keyboardOpen ? 10 : 10 + insets.bottom }}
        >
          {/* Nút đính kèm ảnh camera / gallery */}
          <TouchableOpacity
            accessibilityLabel="Đính kèm ảnh bài toán"
            className="mb-1 mr-2 h-10 w-10 items-center justify-center rounded-full bg-slate-100 active:bg-slate-200"
            disabled={streaming}
            onPress={handleSelectImage}
          >
            <Feather color="#6366f1" name="camera" size={20} />
          </TouchableOpacity>

          <TextInput
            className="min-h-10 flex-1 rounded-3xl bg-slate-100 px-4 py-2 text-[15px] text-slate-800"
            editable={!loading && !loadError}
            maxLength={2000}
            multiline
            onChangeText={setInputText}
            onFocus={() => scrollToBottom(true, true)}
            placeholder={
              selectedImage
                ? 'Nhập câu hỏi kèm theo (hoặc để trống)...'
                : 'Hỏi trợ lý AI...'
            }
            placeholderTextColor="#94a3b8"
            style={{ maxHeight: 120 }}
            textAlignVertical="center"
            value={inputText}
          />

          <TouchableOpacity
            accessibilityLabel={streaming ? 'Dừng trả lời' : 'Gửi câu hỏi'}
            className={`ml-2 h-10 w-10 items-center justify-center rounded-full ${
              streaming
                ? 'bg-rose-500'
                : inputText.trim() || selectedImage
                ? 'bg-indigo-600'
                : 'bg-slate-200'
            }`}
            disabled={!streaming && !inputText.trim() && !selectedImage}
            onPress={streaming ? stopStreaming : () => void sendMessage()}
          >
            <Feather
              color={
                streaming || inputText.trim() || selectedImage ? '#fff' : '#94a3b8'
              }
              name={streaming ? 'square' : 'send'}
              size={17}
            />
          </TouchableOpacity>
        </View>
      </View>

      {/* Xem ảnh toàn màn hình (phóng to, lưu vào máy) */}
      <ChatImageViewer
        imageUrl={selectedImageUrl}
        onClose={() => setSelectedImageUrl(null)}
      />
    </SafeAreaView>
  );
}
