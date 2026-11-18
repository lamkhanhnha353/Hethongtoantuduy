<template>
  <Teleport to="body">
    <transition name="modal-fade">
      <div
        v-if="open && notification"
        class="fixed inset-0 z-50 flex items-center justify-center bg-stone-950/50 p-4 backdrop-blur-xs"
        @click.self="close"
      >
        <section
          role="dialog"
          aria-modal="true"
          aria-labelledby="notif-detail-title"
          class="w-full max-w-lg overflow-hidden rounded-lg border border-stone-300 bg-white shadow-xl"
        >
          <!-- Header -->
          <header class="flex items-start justify-between gap-3 border-b border-stone-200 bg-stone-50 px-5 py-4">
            <div class="flex items-start gap-3 min-w-0">
              <div
                :class="[
                  'mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-md text-white',
                  typeMeta.iconBg,
                ]"
              >
                <component :is="typeMeta.icon" class="h-5 w-5" />
              </div>
              <div class="min-w-0">
                <div class="flex flex-wrap items-center gap-1.5">
                  <span
                    :class="[
                      'inline-flex items-center rounded border px-2 py-0.5 text-[11px] font-semibold',
                      typeMeta.badgeClass,
                    ]"
                  >
                    {{ typeMeta.label }}
                  </span>
                  <span
                    :class="[
                      'inline-flex items-center rounded border px-2 py-0.5 text-[11px] font-semibold',
                      notification.isRead
                        ? 'border-stone-300 bg-stone-100 text-stone-600'
                        : 'border-orange-300 bg-orange-50 text-orange-800',
                    ]"
                  >
                    {{ notification.isRead ? 'Đã đọc' : 'Mới' }}
                  </span>
                </div>
                <h3
                  id="notif-detail-title"
                  class="mt-1.5 text-base font-bold text-stone-900 leading-snug break-words"
                >
                  {{ notification.title }}
                </h3>
              </div>
            </div>

            <button
              type="button"
              aria-label="Đóng"
              class="rounded-md p-1.5 text-stone-400 transition hover:bg-stone-200/70 hover:text-stone-700 cursor-pointer"
              @click="close"
            >
              <XMarkIcon class="h-5 w-5" />
            </button>
          </header>

          <!-- Body -->
          <div class="space-y-4 px-5 py-5">
            <div class="rounded-md border border-stone-200 bg-stone-50/70 p-4 text-sm text-stone-800 leading-relaxed whitespace-pre-line break-words">
              {{ notification.body || 'Thông báo này không có nội dung chi tiết đính kèm.' }}
            </div>

            <div class="flex flex-wrap items-center justify-between gap-2 text-xs text-stone-500 tabular-nums">
              <span class="inline-flex items-center gap-1.5">
                <ClockIcon class="h-4 w-4 text-stone-400" />
                <span>{{ formatFullNotificationTime(notification.createdAt) }}</span>
              </span>
              <span class="text-[11px] text-stone-400">
                {{ formatRelativeNotificationTime(notification.createdAt) }}
              </span>
            </div>

            <div
              v-if="notification.refId"
              class="flex items-center justify-between rounded-md border border-stone-200 bg-white px-3 py-2 text-xs"
            >
              <span class="font-semibold text-stone-500 uppercase tracking-wide text-[11px]">Mã tham chiếu</span>
              <span class="font-mono font-semibold text-stone-800">{{ notification.refId }}</span>
            </div>
          </div>

          <!-- Footer -->
          <footer class="flex flex-wrap items-center justify-between gap-2 border-t border-stone-200 bg-stone-50 px-5 py-3.5">
            <button
              v-if="showViewAll"
              type="button"
              class="inline-flex items-center gap-1.5 text-xs font-semibold text-orange-600 hover:text-orange-700 cursor-pointer"
              @click="goToAllNotifications"
            >
              <span>Xem tất cả thông báo</span>
              <ArrowRightIcon class="h-3.5 w-3.5" />
            </button>
            <span v-else></span>

            <div class="flex items-center gap-2">
              <button
                type="button"
                class="h-9 rounded-md border border-stone-300 bg-white px-3.5 text-xs font-semibold text-stone-800 transition hover:border-stone-400 hover:bg-stone-100 cursor-pointer"
                @click="close"
              >
                Đóng
              </button>
              <button
                v-if="actionRoute"
                type="button"
                class="inline-flex h-9 items-center gap-1.5 rounded-md border border-orange-700 bg-orange-600 px-3.5 text-xs font-semibold text-white shadow-xs transition hover:bg-orange-700 cursor-pointer"
                @click="goToActionRoute"
              >
                <span>{{ actionRoute.label }}</span>
                <ArrowTopRightOnSquareIcon class="h-3.5 w-3.5" />
              </button>
            </div>
          </footer>
        </section>
      </div>
    </transition>
  </Teleport>
</template>

<script setup>
import { computed, onMounted, onUnmounted } from "vue";
import { useRouter } from "vue-router";
import {
  XMarkIcon,
  ClockIcon,
  ArrowRightIcon,
  ArrowTopRightOnSquareIcon,
} from "@heroicons/vue/24/outline";
import {
  getNotificationTypeMeta,
  formatRelativeNotificationTime,
  formatFullNotificationTime,
  resolveNotificationActionRoute,
} from "@/utils/notification.helper";

const props = defineProps({
  open: { type: Boolean, default: false },
  notification: { type: Object, default: null },
  userRole: { type: String, default: "admin" },
  showViewAll: { type: Boolean, default: true },
});

const emit = defineEmits(["close"]);

const router = useRouter();

const typeMeta = computed(() => getNotificationTypeMeta(props.notification?.type));
const actionRoute = computed(() =>
  props.notification ? resolveNotificationActionRoute(props.notification, props.userRole) : null
);

function close() {
  emit("close");
}

function goToActionRoute() {
  if (!actionRoute.value) return;
  const targetPath = actionRoute.value.path;
  close();
  router.push(targetPath);
}

function goToAllNotifications() {
  const target =
    String(props.userRole || "").toLowerCase() === "teacher"
      ? { path: "/teacher/communication/notices", query: { tab: "inbox" } }
      : { path: "/communication/notifications", query: { tab: "inbox" } };
  close();
  router.push(target);
}

function handleKeydown(event) {
  if (event.key === "Escape" && props.open) {
    close();
  }
}

onMounted(() => window.addEventListener("keydown", handleKeydown));
onUnmounted(() => window.removeEventListener("keydown", handleKeydown));
</script>

<style scoped>
.modal-fade-enter-active,
.modal-fade-leave-active {
  transition: opacity 0.15s ease;
}
.modal-fade-enter-from,
.modal-fade-leave-to {
  opacity: 0;
}
</style>
