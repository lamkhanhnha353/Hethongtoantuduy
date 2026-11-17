<template>
  <section class="space-y-6">
    <header>
      <h1 class="text-2xl font-bold text-stone-900">Quản lý Tài khoản &amp; Bảo mật</h1>
      <p class="mt-1 text-sm text-stone-500">
        Theo dõi tài khoản và xử lý các thao tác bảo mật trong hệ thống.
      </p>
    </header>

    <div class="grid gap-4 sm:grid-cols-3">
      <article class="rounded-lg border border-stone-300 bg-white p-5 shadow-[0_1px_2px_rgba(0,0,0,0.04)]">
        <p class="text-xs font-semibold uppercase tracking-wide text-stone-500">Tổng tài khoản</p>
        <p class="mt-2 text-2xl font-bold text-stone-900 tabular-nums">{{ totalAccountsCount }}</p>
      </article>
      <article class="rounded-lg border border-stone-300 bg-white p-5 shadow-[0_1px_2px_rgba(0,0,0,0.04)]">
        <p class="text-xs font-semibold uppercase tracking-wide text-stone-500">Đang hoạt động</p>
        <p class="mt-2 text-2xl font-bold text-emerald-700 tabular-nums">{{ activeCount }}</p>
      </article>
      <article class="rounded-lg border border-stone-300 bg-white p-5 shadow-[0_1px_2px_rgba(0,0,0,0.04)]">
        <p class="text-xs font-semibold uppercase tracking-wide text-stone-500">Đã khóa</p>
        <p class="mt-2 text-2xl font-bold text-rose-700 tabular-nums">{{ blockedCount }}</p>
      </article>
    </div>

    <div class="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
      <div class="flex flex-1 flex-col gap-3 sm:flex-row sm:items-center">
        <label class="sr-only" for="account-search">Tìm tài khoản</label>
        <input
          id="account-search"
          v-model="keyword"
          type="search"
          maxlength="100"
          placeholder="Tìm theo tên, email hoặc số điện thoại..."
          class="h-9 w-full rounded-md border border-stone-300 bg-stone-50/70 px-3.5 text-sm text-stone-900 outline-none transition placeholder:text-stone-400 focus:border-orange-600 focus:bg-white focus:ring-2 focus:ring-orange-500/20 sm:max-w-md"
        />
        <select
          v-model="roleFilter"
          aria-label="Lọc theo vai trò"
          class="h-9 rounded-md border border-stone-300 bg-stone-50/70 px-3 text-sm text-stone-900 outline-none transition focus:border-orange-600 focus:bg-white focus:ring-2 focus:ring-orange-500/20"
        >
          <option value="ALL">Tất cả vai trò</option>
          <option value="Admin">Admin</option>
          <option value="Teacher">Teacher</option>
          <option value="Parent">Parent</option>
        </select>
        <select
          v-model="statusFilter"
          aria-label="Lọc theo trạng thái"
          class="h-9 rounded-md border border-stone-300 bg-stone-50/70 px-3 text-sm text-stone-900 outline-none transition focus:border-orange-600 focus:bg-white focus:ring-2 focus:ring-orange-500/20"
        >
          <option value="ALL">Tất cả trạng thái</option>
          <option value="true">Đang hoạt động</option>
          <option value="false">Đã khóa</option>
        </select>
      </div>
      <button
        type="button"
        :disabled="isLoading"
        class="h-9 rounded-md border border-stone-300 bg-white px-3.5 text-sm font-semibold text-stone-800 transition hover:border-stone-400 hover:bg-stone-100 disabled:cursor-not-allowed disabled:opacity-60"
        @click="loadAccounts"
      >
        Làm mới
      </button>
    </div>

    <p
      v-if="notice"
      role="status"
      class="rounded-md border px-4 py-3 text-sm"
      :class="noticeType === 'error'
        ? 'border-rose-200 bg-rose-50 text-rose-700'
        : 'border-emerald-200 bg-emerald-50 text-emerald-700'"
    >
      {{ notice }}
    </p>

    <div class="overflow-hidden rounded-lg border border-stone-300 bg-white shadow-[0_1px_2px_rgba(0,0,0,0.04)]">
      <div class="overflow-x-auto">
        <table class="w-full min-w-[820px] text-left text-sm">
          <thead class="border-b-2 border-stone-300 bg-stone-100 text-xs font-bold uppercase tracking-wider text-stone-700">
            <tr>
              <th class="px-5 py-3.5">Tài khoản</th>
              <th class="px-5 py-3.5">Vai trò</th>
              <th class="px-5 py-3.5">Ngày tạo</th>
              <th class="px-5 py-3.5">Trạng thái</th>
              <th class="px-5 py-3.5 text-right">Thao tác</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-stone-200 text-stone-700">
            <tr v-if="isLoading">
              <td colspan="5" class="px-5 py-10 text-center text-stone-500">Đang tải tài khoản...</td>
            </tr>
            <tr v-else-if="errorMessage">
              <td colspan="5" class="px-5 py-10 text-center text-rose-700">{{ errorMessage }}</td>
            </tr>
            <tr v-else-if="accounts.length === 0">
              <td colspan="5" class="px-5 py-10 text-center text-stone-500">
                Không tìm thấy tài khoản phù hợp.
              </td>
            </tr>
            <tr
              v-for="account in accounts"
              :key="account.id"
              class="transition-colors hover:bg-orange-50/50"
            >
              <td class="px-5 py-4">
                <p class="font-semibold text-stone-900">{{ account.fullName || 'Chưa cập nhật tên' }}</p>
                <p class="mt-0.5 text-xs text-stone-600 font-mono tabular-nums">{{ account.phone || 'Chưa có số điện thoại' }}</p>
                <p class="mt-0.5 text-xs text-stone-500">
                  {{ account.email || 'Chưa có email' }}
                  <span v-if="account.email && !account.emailConfirmed" class="text-amber-700">
                    · Chưa xác minh
                  </span>
                </p>
              </td>
              <td class="px-5 py-4">
                <span
                  class="inline-flex rounded border px-2 py-0.5 text-xs font-semibold"
                  :class="roleClass(account.role)"
                >
                  {{ roleLabel(account.role) }}
                </span>
              </td>
              <td class="px-5 py-4 text-stone-600 tabular-nums">{{ formatDate(account.createdAt) }}</td>
              <td class="px-5 py-4">
                <div class="flex items-center gap-3">
                  <button
                    type="button"
                    role="switch"
                    :aria-checked="account.isActive"
                    :aria-label="`${account.isActive ? 'Khóa' : 'Mở khóa'} tài khoản ${account.fullName || account.phone}`"
                    :title="isCurrentAdmin(account) ? 'Không thể tự khóa tài khoản đang đăng nhập' : ''"
                    :disabled="Boolean(busyActions[account.id]) || isCurrentAdmin(account)"
                    class="relative inline-flex h-6 w-11 shrink-0 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-orange-500/30 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                    :class="account.isActive ? 'bg-emerald-600' : 'bg-stone-300'"
                    @click="toggleStatus(account)"
                  >
                    <span
                      class="inline-block h-5 w-5 transform rounded-full bg-white shadow transition-transform"
                      :class="account.isActive ? 'translate-x-5' : 'translate-x-0.5'"
                    />
                  </button>
                  <span
                    class="text-xs font-semibold"
                    :class="account.isActive ? 'text-emerald-700' : 'text-rose-700'"
                  >
                    {{ busyActions[account.id] === 'status'
                      ? 'Đang cập nhật...'
                      : account.isActive ? 'Hoạt động' : 'Đã khóa' }}
                  </span>
                </div>
              </td>
              <td class="px-5 py-4">
                <div class="flex justify-end gap-2">
                  <button
                    type="button"
                    :disabled="Boolean(busyActions[account.id]) || !canResetPassword(account)"
                    :title="resetDisabledReason(account)"
                    class="h-8 rounded-md border border-stone-300 bg-white px-3 text-xs font-semibold text-stone-800 transition hover:border-stone-400 hover:bg-stone-100 disabled:cursor-not-allowed disabled:opacity-50"
                    @click="resetPassword(account)"
                  >
                    {{ busyActions[account.id] === 'reset' ? 'Đang gửi...' : 'Đặt lại mật khẩu' }}
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <PaginationBar
        v-model:page="page"
        v-model:page-size="pageSize"
        :total-count="totalCount"
        :total-pages="totalPages"
        :disabled="isLoading"
        item-label="tài khoản"
        @change="loadAccounts"
      />
    </div>
  </section>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue";
