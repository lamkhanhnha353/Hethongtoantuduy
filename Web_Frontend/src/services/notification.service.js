import * as signalR from "@microsoft/signalr";
import createApiClient from "./api.service";
import { getAccessToken } from "./auth-storage";

const BACKEND_URL = (
  import.meta.env.VITE_API_BASE_URL || "http://localhost:5114"
).replace(/\/+$/, "");
const NOTIFICATION_HUB_URL = `${BACKEND_URL}/hubs/notifications`;

class NotificationService {
  constructor(baseUrl = "/api/notifications") {
    this.api = createApiClient(baseUrl);
    this.connection = null;
    this.connectionPromise = null;
  }

  /**
   * Lấy danh sách thông báo của người dùng hiện tại (Admin / Teacher)
   * GET /api/notifications
   */
  async getInbox({ page = 1, pageSize = 20, unreadOnly = false, type = "" } = {}) {
    return (
      await this.api.get("", {
        params: {
          page,
          pageSize,
          unreadOnly,
          type: type || undefined,
        },
      })
    ).data;
  }

  /**
   * Lấy số lượng thông báo chưa đọc
   * GET /api/notifications/unread-count
   */
  async getUnreadCount() {
    return (await this.api.get("/unread-count")).data;
  }

  /**
   * Đánh dấu 1 thông báo đã đọc
   * PUT /api/notifications/{id}/read
   */
  async markRead(id) {
    return (await this.api.put(`/${id}/read`)).data;
  }

  /**
   * Đánh dấu tất cả thông báo đã đọc
   * PUT /api/notifications/read-all
   */
  async markAllRead() {
    return (await this.api.put("/read-all")).data;
  }

  /**
   * Lấy cấu hình nhận thông báo
   * GET /api/notifications/settings
   */
  async getSettings() {
    return (await this.api.get("/settings")).data;
  }

  /**
   * Cập nhật cấu hình nhận thông báo
   * PUT /api/notifications/settings
   */
  async updateSettings(settings) {
    return (await this.api.put("/settings", { settings })).data;
  }

  /**
   * Kết nối SignalR realtime nhận thông báo mới (/hubs/notifications)
   */
  async connect({ onReceiveNotification, onStatusChange } = {}) {
    if (this.connectionPromise) return this.connectionPromise;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(NOTIFICATION_HUB_URL, {
        accessTokenFactory: () => getAccessToken() || "",
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    if (onReceiveNotification) {
      connection.on("ReceiveNotification", onReceiveNotification);
    }

    connection.onreconnecting(() => onStatusChange?.("reconnecting"));
    connection.onreconnected(() => onStatusChange?.("connected"));
    connection.onclose(() => {
      this.connectionPromise = null;
      onStatusChange?.("disconnected");
    });

    this.connection = connection;
    this.connectionPromise = connection
      .start()
      .then(() => {
        onStatusChange?.("connected");
        return connection;
      })
      .catch((error) => {
        this.connection = null;
        this.connectionPromise = null;
        onStatusChange?.("disconnected");
        throw error;
      });

    return this.connectionPromise;
  }

  async disconnect() {
    const connection = this.connection;
    this.connection = null;
    this.connectionPromise = null;
    if (!connection) return;
    try {
      await connection.stop();
    } catch {
      // Bỏ qua lỗi khi ngắt kết nối.
    }
  }
}

export default new NotificationService();
