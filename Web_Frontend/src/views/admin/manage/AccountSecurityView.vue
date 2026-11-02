<template>
  <section class="space-y-6">
    <header>
      <h1 class="text-2xl font-bold text-gray-900">Quản lý Tài khoản &amp; Bảo mật</h1>
      <p class="mt-1 text-sm text-gray-500">
        Theo dõi tài khoản và xử lý các thao tác bảo mật trong hệ thống.
      </p>
    </header>

    <div class="grid gap-4 sm:grid-cols-3">
      <article class="rounded-2xl border border-orange-100 bg-white p-5 shadow-sm">
        <p class="text-sm font-medium text-gray-500">Tổng tài khoản</p>
        <p class="mt-2 text-2xl font-bold text-gray-900">{{ accounts.length }}</p>
      </article>
      <article class="rounded-2xl border border-green-100 bg-white p-5 shadow-sm">
        <p class="text-sm font-medium text-gray-500">Đang hoạt động</p>
        <p class="mt-2 text-2xl font-bold text-green-700">{{ activeCount }}</p>
      </article>
      <article class="rounded-2xl border border-red-100 bg-white p-5 shadow-sm">
        <p class="text-sm font-medium text-gray-500">Đã khóa</p>
        <p class="mt-2 text-2xl font-bold text-red-700">{{ blockedCount }}</p>
      </article>
    </div>

    <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      <label class="sr-only" for="account-search">Tìm tài khoản</label>
      <input
        id="account-search"
        v-model="keyword"
        type="search"
        placeholder="Tìm theo tên, email hoặc số điện thoại..."
        class="w-full rounded-xl border border-gray-200 bg-white px-4 py-2.5 text-sm outline-none transition focus:border-orange-400 focus:ring-2 focus:ring-orange-100 sm:max-w-md"
      />
      <button
        type="button"
        :disabled="isLoading"
        class="rounded-xl border border-gray-200 bg-white px-4 py-2.5 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-60"
        @click="loadAccounts"
      >
        Làm mới
      </button>
    </div>

    <p
      v-if="notice"
      role="status"
      class="rounded-xl border px-4 py-3 text-sm"
      :class="noticeType === 'error'
        ? 'border-red-200 bg-red-50 text-red-700'
        : 'border-green-200 bg-green-50 text-green-700'"
    >
      {{ notice }}
    </p>

    <div class="overflow-hidden rounded-2xl border border-orange-100 bg-white shadow-sm">
      <div class="overflow-x-auto">
        <table class="w-full min-w-[820px] text-left text-sm">
          <thead class="border-b border-orange-100 bg-orange-50 text-orange-950">
            <tr>
              <th class="px-5 py-4 font-semibold">Tài khoản</th>
              <th class="px-5 py-4 font-semibold">Vai trò</th>
              <th class="px-5 py-4 font-semibold">Ngày tạo</th>
              <th class="px-5 py-4 font-semibold">Trạng thái</th>
              <th class="px-5 py-4 text-right font-semibold">Thao tác</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100 text-gray-700">
            <tr v-if="isLoading">
              <td colspan="5" class="px-5 py-10 text-center text-gray-500">Đang tải tài khoản...</td>
            </tr>
            <tr v-else-if="errorMessage">
              <td colspan="5" class="px-5 py-10 text-center text-red-600">{{ errorMessage }}</td>
            </tr>
            <tr v-else-if="filteredAccounts.length === 0">
              <td colspan="5" class="px-5 py-10 text-center text-gray-500">
                Không tìm thấy tài khoản phù hợp.
              </td>
            </tr>
            <tr
              v-for="account in filteredAccounts"
              :key="account.id"
              class="transition-colors hover:bg-orange-50/40"
            >
              <td class="px-5 py-4">
                <p class="font-semibold text-gray-900">{{ account.fullName || 'Chưa cập nhật tên' }}</p>
                <p class="mt-0.5 text-xs text-gray-500">{{ account.phone || 'Chưa có số điện thoại' }}</p>
                <p class="mt-0.5 text-xs text-gray-500">
                  {{ account.email || 'Chưa có email' }}
                  <span v-if="account.email && !account.emailConfirmed" class="text-amber-600">
                    · Chưa xác minh
                  </span>
                </p>
              </td>
              <td class="px-5 py-4">
                <span
                  class="inline-flex rounded-full px-3 py-1 text-xs font-semibold"
                  :class="roleClass(account.role)"
                >
                  {{ roleLabel(account.role) }}
                </span>
              </td>
              <td class="px-5 py-4 text-gray-600">{{ formatDate(account.createdAt) }}</td>
              <td class="px-5 py-4">
                <div class="flex items-center gap-3">
                  <button
                    type="button"
                    role="switch"
                    :aria-checked="account.isActive"
                    :aria-label="`${account.isActive ? 'Khóa' : 'Mở khóa'} tài khoản ${account.fullName || account.phone}`"
                    :title="isCurrentAdmin(account) ? 'Không thể tự khóa tài khoản đang đăng nhập' : ''"
                    :disabled="Boolean(busyActions[account.id]) || isCurrentAdmin(account)"
                    class="relative inline-flex h-6 w-11 shrink-0 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-orange-400 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                    :class="account.isActive ? 'bg-green-500' : 'bg-gray-300'"
                    @click="toggleStatus(account)"
                  >
                    <span
                      class="inline-block h-5 w-5 transform rounded-full bg-white shadow transition-transform"
                      :class="account.isActive ? 'translate-x-5' : 'translate-x-0.5'"
                    />
                  </button>
                  <span
                    class="text-xs font-medium"
                    :class="account.isActive ? 'text-green-700' : 'text-red-700'"
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
                    class="rounded-lg border border-orange-200 px-3 py-2 text-xs font-semibold text-orange-700 hover:bg-orange-50 disabled:cursor-not-allowed disabled:opacity-50"
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
    </div>
  </section>
</template>

<script setup>
import { computed, onMounted, ref } from "vue";
import adminAccountService from "@/services/admin-account.service";

const accounts = ref([]);
const keyword = ref("");
const isLoading = ref(true);
const errorMessage = ref("");
const notice = ref("");
const noticeType = ref("success");
const busyActions = ref({});

const activeCount = computed(() => accounts.value.filter((account) => account.isActive).length);
const blockedCount = computed(() => accounts.value.length - activeCount.value);
const filteredAccounts = computed(() => {
  const search = keyword.value.trim().toLocaleLowerCase();
  if (!search) return accounts.value;
  return accounts.value.filter((account) =>
    [account.fullName, account.email, account.phone, account.role]
      .some((value) => String(value ?? "").toLocaleLowerCase().includes(search)),
  );
});

onMounted(loadAccounts);

async function loadAccounts() {
  isLoading.value = true;
  errorMessage.value = "";
  try {
    accounts.value = await adminAccountService.getAll();
  } catch (error) {
    errorMessage.value = error.response?.data?.message ?? "Không thể tải danh sách tài khoản.";
  } finally {
    isLoading.value = false;
  }
}

function roleLabel(role) {
  return ({ Admin: "Admin", Teacher: "Teacher", Parent: "Parent" })[role] ?? role ?? "Khác";
}

function roleClass(role) {
  return ({
    Admin: "bg-red-100 text-red-700",
    Teacher: "bg-green-100 text-green-700",
    Parent: "bg-orange-100 text-orange-700",
  })[role] ?? "bg-gray-100 text-gray-700";
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
    setNotice(isActive
      ? "Đã mở khóa tài khoản. Người dùng cần đăng nhập lại."
      : "Đã khóa tài khoản và thu hồi các phiên đăng nhập.");
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
