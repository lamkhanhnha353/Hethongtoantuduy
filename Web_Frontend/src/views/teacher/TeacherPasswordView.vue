<template>
  <div class="mx-auto max-w-5xl space-y-7 pb-8">
    <!-- ─── Header ────────────────────────────────────────────────────────── -->
    <header class="flex flex-col gap-3 border-b border-gray-200 pb-6 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <p class="text-xs font-semibold uppercase tracking-[0.18em] text-orange-600">Tài khoản giáo viên</p>
        <h1 class="mt-2 text-3xl font-bold tracking-tight text-gray-900">Bảo mật tài khoản</h1>
        <p class="mt-2 max-w-2xl text-sm leading-6 text-gray-500">
          Quản lý mật khẩu đăng nhập định kỳ để đảm bảo an toàn cho tài khoản giảng dạy của bạn.
        </p>
      </div>
      <router-link
        to="/teacher/account/profile"
        class="inline-flex w-fit items-center gap-2 rounded-xl border border-gray-300 bg-white px-3.5 py-2 text-xs font-semibold text-gray-700 shadow-xs hover:bg-gray-50 transition-colors"
      >
        <ArrowLeftIcon class="w-4 h-4 text-gray-500" />
        Quay lại Hồ sơ cá nhân
      </router-link>
    </header>

    <!-- ─── Change Password Section Card ─────────────────────────────────── -->
    <section class="rounded-2xl border border-gray-200 bg-white shadow-sm">
      <div class="border-b border-gray-100 px-6 py-5 sm:px-8">
        <h2 class="text-lg font-semibold text-gray-900">Đổi mật khẩu đăng nhập</h2>
        <p class="mt-1 text-sm text-gray-500">
          Mật khẩu mới phải có ít nhất 6 ký tự. Nên kết hợp chữ hoa, chữ thường và chữ số để tăng tính bảo mật.
        </p>
      </div>

      <form class="space-y-5 px-6 py-6 sm:px-8" @submit.prevent="changePassword">
        <div class="grid gap-5 md:grid-cols-2 max-w-3xl">
          <div class="md:col-span-2">
            <label for="currentPassword" class="mb-2 block text-sm font-medium text-gray-700">Mật khẩu hiện tại</label>
            <input
              id="currentPassword"
              v-model="passwordForm.currentPassword"
              required
              maxlength="128"
              type="password"
              autocomplete="current-password"
              placeholder="••••••••"
              class="w-full rounded-xl border border-gray-300 px-4 py-3 text-sm outline-none transition focus:border-orange-500 focus:ring-4 focus:ring-orange-100"
            />
          </div>
          <div>
            <label for="newPassword" class="mb-2 block text-sm font-medium text-gray-700">Mật khẩu mới</label>
            <input
              id="newPassword"
              v-model="passwordForm.newPassword"
              required
              minlength="6"
              maxlength="128"
              type="password"
              autocomplete="new-password"
              placeholder="••••••••"
              class="w-full rounded-xl border border-gray-300 px-4 py-3 text-sm outline-none transition focus:border-orange-500 focus:ring-4 focus:ring-orange-100"
            />
          </div>
          <div>
            <label for="confirmNewPassword" class="mb-2 block text-sm font-medium text-gray-700">Xác nhận mật khẩu mới</label>
            <input
              id="confirmNewPassword"
              v-model="passwordForm.confirmNewPassword"
              required
              minlength="6"
              maxlength="128"
              type="password"
              autocomplete="new-password"
              placeholder="••••••••"
              class="w-full rounded-xl border border-gray-300 px-4 py-3 text-sm outline-none transition focus:border-orange-500 focus:ring-4 focus:ring-orange-100"
            />
          </div>
        </div>

        <p
          v-if="passwordMessage"
          role="status"
          class="rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700 max-w-3xl"
        >
          {{ passwordMessage }}
        </p>
        <p
          v-if="passwordError"
          role="alert"
          class="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700 max-w-3xl"
        >
          {{ passwordError }}
        </p>

        <div class="flex flex-wrap items-center gap-3 border-t border-gray-100 pt-5">
          <button
            type="submit"
            :disabled="isChangingPassword"
            class="rounded-lg bg-orange-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-orange-700 focus:outline-none focus:ring-4 focus:ring-orange-200 disabled:cursor-not-allowed disabled:opacity-60 cursor-pointer inline-flex items-center gap-2"
          >
            <span>{{ isChangingPassword ? 'Đang cập nhật...' : 'Lưu mật khẩu mới' }}</span>
          </button>
          <button
            type="button"
            :disabled="isChangingPassword"
            class="rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm font-semibold text-gray-700 transition hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-60 cursor-pointer"
            @click="resetForm"
          >
            Làm lại
          </button>
        </div>
      </form>
    </section>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { ArrowLeftIcon } from '@heroicons/vue/24/outline'
import authService from '@/services/auth.service'

const passwordForm = reactive({
  currentPassword: '',
  newPassword: '',
  confirmNewPassword: '',
})

const isChangingPassword = ref(false)
const passwordMessage = ref('')
const passwordError = ref('')

function resetForm() {
  passwordForm.currentPassword = ''
  passwordForm.newPassword = ''
  passwordForm.confirmNewPassword = ''
  passwordMessage.value = ''
  passwordError.value = ''
}

function getErrorMessage(error) {
  return (
    error.response?.data?.message ??
    error.response?.data?.title ??
    error.message ??
    'Đã xảy ra lỗi khi cập nhật mật khẩu.'
  )
}

async function changePassword() {
  passwordMessage.value = ''
  passwordError.value = ''

  if (!passwordForm.currentPassword) {
    passwordError.value = 'Vui lòng nhập mật khẩu hiện tại.'
    return
  }
  if (passwordForm.newPassword.length < 6) {
    passwordError.value = 'Mật khẩu mới phải có ít nhất 6 ký tự.'
    return
  }
  if (passwordForm.newPassword.length > 128 || passwordForm.currentPassword.length > 128) {
    passwordError.value = 'Mật khẩu không được vượt quá 128 ký tự.'
    return
  }
  if (passwordForm.newPassword !== passwordForm.confirmNewPassword) {
    passwordError.value = 'Mật khẩu xác nhận không khớp.'
    return
  }

  isChangingPassword.value = true
  try {
    const result = await authService.changePassword(passwordForm)
    passwordMessage.value = result.message ?? 'Đổi mật khẩu thành công!'
    resetForm()
  } catch (error) {
    passwordError.value = getErrorMessage(error)
  } finally {
    isChangingPassword.value = false
  }
}
</script>
