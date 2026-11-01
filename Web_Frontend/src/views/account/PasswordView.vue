<template>
  <div class="space-y-6 max-w-6xl mx-auto pb-12">
    <!-- ─── Breadcrumb & Header ────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
      <div>
        <nav class="flex items-center gap-2 text-xs font-medium text-orange-600 mb-1">
          <span>Tài khoản của tôi</span>
          <ChevronRightIcon class="w-3.5 h-3.5 text-orange-400" />
          <span class="text-orange-950 font-semibold">Đổi mật khẩu & Bảo mật</span>
        </nav>
        <h1 class="text-2xl md:text-3xl font-bold text-gray-900 tracking-tight">Đổi mật khẩu / PIN</h1>
        <p class="text-sm text-gray-500 mt-0.5">Thiết lập mật khẩu bảo vệ và mã PIN bảo mật cho tài khoản của bạn</p>
      </div>

      <!-- Quick Tab Navigation Switcher -->
      <div class="flex items-center bg-white p-1 rounded-2xl border border-orange-200/80 shadow-xs self-start sm:self-auto">
        <router-link
          to="/account/profile"
          class="flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-medium text-gray-600 hover:text-orange-700 hover:bg-orange-50 transition-all"
        >
          <UserIcon class="w-3.5 h-3.5 text-orange-500" />
          <span>Thông tin cá nhân</span>
        </router-link>
        <router-link
          to="/account/password"
          class="flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-semibold bg-orange-500 text-white shadow-xs transition-all"
        >
          <ShieldCheckIcon class="w-3.5 h-3.5" />
          <span>Đổi mật khẩu / PIN</span>
        </router-link>
      </div>
    </div>

    <!-- ─── Feedback Toast ─────────────────────────────────────────────── -->
    <transition name="slide-fade">
      <div
        v-if="feedback.message"
        :class="[
          'flex items-center justify-between p-4 rounded-2xl shadow-xs border text-sm',
          feedback.type === 'success'
            ? 'bg-emerald-50 border-emerald-200 text-emerald-800'
            : 'bg-rose-50 border-rose-200 text-rose-800'
        ]"
      >
        <div class="flex items-center gap-3">
          <CheckCircleIcon v-if="feedback.type === 'success'" class="w-5 h-5 text-emerald-600 flex-shrink-0" />
          <ExclamationCircleIcon v-else class="w-5 h-5 text-rose-600 flex-shrink-0" />
          <span class="font-medium">{{ feedback.message }}</span>
        </div>
        <button
          type="button"
          @click="clearFeedback"
          class="text-gray-400 hover:text-gray-600 p-1 rounded-lg transition-colors cursor-pointer"
        >
          <XMarkIcon class="w-4 h-4" />
        </button>
      </div>
    </transition>

    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <!-- ─── Left 2 Cols: Password & PIN Forms ───────────────────────── -->
      <div class="lg:col-span-2 space-y-6">
        <!-- Change Password Form -->
        <div class="bg-white rounded-3xl border border-orange-100 shadow-sm p-6 sm:p-8">
          <div class="flex items-center gap-3 pb-5 border-b border-gray-100 mb-6">
            <div class="w-10 h-10 rounded-2xl bg-orange-100 text-orange-600 flex items-center justify-center">
              <KeyIcon class="w-5 h-5" />
            </div>
            <div>
              <h3 class="text-lg font-bold text-gray-900">Thay đổi mật khẩu đăng nhập</h3>
              <p class="text-xs text-gray-500">Mật khẩu mới cần có ít nhất 6 ký tự để đảm bảo an toàn</p>
            </div>
          </div>

          <form @submit.prevent="handleChangePassword" class="space-y-5">
            <!-- Current Password -->
            <div>
              <label class="block text-xs font-bold uppercase tracking-wider text-gray-700 mb-1.5">
                Mật khẩu hiện tại <span class="text-rose-500">*</span>
              </label>
              <div class="relative">
                <input
                  v-model="passwordForm.currentPassword"
                  :type="showCurrentPassword ? 'text' : 'password'"
                  required
                  placeholder="Nhập mật khẩu hiện đang sử dụng..."
                  class="w-full pl-4 pr-11 py-2.5 rounded-xl border border-gray-200 focus:border-orange-500 focus:ring-3 focus:ring-orange-100 text-sm transition-all text-gray-900"
                />
                <button
                  type="button"
                  @click="showCurrentPassword = !showCurrentPassword"
                  class="absolute right-3.5 top-2.5 text-gray-400 hover:text-gray-600 transition-colors cursor-pointer"
                  tabindex="-1"
                >
                  <EyeSlashIcon v-if="showCurrentPassword" class="w-4 h-4" />
                  <EyeIcon v-else class="w-4 h-4" />
                </button>
              </div>
            </div>

            <!-- New Password -->
            <div>
              <label class="block text-xs font-bold uppercase tracking-wider text-gray-700 mb-1.5">
                Mật khẩu mới <span class="text-rose-500">*</span>
              </label>
              <div class="relative">
                <input
                  v-model="passwordForm.newPassword"
                  :type="showNewPassword ? 'text' : 'password'"
                  required
                  minlength="6"
                  placeholder="Nhập mật khẩu mới (tối thiểu 6 ký tự)..."
                  class="w-full pl-4 pr-11 py-2.5 rounded-xl border border-gray-200 focus:border-orange-500 focus:ring-3 focus:ring-orange-100 text-sm transition-all text-gray-900"
                />
                <button
                  type="button"
                  @click="showNewPassword = !showNewPassword"
                  class="absolute right-3.5 top-2.5 text-gray-400 hover:text-gray-600 transition-colors cursor-pointer"
                  tabindex="-1"
                >
                  <EyeSlashIcon v-if="showNewPassword" class="w-4 h-4" />
                  <EyeIcon v-else class="w-4 h-4" />
                </button>
              </div>

              <!-- Password Strength Meter -->
              <div v-if="passwordForm.newPassword" class="mt-3 space-y-2">
                <div class="flex items-center justify-between text-xs">
                  <span class="text-gray-500 font-medium">Độ mạnh mật khẩu:</span>
                  <span :class="strengthTextColor" class="font-bold">{{ strengthLabel }}</span>
                </div>
                <div class="w-full bg-gray-100 rounded-full h-1.5 overflow-hidden">
                  <div
                    :class="['h-full transition-all duration-300 rounded-full', strengthColor]"
                    :style="{ width: strengthPercent + '%' }"
                  ></div>
                </div>

                <!-- Checklist -->
                <div class="grid grid-cols-2 gap-1.5 pt-1 text-[11px]">
                  <div :class="rules.hasMinLength ? 'text-emerald-700' : 'text-gray-400'" class="flex items-center gap-1.5">
                    <CheckCircleIcon class="w-3.5 h-3.5" />
                    <span>Tối thiểu 6 ký tự</span>
                  </div>
                  <div :class="rules.hasNumber ? 'text-emerald-700' : 'text-gray-400'" class="flex items-center gap-1.5">
                    <CheckCircleIcon class="w-3.5 h-3.5" />
                    <span>Chứa chữ số (0-9)</span>
                  </div>
                  <div :class="rules.hasUpper ? 'text-emerald-700' : 'text-gray-400'" class="flex items-center gap-1.5">
                    <CheckCircleIcon class="w-3.5 h-3.5" />
                    <span>Chữ hoa và chữ thường</span>
                  </div>
                  <div :class="rules.hasSpecial ? 'text-emerald-700' : 'text-gray-400'" class="flex items-center gap-1.5">
                    <CheckCircleIcon class="w-3.5 h-3.5" />
                    <span>Ký tự đặc biệt (!@#...)</span>
                  </div>
                </div>
              </div>
            </div>

            <!-- Confirm New Password -->
            <div>
              <label class="block text-xs font-bold uppercase tracking-wider text-gray-700 mb-1.5">
                Xác nhận mật khẩu mới <span class="text-rose-500">*</span>
              </label>
              <div class="relative">
                <input
                  v-model="passwordForm.confirmNewPassword"
                  :type="showConfirmPassword ? 'text' : 'password'"
                  required
                  placeholder="Nhập lại mật khẩu mới..."
                  :class="[
                    'w-full pl-4 pr-11 py-2.5 rounded-xl border text-sm transition-all focus:outline-none',
                    passwordForm.confirmNewPassword && !isPasswordMatch
                      ? 'border-rose-400 focus:ring-3 focus:ring-rose-100'
                      : 'border-gray-200 focus:border-orange-500 focus:ring-3 focus:ring-orange-100 text-gray-900'
                  ]"
                />
                <button
                  type="button"
                  @click="showConfirmPassword = !showConfirmPassword"
                  class="absolute right-3.5 top-2.5 text-gray-400 hover:text-gray-600 transition-colors cursor-pointer"
                  tabindex="-1"
                >
                  <EyeSlashIcon v-if="showConfirmPassword" class="w-4 h-4" />
                  <EyeIcon v-else class="w-4 h-4" />
                </button>
              </div>
              <p
                v-if="passwordForm.confirmNewPassword && !isPasswordMatch"
                class="text-xs text-rose-600 mt-1"
              >
                Mật khẩu xác nhận không trùng khớp với mật khẩu mới.
              </p>
              <p
                v-else-if="passwordForm.confirmNewPassword && isPasswordMatch"
                class="text-xs text-emerald-600 mt-1 flex items-center gap-1"
              >
                <CheckIcon class="w-3.5 h-3.5" /> Mật khẩu khớp hoàn toàn.
              </p>
            </div>

            <!-- Form Actions -->
            <div class="flex items-center justify-end gap-3 pt-4 border-t border-gray-100">
              <button
                type="button"
                @click="resetPasswordForm"
                class="px-4 py-2.5 rounded-xl text-xs font-semibold text-gray-600 hover:bg-gray-100 transition-colors cursor-pointer"
                :disabled="isSubmittingPassword"
              >
                Xóa biểu mẫu
              </button>
              <button
                type="submit"
                :disabled="isSubmittingPassword || !isPasswordMatch || !passwordForm.newPassword"
                class="flex items-center gap-2 px-5 py-2.5 rounded-xl text-xs font-semibold text-white bg-orange-500 hover:bg-orange-600 shadow-sm transition-all cursor-pointer disabled:opacity-50"
              >
                <ArrowPathIcon v-if="isSubmittingPassword" class="w-4 h-4 animate-spin" />
                <LockClosedIcon v-else class="w-4 h-4" />
                <span>{{ isSubmittingPassword ? 'Đang cập nhật...' : 'Cập nhật mật khẩu' }}</span>
              </button>
            </div>
          </form>
        </div>

        <!-- Security PIN Form -->
        <div class="bg-white rounded-3xl border border-orange-100 shadow-sm p-6 sm:p-8">
          <div class="flex items-center justify-between pb-5 border-b border-gray-100 mb-6">
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-2xl bg-amber-100 text-amber-600 flex items-center justify-center">
                <ShieldCheckIcon class="w-5 h-5" />
              </div>
              <div>
                <h3 class="text-lg font-bold text-gray-900">Mã PIN bảo mật</h3>
                <p class="text-xs text-gray-500">Mã PIN dùng để xác thực các giao dịch hoặc thao tác kiểm duyệt an toàn</p>
              </div>
            </div>
            <span
              :class="[
                'inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-semibold',
                profile.hasPin ? 'bg-emerald-50 text-emerald-700 border border-emerald-200' : 'bg-gray-100 text-gray-600'
              ]"
            >
              {{ profile.hasPin ? 'Đã cài đặt PIN' : 'Chưa thiết lập' }}
            </span>
          </div>

          <form @submit.prevent="handleSavePin" class="space-y-5">
            <div>
              <label class="block text-xs font-bold uppercase tracking-wider text-gray-700 mb-1.5">
                {{ profile.hasPin ? 'Mã PIN mới (4 - 6 số)' : 'Thiết lập mã PIN (4 - 6 số)' }} <span class="text-rose-500">*</span>
              </label>
              <input
                v-model="pinForm.pin"
                type="password"
                maxlength="6"
                inputmode="numeric"
                pattern="[0-9]*"
                required
                placeholder="Nhập 4 hoặc 6 chữ số..."
                class="w-full px-4 py-2.5 rounded-xl border border-gray-200 focus:border-orange-500 focus:ring-3 focus:ring-orange-100 text-sm tracking-widest text-gray-900 font-mono"
              />
              <p class="text-[11px] text-gray-400 mt-1">Chỉ bao gồm các ký tự số (0-9).</p>
            </div>

            <div>
              <label class="block text-xs font-bold uppercase tracking-wider text-gray-700 mb-1.5">
                Xác nhận mã PIN <span class="text-rose-500">*</span>
              </label>
              <input
                v-model="pinForm.confirmPin"
                type="password"
                maxlength="6"
                inputmode="numeric"
                pattern="[0-9]*"
                required
                placeholder="Nhập lại mã PIN..."
                class="w-full px-4 py-2.5 rounded-xl border border-gray-200 focus:border-orange-500 focus:ring-3 focus:ring-orange-100 text-sm tracking-widest text-gray-900 font-mono"
              />
            </div>

            <div class="flex items-center justify-end gap-3 pt-3">
              <button
                type="submit"
                :disabled="isSubmittingPin || !pinForm.pin"
                class="flex items-center gap-2 px-5 py-2.5 rounded-xl text-xs font-semibold text-white bg-amber-600 hover:bg-amber-700 shadow-sm transition-all cursor-pointer disabled:opacity-50"
              >
                <ArrowPathIcon v-if="isSubmittingPin" class="w-4 h-4 animate-spin" />
                <CheckIcon v-else class="w-4 h-4" />
                <span>{{ isSubmittingPin ? 'Đang lưu...' : 'Lưu mã PIN' }}</span>
              </button>
            </div>
          </form>
        </div>
      </div>

      <!-- ─── Right Col: Security Tips & Sessions ─────────────────────── -->
      <div class="space-y-6">
        <!-- Security Tips Card -->
        <div class="bg-white rounded-3xl border border-orange-100 shadow-sm p-6">
          <div class="w-10 h-10 rounded-2xl bg-orange-100 text-orange-600 flex items-center justify-center mb-4">
            <LightBulbIcon class="w-5 h-5" />
          </div>
          <h4 class="text-sm font-bold text-gray-900 mb-2">Quy tắc bảo mật cần biết</h4>
          <p class="text-xs text-gray-500 leading-relaxed mb-4">
            Để bảo vệ hệ thống quản lý đào tạo và thông tin học viên, vui lòng tuân thủ các hướng dẫn sau:
          </p>

          <div class="space-y-3 text-xs text-gray-600">
            <div class="flex items-start gap-2.5">
              <CheckCircleIcon class="w-4 h-4 text-orange-500 flex-shrink-0 mt-0.5" />
              <span>Không sử dụng chung mật khẩu của tài khoản cá nhân khác (Facebook, Email...).</span>
            </div>
            <div class="flex items-start gap-2.5">
              <CheckCircleIcon class="w-4 h-4 text-orange-500 flex-shrink-0 mt-0.5" />
              <span>Không bao giờ tiết lộ mã PIN hoặc mật khẩu đăng nhập cho người khác.</span>
            </div>
            <div class="flex items-start gap-2.5">
              <CheckCircleIcon class="w-4 h-4 text-orange-500 flex-shrink-0 mt-0.5" />
              <span>Luôn đăng xuất khi sử dụng máy tính nơi công cộng hoặc thiết bị dùng chung.</span>
            </div>
          </div>
        </div>

        <!-- Current Session Card -->
        <div class="bg-white rounded-3xl border border-orange-100 shadow-sm p-6">
          <h4 class="text-sm font-bold text-gray-900 mb-4 flex items-center gap-2">
            <ComputerDesktopIcon class="w-4 h-4 text-orange-500" />
            <span>Phiên đăng nhập hiện tại</span>
          </h4>

          <div class="p-4 rounded-2xl bg-orange-50/60 border border-orange-100 space-y-2">
            <div class="flex items-center justify-between">
              <span class="text-xs font-bold text-gray-900">Thiết bị này</span>
              <span class="inline-flex items-center gap-1 text-[11px] font-semibold text-emerald-700 bg-emerald-100/70 px-2 py-0.5 rounded-full">
                ● Trực tuyến
              </span>
            </div>
            <p class="text-xs text-gray-600">Hệ điều hành: Windows &bull; Trình duyệt Web</p>
            <p class="text-[11px] text-gray-400 font-mono">Xác thực: Bearer Token (Hiệu lực 7 ngày)</p>
          </div>

          <div class="mt-4 pt-4 border-t border-gray-100">
            <button
              type="button"
              @click="handleLogoutSession"
              class="w-full flex items-center justify-center gap-2 py-2.5 rounded-xl text-xs font-semibold text-rose-600 bg-rose-50 hover:bg-rose-100 transition-colors cursor-pointer"
            >
              <ArrowRightOnRectangleIcon class="w-4 h-4" />
              <span>Đăng xuất phiên hiện tại</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed } from 'vue'
