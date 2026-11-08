import { fetch as expoFetch } from 'expo/fetch';

import { AI_API_URL } from '@/config/env';
import { handleSessionExpired, SESSION_EXPIRED_MESSAGE } from '@/lib/sessionExpiry';
import { ApiError } from '@/services/apiError';
import { getAccessToken } from '@/store/authStore';
import type {
  AIChatDeltaEventDto,
  AIChatDoneEventDto,
  AIChatErrorEventDto,
  AIChatSourcesEventDto,
  AIChatStatusEventDto,
  AIChatStreamEvent,
  AIConversationDto,
  AIConversationListDto,
  AIMessageListDto,
} from '@/types/aiChat';

const NETWORK_ERROR_STATUS = 0;
const INVALID_RESPONSE_STATUS = 502;

type AIRequestOptions = {
  method?: 'GET' | 'POST' | 'PATCH' | 'DELETE';
  body?: unknown;
  signal?: AbortSignal;
};

function getRequiredToken(): string {
  const token = getAccessToken();

  if (!token) {
    void handleSessionExpired();
    throw new ApiError(401, SESSION_EXPIRED_MESSAGE);
  }

  return token;
}

function isAbortError(error: unknown) {
  return error instanceof Error && error.name === 'AbortError';
}
//Hàm giúp bạn quy đổi chúng về một dạng string duy nhất để dễ dàng hiển thị thông báo cho người dùng
function readDetail(detail: unknown): string | undefined {
  // nếu detail là chuỗi đơn thì trả về chuỗi đó sau khi trim.
  if (typeof detail === 'string' && detail.trim()) {
    return detail;
  }
  // nếu detail không là mảng or chuổi đơn thì trả về undefined.
  if (!Array.isArray(detail)) {
    return undefined;
  }

  const messages = detail.map((item) => {
    if (typeof item === 'string') {
      return item;
    }

    if (item && typeof item === 'object' && 'msg' in item) {
      const message = (item as { msg?: unknown }).msg;
      return typeof message === 'string' ? message : null;
    }
    return null;
  }).filter((item): item is string => Boolean(item));

  return messages.length ? messages.join('\n') : undefined;
}

async function toAIError(response: Response): Promise<ApiError> {
  if (response.status === 401) {
    void handleSessionExpired();
    return new ApiError(401, SESSION_EXPIRED_MESSAGE);
  }

  let payload: unknown;
  try {
    payload = await response.json();
  } catch {
    payload = null;
  }

  if (payload && typeof payload === 'object') {
    const detail = readDetail((payload as { detail?: unknown }).detail);
    const message = (payload as { message?: unknown }).message;
    const resolved = detail ?? (typeof message === 'string' ? message : undefined);

    if (resolved) {
      return new ApiError(response.status, resolved);
    }
  }

  return new ApiError(response.status, 'Đã xảy ra lỗi, vui lòng thử lại.');
}

