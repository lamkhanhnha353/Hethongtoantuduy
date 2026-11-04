import type { AxiosResponse } from 'axios';

import { apiClient } from './apiClient';

export type ChatMessage = {
  id: number;
  conversationId: number;
  senderType: string;
  senderId: number | null;
  messageType: 'TEXT' | 'IMAGE' | 'FILE' | 'RECALLED' | string;
  content: string | null;
  attachmentUrl: string | null;
  isRead: boolean;
  createdAt: string;
  pending?: boolean;
};

export type ChatConversation = {
  id: number;
  chatType: string;
  partnerId: number;
  partnerName: string;
  partnerAvatar: string | null;
  lastMessage: string | null;
  lastMessageAt: string;
  unreadCount: number;
};

type ChatResponse<T> = {
  success: boolean;
  data: T;
};

export type ChatAttachment = {
  uri: string;
  name: string;
  type: string;
  file?: Blob | null;
};

function toAttachmentFormData(
  receiverId: number,
  receiverType: string,
  attachment: ChatAttachment,
): FormData {
  const formData = new FormData();
  formData.append('receiverId', String(receiverId));
  formData.append('receiverType', receiverType);

  if (attachment.file) {
    formData.append('file', attachment.file);
  } else {
    formData.append(
      'file',
      { uri: attachment.uri, name: attachment.name, type: attachment.type } as unknown as Blob,
    );
  }

  return formData;
}

export const chatService = {
  getConversations: () =>
    apiClient.get<ChatResponse<ChatConversation[]>>('/api/chat/conversations'),

  getMessages: (conversationId: number, page = 1) =>
    apiClient.get<ChatResponse<ChatMessage[]>>(
      `/api/chat/${conversationId}/messages?page=${page}`,
    ),

  sendMessage: (data: {
    receiverId: number;
    receiverType: string;
    messageType: 'TEXT';
    content: string;
  }) => apiClient.post<ChatResponse<ChatMessage>>('/api/chat/send', data),

  sendAttachment: (
    receiverId: number,
    receiverType: string,
    attachment: ChatAttachment,
  ): Promise<AxiosResponse<ChatResponse<ChatMessage>>> =>
    apiClient.post(
      '/api/chat/send-attachment',
      toAttachmentFormData(receiverId, receiverType, attachment),
      { timeout: 120_000 },
    ),

  deleteMessage: (conversationId: number, messageId: number) =>
    apiClient.delete<ChatResponse<null>>(
      `/api/chat/${conversationId}/messages/${messageId}`,
    ),

};
