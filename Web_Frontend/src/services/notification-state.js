import { reactive, readonly, toRefs } from "vue";
import notificationService from "./notification.service";
import authService from "./auth.service";

const state = reactive({
  items: [],
  unreadCount: 0,
  total: 0,
  isLoading: false,
  isInitialized: false,
  error: null,
});

function normalizeNotification(raw) {
  if (!raw) return null;
  return {
    id: raw.id ?? raw.Id,
    title: raw.title ?? raw.Title ?? "Thông báo hệ thống",
    body: raw.body ?? raw.Body ?? "",
    type: (raw.type ?? raw.Type ?? "SYSTEM").toUpperCase(),
    refId: raw.refId ?? raw.RefId ?? null,
    isRead: Boolean(raw.isRead ?? raw.IsRead),
    createdAt: raw.createdAt ?? raw.CreatedAt ?? new Date().toISOString(),
  };
}

export function useNotificationState() {
  async function fetchRecentNotifications({ pageSize = 10, force = false } = {}) {
    if (!authService.isAuthenticated()) {
      resetNotificationState();
      return [];
    }
    if (state.isInitialized && !force && state.items.length > 0) {
      return state.items;
    }

    state.isLoading = true;
    state.error = null;
    try {
      const res = await notificationService.getInbox({ page: 1, pageSize });
      const rawItems = Array.isArray(res?.items) ? res.items : (Array.isArray(res) ? res : []);
      state.items = rawItems.map(normalizeNotification).filter(Boolean);
      state.total = res?.total ?? state.items.length;
      if (typeof res?.unreadCount === "number") {
        state.unreadCount = res.unreadCount;
      } else {
        state.unreadCount = state.items.filter((n) => !n.isRead).length;
      }
      state.isInitialized = true;
      return state.items;
    } catch (err) {
      state.error = err.response?.data?.message || "Không thể tải danh sách thông báo.";
      return state.items;
    } finally {
      state.isLoading = false;
    }
  }

  async function refreshUnreadCount() {
    if (!authService.isAuthenticated()) return 0;
    try {
      const res = await notificationService.getUnreadCount();
      if (typeof res?.unreadCount === "number") {
        state.unreadCount = res.unreadCount;
      }
    } catch {
      // Giữ nguyên số lượng hiện tại nếu mạng tạm thời lỗi
    }
    return state.unreadCount;
  }

  async function markNotificationRead(id) {
    if (!id) return false;
    const target = state.items.find((n) => n.id === id);
    const wasUnread = target ? !target.isRead : false;

    if (target && !target.isRead) {
      target.isRead = true;
      if (state.unreadCount > 0) {
        state.unreadCount -= 1;
      }
    }

    try {
      await notificationService.markRead(id);
      if (!target) {
        await refreshUnreadCount();
      }
      return true;
    } catch {
      if (target && wasUnread) {
        target.isRead = false;
        state.unreadCount += 1;
      }
      return false;
    }
  }

  async function markAllNotificationsRead() {
    const previousUnread = state.unreadCount;
    const previousReadMap = new Map(state.items.map((n) => [n.id, n.isRead]));

    state.items.forEach((n) => {
      n.isRead = true;
    });
    state.unreadCount = 0;

    try {
      await notificationService.markAllRead();
      return true;
    } catch {
      state.items.forEach((n) => {
        if (previousReadMap.has(n.id)) {
          n.isRead = previousReadMap.get(n.id);
        }
      });
      state.unreadCount = previousUnread;
      return false;
    }
  }

  function handleIncomingNotification(payload) {
    const incoming = normalizeNotification(payload);
    if (!incoming || !incoming.id) return;

    const existingIdx = state.items.findIndex((n) => n.id === incoming.id);
    if (existingIdx !== -1) {
      state.items.splice(existingIdx, 1, incoming);
    } else {
      state.items.unshift(incoming);
      state.total += 1;
      if (!incoming.isRead) {
        state.unreadCount += 1;
      }
    }
  }

  async function connectRealtime() {
    if (!authService.isAuthenticated()) return;
    try {
      await notificationService.connect({
        onReceiveNotification: handleIncomingNotification,
      });
    } catch {
      // Realtime là best-effort; người dùng vẫn có thể tải thủ công qua REST API
    }
  }

  async function disconnectRealtime() {
    await notificationService.disconnect();
  }

  function resetNotificationState() {
    state.items = [];
    state.unreadCount = 0;
    state.total = 0;
    state.isLoading = false;
    state.isInitialized = false;
    state.error = null;
  }

  return {
    ...toRefs(readonly(state)),
    normalizeNotification,
    fetchRecentNotifications,
    refreshUnreadCount,
    markNotificationRead,
    markAllNotificationsRead,
    connectRealtime,
    disconnectRealtime,
    resetNotificationState,
  };
}
