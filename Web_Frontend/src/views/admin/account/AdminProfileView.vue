<template>
  <div class="mx-auto max-w-5xl space-y-7 pb-8">
    <header class="flex flex-col gap-3 border-b border-gray-200 pb-6 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <p class="text-xs font-semibold uppercase tracking-[0.18em] text-orange-600">Tài khoản quản trị</p>
        <h1 class="mt-2 text-3xl font-bold tracking-tight text-gray-900">Thông tin cá nhân</h1>
        <p class="mt-2 max-w-2xl text-sm leading-6 text-gray-500">
          Quản lý thông tin đăng nhập, liên hệ và bảo mật tài khoản của bạn.
        </p>
      </div>
      <span class="inline-flex w-fit items-center gap-2 rounded-full bg-orange-50 px-3 py-1.5 text-xs font-semibold text-orange-700 ring-1 ring-inset ring-orange-200">
        <span class="h-2 w-2 rounded-full bg-orange-500"></span>
        Hồ sơ Admin
      </span>
    </header>

    <div v-if="loadError" role="alert" class="flex flex-col gap-3 rounded-xl border border-red-200 bg-red-50 px-4 py-4 text-sm text-red-700 sm:flex-row sm:items-center sm:justify-between">
      <span>{{ loadError }}</span>
      <button type="button" class="w-fit rounded-lg border border-red-200 bg-white px-3 py-2 font-semibold text-red-700 hover:bg-red-100" @click="loadProfile">
        Tải lại hồ sơ
      </button>
    </div>

    <div v-if="isLoading" class="space-y-5" aria-label="Đang tải hồ sơ">
      <div class="h-48 animate-pulse rounded-2xl bg-gray-100"></div>
      <div class="h-64 animate-pulse rounded-2xl bg-gray-100"></div>
    </div>

    <template v-else-if="profile">
      <section class="overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-sm">
        <div class="bg-gradient-to-r from-orange-600 via-orange-500 to-amber-400 px-6 py-7 text-white sm:px-8">
          <div class="flex flex-col gap-6 sm:flex-row sm:items-center sm:justify-between">
            <div class="flex items-center gap-5">
              <div class="flex h-20 w-20 shrink-0 items-center justify-center overflow-hidden rounded-2xl border-4 border-white/30 bg-white/20 text-2xl font-bold shadow-sm">
                <img v-if="displayedAvatarUrl" :src="displayedAvatarUrl" alt="Ảnh đại diện Admin" class="h-full w-full object-cover" />
                <span v-else>{{ initials }}</span>
              </div>
              <div class="min-w-0">
                <p class="text-sm font-medium text-orange-50">Xin chào,</p>
                <h2 class="mt-1 truncate text-2xl font-bold">{{ profile.fullName || 'Admin' }}</h2>
                <p class="mt-1 text-sm text-orange-50">{{ profile.role || 'Quản trị viên' }}</p>
              </div>
            </div>
            <div class="flex flex-wrap gap-2">
              <button
                v-if="!isEditingAvatar"
                type="button"
                class="rounded-lg border border-white/50 bg-white/10 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-white/20 focus:outline-none focus:ring-2 focus:ring-white/70"
                @click="startEditingAvatar"
              >
                Thay đổi ảnh đại diện
              </button>
              <template v-else>
                <label class="cursor-pointer rounded-lg border border-white/50 bg-white/10 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-white/20">
                  Chọn ảnh mới
                  <input type="file" accept="image/jpeg,image/png,image/webp" class="sr-only" :disabled="isUploadingAvatar" @change="selectAvatar" />
                </label>
                <button
                  type="button"
                  :disabled="isUploadingAvatar || (!avatarFile && !isAvatarRemovalPending)"
                  class="rounded-lg bg-white px-4 py-2.5 text-sm font-semibold text-orange-700 transition hover:bg-orange-50 disabled:cursor-not-allowed disabled:opacity-60"
                  @click="saveAvatar"
                >
                  {{ isUploadingAvatar ? 'Đang tải ảnh...' : 'Lưu ảnh đại diện' }}
                </button>
                <button
                  type="button"
                  :disabled="isUploadingAvatar"
                  class="rounded-lg border border-white/50 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-white/10 disabled:opacity-60"
                  @click="cancelEditingAvatar"
                >
                  Hủy
                </button>
              </template>
            </div>
          </div>
          <div v-if="isEditingAvatar" class="mt-4 flex flex-wrap items-center gap-x-3 gap-y-2 text-xs text-orange-50">
            <span>Chọn ảnh JPG, PNG hoặc WEBP (tối đa 5MB). Ảnh sẽ được cập nhật khi lưu.</span>
            <span v-if="avatarFile" class="font-semibold text-white">Đang xem trước: {{ avatarFile.name }}</span>
            <button v-if="profile.avatar && !isAvatarRemovalPending" type="button" class="underline underline-offset-2 hover:text-white" @click="stageAvatarRemoval">
              Xóa ảnh hiện tại
            </button>
            <span v-if="isAvatarRemovalPending">Ảnh sẽ bị xóa khi lưu.</span>
          </div>
        </div>
        <div class="px-6 py-4 sm:px-8">
          <p v-if="profileMessage" role="status" class="mb-3 rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700">{{ profileMessage }}</p>
          <p v-if="profileError" role="alert" class="mb-3 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{{ profileError }}</p>
          <div class="flex flex-wrap gap-x-6 gap-y-2 text-xs text-gray-500">
            <span>Ảnh đại diện giúp nhận diện tài khoản của bạn.</span>
            <span v-if="profile.pendingEmail" class="font-medium text-amber-700">Email mới đang chờ xác minh.</span>
          </div>
        </div>
      </section>

      <section class="rounded-2xl border border-gray-200 bg-white shadow-sm">
        <div class="flex flex-col gap-3 border-b border-gray-100 px-6 py-5 sm:flex-row sm:items-center sm:justify-between sm:px-8">
          <div>
            <h2 class="text-lg font-semibold text-gray-900">Thông tin cơ bản</h2>
            <p class="mt-1 text-sm text-gray-500">Tên và số điện thoại dùng để đăng nhập tài khoản.</p>
          </div>
          <span class="w-fit rounded-full bg-gray-100 px-3 py-1 text-xs font-medium text-gray-600">Vai trò: {{ profile.role }}</span>
        </div>
        <form class="space-y-6 px-6 py-6 sm:px-8" @submit.prevent="saveProfile">
          <div class="grid gap-5 md:grid-cols-2">
            <div>
              <label for="fullName" class="mb-2 block text-sm font-medium text-gray-700">Họ và tên</label>
              <input id="fullName" v-model.trim="form.fullName" :disabled="!isEditingProfile" required maxlength="100" autocomplete="name" class="w-full rounded-xl border border-gray-300 bg-white px-4 py-3 text-sm text-gray-900 outline-none transition placeholder:text-gray-400 focus:border-orange-500 focus:ring-4 focus:ring-orange-100 disabled:border-gray-200 disabled:bg-gray-50 disabled:text-gray-600" />
            </div>
            <div>
              <label for="phone" class="mb-2 block text-sm font-medium text-gray-700">Số điện thoại đăng nhập</label>
              <input id="phone" v-model.trim="form.phone" :disabled="!isEditingProfile" required type="tel" maxlength="12" pattern="(?:\+84|0)[0-9]{9}" autocomplete="tel" class="w-full rounded-xl border border-gray-300 bg-white px-4 py-3 text-sm text-gray-900 outline-none transition placeholder:text-gray-400 focus:border-orange-500 focus:ring-4 focus:ring-orange-100 disabled:border-gray-200 disabled:bg-gray-50 disabled:text-gray-600" />
              <p class="mt-2 text-xs leading-5 text-gray-500">Số điện thoại mới có hiệu lực cho lần đăng nhập tiếp theo.</p>
            </div>
          </div>
          <div class="flex flex-wrap items-center gap-3 border-t border-gray-100 pt-5">
            <button v-if="!isEditingProfile" type="button" class="rounded-lg bg-orange-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-orange-700 focus:outline-none focus:ring-4 focus:ring-orange-200" @click="startEditingProfile">Chỉnh sửa thông tin</button>
            <template v-else>
              <button type="submit" :disabled="isSavingProfile" class="rounded-lg bg-orange-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-orange-700 focus:outline-none focus:ring-4 focus:ring-orange-200 disabled:cursor-not-allowed disabled:opacity-60">{{ isSavingProfile ? 'Đang lưu...' : 'Lưu thay đổi' }}</button>
              <button type="button" :disabled="isSavingProfile" class="rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm font-semibold text-gray-700 transition hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-60" @click="cancelEditingProfile">Hủy</button>
              <span class="text-xs text-gray-500">Thông tin chỉ được cập nhật sau khi lưu.</span>
            </template>
          </div>
        </form>
      </section>

      <section class="rounded-2xl border border-gray-200 bg-white shadow-sm">
        <div class="border-b border-gray-100 px-6 py-5 sm:px-8">
          <h2 class="text-lg font-semibold text-gray-900">Email liên hệ</h2>
          <p class="mt-1 text-sm text-gray-500">Email mới cần được xác minh trước khi thay thế email hiện tại.</p>
        </div>
        <form class="space-y-5 px-6 py-6 sm:px-8" @submit.prevent="saveEmail">
          <div class="max-w-2xl">
            <label for="email" class="mb-2 block text-sm font-medium text-gray-700">Địa chỉ email</label>
            <input id="email" v-model.trim="email" :disabled="!isEditingEmail" required type="email" maxlength="256" autocomplete="email" class="w-full rounded-xl border border-gray-300 bg-white px-4 py-3 text-sm text-gray-900 outline-none transition focus:border-orange-500 focus:ring-4 focus:ring-orange-100 disabled:border-gray-200 disabled:bg-gray-50 disabled:text-gray-600" />
          </div>
          <div v-if="profile.pendingEmail" class="flex flex-col gap-3 rounded-xl border border-amber-200 bg-amber-50 p-4 sm:flex-row sm:items-center sm:justify-between">
            <p class="text-sm leading-6 text-amber-800">Đang chờ xác minh <strong class="break-all">{{ profile.pendingEmail }}</strong>. Email hiện tại vẫn được giữ đến khi xác minh hoàn tất.</p>
            <button type="button" :disabled="isResendingEmail" class="shrink-0 rounded-lg border border-amber-300 bg-white px-3 py-2 text-sm font-semibold text-amber-800 hover:bg-amber-100 disabled:opacity-60" @click="resendVerification">{{ isResendingEmail ? 'Đang gửi...' : 'Gửi lại link' }}</button>
          </div>
          <p v-else-if="profile.email" class="flex items-center gap-2 text-sm" :class="profile.emailConfirm ? 'text-green-700' : 'text-amber-700'">
            <span class="h-2 w-2 rounded-full" :class="profile.emailConfirm ? 'bg-green-500' : 'bg-amber-500'"></span>
            {{ profile.emailConfirm ? 'Email đã được xác minh.' : 'Email chưa được xác minh.' }}
          </p>
          <p v-if="emailMessage" role="status" class="rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700">{{ emailMessage }}</p>
          <p v-if="emailError" role="alert" class="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{{ emailError }}</p>
          <div class="flex flex-wrap items-center gap-3 border-t border-gray-100 pt-5">
            <button v-if="!isEditingEmail" type="button" class="rounded-lg bg-orange-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-orange-700 focus:outline-none focus:ring-4 focus:ring-orange-200" @click="startEditingEmail">Đổi email</button>
            <template v-else>
              <button type="submit" :disabled="isSavingEmail || profile.pendingEmail === email" class="rounded-lg bg-orange-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-orange-700 focus:outline-none focus:ring-4 focus:ring-orange-200 disabled:cursor-not-allowed disabled:opacity-60">{{ isSavingEmail ? 'Đang gửi...' : profile.pendingEmail === email ? 'Đang chờ xác minh' : 'Lưu thay đổi' }}</button>
              <button type="button" :disabled="isSavingEmail" class="rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm font-semibold text-gray-700 transition hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-60" @click="cancelEditingEmail">Hủy</button>
            </template>
          </div>
        </form>
      </section>

      <section class="rounded-2xl border border-gray-200 bg-white shadow-sm">
        <div class="border-b border-gray-100 px-6 py-5 sm:px-8">
          <h2 class="text-lg font-semibold text-gray-900">Bảo mật</h2>
          <p class="mt-1 text-sm text-gray-500">Đổi mật khẩu định kỳ để bảo vệ tài khoản quản trị.</p>
        </div>
        <form class="space-y-5 px-6 py-6 sm:px-8" @submit.prevent="changePassword">
          <template v-if="isEditingPassword">
            <div class="grid gap-5 md:grid-cols-2">
              <div class="md:col-span-2">
                <label for="currentPassword" class="mb-2 block text-sm font-medium text-gray-700">Mật khẩu hiện tại</label>
                <input id="currentPassword" v-model="passwordForm.currentPassword" required type="password" maxlength="128" autocomplete="current-password" class="w-full rounded-xl border border-gray-300 px-4 py-3 text-sm outline-none transition focus:border-orange-500 focus:ring-4 focus:ring-orange-100" />
              </div>
              <div>
                <label for="newPassword" class="mb-2 block text-sm font-medium text-gray-700">Mật khẩu mới</label>
                <input id="newPassword" v-model="passwordForm.newPassword" required minlength="6" maxlength="128" type="password" autocomplete="new-password" class="w-full rounded-xl border border-gray-300 px-4 py-3 text-sm outline-none transition focus:border-orange-500 focus:ring-4 focus:ring-orange-100" />
              </div>
              <div>
                <label for="confirmNewPassword" class="mb-2 block text-sm font-medium text-gray-700">Xác nhận mật khẩu mới</label>
                <input id="confirmNewPassword" v-model="passwordForm.confirmNewPassword" required minlength="6" maxlength="128" type="password" autocomplete="new-password" class="w-full rounded-xl border border-gray-300 px-4 py-3 text-sm outline-none transition focus:border-orange-500 focus:ring-4 focus:ring-orange-100" />
              </div>
            </div>
          </template>
          <p v-else class="rounded-xl bg-gray-50 px-4 py-3 text-sm text-gray-600">Mật khẩu hiện tại được ẩn. Chọn “Đổi mật khẩu” để nhập mật khẩu mới.</p>
          <p v-if="passwordMessage" role="status" class="rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700">{{ passwordMessage }}</p>
          <p v-if="passwordError" role="alert" class="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{{ passwordError }}</p>
          <div class="flex flex-wrap items-center gap-3 border-t border-gray-100 pt-5">
            <button v-if="!isEditingPassword" type="button" class="rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm font-semibold text-gray-700 transition hover:bg-gray-50 focus:outline-none focus:ring-4 focus:ring-gray-100" @click="startEditingPassword">Đổi mật khẩu</button>
            <template v-else>
              <button type="submit" :disabled="isChangingPassword" class="rounded-lg bg-orange-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-orange-700 focus:outline-none focus:ring-4 focus:ring-orange-200 disabled:cursor-not-allowed disabled:opacity-60">{{ isChangingPassword ? 'Đang cập nhật...' : 'Lưu mật khẩu' }}</button>
              <button type="button" :disabled="isChangingPassword" class="rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm font-semibold text-gray-700 transition hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-60" @click="cancelEditingPassword">Hủy</button>
            </template>
          </div>
        </form>
      </section>
    </template>
  </div>
