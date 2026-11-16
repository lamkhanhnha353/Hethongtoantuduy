<template>
  <div class="space-y-4">
    <!-- Filter & Action Toolbar -->
    <div class="bg-white rounded-lg border border-stone-300 p-4 shadow-[0_1px_2px_rgba(0,0,0,0.04)] flex flex-col lg:flex-row lg:items-center justify-between gap-3">
      <div class="flex flex-wrap items-center gap-3">
        <div class="inline-flex items-center bg-stone-100 border border-stone-300 p-1 rounded-md text-xs font-semibold tabular-nums">
          <button
            type="button"
            @click="setUnreadFilter(false)"
            :class="[
              'h-7 px-3 rounded transition-colors cursor-pointer',
              !unreadOnly ? 'bg-orange-600 text-white shadow-xs' : 'text-stone-700 hover:text-stone-900',
            ]"
          >
            Tất cả
          </button>
          <button
            type="button"
            @click="setUnreadFilter(true)"
            :class="[
              'h-7 px-3 rounded transition-colors cursor-pointer',
              unreadOnly ? 'bg-orange-600 text-white shadow-xs' : 'text-stone-700 hover:text-stone-900',
            ]"
          >
            Chưa đọc ({{ unreadCount }})
          </button>
        </div>

        <div class="flex items-center gap-2 text-xs">
          <label for="notif-inbox-type-filter" class="font-semibold text-stone-600 whitespace-nowrap">
            Loại:
          </label>
          <select
            id="notif-inbox-type-filter"
            v-model="typeFilter"
            class="filter-select min-w-44"
            @change="handleTypeChange"
          >
            <option v-for="opt in NOTIFICATION_TYPE_OPTIONS" :key="opt.value" :value="opt.value">
              {{ opt.label }}
            </option>
          </select>
        </div>
      </div>

      <div class="flex flex-wrap items-center gap-2">
        <button
          type="button"
          :disabled="loading"
          class="inline-flex h-9 items-center gap-1.5 rounded-md border border-stone-300 bg-white px-3 text-xs font-semibold text-stone-800 transition hover:border-stone-400 hover:bg-stone-100 disabled:cursor-not-allowed disabled:opacity-60 cursor-pointer"
          @click="loadInbox"
        >
          <ArrowPathIcon class="h-4 w-4 text-orange-600" :class="{ 'animate-spin': loading }" />
          <span>Làm mới</span>
        </button>

        <button
          type="button"
          :disabled="loading || unreadCount === 0"
          class="inline-flex h-9 items-center gap-1.5 rounded-md border border-orange-700 bg-orange-600 px-3 text-xs font-semibold text-white shadow-xs transition hover:bg-orange-700 disabled:cursor-not-allowed disabled:opacity-50 cursor-pointer"
          @click="handleMarkAllRead"
        >
          <CheckIcon class="h-4 w-4" />
          <span>Đánh dấu tất cả đã đọc</span>
        </button>
      </div>
    </div>

    <!-- Error Banner -->
    <div
      v-if="errorMessage"
      class="flex flex-col gap-3 rounded-md border border-rose-200 bg-rose-50 p-4 sm:flex-row sm:items-center sm:justify-between"
    >
      <p class="text-sm text-rose-700">{{ errorMessage }}</p>
      <button type="button" class="shrink-0 text-sm font-semibold text-rose-700 underline cursor-pointer" @click="loadInbox">
        Thử lại
      </button>
    </div>

    <!-- Inbox List -->
    <div class="overflow-hidden rounded-lg border border-stone-300 bg-white shadow-[0_1px_2px_rgba(0,0,0,0.04)]">
      <div v-if="loading" class="flex min-h-56 flex-col items-center justify-center gap-3 text-sm text-stone-600 p-8">
        <ArrowPathIcon class="h-6 w-6 animate-spin text-orange-600" />
        <span>Đang tải danh sách thông báo...</span>
      </div>

      <div v-else-if="items.length === 0" class="p-12">
        <EmptyState
          :icon="BellAlertIcon"
          :message="unreadOnly ? 'Không có thông báo chưa đọc' : 'Hộp thư thông báo trống'"
          :sub="unreadOnly ? 'Bạn đã đọc hết tất cả thông báo hiện có.' : 'Các thông báo mới từ trung tâm và hệ thống sẽ hiển thị tại đây.'"
        />
      </div>

      <div v-else class="divide-y divide-stone-200">
        <article
          v-for="item in items"
          :key="item.id"
          :class="[
            'flex flex-col sm:flex-row sm:items-center justify-between gap-4 px-5 py-4 transition-colors cursor-pointer',
            !item.isRead ? 'bg-orange-50/45 hover:bg-orange-50/80' : 'hover:bg-stone-50',
          ]"
          @click="openDetail(item)"
        >
          <div class="flex items-start gap-3.5 min-w-0 flex-1">
            <div
              :class="[
                'mt-0.5 flex h-10 w-10 shrink-0 items-center justify-center rounded-md text-white',
                getNotificationTypeMeta(item.type).iconBg,
              ]"
            >
              <component :is="getNotificationTypeMeta(item.type).icon" class="h-5 w-5" />
            </div>

            <div class="min-w-0 flex-1">
              <div class="flex flex-wrap items-center gap-2">
                <span
                  v-if="!item.isRead"
                  class="inline-block h-2 w-2 rounded-full bg-orange-600 shrink-0"
                  title="Chưa đọc"
                ></span>
                <h2 :class="['text-sm leading-snug truncate', !item.isRead ? 'font-bold text-stone-900' : 'font-semibold text-stone-800']">
                  {{ item.title }}
                </h2>
                <span
                  :class="[
                    'inline-flex rounded border px-2 py-0.5 text-[10px] font-semibold',
                    getNotificationTypeMeta(item.type).badgeClass,
                  ]"
                >
                  {{ getNotificationTypeMeta(item.type).label }}
                </span>
              </div>

              <p class="mt-1 text-xs text-stone-600 line-clamp-2 leading-relaxed">
                {{ item.body || 'Không có nội dung chi tiết.' }}
              </p>

              <p class="mt-1.5 text-[11px] text-stone-400 tabular-nums">
                {{ formatRelativeNotificationTime(item.createdAt) }}
              </p>
            </div>
          </div>

          <div class="flex items-center gap-2 self-end sm:self-center shrink-0" @click.stop>
            <button
              v-if="!item.isRead"
              type="button"
              class="h-8 rounded-md border border-stone-300 bg-white px-2.5 text-xs font-semibold text-stone-700 hover:bg-stone-100 hover:border-stone-400 transition-colors cursor-pointer"
              @click="handleMarkSingleRead(item)"
            >
              Đánh dấu đã đọc
            </button>
            <button
              type="button"
              class="inline-flex h-8 items-center gap-1 rounded-md border border-orange-300 bg-orange-50 px-3 text-xs font-semibold text-orange-800 hover:bg-orange-100 transition-colors cursor-pointer"
              @click="openDetail(item)"
            >
              <EyeIcon class="h-3.5 w-3.5" />
              <span>Xem chi tiết</span>
            </button>
          </div>
        </article>
      </div>

      <PaginationBar
        v-if="totalCount > 0"
        v-model:page="page"
        v-model:page-size="pageSize"
        :total-count="totalCount"
        :total-pages="totalPages"
        :disabled="loading"
        item-label="thông báo"
        @change="loadInbox"
      />
    </div>

    <NotificationDetailModal
      :open="showDetailModal"
      :notification="selectedNotification"
      :user-role="userRole"
      :show-view-all="false"
      @close="showDetailModal = false"
    />
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from "vue";
import { useRoute } from "vue-router";
import {
  ArrowPathIcon,
  BellAlertIcon,
  CheckIcon,
  EyeIcon,
} from "@heroicons/vue/24/outline";
import authService from "@/services/auth.service";
import notificationService from "@/services/notification.service";
import { useNotificationState } from "@/services/notification-state";
import {
  NOTIFICATION_TYPE_OPTIONS,
  getNotificationTypeMeta,
  formatRelativeNotificationTime,
} from "@/utils/notification.helper";
import EmptyState from "@/components/common/EmptyState.vue";
import PaginationBar from "@/components/common/PaginationBar.vue";
import NotificationDetailModal from "@/components/notification/NotificationDetailModal.vue";

