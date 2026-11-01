<template>
  <div class="space-y-6 max-w-5xl mx-auto pb-12">
    <!-- ─── Breadcrumb & Header ────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
      <div>
        <nav class="flex items-center gap-2 text-xs font-medium text-orange-600 mb-1">
          <span>Tài khoản giáo viên</span>
          <span>›</span>
          <span class="text-orange-950 font-semibold">Đổi mật khẩu & Mã PIN</span>
        </nav>
        <h1 class="text-2xl font-bold text-gray-900 tracking-tight">Bảo mật tài khoản giáo viên</h1>
        <p class="text-xs text-gray-500 mt-0.5">Thay đổi mật khẩu đăng nhập và mã PIN xác thực khi thao tác nghiệp vụ</p>
      </div>

      <!-- Tab Switcher -->
      <div class="flex items-center bg-white p-1 rounded-2xl border border-orange-200/80 shadow-xs self-start sm:self-auto text-xs">
        <router-link
          to="/teacher/account/profile"
          class="flex items-center gap-2 px-3.5 py-2 rounded-xl font-medium text-gray-600 hover:text-orange-700 hover:bg-orange-50 transition-all"
        >
          <UserIcon class="w-3.5 h-3.5 text-orange-500" />
          <span>Hồ sơ & Chuyên môn</span>
        </router-link>
        <router-link
          to="/teacher/account/password"
          class="flex items-center gap-2 px-3.5 py-2 rounded-xl font-semibold bg-orange-400 text-white shadow-xs"
        >
          <ShieldCheckIcon class="w-3.5 h-3.5" />
          <span>Đổi mật khẩu / PIN</span>
        </router-link>
      </div>
    </div>

    <!-- ─── Two Security Cards ──────────────────────────────────────────── -->
    <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
      <!-- 1. Đổi Mật Khẩu Đăng Nhập -->
      <div class="bg-white rounded-3xl border border-orange-100 p-6 shadow-sm space-y-4">
        <div class="flex items-center gap-3 pb-3 border-b border-orange-100">
          <div class="w-10 h-10 rounded-xl bg-orange-100 text-orange-600 flex items-center justify-center font-bold">
            <KeyIcon class="w-5 h-5" />
          </div>
          <div>
            <h3 class="text-sm font-bold text-gray-900">Đổi mật khẩu đăng nhập</h3>
            <p class="text-[11px] text-gray-400">Nên dùng mật khẩu có ít nhất 8 ký tự</p>
          </div>
        </div>

        <div class="space-y-3 text-xs">
          <div>
            <label class="block font-semibold text-gray-700 mb-1">Mật khẩu hiện tại</label>
            <input
              type="password"
              v-model="pwdForm.current"
              class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
              placeholder="••••••••"
            />
          </div>

          <div>
            <label class="block font-semibold text-gray-700 mb-1">Mật khẩu mới</label>
            <input
              type="password"
              v-model="pwdForm.newPwd"
              class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
              placeholder="••••••••"
            />
          </div>

          <div>
            <label class="block font-semibold text-gray-700 mb-1">Xác nhận mật khẩu mới</label>
            <input
              type="password"
              v-model="pwdForm.confirm"
              class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
              placeholder="••••••••"
            />
          </div>

          <div class="pt-2">
            <button
              type="button"
              class="w-full py-2.5 rounded-xl bg-orange-400 hover:bg-orange-500 text-white font-bold transition-colors cursor-pointer shadow-xs"
              @click="changePassword"
            >
              Cập nhật mật khẩu
            </button>
          </div>
        </div>
      </div>

      <!-- 2. Cài Đặt / Đổi Mã PIN Bảo Mật -->
      <div class="bg-white rounded-3xl border border-orange-100 p-6 shadow-sm space-y-4">
        <div class="flex items-center gap-3 pb-3 border-b border-orange-100">
          <div class="w-10 h-10 rounded-xl bg-amber-100 text-amber-600 flex items-center justify-center font-bold">
            <ShieldCheckIcon class="w-5 h-5" />
          </div>
          <div>
            <h3 class="text-sm font-bold text-gray-900">Mã PIN xác thực (6 số)</h3>
            <p class="text-[11px] text-gray-400">Dùng để xác thực khi khóa bảng điểm hoặc điểm danh</p>
          </div>
        </div>

        <div class="rounded-2xl bg-amber-50/70 border border-amber-200/80 p-3 text-xs text-amber-900 leading-relaxed">
          Mã PIN bảo mật giúp ngăn chặn việc nhập nhầm hoặc sửa đổi dữ liệu đánh giá khi tài khoản đang đăng nhập trên thiết bị chung.
        </div>

        <div class="space-y-3 text-xs">
          <div>
            <label class="block font-semibold text-gray-700 mb-1">Nhập mã PIN mới (6 chữ số)</label>
            <input
              type="password"
              maxlength="6"
              v-model="pinForm.newPin"
              class="w-full rounded-xl border border-orange-200 p-2.5 text-center font-mono tracking-widest text-base focus:outline-none focus:ring-2 focus:ring-orange-300"
              placeholder="• • • • • •"
            />
          </div>

          <div>
            <label class="block font-semibold text-gray-700 mb-1">Xác nhận mã PIN</label>
            <input
              type="password"
              maxlength="6"
              v-model="pinForm.confirmPin"
              class="w-full rounded-xl border border-orange-200 p-2.5 text-center font-mono tracking-widest text-base focus:outline-none focus:ring-2 focus:ring-orange-300"
              placeholder="• • • • • •"
            />
          </div>

          <div class="pt-2">
            <button
              type="button"
              class="w-full py-2.5 rounded-xl border border-orange-300 bg-orange-50 hover:bg-orange-100 text-orange-900 font-bold transition-colors cursor-pointer"
              @click="changePin"
            >
              Thiết lập mã PIN
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import {
  UserIcon,
  ShieldCheckIcon,
  KeyIcon,
} from '@heroicons/vue/24/outline'

const pwdForm = ref({
  current: '',
  newPwd: '',
  confirm: '',
})

const pinForm = ref({
  newPin: '',
  confirmPin: '',
})

function changePassword() {
  if (!pwdForm.value.current || !pwdForm.value.newPwd) {
    alert('Vui lòng điền đầy đủ thông tin mật khẩu!')
    return
  }
  if (pwdForm.value.newPwd !== pwdForm.value.confirm) {
    alert('Mật khẩu xác nhận không khớp!')
    return
  }
  alert('Đã cập nhật mật khẩu thành công!')
  pwdForm.value = { current: '', newPwd: '', confirm: '' }
}

function changePin() {
  if (pinForm.value.newPin.length !== 6) {
    alert('Mã PIN phải bao gồm đúng 6 chữ số!')
    return
  }
  if (pinForm.value.newPin !== pinForm.value.confirmPin) {
    alert('Mã PIN xác nhận không khớp!')
    return
  }
  alert('Đã thiết lập mã PIN bảo mật thành công!')
  pinForm.value = { newPin: '', confirmPin: '' }
}
</script>
