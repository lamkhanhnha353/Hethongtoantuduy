export type AIConversationDto = {
  id: number;
  title: string;
  lastMessageAt: string | null;
  createdAt: string;
  updatedAt: string;
};

export type AIConversationSummaryDto = AIConversationDto & {
  lastMessage: string | null;
  messageCount: number;
};

export type AIConversationListDto = {
  items: AIConversationSummaryDto[];
  page: number;
  pageSize: number;
  total: number;
  hasMore: boolean;
};

export type AIChatSourceDto = {
  docId: string;
  title: string;
  chunkIndex: number;
};

export type AIMessageDto = {
  id: number;
  conversationId: number;
  role: 'user' | 'assistant';
  content: string;
  tokenCount: number;
  sources: AIChatSourceDto[];
  status: 'done' | 'partial';
  createdAt: string;
};

export type AIMessageListDto = {
  items: AIMessageDto[];
  page: number;
  pageSize: number;
  total: number;
  hasMore: boolean;
};

export type AIChatStatusEventDto = {
  stage: 'retrieving' | 'summarizing' | string;
};

export type AIChatSourcesEventDto = {
  sources: AIChatSourceDto[];
  sourceChunksUsed: number;
};

export type AIChatDeltaEventDto = {
  content: string;
};

export type AIChatDoneEventDto = {
  content: string;
  sourceChunksUsed: number;
  conversationId?: number;
  messageId?: number | null;
  title?: string | null;
};

export type AIChatErrorEventDto = {
  message: string;
  conversationId?: number;
};

export type AIChatStreamEvent =
  | { event: 'status'; data: AIChatStatusEventDto }
  | { event: 'sources'; data: AIChatSourcesEventDto }
  | { event: 'delta'; data: AIChatDeltaEventDto }
  | { event: 'done'; data: AIChatDoneEventDto }
  | { event: 'error'; data: AIChatErrorEventDto };

export type AIChatMessage = AIMessageDto & {
  pending?: boolean;
  streaming?: boolean;
  error?: boolean;
};
