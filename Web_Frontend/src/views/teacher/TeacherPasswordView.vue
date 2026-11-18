<template>
  <div class="mx-auto max-w-5xl space-y-6 pb-8">
    <!-- ─── Header ────────────────────────────────────────────────────────── -->
    <header class="flex flex-col gap-3 border-b border-stone-200 pb-5 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <p class="text-xs font-semibold uppercase tracking-[0.14em] text-orange-600">Tài khoản giáo viên</p>
        <h1 class="mt-1.5 text-2xl sm:text-3xl font-bold tracking-tight text-stone-900">Bảo mật tài khoản</h1>
        <p class="mt-1.5 max-w-2xl text-xs sm:text-sm leading-6 text-stone-500">
          Quản lý mật khẩu đăng nhập định kỳ để đảm bảo an toàn cho tài khoản giảng dạy của bạn.
        </p>
      </div>
      <router-link
        to="/teacher/account/profile"
        class="inline-flex w-fit items-center gap-2 h-9 rounded-md border border-stone-300 bg-white px-3.5 text-xs font-semibold text-stone-800 hover:bg-stone-100 hover:border-stone-400 transition-colors"
      >
        <ArrowLeftIcon class="w-4 h-4 text-stone-500" />
        Quay lại Hồ sơ cá nhân
      </router-link>
    </header>

    <!-- ─── Change Password Section Card ─────────────────────────────────── -->
    <section class="rounded-lg border border-stone-300 bg-white shadow-[0_1px_2px_rgba(0,0,0,0.04)] overflow-hidden">
      <div class="border-b border-stone-200 bg-stone-50 px-6 py-4 sm:px-8">
        <h2 class="text-base font-bold text-stone-900">Đổi mật khẩu đăng nhập</h2>
        <p class="mt-0.5 text-xs sm:text-sm text-stone-500">
          Mật khẩu mới phải có ít nhất 6 ký tự. Nên kết hợp chữ hoa, chữ thường và chữ số để tăng tính bảo mật.
        </p>
      </div>

      <form class="space-y-5 px-6 py-6 sm:px-8" @submit.prevent="changePassword">
        <div class="grid gap-5 md:grid-cols-2 max-w-3xl">
          <div class="md:col-span-2">
            <label for="currentPassword" class="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-stone-700">Mật khẩu hiện tại</label>
            <input
              id="currentPassword"
              v-model="passwordForm.currentPassword"
              required
              maxlength="128"
              type="password"
              autocomplete="current-password"
              placeholder="••••••••"
              class="w-full h-10 rounded-md border border-stone-300 bg-stone-50/70 px-3.5 text-sm text-stone-900 outline-none transition-colors focus:bg-white focus:border-orange-600 focus:ring-2 focus:ring-orange-500/20"
            />
          </div>
          <div>
            <label for="newPassword" class="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-stone-700">Mật khẩu mới</label>
            <input
              id="newPassword"
              v-model="passwordForm.newPassword"
              required
              minlength="6"
              maxlength="128"
              type="password"
              autocomplete="new-password"
              placeholder="••••••••"
              class="w-full h-10 rounded-md border border-stone-300 bg-stone-50/70 px-3.5 text-sm text-stone-900 outline-none transition-colors focus:bg-white focus:border-orange-600 focus:ring-2 focus:ring-orange-500/20"
            />
          </div>
          <div>
            <label for="confirmNewPassword" class="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-stone-700">Xác nhận mật khẩu mới</label>
            <input
              id="confirmNewPassword"
              v-model="passwordForm.confirmNewPassword"
              required
              minlength="6"
              maxlength="128"
              type="password"
              autocomplete="new-password"
              placeholder="••••••••"
              class="w-full h-10 rounded-md border border-stone-300 bg-stone-50/70 px-3.5 text-sm text-stone-900 outline-none transition-colors focus:bg-white focus:border-orange-600 focus:ring-2 focus:ring-orange-500/20"
            />
          </div>
        </div>

        <p
          v-if="passwordMessage"
          role="status"
          class="rounded-md border border-emerald-300 bg-emerald-50 px-4 py-3 text-sm text-emerald-800 max-w-3xl"
        >
          {{ passwordMessage }}
        </p>
        <p
          v-if="passwordError"
          role="alert"
          class="rounded-md border border-rose-300 bg-rose-50 px-4 py-3 text-sm text-rose-800 max-w-3xl"
        >
          {{ passwordError }}
        </p>

        <div class="flex flex-wrap items-center gap-3 border-t border-stone-200 pt-5">
          <button
            type="submit"
            :disabled="isChangingPassword"
            class="h-9 rounded-md bg-orange-600 border border-orange-700 px-4 text-xs sm:text-sm font-semibold text-white shadow-xs transition-colors hover:bg-orange-700 active:bg-orange-800 focus:outline-none focus:ring-2 focus:ring-orange-500/30 disabled:cursor-not-allowed disabled:opacity-60 cursor-pointer inline-flex items-center gap-2"
          >
            <span>{{ isChangingPassword ? 'Đang cập nhật...' : 'Lưu mật khẩu mới' }}</span>
          </button>
          <button
            type="button"
            :disabled="isChangingPassword"
            class="h-9 rounded-md border border-stone-300 bg-white px-4 text-xs sm:text-sm font-semibold text-stone-800 transition-colors hover:bg-stone-100 hover:border-stone-400 disabled:cursor-not-allowed disabled:opacity-60 cursor-pointer"
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
