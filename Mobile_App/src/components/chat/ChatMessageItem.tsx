import { Feather } from '@expo/vector-icons';
import { Image } from 'expo-image';
import { useState } from 'react';
import { Alert, Linking, Pressable, Text, View } from 'react-native';

import type { ChatMessage } from '@/services/chat.service';

import { formatChatTime } from './chatDateTime';

export function ChatMessageItem({
  item,
  isMine,
  showTime,
  onOpenImage,
  onDelete,
}: {
  item: ChatMessage;
  isMine: boolean;
  showTime: boolean;
  onOpenImage: (url: string) => void;
  onDelete: (messageId: number) => void;
}) {
  const [imageFailed, setImageFailed] = useState(false);
  const time = formatChatTime(item.createdAt);
  const textParts = (item.content ?? '').split(/(https?:\/\/[^\s<>()]+)/gi);

  function showMessageActions() {
    if (item.pending) return;
    Alert.alert('Xóa tin nhắn', 'Bạn có chắc chắn muốn xóa tin nhắn này không?', [
      { text: 'Hủy', style: 'cancel' },
      {
        text: 'Xóa',
        style: 'destructive',
        onPress: () => onDelete(item.id),
      },
    ]);
  }

  return (
    <View className={`mb-3 max-w-[84%] ${isMine ? 'self-end' : 'self-start'}`}>
      <View
        className={`overflow-hidden rounded-2xl ${
          isMine ? 'bg-sky-100' : 'border border-slate-200 bg-white'
        }`}
      >
        {item.messageType === 'IMAGE' && item.attachmentUrl ? (
          <Pressable
            onPress={() => onOpenImage(item.attachmentUrl!)}
            onLongPress={showMessageActions}
            delayLongPress={400}
            className="h-56 w-64 items-center justify-center bg-slate-200"
          >
            {imageFailed ? (
              <View className="items-center px-4">
                <Feather name="image" size={28} color="#64748b" />
                <Text className="mt-2 text-center text-xs text-slate-600">
                  Không tải được ảnh
                </Text>
              </View>
            ) : (
              <Image
                source={{ uri: item.attachmentUrl }}
                style={{ width: '100%', height: '100%' }}
                contentFit="cover"
                cachePolicy="disk"
                transition={150}
                onError={() => setImageFailed(true)}
              />
            )}
          </Pressable>
        ) : item.messageType === 'FILE' && item.attachmentUrl ? (
          <Pressable
            className="flex-row items-center p-3.5"
            onPress={() => void Linking.openURL(item.attachmentUrl!)}
            onLongPress={showMessageActions}
            delayLongPress={400}
          >
            <View className="mr-3 h-12 w-12 items-center justify-center rounded-xl bg-indigo-50">
              <Feather name="file-text" size={27} color="#4f46e5" />
            </View>
            <Text className="ml-2 flex-1 text-sm font-medium text-slate-800" numberOfLines={2}>
              {item.content || 'Mở tệp đính kèm'}
            </Text>
            <Feather name="download" size={19} color="#64748b" />
          </Pressable>
        ) : item.messageType === 'RECALLED' ? (
          <Pressable onLongPress={showMessageActions} delayLongPress={400}>
            <Text className="px-3.5 py-2.5 text-sm italic text-slate-500">
              Tin nhắn đã được thu hồi
            </Text>
          </Pressable>
        ) : (
          <Pressable onLongPress={showMessageActions} delayLongPress={400}>
            <Text className="px-3.5 py-2.5 text-[16px] text-slate-800">
              {textParts.map((part, index) => {
                if (!/^https?:\/\//i.test(part)) return part;
                const url = part.replace(/[.,!?;:]+$/, '');
                const punctuation = part.slice(url.length);

                return (
                  <Text key={`${index}-${part}`}>
                    <Text
                      className="text-blue-700 underline"
                      onPress={() => void Linking.openURL(url)}
                    >
                      {url}
                    </Text>
                    {punctuation}
                  </Text>
                );
              })}
            </Text>
          </Pressable>
        )}
      </View>
      {item.pending || showTime ? (
        <Text
          className={`mt-1 text-[10px] text-slate-400 ${isMine ? 'text-right' : 'text-left'}`}
        >
          {item.pending ? 'Đang gửi...' : time}
        </Text>
      ) : null}
    </View>
  );
}
