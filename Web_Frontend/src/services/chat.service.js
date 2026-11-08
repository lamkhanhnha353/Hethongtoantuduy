import * as signalR from "@microsoft/signalr";
import createApiClient from "./api.service";
import { getAccessToken } from "./auth-storage";

const BACKEND_URL = (
  import.meta.env.VITE_API_BASE_URL || "http://localhost:5114"
).replace(/\/+$/, "");
const CHAT_HUB_URL = `${BACKEND_URL}/chathub`;

function unwrapList(response) {
  const payload = response?.data ?? response;
  if (Array.isArray(payload)) return payload;
  if (Array.isArray(payload?.items)) return payload.items;
  return [];
}

class ChatService {
  constructor(baseUrl = "/api/chat") {
    this.api = createApiClient(baseUrl);
    this.connection = null;
    this.connectionPromise = null;
  }

  // Danh sách hội thoại của giáo viên hiện tại
  async getConversations() {
    const response = (await this.api.get("/conversations")).data;
    return unwrapList(response);
  }

  // Tin nhắn theo trang (30 tin/trang, backend trả về theo thứ tự thời gian)
  async getMessages(conversationId, page = 1) {
    const response = (
      await this.api.get(`/${conversationId}/messages`, { params: { page } })
    ).data;
    return unwrapList(response);
  }

  // Gửi tin nhắn văn bản
  async sendMessage({ receiverId, receiverType = "PARENT", messageType = "TEXT", content }) {
    const response = (
      await this.api.post("/send", {
        receiverId,
        receiverType,
        messageType,
        content,
      })
    ).data;
    return response?.data ?? response;
  }

  // Gửi tệp/hình ảnh đính kèm
  async sendAttachment({ receiverId, receiverType = "PARENT", file }) {
    const formData = new FormData();
    formData.append("receiverId", String(receiverId));
    formData.append("receiverType", receiverType);
    formData.append("file", file);

    const response = (
      await this.api.post("/send-attachment", formData, {
        headers: { "Content-Type": "multipart/form-data" },
      })
    ).data;
    return response?.data ?? response;
  }

  // Xóa vĩnh viễn một tin nhắn
  async deleteMessage(conversationId, messageId) {
    return (await this.api.delete(`/${conversationId}/messages/${messageId}`)).data;
  }

  // ─── SignalR realtime ──────────────────────────────────────────────────────
  async connect({ onNewMessage, onMessageDeleted, onStatusChange } = {}) {
    if (this.connectionPromise) return this.connectionPromise;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(CHAT_HUB_URL, {
        accessTokenFactory: () => getAccessToken() || "",
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    if (onNewMessage) connection.on("ReceiveNewMessage", onNewMessage);
    if (onMessageDeleted) connection.on("MessageDeleted", onMessageDeleted);

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

export default new ChatService();
