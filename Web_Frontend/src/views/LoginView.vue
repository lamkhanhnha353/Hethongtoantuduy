<template>
  <main class="flex min-h-screen items-center justify-center bg-orange-50 px-4 py-10">
    <section class="w-full max-w-md rounded-3xl bg-white p-8 shadow-xl shadow-orange-100 sm:p-10">
      <div class="mb-8 text-center">
        <p class="mb-3 text-sm font-semibold uppercase tracking-[0.2em] text-orange-600">
          Toán Tư Duy
        </p>
        <h1 class="text-3xl font-bold text-gray-900">Đăng nhập</h1>
        <p class="mt-2 text-sm text-gray-500">Đăng nhập để tiếp tục vào hệ thống.</p>
      </div>

      <form class="space-y-5" @submit.prevent="handleSubmit">
        <div>
          <label for="phone" class="mb-2 block text-sm font-medium text-gray-700">
            Số điện thoại
          </label>
          <input
            id="phone"
            v-model.trim="phone"
            type="tel"
            autocomplete="username"
            required
            class="w-full rounded-xl border border-orange-200 px-4 py-3 text-gray-900 outline-none transition focus:border-orange-400 focus:ring-2 focus:ring-orange-100"
            placeholder="Nhập số điện thoại"
          />
        </div>

        <div>
          <label for="password" class="mb-2 block text-sm font-medium text-gray-700">
            Mật khẩu
          </label>
          <input
            id="password"
            v-model="password"
            type="password"
            autocomplete="current-password"
            required
            class="w-full rounded-xl border border-orange-200 px-4 py-3 text-gray-900 outline-none transition focus:border-orange-400 focus:ring-2 focus:ring-orange-100"
            placeholder="Nhập mật khẩu"
          />
        </div>

        <p
          v-if="errorMessage"
          role="alert"
          class="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700"
        >
          {{ errorMessage }}
        </p>

        <button
          type="submit"
          :disabled="isSubmitting"
          class="w-full rounded-xl bg-orange-600 px-4 py-3 font-semibold text-white transition hover:bg-orange-700 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {{ isSubmitting ? "Đang đăng nhập..." : "Đăng nhập" }}
        </button>
      </form>
    </section>
  </main>
</template>

<script setup>
import { ref } from "vue";
import { useRouter } from "vue-router";
import authService from "@/services/auth.service";

const router = useRouter();
const phone = ref("");
const password = ref("");
const errorMessage = ref("");
const isSubmitting = ref(false);

async function handleSubmit() {
  errorMessage.value = "";
  isSubmitting.value = true;

  try {
    const session = await authService.login({
      phone: phone.value,
      password: password.value,
    });

    await router.replace(
      session.role.toLowerCase() === "admin"
        ? { name: "dashboard" }
        : { name: "teacher-dashboard" },
    );
  } catch (error) {
    const response = error.response?.data;
    errorMessage.value =
      response?.message ||
      response?.detail ||
      response?.title ||
      error.message ||
      "Đăng nhập không thành công. Vui lòng thử lại.";
  } finally {
    isSubmitting.value = false;
  }
}
</script>