</template>

<script setup>
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue'
import authService from '@/services/auth.service'
import profileService from '@/services/profile.service'
import { validateEmail, validatePhone, validateTextLength } from '@/utils/validation.helper'

const profile = ref(null)
const isLoading = ref(true)
const loadError = ref('')
const isEditingAvatar = ref(false)
const isEditingProfile = ref(false)
const isEditingEmail = ref(false)
const isEditingPassword = ref(false)
const avatarFile = ref(null)
const avatarPreviewUrl = ref('')
const isAvatarRemovalPending = ref(false)
const form = reactive({ fullName: '', phone: '' })
const email = ref('')
const passwordForm = reactive({
  currentPassword: '',
  newPassword: '',
  confirmNewPassword: '',
})

const isSavingProfile = ref(false)
const isSavingEmail = ref(false)
const isResendingEmail = ref(false)
const isUploadingAvatar = ref(false)
const isChangingPassword = ref(false)
const profileMessage = ref('')
const profileError = ref('')
const emailMessage = ref('')
const emailError = ref('')
const passwordMessage = ref('')
const passwordError = ref('')

const displayedAvatarUrl = computed(() => {
  if (isAvatarRemovalPending.value) return ''
  return avatarPreviewUrl.value || profile.value?.avatar || ''
})