async function requestAI<T>(path: string, options: AIRequestOptions = {},): Promise<T> {
  const token = getRequiredToken();
  const headers: Record<string, string> = {
    Accept: 'application/json',
    Authorization: `Bearer ${token}`,
  };

  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  let response: Response;
  //bắt lỗi mạng, và lỗi cấu hình AI API URL
  try {
    response = await expoFetch(`${AI_API_URL}${path}`, {
      method: options.method ?? 'GET',
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
      signal: options.signal,
    });
  } catch (error) {
    if (isAbortError(error)) {
      throw error;
    }

    throw new ApiError(
      NETWORK_ERROR_STATUS,
      'Không kết nối được tới trợ lý AI. Kiểm tra mạng và cấu hình AI API URL.',
    );
  }
  // bắt lỗi phản hồi từ server, và lỗi dữ liệu trả về không hợp lệ
  if (!response.ok) {
    throw await toAIError(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  if (!text) {
    return undefined as T;
  }
  // bắt lỗi server bị lỗi dữ liệu trả về không hợp lệ, không parse được JSON
  try {
    return JSON.parse(text) as T;
  } catch {
    throw new ApiError(INVALID_RESPONSE_STATUS, 'Máy chủ AI trả về dữ liệu không hợp lệ.');
  }
}

function emitSSEBlock(block: string, onEvent: (event: AIChatStreamEvent) => void,) {
  if (!block.trim()) {
    return;
  }

  let eventName = 'message';
  const dataLines: string[] = [];

  for (const line of block.split(/\r?\n/)) {
    if (!line || line.startsWith(':')) {
      continue;
    }

    const colonIndex = line.indexOf(':');
    const field = colonIndex === -1 ? line : line.slice(0, colonIndex);
    const value = colonIndex === -1 ? '' : line.slice(colonIndex + 1).replace(/^ /, '');

    if (field === 'event') {
      eventName = value;
    } else if (field === 'data') {
      dataLines.push(value);
    }
  }

  if (!dataLines.length) {
    return;
  }

  let payload: unknown;
  try {
    payload = JSON.parse(dataLines.join('\n'));
  } catch {
    return;
  }

  if (!payload || typeof payload !== 'object') {
    return;
  }

  switch (eventName) {
    //Thông báo trạng thái hiện tại (ví dụ: "Đang suy nghĩ...", "Đang tìm kiếm tài liệu...").
    case 'status':
      onEvent({ event: 'status', data: payload as AIChatStatusEventDto });
      break;
    //Danh sách các tài liệu / nguồn tham khảo(sources) mà AI sử dụng.
    case 'sources':
      onEvent({ event: 'sources', data: payload as AIChatSourcesEventDto });
      break;
    //Nội dung văn bản sinh ra từng phần nhỏ (delta / token chunks) để gõ chữ lên màn hình.
    case 'delta':
      onEvent({ event: 'delta', data: payload as AIChatDeltaEventDto });
      break;
    //Báo hiệu quá trình sinh phản hồi đã kết thúc thành công.
    case 'done':
      onEvent({ event: 'done', data: payload as AIChatDoneEventDto });
      break;
    //Báo lỗi xảy ra trong quá trình stream từ phía server AI.
    case 'error':
      onEvent({ event: 'error', data: payload as AIChatErrorEventDto });
      break;
  }
}
// Điều phối luồng dữ liệu ở 
function consumeSSEBlocks(incoming: string, onEvent: (event: AIChatStreamEvent) => void,
): string {
  let buffer = incoming;
  while (true) {
    const match = buffer.match(/\r?\n\r?\n/);
    if (!match || match.index === undefined) {
      return buffer;
    }

    emitSSEBlock(buffer.slice(0, match.index), onEvent);
    buffer = buffer.slice(match.index + match[0].length);
  }
}

export async function streamAIChat({ prompt, conversationId, signal, onEvent, }: {
  prompt: string;
  conversationId: number;
  signal?: AbortSignal;
  onEvent: (event: AIChatStreamEvent) => void;
}): Promise<void> {
  const token = getRequiredToken();

  let response: Response;
  try {
    response = await expoFetch(`${AI_API_URL}/api/ai/chat/stream`, {
      method: 'POST',
      headers: {
        Accept: 'text/event-stream',
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        prompt,
        conversation_id: conversationId,
      }),
      signal,
    });
  } catch (error) {
    if (isAbortError(error)) {
      throw error;
    }
    throw new ApiError(NETWORK_ERROR_STATUS, 'Không kết nối được tới trợ lý AI. Kiểm tra mạng và cấu hình AI API URL.',
    );
  }

  if (!response.ok) {
    throw await toAIError(response);
  }

  if (!response.body) {
    throw new ApiError(INVALID_RESPONSE_STATUS, 'Máy chủ AI không trả về dữ liệu stream.');
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  try {
    while (true) {
      const { done, value } = await reader.read();
      if (done) {
        break;
      }

      const chunk = decoder.decode(value, { stream: true });
      buffer += chunk;
      buffer = consumeSSEBlocks(buffer, onEvent);
    }

    buffer += decoder.decode();
    // Xử lý tất cả các block còn lại trong buffer (không chỉ block đầu tiên)
    buffer = consumeSSEBlocks(buffer, onEvent);
    if (buffer.trim()) {
      emitSSEBlock(buffer, onEvent);
    }
  } finally {
    reader.releaseLock();
  }
}

export const aiChatService = {
  listConversations: (page = 1, pageSize = 50) =>
    requestAI<AIConversationListDto>(
      `/api/ai/conversations?page=${page}&page_size=${pageSize}`,
    ),

  createConversation: (title?: string) =>
    requestAI<AIConversationDto>('/api/ai/conversations', {
      method: 'POST',
      body: title ? { title } : {},
    }),

  getConversation: (conversationId: number) =>
    requestAI<AIConversationDto>(`/api/ai/conversations/${conversationId}`),

  getMessages: (conversationId: number, page = 1, pageSize = 50) =>
    requestAI<AIMessageListDto>(
      `/api/ai/conversations/${conversationId}/messages?page=${page}&page_size=${pageSize}`,
    ),

  deleteConversation: (conversationId: number) =>
    requestAI<void>(`/api/ai/conversations/${conversationId}`, {
      method: 'DELETE',
    }),

  streamChat: streamAIChat,
};
