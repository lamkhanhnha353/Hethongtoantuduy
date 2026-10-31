<template>
  <main class="min-h-screen bg-orange-50 px-4 py-8 sm:px-8">
    <div class="mx-auto max-w-5xl">
      <header class="flex items-center justify-between gap-4">
        <div>
          <p class="text-sm font-semibold uppercase tracking-[0.16em] text-orange-600">
            Khu vực giáo viên
          </p>
          <h1 class="mt-2 text-3xl font-bold text-gray-900">Xin chào!</h1>
        </div>
        <button
          type="button"
          class="rounded-xl border border-orange-200 bg-white px-4 py-2 text-sm font-semibold text-gray-700 transition hover:bg-orange-100"
          @click="logout"
        >
          Đăng xuất
        </button>
      </header>

      <section class="mt-8 rounded-3xl bg-white p-6 shadow-lg shadow-orange-100 sm:p-8">
        <h2 class="text-xl font-semibold text-gray-900">Trang dành cho giáo viên</h2>
        <p class="mt-2 text-gray-600">
          Bạn đã đăng nhập thành công. Các chức năng quản lý lớp học và giảng dạy sẽ được
          hiển thị tại đây.
        </p>
      </section>
    </div>
  </main>
</template>

<script setup>
import { useRouter } from "vue-router";
import authService from "@/services/auth.service";

const router = useRouter();

function logout() {
  authService.logout();
  router.replace({ name: "login" });
}
</script>