const initials = computed(() => {
  const words = (form.fullName || 'Admin').trim().split(/\s+/)
  return words.slice(-2).map((word) => word[0]).join('').toUpperCase()
})

function getErrorMessage(error) {
  return error.response?.data?.message
    ?? error.response?.data?.title
    ?? error.message
    ?? 'Đã xảy ra lỗi. Vui lòng thử lại.'
}

function applyProfile(value) {
  profile.value = value
  form.fullName = value.fullName ?? ''
  form.phone = value.phone ?? ''
  email.value = value.pendingEmail ?? value.email ?? ''
}

function startEditingProfile() {
  profileMessage.value = ''
  profileError.value = ''
  isEditingProfile.value = true
}

function cancelEditingProfile() {
  form.fullName = profile.value.fullName ?? ''
  form.phone = profile.value.phone ?? ''
  profileMessage.value = ''
  profileError.value = ''
  isEditingProfile.value = false
}

function startEditingEmail() {
  emailMessage.value = ''
  emailError.value = ''
  isEditingEmail.value = true
}

function cancelEditingEmail() {
  email.value = profile.value.pendingEmail ?? profile.value.email ?? ''
  emailMessage.value = ''
  emailError.value = ''
  isEditingEmail.value = false
}

function startEditingAvatar() {
  profileMessage.value = ''
  profileError.value = ''
  avatarFile.value = null
  clearAvatarPreview()
  isAvatarRemovalPending.value = false
  isEditingAvatar.value = true
}

