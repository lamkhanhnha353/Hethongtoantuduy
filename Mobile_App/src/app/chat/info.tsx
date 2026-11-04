import { Feather } from '@expo/vector-icons';
import { useFocusEffect, useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback, useMemo, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  Image,
  Linking,
  Pressable,
  ScrollView,
  Text,
  TouchableOpacity,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { chatService, type ChatMessage } from '@/services/chat.service';
import { toApiError } from '@/services/apiError';

import { formatChatDate } from './chatDateTime';
import { ChatImageViewer } from './components/ChatImageViewer';

const PAGE_SIZE = 30;
const MAX_HISTORY_PAGES = 100;

type SharedLink = {
  url: string;
  messageId: number;
  createdAt: string;
};

function findLinks(messages: ChatMessage[]): SharedLink[] {
  const links = new Map<string, SharedLink>();

  for (const message of messages) {
    if (message.messageType !== 'TEXT' || !message.content) continue;

    const matches = message.content.match(/https?:\/\/[^\s<>()]+/gi) ?? [];
    for (const match of matches) {
      const url = match.replace(/[.,!?;:]+$/, '');
      if (!links.has(url)) {
        links.set(url, { url, messageId: message.id, createdAt: message.createdAt });
      }
    }
  }

  return Array.from(links.values()).reverse();
}

function SectionHeader({
  title,
  count,
  expanded,
  onPress,
}: {
  title: string;
  count: number;
  expanded: boolean;
  onPress: () => void;
}) {
  return (
    <TouchableOpacity
      onPress={onPress}
      activeOpacity={0.75}
      className="flex-row items-center justify-between px-4 py-4"
    >
      <View className="flex-row items-center">
        <Text className="text-base font-semibold text-slate-800">{title}</Text>
        <Text className="ml-2 text-sm text-slate-400">{count}</Text>
      </View>
      <Feather name={expanded ? 'chevron-up' : 'chevron-down'} size={20} color="#64748b" />
    </TouchableOpacity>
  );
}

function formatDate(value: string) {
  return formatChatDate(value);
}

export default function ChatInfoScreen() {
  const router = useRouter();
  const { conversationId, partnerName } = useLocalSearchParams<{
    conversationId: string;
    partnerName?: string;
  }>();
  const id = Number(conversationId);

  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [loading, setLoading] = useState(true);
  const [expanded, setExpanded] = useState({
    media: true,
    files: true,
    links: true,
  });
  const [selectedImageUrl, setSelectedImageUrl] = useState<string | null>(null);

  useFocusEffect(useCallback(() => {
    let active = true;

    const loadSharedItems = async () => {
      setLoading(true);
      try {
        const history: ChatMessage[] = [];

        for (let page = 1; page <= MAX_HISTORY_PAGES; page += 1) {
          const response = await chatService.getMessages(id, page);
          const pageMessages = response.data.data;
          history.push(...pageMessages);
          if (pageMessages.length < PAGE_SIZE) break;
        }

        if (active) setMessages(history);
      } catch (error) {
        if (active) {
          Alert.alert('Không tải được tệp đã chia sẻ', toApiError(error).message);
        }
      } finally {
        if (active) setLoading(false);
      }
    };

    if (Number.isInteger(id) && id > 0) {
      void loadSharedItems();
    } else {
      setLoading(false);
    }

    return () => {
      active = false;
    };
  }, [id]));

  const media = useMemo(
    () => messages.filter((message) => message.messageType === 'IMAGE' && message.attachmentUrl),
    [messages],
  );
  const files = useMemo(
    () => messages.filter((message) => message.messageType === 'FILE' && message.attachmentUrl),
    [messages],
  );
  const links = useMemo(() => findLinks(messages), [messages]);
  const invalidConversationId = !Number.isInteger(id) || id < 1;

  function toggleSection(section: 'media' | 'files' | 'links') {
    setExpanded((current) => ({ ...current, [section]: !current[section] }));
  }

  return (
    <SafeAreaView className="flex-1 bg-slate-100" edges={['top', 'bottom']}>
      <View className="flex-row items-center justify-center border-b border-slate-200 bg-white px-4 py-4">
        <TouchableOpacity
          onPress={() => router.back()}
          className="absolute left-4 p-1"
          accessibilityLabel="Quay lại cuộc trò chuyện"
        >
          <Feather name="arrow-left" size={23} color="#1e293b" />
        </TouchableOpacity>
        <Text className="text-lg font-bold text-slate-800">Thông tin hội thoại</Text>
      </View>

      <ScrollView contentContainerStyle={{ paddingBottom: 28 }}>
        <View className="mb-2 flex-row items-center bg-white px-5 py-4">
          <View className="mr-3 h-11 w-11 items-center justify-center rounded-full bg-indigo-100">
            <Text className="font-bold text-indigo-700">
              {partnerName?.slice(0, 2).toUpperCase() || 'GV'}
            </Text>
          </View>
          <View className="flex-1">
            <Text className="font-semibold text-slate-800">{partnerName || 'Giáo viên'}</Text>
            <Text className="mt-0.5 text-xs text-slate-500">Cuộc trò chuyện riêng</Text>
          </View>
        </View>

        {invalidConversationId ? (
          <View className="items-center bg-white px-6 py-10">
            <Feather name="alert-circle" size={30} color="#ef4444" />
            <Text className="mt-3 text-center text-sm text-slate-600">
              Không thể mở thông tin hội thoại vì mã cuộc trò chuyện không hợp lệ.
            </Text>
          </View>
        ) : loading ? (
          <View className="items-center bg-white py-10">
            <ActivityIndicator size="large" color="#4f46e5" />
            <Text className="mt-3 text-sm text-slate-500">Đang tải nội dung đã chia sẻ...</Text>
          </View>
        ) : (
          <>
            <View className="mb-2 bg-white">
              <SectionHeader
                title="Ảnh/Video"
                count={media.length}
                expanded={expanded.media}
                onPress={() => toggleSection('media')}
              />
              {expanded.media && (
                <View className="px-4 pb-4">
                  {media.length ? (
                    <View className="flex-row flex-wrap">
                      {media.map((item) => (
                        <Pressable
                          key={item.id}
                          onPress={() => setSelectedImageUrl(item.attachmentUrl!)}
                          className="mb-2 mr-[1%] h-[78px] w-[23%] overflow-hidden rounded-lg bg-slate-100"
                        >
                          <Image
                            source={{ uri: item.attachmentUrl! }}
                            className="h-full w-full"
                            resizeMode="cover"
                          />
                        </Pressable>
                      ))}
                    </View>
                  ) : (
                    <Text className="py-2 text-sm text-slate-400">Chưa có ảnh/video được chia sẻ.</Text>
                  )}
                  {media.length > 0 && (
                    <Text className="mt-2 text-center text-xs text-slate-500">
                      Chạm vào ảnh để xem
                    </Text>
                  )}
                </View>
              )}
            </View>

            <View className="mb-2 bg-white">
              <SectionHeader
                title="File"
                count={files.length}
                expanded={expanded.files}
                onPress={() => toggleSection('files')}
              />
              {expanded.files && (
                <View className="px-4 pb-3">
                  {files.length ? (
                    files.map((item) => (
                      <Pressable
                        key={item.id}
                        onPress={() => void Linking.openURL(item.attachmentUrl!)}
                        className="flex-row items-center py-3"
                      >
                        <View className="mr-3 h-11 w-11 items-center justify-center rounded-lg bg-sky-100">
                          <Feather name="file-text" size={21} color="#0284c7" />
                        </View>
                        <View className="flex-1">
                          <Text className="font-medium text-slate-800" numberOfLines={1}>
                            {item.content || 'Tệp đính kèm'}
                          </Text>
                          <Text className="mt-1 text-xs text-slate-500">
                            {formatDate(item.createdAt)}
                          </Text>
                        </View>
                        <Feather name="download" size={18} color="#64748b" />
                      </Pressable>
                    ))
                  ) : (
                    <Text className="py-2 text-sm text-slate-400">Chưa có file được chia sẻ.</Text>
                  )}
                </View>
              )}
            </View>

            <View className="bg-white">
              <SectionHeader
                title="Link"
                count={links.length}
                expanded={expanded.links}
                onPress={() => toggleSection('links')}
              />
              {expanded.links && (
                <View className="px-4 pb-3">
                  {links.length ? (
                    links.map((item) => {
                      let host = item.url;
                      try {
                        host = new URL(item.url).hostname;
                      } catch {
                        // Keep the original URL visible if it cannot be parsed.
                      }

                      return (
                        <Pressable
                          key={`${item.messageId}-${item.url}`}
                          onPress={() => void Linking.openURL(item.url)}
                          className="flex-row items-center py-3"
                        >
                          <View className="mr-3 h-11 w-11 items-center justify-center rounded-lg bg-slate-100">
                            <Feather name="link" size={20} color="#64748b" />
                          </View>
                          <View className="flex-1">
                            <Text className="font-medium text-slate-800" numberOfLines={1}>
                              {item.url}
                            </Text>
                            <Text className="mt-1 text-xs text-blue-600" numberOfLines={1}>
                              {host}
                            </Text>
                          </View>
                          <Text className="ml-2 text-xs text-slate-400">
                            {formatDate(item.createdAt)}
                          </Text>
                        </Pressable>
                      );
                    })
                  ) : (
                    <Text className="py-2 text-sm text-slate-400">Chưa có liên kết được chia sẻ.</Text>
                  )}
                </View>
              )}
            </View>
          </>
        )}
      </ScrollView>
      <ChatImageViewer
        imageUrl={selectedImageUrl}
        onClose={() => setSelectedImageUrl(null)}
      />
    </SafeAreaView>
  );
}