import { useRouter } from 'vue-router'
import authService from '@/services/auth.service'
import profileService from '@/services/profile.service'
import { useUserState } from '@/services/user-state'
import {
  UserIcon,
  ShieldCheckIcon,
  KeyIcon,
  LockClosedIcon,
  CheckIcon,
  ArrowPathIcon,
  CheckCircleIcon,
  ExclamationCircleIcon,
  XMarkIcon,
  ChevronRightIcon,
  EyeIcon,
  EyeSlashIcon,
  LightBulbIcon,
  ComputerDesktopIcon,
  ArrowRightOnRectangleIcon,
} from '@heroicons/vue/24/outline'

const router = useRouter()
const { profile, setProfile } = useUserState()

// ─── Forms & State ───────────────────────────────────────────────────────────
const passwordForm = reactive({
  currentPassword: '',
  newPassword: '',
  confirmNewPassword: '',
})

const pinForm = reactive({
  pin: '',
  confirmPin: '',
})

const showCurrentPassword = ref(false)
const showNewPassword = ref(false)
const showConfirmPassword = ref(false)

const isSubmittingPassword = ref(false)
const isSubmittingPin = ref(false)

const feedback = reactive({
  message: '',
  type: 'success',
})

function showFeedback(message, type = 'success') {
  feedback.message = message
  feedback.type = type
  setTimeout(() => {
    if (feedback.message === message) {
      clearFeedback()
    }
  }, 4500)
}