function clearAvatarPreview() {
  if (avatarPreviewUrl.value) {
    URL.revokeObjectURL(avatarPreviewUrl.value)
    avatarPreviewUrl.value = ''
  }
}

function selectAvatar(event) {
  const file = event.target.files?.[0]
  event.target.value = ''
  if (!file) return
  if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) {
    avatarFile.value = null
    clearAvatarPreview()
    isAvatarRemovalPending.value = false
    profileError.value = 'Vui lòng chọn ảnh JPG, PNG hoặc WEBP.'
    return
  }
  if (file.size > 5 * 1024 * 1024) {
    avatarFile.value = null
    clearAvatarPreview()
    isAvatarRemovalPending.value = false
    profileError.value = 'Ảnh vượt quá giới hạn 5MB.'
    return
  }

  clearAvatarPreview()
  avatarFile.value = file
  avatarPreviewUrl.value = URL.createObjectURL(file)
  isAvatarRemovalPending.value = false
  profileError.value = ''
}

function stageAvatarRemoval() {
  avatarFile.value = null
  clearAvatarPreview()
  isAvatarRemovalPending.value = true
  profileError.value = ''
}

function cancelEditingAvatar() {
  avatarFile.value = null
  clearAvatarPreview()
  isAvatarRemovalPending.value = false
  profileMessage.value = ''
  profileError.value = ''
  isEditingAvatar.value = false
}

