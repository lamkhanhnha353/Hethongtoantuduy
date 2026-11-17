import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';

import { API_URL } from '@/config/env';
import type { RealtimeNotificationDto } from '@/types/notification';

export type NotificationCallback = (notification: RealtimeNotificationDto) => void;

class NotificationHubService {
  private connection: HubConnection | null = null;
  private pendingStart: Promise<void> | null = null;
  private listeners: Set<NotificationCallback> = new Set();

  public get isConnected(): boolean {
    return this.connection?.state === HubConnectionState.Connected;
  }

  public get state(): HubConnectionState {
    return this.connection?.state ?? HubConnectionState.Disconnected;
  }

  public async startConnection(
    baseUrl: string = API_URL,
    getToken: () => Promise<string | null>,
  ): Promise<void> {
    // Nếu đã kết nối thì không cần kết nối lại
    if (this.connection?.state === HubConnectionState.Connected) {
      return;
    }

    // Nếu đang trong tiến trình kết nối thì đợi hoàn thành
    if (this.pendingStart) {
      return this.pendingStart;
    }

    this.pendingStart = (async () => {
      try {
        const token = await getToken();
        if (!token) {
          console.warn('[SignalR] No access token available, skipping connection.');
          return;
        }

        const cleanBaseUrl = (baseUrl || API_URL).replace(/\/+$/, '');
        const hubUrl = `${cleanBaseUrl}/hubs/notifications`;

        // Tạo mới connection nếu chưa có hoặc đã ở trạng thái Disconnected
        if (!this.connection || this.connection.state === HubConnectionState.Disconnected) {
          this.connection = new HubConnectionBuilder()
            .withUrl(hubUrl, {
              accessTokenFactory: async () => {
                const currentToken = await getToken();
                return currentToken ?? '';
              },
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(LogLevel.Information)
            .build();

          this.connection.on('ReceiveNotification', (notification: RealtimeNotificationDto) => {
            this.listeners.forEach((callback) => {
              try {
                callback(notification);
              } catch (err) {
                console.error('[SignalR] Error in notification callback:', err);
              }
            });
          });

          this.connection.onclose((error) => {
            if (error) {
              console.warn('[SignalR] Connection closed with error:', error);
            }
          });

          this.connection.onreconnecting((error) => {
            console.warn('[SignalR] Reconnecting...', error);
          });

          this.connection.onreconnected((connectionId) => {
            console.log('[SignalR] Reconnected successfully. ConnectionId:', connectionId);
          });
        }

        if (this.connection.state === HubConnectionState.Disconnected) {
          await this.connection.start();
          console.log('[SignalR] Connected successfully. ConnectionId:', this.connection.connectionId);
        }
      } catch (error) {
        console.error('[SignalR] Connection Error:', error);
      } finally {
        this.pendingStart = null;
      }
    })();

    return this.pendingStart;
  }

  public async stopConnection(): Promise<void> {
    this.pendingStart = null;
    if (this.connection) {
      try {
        if (
          this.connection.state === HubConnectionState.Connected ||
          this.connection.state === HubConnectionState.Connecting ||
          this.connection.state === HubConnectionState.Reconnecting
        ) {
          await this.connection.stop();
          console.log('[SignalR] Disconnected.');
        }
      } catch (error) {
        console.error('[SignalR] Error stopping connection:', error);
      } finally {
        this.connection = null;
      }
    }
  }

  /**
   * Đăng ký lắng nghe sự kiện ReceiveNotification từ Backend đẩy xuống.
   * Trả về hàm unsubscribe để cleanup dễ dàng trong useEffect.
   */
  public onReceiveNotification(callback: NotificationCallback): () => void {
    this.listeners.add(callback);
    return () => {
      this.listeners.delete(callback);
    };
  }

  /**
   * Huỷ lắng nghe: truyền callback cụ thể hoặc không truyền để huỷ tất cả.
   */
  public offReceiveNotification(callback?: NotificationCallback): void {
    if (callback) {
      this.listeners.delete(callback);
    } else {
      this.listeners.clear();
    }
  }
}

export const notificationHubService = new NotificationHubService();
