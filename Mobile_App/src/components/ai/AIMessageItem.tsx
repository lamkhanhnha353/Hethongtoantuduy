import { Feather, Ionicons } from '@expo/vector-icons';
import { ActivityIndicator, Text, View } from 'react-native';

import { formatChatTime } from '@/components/chat/chatDateTime';
import type { AIChatMessage } from '@/types/aiChat';

export function AIMessageItem({
  message,
  statusText,
}: {
  message: AIChatMessage;
  statusText?: string | null;
}) {
  const isUser = message.role === 'user';

  if (isUser) {
    return (
      <View className="mb-4 max-w-[84%] self-end">
        <View className="rounded-2xl rounded-tr-md bg-sky-600 px-4 py-2.5">
          <Text className="text-[16px] leading-6 text-white" selectable>
            {message.content}
          </Text>
        </View>
        <Text className="mt-1 text-right text-[10px] text-slate-400">
          {formatChatTime(message.createdAt)}
        </Text>
      </View>
    );
  }

  const sources = message.sources ?? [];
  const showSources = Boolean(message.content) && sources.length > 0;

  return (
    <View className="mb-4 w-full max-w-[92%] self-start">
      <View className="flex-row items-start gap-2">
        <View className="mt-0.5 h-8 w-8 items-center justify-center rounded-full bg-indigo-100">
          <Ionicons color="#4f46e5" name="sparkles" size={16} />
        </View>

        <View className="flex-1">
          <View
            className={`rounded-2xl rounded-tl-md border px-4 py-2.5 ${message.error
                ? 'border-rose-200 bg-rose-50'
                : 'border-slate-200 bg-white'
              }`}
          >
            {message.streaming && !message.content ? (
              <View className="flex-row items-center gap-2">
                <ActivityIndicator color="#4f46e5" size="small" />
                <Text className="text-sm text-slate-500">
                  {statusText ?? 'Đang suy nghĩ...'}
                </Text>
              </View>
            ) : (
              <Text
                className={`text-[16px] leading-6 ${message.error ? 'text-rose-700' : 'text-slate-800'
                  }`}
                selectable
              >
                {message.content}
                {message.streaming ? (
                  <Text className="text-indigo-500"> ▍</Text>
                ) : null}
              </Text>
            )}
          </View>

          {showSources ? (
            <View className="mt-2">
              <View className="mb-1 flex-row items-center gap-1">
                <Feather color="#64748b" name="book-open" size={11} />
                <Text className="text-[10px] font-semibold uppercase tracking-wide text-slate-400">
                  Nguồn tham khảo
                </Text>
              </View>
              <View className="flex-row flex-wrap gap-1.5">
                {sources.slice(0, 4).map((source) => (
                  <View
                    key={`${source.docId}-${source.chunkIndex}`}
                    className="max-w-full rounded-full border border-indigo-100 bg-indigo-50 px-2.5 py-1"
                  >
                    <Text
                      className="text-[10px] font-medium text-indigo-700"
                      numberOfLines={1}
                    >
                      {source.title || source.docId}
                    </Text>
                  </View>
                ))}
              </View>
            </View>
          ) : null}

          <Text className="mt-1 text-[10px] text-slate-400">
            {message.pending ? 'Đang trả lời...' : formatChatTime(message.createdAt)}
          </Text>
        </View>
      </View>
    </View>
  );
}