function startEditingPassword() {
  passwordMessage.value = ''
  passwordError.value = ''
  isEditingPassword.value = true
}

function cancelEditingPassword() {
  passwordForm.currentPassword = ''
  passwordForm.newPassword = ''
  passwordForm.confirmNewPassword = ''
  passwordMessage.value = ''
  passwordError.value = ''
  isEditingPassword.value = false
}

function notifyProfileUpdated() {
  window.dispatchEvent(new Event('admin-profile-updated'))
}

async function loadProfile() {
  isLoading.value = true
  loadError.value = ''
  try {
    applyProfile(await profileService.get())
  } catch (error) {
    loadError.value = getErrorMessage(error)
  } finally {
    isLoading.value = false
  }
}

async function saveProfile() {
  profileMessage.value = ''
  profileError.value = ''

  const nameCheck = validateTextLength(form.fullName, {
    label: 'Họ và tên',
    min: 2,
    max: 100,
    required: true,
  })
  if (!nameCheck.valid) {
    profileError.value = nameCheck.message
    return
  }

  const phoneCheck = validatePhone(form.phone, { label: 'Số điện thoại', required: true })
  if (!phoneCheck.valid) {
    profileError.value = phoneCheck.message
    return
  }

  isSavingProfile.value = true
  try {
    applyProfile(await profileService.update({
      fullName: form.fullName,
      phone: form.phone,
    }))
    notifyProfileUpdated()
    profileMessage.value = 'Đã cập nhật thông tin cá nhân.'
    isEditingProfile.value = false
  } catch (error) {
    profileError.value = getErrorMessage(error)
  } finally {
    isSavingProfile.value = false
  }
}