import PaginationBar from "@/components/common/PaginationBar.vue";
import { usePagination } from "@/composables/usePagination";
import adminAccountService from "@/services/admin-account.service";

const { page, pageSize, totalCount, totalPages, applyPagedResult, resetPage } = usePagination({
  initialPageSize: 20,
});

const accounts = ref([]);
const keyword = ref("");
const roleFilter = ref("ALL");
const statusFilter = ref("ALL");
const activeCount = ref(0);
const blockedCount = ref(0);
const isLoading = ref(true);
const errorMessage = ref("");
const notice = ref("");
const noticeType = ref("success");
const busyActions = ref({});

let searchTimer = null;
let loadRequestId = 0;

const totalAccountsCount = computed(() => activeCount.value + blockedCount.value);

onMounted(loadAccounts);

onBeforeUnmount(() => {
  if (searchTimer) clearTimeout(searchTimer);
});

watch(keyword, () => {
  if (searchTimer) clearTimeout(searchTimer);
  searchTimer = setTimeout(() => {
    resetPage();
    loadAccounts();
  }, 300);
});

watch([roleFilter, statusFilter], () => {
  resetPage();
  loadAccounts();
});

async function loadAccounts() {
  const currentReq = ++loadRequestId;
  isLoading.value = true;
  errorMessage.value = "";
  try {
    const isActiveParam = statusFilter.value === "ALL" ? undefined : statusFilter.value === "true";
    const paged = await adminAccountService.getPaged({
      page: page.value,
      pageSize: pageSize.value,
      keyword: keyword.value,
      role: roleFilter.value,
      isActive: isActiveParam,
    });
    if (currentReq !== loadRequestId) return;

    const items = applyPagedResult(paged);
    if (items.length === 0 && page.value > 1 && totalPages.value > 0 && page.value > totalPages.value) {
      page.value = totalPages.value;
      await loadAccounts();
      return;
    }

    accounts.value = items;
    activeCount.value = Number(paged?.activeCount ?? 0);
    blockedCount.value = Number(paged?.blockedCount ?? 0);
  } catch (error) {
    if (currentReq !== loadRequestId) return;
    errorMessage.value = error.response?.data?.message ?? "Không thể tải danh sách tài khoản.";
  } finally {
    if (currentReq === loadRequestId) {
      isLoading.value = false;
    }
  }
}

