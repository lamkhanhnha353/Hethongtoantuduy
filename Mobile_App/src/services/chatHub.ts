import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';

import { API_URL } from '@/config/env';
import type { ChatMessage } from '@/services/chat.service';

export type ChatMessageCallback = (message: ChatMessage) => void;
export type ChatMessageDeletedCallback = (conversationId: number, messageId: number) => void;

class ChatHubService {
  private connection: HubConnection | null = null;
  private pendingStart: Promise<void> | null = null;
  private messageListeners: Set<ChatMessageCallback> = new Set();
  private deletedListeners: Set<ChatMessageDeletedCallback> = new Set();
  private activeConversationId: number | null = null;

  public get isConnected(): boolean {
    return this.connection?.state === HubConnectionState.Connected;
  }

  public setActiveConversationId(conversationId: number | null): void {
    this.activeConversationId = conversationId;
  }

  public getActiveConversationId(): number | null {
    return this.activeConversationId;
  }

  public async startConnection(
    baseUrl: string = API_URL,
    getToken: () => Promise<string | null>,
  ): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      return;
    }

    if (this.pendingStart) {
      return this.pendingStart;
    }

    this.pendingStart = (async () => {
      try {
        const token = await getToken();
        if (!token) {
          return;
        }

        const cleanBaseUrl = (baseUrl || API_URL).replace(/\/+$/, '');
        const hubUrl = `${cleanBaseUrl}/chathub`;

        if (!this.connection || this.connection.state === HubConnectionState.Disconnected) {
          this.connection = new HubConnectionBuilder()
            .withUrl(hubUrl, {
              accessTokenFactory: async () => {
                const currentToken = await getToken();
                return currentToken ?? '';
              },
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(LogLevel.None)
            .build();

          this.connection.on('ReceiveNewMessage', (message: ChatMessage) => {
            this.messageListeners.forEach((callback) => {
              try {
                callback(message);
              } catch (err) {
                console.warn('[ChatHub] Error in ReceiveNewMessage callback:', err);
              }
            });
          });

          this.connection.on(
            'MessageDeleted',
            (conversationId: number, messageId: number) => {
              this.deletedListeners.forEach((callback) => {
                try {
                  callback(conversationId, messageId);
                } catch (err) {
                  console.warn('[ChatHub] Error in MessageDeleted callback:', err);
                }
              });
            },
          );
        }

        if (this.connection.state === HubConnectionState.Disconnected) {
          await this.connection.start();
        }
      } catch (error) {
        console.warn('[ChatHub] Connection warning:', error);
      } finally {
        this.pendingStart = null;
      }
    })();

    return this.pendingStart;
  }

  public async stopConnection(): Promise<void> {
    this.activeConversationId = null;

    if (this.pendingStart) {
      try {
        await this.pendingStart;
      } catch {
        // Bỏ qua lỗi khởi động trước đó
      }
    }

    if (this.connection) {
      try {
        if (
          this.connection.state === HubConnectionState.Connected ||
          this.connection.state === HubConnectionState.Reconnecting
        ) {
          await this.connection.stop();
        }
      } catch (error) {
        console.warn('[ChatHub] Warning stopping connection:', error);
      } finally {
        this.connection = null;
      }
    }
  }

  public onReceiveNewMessage(callback: ChatMessageCallback): () => void {
    this.messageListeners.add(callback);
    return () => {
      this.messageListeners.delete(callback);
    };
  }

  public onMessageDeleted(callback: ChatMessageDeletedCallback): () => void {
    this.deletedListeners.add(callback);
    return () => {
      this.deletedListeners.delete(callback);
    };
  }
}

export const chatHubService = new ChatHubService();
