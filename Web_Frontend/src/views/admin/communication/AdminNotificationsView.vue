<template>
  <section class="space-y-5">
    <header class="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <p class="text-xs font-semibold uppercase tracking-[0.18em] text-orange-500">Giao tiếp &amp; CSKH</p>
        <h1 class="mt-1 text-2xl font-bold text-orange-950">Quản lý thông báo</h1>
        <p class="mt-1 text-sm text-gray-500">
          Tạo thông báo cho phụ huynh, giáo viên và theo dõi trạng thái gửi trong hệ thống.
        </p>
      </div>
      <div class="flex gap-2">
        <button
          type="button"
          :disabled="loading"
          class="inline-flex items-center gap-2 rounded-xl border border-orange-200 bg-white px-4 py-2.5 text-sm font-semibold text-orange-700 shadow-sm transition hover:bg-orange-50 disabled:cursor-not-allowed disabled:opacity-60"
          @click="loadHistory"
        >
          <ArrowPathIcon class="h-4 w-4" :class="{ 'animate-spin': loading }" />
          Làm mới
        </button>
        <button
          type="button"
          class="inline-flex items-center gap-2 rounded-xl bg-orange-500 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-orange-600"
          @click="showCompose = true"
        >
          <PlusIcon class="h-4 w-4" />
          Soạn thông báo
        </button>
      </div>
    </header>

    <section class="grid grid-cols-2 gap-3 sm:grid-cols-4" aria-label="Thống kê thông báo">
      <article
        v-for="stat in stats"
        :key="stat.label"
        class="flex items-center gap-3 rounded-2xl border border-orange-100 bg-white p-4 shadow-sm"
      >
        <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl" :class="stat.background">
          <component :is="stat.icon" class="h-5 w-5" :class="stat.color" />
        </div>
        <div class="min-w-0">
          <p class="text-xs font-medium text-gray-500">{{ stat.label }}</p>
          <p class="mt-1 truncate text-xl font-bold text-gray-900">{{ stat.value }}</p>
        </div>
      </article>
    </section>

    <p
      v-if="successMessage"
      role="status"
      class="rounded-xl border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700"
    >
      {{ successMessage }}
    </p>

    <section class="overflow-hidden rounded-2xl border border-orange-100 bg-white shadow-sm">
      <div class="flex flex-col gap-3 border-b border-orange-100 p-4 sm:flex-row sm:items-center sm:justify-between">
        <form class="flex w-full gap-2 sm:max-w-lg" @submit.prevent="searchHistory">
          <label class="relative min-w-0 flex-1">
            <span class="sr-only">Tìm thông báo</span>
            <MagnifyingGlassIcon class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-gray-400" />
            <input
              v-model="keyword"
              type="search"
              maxlength="100"
              placeholder="Tìm theo tiêu đề hoặc nội dung..."
              class="w-full rounded-xl border border-orange-200 bg-white py-2.5 pl-9 pr-3 text-sm outline-none transition placeholder:text-gray-400 focus:border-orange-400 focus:ring-2 focus:ring-orange-100"
            />
          </label>
          <button
            type="submit"
            :disabled="loading"
            class="rounded-xl bg-orange-100 px-4 py-2.5 text-sm font-semibold text-orange-800 transition hover:bg-orange-200 disabled:opacity-60"
          >
            Tìm kiếm
          </button>
        </form>

        <label class="flex items-center gap-2 text-sm text-gray-600">
          <span>Loại</span>
          <select
            v-model="typeFilter"
            class="filter-select min-w-44"
            @change="searchHistory"
          >
            <option value="">Tất cả loại</option>
            <option value="ANNOUNCEMENT">Thông báo chung</option>
            <option value="SYSTEM">Hệ thống</option>
          </select>
        </label>
      </div>

      <div
        v-if="errorMessage"
        class="m-4 flex flex-col gap-3 rounded-xl border border-red-200 bg-red-50 p-4 sm:flex-row sm:items-center sm:justify-between"
      >
        <p class="text-sm text-red-700">{{ errorMessage }}</p>
        <button type="button" class="shrink-0 text-sm font-semibold text-red-700 underline" @click="loadHistory">
          Thử lại
        </button>
      </div>

      <div v-if="loading" class="flex min-h-56 items-center justify-center gap-3 text-sm text-orange-700">
        <ArrowPathIcon class="h-5 w-5 animate-spin" />
        Đang tải lịch sử thông báo...
      </div>
      <div
        v-else-if="!errorMessage && items.length === 0"
        class="flex min-h-56 flex-col items-center justify-center px-4 text-center"
      >
        <BellAlertIcon class="h-10 w-10 text-orange-300" />
        <p class="mt-3 font-semibold text-gray-700">Chưa có thông báo phù hợp</p>
        <p class="mt-1 text-sm text-gray-500">Tạo thông báo mới hoặc thử thay đổi bộ lọc tìm kiếm.</p>
      </div>

      <template v-else-if="!errorMessage">
        <div class="border-b border-orange-50 px-4 py-3 text-xs text-gray-500">
          Hiển thị
          <span class="font-semibold text-orange-700">{{ items.length }}</span>
          thông báo · Tổng cộng {{ total }} thông báo
        </div>
        <div class="overflow-x-auto">
          <table class="w-full min-w-[900px] text-left text-sm">
            <thead class="bg-orange-50/80 text-xs uppercase tracking-wide text-orange-900">
              <tr>
                <th scope="col" class="px-5 py-3.5 font-semibold">Thông báo</th>
                <th scope="col" class="px-5 py-3.5 font-semibold">Đối tượng</th>
                <th scope="col" class="px-5 py-3.5 font-semibold">Ngày tạo</th>
                <th scope="col" class="px-5 py-3.5 font-semibold">Trạng thái</th>
                <th scope="col" class="px-5 py-3.5 text-right font-semibold">Đã đọc</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100">
              <tr v-for="item in items" :key="item.id" class="transition hover:bg-orange-50/40">
                <td class="max-w-[420px] px-5 py-4">
                  <div class="flex items-start gap-3">
                    <span class="mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-orange-100 text-orange-600">
                      <BellAlertIcon class="h-4 w-4" />
                    </span>
                    <div class="min-w-0">
                      <p class="truncate font-semibold text-gray-900">{{ item.title }}</p>
                      <p class="mt-1 line-clamp-2 text-xs leading-5 text-gray-500">{{ item.body || 'Không có nội dung chi tiết.' }}</p>
                      <span class="mt-1 inline-flex rounded-full bg-gray-100 px-2 py-0.5 text-[10px] font-semibold text-gray-600">
                        {{ typeLabel(item.type) }}
                      </span>
                    </div>
                  </div>
                </td>
                <td class="px-5 py-4 text-gray-700">{{ item.recipientGroup }}</td>
                <td class="whitespace-nowrap px-5 py-4 text-gray-600">{{ formatDate(item.createdAt) }}</td>
                <td class="px-5 py-4">
                  <span
                    class="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold"
                    :class="statusClass(item.deliveryStatus)"
                  >
                    <span class="h-1.5 w-1.5 rounded-full" :class="statusDotClass(item.deliveryStatus)"></span>
                    {{ statusLabel(item.deliveryStatus) }}
                  </span>
                  <p class="mt-1 text-xs text-gray-500">{{ item.recipientCount }} người nhận</p>
                </td>
                <td class="whitespace-nowrap px-5 py-4 text-right text-gray-700">
                  {{ item.readCount }} / {{ item.recipientCount }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="flex items-center justify-between border-t border-orange-50 px-4 py-3">
          <p class="text-xs text-gray-500">Trang {{ page }} / {{ pageCount }}</p>
          <div class="flex gap-2">
            <button
              type="button"
              :disabled="page <= 1 || loading"
              class="rounded-lg border border-gray-200 px-3 py-1.5 text-xs font-semibold text-gray-700 hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-50"
              @click="changePage(page - 1)"
            >
              Trước
            </button>
            <button
              type="button"
              :disabled="page >= pageCount || loading"
              class="rounded-lg border border-gray-200 px-3 py-1.5 text-xs font-semibold text-gray-700 hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-50"
              @click="changePage(page + 1)"
            >
              Sau
            </button>
          </div>
        </div>
      </template>
    </section>

    <div
      v-if="showCompose"
      class="fixed inset-0 z-50 flex items-center justify-center bg-gray-950/50 p-4"
      @click.self="closeCompose"
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="notification-compose-title"
        class="w-full max-w-xl space-y-4 rounded-3xl border border-orange-100 bg-white p-6 shadow-xl"
      >
        <header class="flex items-start justify-between border-b border-orange-100 pb-3">
          <div>
            <h2 id="notification-compose-title" class="text-lg font-bold text-orange-950">Soạn thông báo mới</h2>
            <p class="mt-1 text-xs text-gray-500">Thông báo sẽ được xếp hàng và gửi qua hộp thư trong ứng dụng.</p>
          </div>
          <button
            type="button"
            aria-label="Đóng"
            :disabled="submitting"
            class="rounded-lg px-2 py-1 text-gray-400 hover:bg-gray-100 hover:text-gray-700 disabled:opacity-50"
            @click="closeCompose"
          >
            ✕
          </button>
        </header>

        <form class="space-y-4" @submit.prevent="submitNotification">
          <label class="block">
            <span class="mb-1.5 block text-sm font-semibold text-gray-700">Đối tượng nhận</span>
            <select v-model="form.recipientGroup" required class="filter-select">
              <option value="PARENT">Tất cả phụ huynh</option>
              <option value="TEACHER">Tất cả giáo viên</option>
              <option value="PARENT_AND_TEACHER">Phụ huynh và giáo viên</option>
            </select>
          </label>

          <label class="block">
            <span class="mb-1.5 block text-sm font-semibold text-gray-700">Loại thông báo</span>
            <select v-model="form.type" required class="filter-select">
              <option value="ANNOUNCEMENT">Thông báo chung</option>
              <option value="SYSTEM">Hệ thống</option>
            </select>
          </label>

          <label class="block">
            <span class="mb-1.5 block text-sm font-semibold text-gray-700">Tiêu đề</span>
            <input
              v-model.trim="form.title"
              required
              maxlength="200"
              type="text"
              placeholder="Nhập tiêu đề thông báo"
              class="w-full rounded-xl border border-orange-200 px-3 py-2.5 text-sm outline-none transition focus:border-orange-400 focus:ring-2 focus:ring-orange-100"
            />
          </label>

          <label class="block">
            <span class="mb-1.5 block text-sm font-semibold text-gray-700">Nội dung</span>
            <textarea
              v-model.trim="form.body"
              maxlength="2000"
              rows="5"
              placeholder="Nhập nội dung gửi đến người nhận..."
              class="w-full resize-y rounded-xl border border-orange-200 px-3 py-2.5 text-sm outline-none transition focus:border-orange-400 focus:ring-2 focus:ring-orange-100"
            ></textarea>
          </label>

          <p v-if="composeError" role="alert" class="rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
            {{ composeError }}
          </p>

          <footer class="flex justify-end gap-2 border-t border-orange-100 pt-4">
            <button
              type="button"
              :disabled="submitting"
              class="rounded-xl px-4 py-2.5 text-sm font-semibold text-gray-600 hover:bg-gray-100 disabled:opacity-50"
              @click="closeCompose"
            >
              Hủy
            </button>
            <button
              type="submit"
              :disabled="submitting || !form.title.trim()"
              class="inline-flex items-center gap-2 rounded-xl bg-orange-500 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-orange-600 disabled:cursor-not-allowed disabled:opacity-60"
            >
              <PaperAirplaneIcon class="h-4 w-4" />
              {{ submitting ? 'Đang gửi...' : 'Đưa vào hàng đợi' }}
            </button>
          </footer>
        </form>
      </section>
    </div>
  </section>
</template>

<script setup>
import { computed, onMounted, ref } from "vue";
import {
  ArrowPathIcon,
  BellAlertIcon,
  CheckCircleIcon,
  ClockIcon,
  ExclamationTriangleIcon,
  MagnifyingGlassIcon,
  PaperAirplaneIcon,
  PlusIcon,
  UsersIcon,
} from "@heroicons/vue/24/outline";
import adminNotificationService from "@/services/admin-notification.service";
import { validateTextLength } from "@/utils/validation.helper";

const pageSize = 20;
const page = ref(1);
const keyword = ref("");
const appliedKeyword = ref("");
const typeFilter = ref("");
const items = ref([]);
const total = ref(0);
const summary = ref({ pendingCount: 0, failedCount: 0, deliveredCount: 0, readCount: 0 });
const loading = ref(false);
const submitting = ref(false);
const errorMessage = ref("");
const successMessage = ref("");
const showCompose = ref(false);
const composeError = ref("");
const form = ref({
  recipientGroup: "PARENT",
  type: "ANNOUNCEMENT",
  title: "",
  body: "",
});

const pageCount = computed(() => Math.max(1, Math.ceil(total.value / pageSize)));
const stats = computed(() => [
  {
    label: "Tổng thông báo",
    value: total.value,
    icon: BellAlertIcon,
    background: "bg-orange-100",
    color: "text-orange-600",
  },
  {
    label: "Đang xử lý",
    value: summary.value.pendingCount,
    icon: ClockIcon,
    background: "bg-amber-100",
    color: "text-amber-600",
  },
  {
    label: "Lượt nhận",
    value: summary.value.deliveredCount,
    icon: UsersIcon,
    background: "bg-blue-100",
    color: "text-blue-600",
  },
  {
    label: "Đã đọc",
    value: summary.value.readCount,
    icon: CheckCircleIcon,
    background: "bg-green-100",
    color: "text-green-600",
  },
]);

async function loadHistory() {
  loading.value = true;
  errorMessage.value = "";
  try {
    const response = await adminNotificationService.getHistory({
      page: page.value,
      pageSize,
      keyword: appliedKeyword.value || undefined,
      type: typeFilter.value || undefined,
    });
    items.value = response.items ?? [];
    total.value = response.total ?? 0;
    summary.value = {
      pendingCount: response.pendingCount ?? 0,
      failedCount: response.failedCount ?? 0,
      deliveredCount: response.deliveredCount ?? 0,
      readCount: response.readCount ?? 0,
    };
  } catch (error) {
    errorMessage.value = error.response?.data?.message || "Không thể tải lịch sử thông báo. Vui lòng thử lại.";
  } finally {
    loading.value = false;
  }
}

function searchHistory() {
  appliedKeyword.value = keyword.value.trim();
  page.value = 1;
  loadHistory();
}

function changePage(nextPage) {
  page.value = nextPage;
  loadHistory();
}

async function submitNotification() {
  const titleCheck = validateTextLength(form.value.title, {
    label: "Tiêu đề thông báo",
    max: 200,
    required: true,
  });
  if (!titleCheck.valid) {
    composeError.value = titleCheck.message;
    return;
  }

  const bodyCheck = validateTextLength(form.value.body, {
    label: "Nội dung thông báo",
    max: 2000,
  });
  if (!bodyCheck.valid) {
    composeError.value = bodyCheck.message;
    return;
  }

  submitting.value = true;
  composeError.value = "";
  try {
    const result = await adminNotificationService.create({
      ...form.value,
      title: form.value.title.trim(),
      body: form.value.body.trim() || null,
    });
    successMessage.value = result.message || "Thông báo đã được đưa vào hàng đợi gửi.";
    showCompose.value = false;
    form.value = { recipientGroup: "PARENT", type: "ANNOUNCEMENT", title: "", body: "" };
    page.value = 1;
    appliedKeyword.value = "";
    keyword.value = "";
    typeFilter.value = "";
    await loadHistory();
  } catch (error) {
    composeError.value = error.response?.data?.message || "Không thể tạo thông báo. Vui lòng thử lại.";
  } finally {
    submitting.value = false;
  }
}

function closeCompose() {
  if (submitting.value) return;
  showCompose.value = false;
  composeError.value = "";
}

function typeLabel(type) {
  return type === "ANNOUNCEMENT" ? "Thông báo chung" : type === "SYSTEM" ? "Hệ thống" : type || "Khác";
}

function statusLabel(status) {
  if (status === 0) return "Chờ gửi";
  if (status === 1) return "Đang gửi";
  if (status === 2 || status === 4) return "Hoàn tất";
  if (status === 3) return "Thất bại";
  return "Chưa xử lý";
}

function statusClass(status) {
  if (status === 2 || status === 4) return "bg-green-100 text-green-800";
  if (status === 3) return "bg-red-100 text-red-800";
  if (status === 1) return "bg-blue-100 text-blue-800";
  return "bg-amber-100 text-amber-800";
}

function statusDotClass(status) {
  if (status === 2 || status === 4) return "bg-green-500";
  if (status === 3) return "bg-red-500";
  if (status === 1) return "bg-blue-500";
  if (status === 0) return "bg-amber-500";
  return "bg-gray-400";
}

function formatDate(value) {
  if (!value) return "—";
  return new Intl.DateTimeFormat("vi-VN", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

onMounted(loadHistory);
</script>
