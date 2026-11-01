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
          <div class="relative">
            <input
              id="password"
              v-model="password"
              :type="showPassword ? 'text' : 'password'"
              autocomplete="current-password"
              required
              class="w-full rounded-xl border border-orange-200 px-4 py-3 pr-20 text-gray-900 outline-none transition focus:border-orange-400 focus:ring-2 focus:ring-orange-100"
              placeholder="Nhập mật khẩu"
            />
            <button
              type="button"
              :aria-label="showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'"
              :aria-pressed="showPassword"
              class="absolute inset-y-0 right-3 my-auto flex items-center text-orange-600 hover:text-orange-700"
              @click="showPassword = !showPassword"
            >
              <svg
                v-if="showPassword"
                aria-hidden="true"
                class="h-5 w-5"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M3 3l18 18M10.6 10.6a2 2 0 002.8 2.8M9.9 5.2A10.7 10.7 0 0112 5c5 0 8.3 4.5 9 7-.3 1.1-1 2.2-1.8 3.2M6.2 6.2C4.4 7.4 3.3 9.2 3 12c.7 2.5 4 7 9 7 1.5 0 2.8-.4 4-1.1"
                />
              </svg>
              <svg
                v-else
                aria-hidden="true"
                class="h-5 w-5"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M3 12s3.3-7 9-7 9 7 9 7-3.3 7-9 7-9-7-9-7z"
                />
                <circle cx="12" cy="12" r="3" stroke-width="2" />
              </svg>
            </button>
          </div>
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
const showPassword = ref(false);
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