function clearFeedback() {
  feedback.message = ''
}

// ─── Password Strength Calculation ───────────────────────────────────────────
const rules = computed(() => {
  const p = passwordForm.newPassword || ''
  return {
    hasMinLength: p.length >= 6,
    hasNumber: /[0-9]/.test(p),
    hasUpper: /[A-Z]/.test(p) && /[a-z]/.test(p),
    hasSpecial: /[!@#$%^&*(),.?":{}|<>]/.test(p),
  }
})

const strengthScore = computed(() => {
  const r = rules.value
  let score = 0
  if (r.hasMinLength) score += 25
  if (r.hasNumber) score += 25
  if (r.hasUpper) score += 25
  if (r.hasSpecial) score += 25
  return score
})

const strengthPercent = computed(() => strengthScore.value)

const strengthLabel = computed(() => {
  const s = strengthScore.value
  if (s <= 25) return 'Yếu'
  if (s <= 50) return 'Trung bình'
  if (s <= 75) return 'Khá mạnh'
  return 'Rất mạnh'
})

const strengthColor = computed(() => {
  const s = strengthScore.value
  if (s <= 25) return 'bg-rose-500'
  if (s <= 50) return 'bg-amber-500'
  if (s <= 75) return 'bg-blue-500'
  return 'bg-emerald-500'
})

const strengthTextColor = computed(() => {
  const s = strengthScore.value
  if (s <= 25) return 'text-rose-600'
  if (s <= 50) return 'text-amber-600'
  if (s <= 75) return 'text-blue-600'
  return 'text-emerald-600'
})

const isPasswordMatch = computed(() => {
  return passwordForm.newPassword === passwordForm.confirmNewPassword
})

function resetPasswordForm() {
  passwordForm.currentPassword = ''
  passwordForm.newPassword = ''
  passwordForm.confirmNewPassword = ''
}

// ─── Change Password Handler ─────────────────────────────────────────────────
async function handleChangePassword() {
  if (!isPasswordMatch.value) {
    showFeedback('Mật khẩu xác nhận không khớp.', 'error')
    return
  }
  if (passwordForm.newPassword.length < 6) {
    showFeedback('Mật khẩu mới phải có ít nhất 6 ký tự.', 'error')
    return
  }

  isSubmittingPassword.value = true
  try {
    await authService.changePassword({
      currentPassword: passwordForm.currentPassword,
      newPassword: passwordForm.newPassword,
      confirmNewPassword: passwordForm.confirmNewPassword,
    })
    resetPasswordForm()
    showFeedback('Đổi mật khẩu thành công! Vui lòng ghi nhớ mật khẩu mới.', 'success')
  } catch (err) {
    const msg = err.response?.data?.message || err.message || 'Không thể đổi mật khẩu. Vui lòng kiểm tra lại mật khẩu hiện tại.'
    showFeedback(msg, 'error')
  } finally {
    isSubmittingPassword.value = false
  }
}

// ─── PIN Handler ─────────────────────────────────────────────────────────────
async function handleSavePin() {
  if (pinForm.pin !== pinForm.confirmPin) {
    showFeedback('Mã PIN xác nhận không trùng khớp.', 'error')
    return
  }
  if (!/^\d{4,6}$/.test(pinForm.pin)) {
    showFeedback('Mã PIN phải gồm từ 4 đến 6 chữ số.', 'error')
    return
  }

  isSubmittingPin.value = true
  try {
    await profileService.updatePin({ pin: pinForm.pin })
    setProfile({ hasPin: true })
    pinForm.pin = ''
    pinForm.confirmPin = ''
    showFeedback('Thiết lập mã PIN bảo mật thành công!', 'success')
  } catch (err) {
    const msg = err.response?.data?.message || 'Không thể cập nhật mã PIN lúc này.'
    showFeedback(msg, 'error')
  } finally {
    isSubmittingPin.value = false
  }
}

function handleLogoutSession() {
  authService.logout()
  router.replace({ name: 'login' })
}
</script>

<style scoped>
.slide-fade-enter-active {
  transition: all 0.25s ease-out;
}
.slide-fade-leave-active {
  transition: all 0.2s cubic-bezier(1, 0.5, 0.8, 1);
}
.slide-fade-enter-from,
.slide-fade-leave-to {
  transform: translateY(-8px);
  opacity: 0;
}
</style>