function roleLabel(role) {
  return ({ Admin: "Admin", Teacher: "Teacher", Parent: "Parent" })[role] ?? role ?? "Khác";
}

function roleClass(role) {
  return ({
    Admin: "border-rose-300 bg-rose-50 text-rose-800",
    Teacher: "border-emerald-300 bg-emerald-50 text-emerald-800",
    Parent: "border-orange-300 bg-orange-50 text-orange-800",
  })[role] ?? "border-stone-300 bg-stone-100 text-stone-700";
}

function formatDate(value) {
  if (!value) return "—";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "—" : new Intl.DateTimeFormat("vi-VN").format(date);
}

function isCurrentAdmin(account) {
  return account.role?.toLowerCase() === "admin" && account.isCurrentUser;
}

function canResetPassword(account) {
  return ["Teacher", "Parent"].includes(account.role)
    && Boolean(account.email)
    && account.emailConfirmed;
}

function resetDisabledReason(account) {
  if (!["Teacher", "Parent"].includes(account.role)) return "Chỉ hỗ trợ Teacher và Parent.";
  if (!account.email) return "Tài khoản chưa có email.";
  if (!account.emailConfirmed) return "Email cần được xác minh trước khi gửi mật khẩu tạm.";
  return "";
}

function setNotice(message, type = "success") {
  notice.value = message;
  noticeType.value = type;
}

async function toggleStatus(account) {
  const isActive = !account.isActive;
  const action = isActive ? "mở khóa" : "khóa";
  if (!window.confirm(`Bạn có chắc muốn ${action} tài khoản ${account.fullName || account.phone}?`)) return;

  busyActions.value = { ...busyActions.value, [account.id]: "status" };
  notice.value = "";
  try {
    await adminAccountService.updateStatus(account.id, isActive);
    account.isActive = isActive;
    activeCount.value = Math.max(0, activeCount.value + (isActive ? 1 : -1));
    blockedCount.value = Math.max(0, blockedCount.value + (isActive ? -1 : 1));
    setNotice(isActive
      ? "Đã mở khóa tài khoản. Người dùng cần đăng nhập lại."
      : "Đã khóa tài khoản và thu hồi các phiên đăng nhập.");
    if (statusFilter.value !== "ALL") {
      await loadAccounts();
    }
  } catch (error) {
    setNotice(error.response?.data?.message ?? "Không thể cập nhật trạng thái tài khoản.", "error");
  } finally {
    const next = { ...busyActions.value };
    delete next[account.id];
    busyActions.value = next;
  }
}

async function resetPassword(account) {
  if (!window.confirm(`Đặt lại mật khẩu và gửi mật khẩu tạm đến ${account.email}?`)) return;

  busyActions.value = { ...busyActions.value, [account.id]: "reset" };
  notice.value = "";
  try {
    const response = await adminAccountService.resetPassword(account.id);
    setNotice(response.message ?? "Đã gửi mật khẩu tạm qua email.");
  } catch (error) {
    setNotice(error.response?.data?.message ?? "Không thể đặt lại mật khẩu.", "error");
  } finally {
    const next = { ...busyActions.value };
    delete next[account.id];
    busyActions.value = next;
  }
}
</script>
