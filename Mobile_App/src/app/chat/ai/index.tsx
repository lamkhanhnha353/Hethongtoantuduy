import { Feather, Ionicons } from '@expo/vector-icons';
import { useRouter } from 'expo-router';
import { useCallback } from 'react';
import {
  ActivityIndicator,
  Alert,
  FlatList,
  RefreshControl,
  Text,
  TouchableOpacity,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { formatChatDate } from '@/components/chat/chatDateTime';
import { useAIConversations } from '@/hooks/useAIConversations';
import { toApiError } from '@/services/apiError';
import type { AIConversationSummaryDto } from '@/types/aiChat';

export default function AIChatListScreen() {
  const router = useRouter();
  const {
    conversations,
    loading,
    refreshing,
    creating,
    errorMessage,
    loadConversations,
    refreshConversations,
    createConversation,
    deleteConversation,
  } = useAIConversations();

  const handleCreateConversation = useCallback(async () => {
    try {
      const conversation = await createConversation();

      if (!conversation) {
        return;
      }

      router.push({
        pathname: '/chat/ai/[id]',
        params: { id: String(conversation.id), title: conversation.title },
      });
    } catch (error) {
      Alert.alert('Không thể bắt đầu hội thoại', toApiError(error).message);
    }
  }, [createConversation, router]);

  const handleDeleteConversation = useCallback(
    (conversation: AIConversationSummaryDto) => {
      Alert.alert(
        'Xóa hội thoại',
        `Xóa "${conversation.title}" và toàn bộ tin nhắn bên trong?`,
        [
          { text: 'Hủy', style: 'cancel' },
          {
            text: 'Xóa',
            style: 'destructive',
            onPress: () => {
              void (async () => {
                try {
                  await deleteConversation(conversation.id);
                } catch (error) {
                  Alert.alert('Không thể xóa hội thoại', toApiError(error).message);
                }
              })();
            },
          },
        ],
      );
    },
    [deleteConversation],
  );

  const renderConversation = ({ item }: { item: AIConversationSummaryDto }) => (
    <TouchableOpacity
      activeOpacity={0.75}
      className="mx-4 mb-2 flex-row items-center rounded-2xl border border-slate-200 bg-white px-4 py-3.5"
      delayLongPress={400}
      onLongPress={() => handleDeleteConversation(item)}
      onPress={() =>
        router.push({
          pathname: '/chat/ai/[id]',
          params: { id: String(item.id), title: item.title },
        })
      }
    >
      <View className="mr-3 h-11 w-11 items-center justify-center rounded-full bg-indigo-50">
        <Ionicons color="#4f46e5" name="sparkles" size={19} />
      </View>

      <View className="flex-1">
        <Text className="text-[15px] font-bold text-slate-800" numberOfLines={1}>
          {item.title}
        </Text>
        <Text className="mt-0.5 text-sm text-slate-500" numberOfLines={1}>
          {item.lastMessage || 'Chưa có tin nhắn'}
        </Text>
      </View>

      <View className="ml-3 items-end">
        <Text className="text-xs text-slate-400">
          {formatChatDate(item.lastMessageAt ?? item.createdAt)}
        </Text>
        <View className="mt-1">
          <Feather color="#cbd5e1" name="chevron-right" size={17} />
        </View>
      </View>
    </TouchableOpacity>
  );

  return (
    <SafeAreaView className="flex-1 bg-slate-100" edges={['top']}>
      <View className="flex-row items-center border-b border-slate-200 bg-white px-4 py-3.5">
        <TouchableOpacity
          accessibilityLabel="Quay lại"
          className="mr-3 p-1"
          onPress={() => router.back()}
        >
          <Feather color="#1e293b" name="arrow-left" size={23} />
        </TouchableOpacity>

        <View className="flex-1">
          <Text className="text-lg font-bold text-slate-800">Trợ lý AI</Text>
          <Text className="text-[11px] text-slate-500">
            Hỏi đáp cùng Toán Tư Duy
          </Text>
        </View>

        <TouchableOpacity
          accessibilityLabel="Bắt đầu hội thoại mới"
          className={`h-10 w-10 items-center justify-center rounded-full ${creating ? 'bg-slate-100' : 'bg-indigo-600'
            }`}
          disabled={creating}
          onPress={() => void handleCreateConversation()}
        >
          {creating ? (
            <ActivityIndicator color="#4f46e5" size="small" />
          ) : (
            <Feather color="#fff" name="plus" size={23} />
          )}
        </TouchableOpacity>
      </View>

      {loading ? (
        <View className="flex-1 items-center justify-center">
          <ActivityIndicator color="#4f46e5" size="large" />
          <Text className="mt-3 text-sm text-slate-500">Đang tải hội thoại...</Text>
        </View>
      ) : (
        <FlatList
          contentContainerStyle={{ flexGrow: 1, paddingBottom: 32, paddingTop: 16 }}
          data={conversations}
          keyExtractor={(item) => String(item.id)}
          ListHeaderComponent={
            <View className="px-4">
              <View className="overflow-hidden rounded-3xl bg-indigo-600 p-4">
                <View className="absolute -right-6 -top-8 h-32 w-32 rounded-full bg-white/10" />
                <View className="absolute -bottom-10 right-10 h-24 w-24 rounded-full bg-white/10" />

                <View className="flex-row items-center gap-3">
                  <View className="h-12 w-12 items-center justify-center rounded-2xl bg-white/20">
                    <Ionicons color="#fff" name="sparkles" size={24} />
                  </View>
                  <View className="flex-1">
                    <Text className="text-base font-bold text-white">
                      Trợ lý AI Toán Tư Duy
                    </Text>
                    <Text className="mt-1 text-xs leading-5 text-indigo-100">
                      Hỏi nhanh về khóa học, lịch học, học phí và chính sách.
                    </Text>
                  </View>
                </View>

                <TouchableOpacity
                  activeOpacity={0.8}
                  className="mt-4 flex-row items-center justify-center gap-2 rounded-xl bg-white py-2.5"
                  disabled={creating}
                  onPress={() => void handleCreateConversation()}
                >
                  {creating ? (
                    <ActivityIndicator color="#4f46e5" size="small" />
                  ) : (
                    <Feather color="#4f46e5" name="edit-3" size={15} />
                  )}
                  <Text className="text-sm font-bold text-indigo-700">
                    Bắt đầu trò chuyện mới
                  </Text>
                </TouchableOpacity>
              </View>

              {errorMessage ? (
                <View className="mt-4 rounded-2xl border border-rose-200 bg-rose-50 p-4">
                  <Text className="text-[13px] leading-5 text-rose-800">{errorMessage}</Text>
                  <TouchableOpacity
                    className="mt-3 self-start rounded-xl bg-rose-600 px-4 py-2"
                    onPress={() => void loadConversations()}
                  >
                    <Text className="text-xs font-bold text-white">Thử lại</Text>
                  </TouchableOpacity>
                </View>
              ) : null}

              <View className="mb-3 mt-5 flex-row items-center justify-between">
                <Text className="text-xs font-bold uppercase tracking-wider text-slate-400">
                  Hội thoại gần đây
                </Text>
                <Text className="text-xs font-semibold text-slate-400">
                  {conversations.length} hội thoại
                </Text>
              </View>
            </View>
          }
          ListEmptyComponent={
            <View className="flex-1 items-center justify-center px-8 py-12">
              <View className="mb-4 h-20 w-20 items-center justify-center rounded-full bg-white">
                <Ionicons color="#94a3b8" name="chatbubbles-outline" size={34} />
              </View>
              <Text className="text-center font-semibold text-slate-600">
                Chưa có hội thoại nào
              </Text>
              <Text className="mt-2 text-center text-xs leading-5 text-slate-400">
                Bắt đầu trò chuyện để được trợ lý AI giải đáp về Toán Tư Duy.
              </Text>
            </View>
          }
          refreshControl={
            <RefreshControl
              colors={['#4f46e5']}
              onRefresh={refreshConversations}
              refreshing={refreshing}
              tintColor="#4f46e5"
            />
          }
          renderItem={renderConversation}
          showsVerticalScrollIndicator={false}
        />
      )}
    </SafeAreaView>
  );
}