async function saveEmail() {
  emailMessage.value = ''
  emailError.value = ''

  const emailCheck = validateEmail(email.value, { label: 'Email', required: true })
  if (!emailCheck.valid) {
    emailError.value = emailCheck.message
    return
  }

  isSavingEmail.value = true
  try {
    applyProfile(await profileService.updateEmail({ email: email.value }))
    emailMessage.value = 'Đã gửi link xác minh đến email mới. Email hiện tại chỉ đổi sau khi xác minh.'
    isEditingEmail.value = false
  } catch (error) {
    emailError.value = getErrorMessage(error)
  } finally {
    isSavingEmail.value = false
  }
}

async function resendVerification() {
  isResendingEmail.value = true
  emailMessage.value = ''
  emailError.value = ''
  try {
    await profileService.resendEmailVerification()
    emailMessage.value = 'Đã gửi lại link xác minh.'
  } catch (error) {
    emailError.value = getErrorMessage(error)
  } finally {
    isResendingEmail.value = false
  }
}

async function saveAvatar() {
  isUploadingAvatar.value = true
  profileError.value = ''
  profileMessage.value = ''
  try {
    const removingAvatar = isAvatarRemovalPending.value
    if (avatarFile.value) {
      const data = new FormData()
      data.append('file', avatarFile.value)
      applyProfile(await profileService.uploadAvatar(data))
    } else if (removingAvatar) {
      applyProfile(await profileService.deleteAvatar())
    } else {
      return
    }

    notifyProfileUpdated()
    profileMessage.value = removingAvatar ? 'Đã xóa ảnh đại diện.' : 'Đã cập nhật ảnh đại diện.'
    avatarFile.value = null
    clearAvatarPreview()
    isAvatarRemovalPending.value = false
    isEditingAvatar.value = false
  } catch (error) {
    profileError.value = getErrorMessage(error)
  } finally {
    isUploadingAvatar.value = false
  }
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
    passwordMessage.value = result.message ?? 'Đổi mật khẩu thành công.'
    passwordForm.currentPassword = ''
    passwordForm.newPassword = ''
    passwordForm.confirmNewPassword = ''
    isEditingPassword.value = false
  } catch (error) {
    passwordError.value = getErrorMessage(error)
  } finally {
    isChangingPassword.value = false
  }
}

onMounted(loadProfile)
onUnmounted(clearAvatarPreview)
</script>