const route = useRoute();
const {
  unreadCount,
  normalizeNotification,
  fetchRecentNotifications,
  markNotificationRead,
  markAllNotificationsRead,
} = useNotificationState();

const userRole = computed(() => authService.getRole()?.toLowerCase() || "admin");

const items = ref([]);
const page = ref(1);
const pageSize = ref(15);
const totalCount = ref(0);
const unreadOnly = ref(false);
const typeFilter = ref("");
const loading = ref(false);
const errorMessage = ref("");

const showDetailModal = ref(false);
const selectedNotification = ref(null);

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)));

async function loadInbox() {
  loading.value = true;
  errorMessage.value = "";
  try {
    const res = await notificationService.getInbox({
      page: page.value,
      pageSize: pageSize.value,
      unreadOnly: unreadOnly.value,
      type: typeFilter.value,
    });
    const rawItems = Array.isArray(res?.items) ? res.items : [];
    items.value = rawItems.map(normalizeNotification).filter(Boolean);
    totalCount.value = res?.total ?? items.value.length;

    const queryId = Number(route.query.id);
    if (queryId && !showDetailModal.value) {
      const target = items.value.find((n) => n.id === queryId);
      if (target) {
        await openDetail(target);
      }
    }
  } catch (err) {
    errorMessage.value =
      err.response?.data?.message || "Không thể tải danh sách thông báo. Vui lòng thử lại.";
  } finally {
    loading.value = false;
  }
}

function setUnreadFilter(val) {
  unreadOnly.value = val;
  page.value = 1;
  loadInbox();
}

function handleTypeChange() {
  page.value = 1;
  loadInbox();
}

async function handleMarkSingleRead(item) {
  if (!item || item.isRead) return;
  item.isRead = true;
  await markNotificationRead(item.id);
  if (unreadOnly.value) {
    await loadInbox();
  }
}

async function handleMarkAllRead() {
  await markAllNotificationsRead();
  items.value.forEach((item) => {
    item.isRead = true;
  });
  if (unreadOnly.value) {
    await loadInbox();
  }
}

async function openDetail(item) {
  selectedNotification.value = { ...item, isRead: true };
  showDetailModal.value = true;
  if (!item.isRead) {
    item.isRead = true;
    await markNotificationRead(item.id);
  }
}

watch(
  () => route.query.id,
  (newId) => {
    const idNum = Number(newId);
    if (!idNum) return;
    const found = items.value.find((n) => n.id === idNum);
    if (found) openDetail(found);
  }
);

onMounted(async () => {
  await Promise.all([loadInbox(), fetchRecentNotifications({ force: true })]);
});
</script>
