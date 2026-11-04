import { Feather } from '@expo/vector-icons';
import { useEffect, useState } from 'react';
import { ActivityIndicator, Alert, FlatList, Text, TouchableOpacity, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useRouter } from 'expo-router';

import { chatService, type ChatConversation } from '@/services/chat.service';
import { toApiError } from '@/services/apiError';

import { formatChatDate } from './chatDateTime';

export default function ChatListScreen() {
  const router = useRouter();
  const [conversations, setConversations] = useState<ChatConversation[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;

    chatService
      .getConversations()
      .then(({ data }) => {
        if (active) setConversations(data.data);
      })
      .catch((error: unknown) => {
        if (active) Alert.alert('Không tải được tin nhắn', toApiError(error).message);
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => {
      active = false;
    };
  }, []);

  const renderItem = ({ item }: { item: ChatConversation }) => (
    <TouchableOpacity
      activeOpacity={0.75}
      className="flex-row items-center border-b border-slate-100 bg-white px-4 py-3.5"
      onPress={() =>
        router.push({
          pathname: '/chat/[id]',
          params: {
            id: String(item.id),
            partnerId: String(item.partnerId),
            partnerName: item.partnerName,
          },
        })
      }
    >
      <View className="mr-3 h-12 w-12 items-center justify-center rounded-full bg-indigo-100">
        <Text className="text-base font-bold text-indigo-700">
          {item.partnerName?.slice(0, 2).toUpperCase() || 'GV'}
        </Text>
      </View>
      <View className="flex-1">
        <Text className="text-[15px] font-bold text-slate-800" numberOfLines={1}>
          {item.partnerName}
        </Text>
        <Text className="mt-0.5 text-sm text-slate-500" numberOfLines={1}>
          {item.lastMessage || 'Chưa có tin nhắn'}
        </Text>
      </View>
      <View className="ml-3 items-end">
        <Text className="text-xs text-slate-400">
          {item.lastMessageAt ? formatChatDate(item.lastMessageAt) : ''}
        </Text>
        {item.unreadCount > 0 && (
          <View className="mt-1 min-w-5 items-center rounded-full bg-indigo-600 px-1.5 py-0.5">
            <Text className="text-[10px] font-bold text-white">{item.unreadCount}</Text>
          </View>
        )}
      </View>
    </TouchableOpacity>
  );

  if (loading) {
    return (
      <SafeAreaView className="flex-1 items-center justify-center bg-white">
        <ActivityIndicator size="large" color="#4f46e5" />
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView className="flex-1 bg-slate-100" edges={['top']}>
      <View className="flex-row items-center border-b border-slate-200 bg-white px-4 py-3.5">
        <TouchableOpacity onPress={() => router.back()} className="mr-3 p-1">
          <Feather name="arrow-left" size={23} color="#1e293b" />
        </TouchableOpacity>
        <Text className="text-lg font-bold text-slate-800">Tin nhắn hỗ trợ</Text>
      </View>

      <FlatList
        data={conversations}
        keyExtractor={(item) => item.id.toString()}
        renderItem={renderItem}
        contentContainerStyle={{ flexGrow: 1 }}
        ListEmptyComponent={
          <View className="flex-1 items-center justify-center p-8">
            <View className="mb-4 h-20 w-20 items-center justify-center rounded-full bg-white">
              <Feather name="message-circle" size={32} color="#94a3b8" />
            </View>
            <Text className="text-center font-medium text-slate-500">
              Bạn chưa có cuộc hội thoại nào.
            </Text>
            <Text className="mt-1 text-center text-xs text-slate-400">
              Danh sách giáo viên sẽ xuất hiện khi có lớp học.
            </Text>
          </View>
        }
      />
    </SafeAreaView>
  );
}
