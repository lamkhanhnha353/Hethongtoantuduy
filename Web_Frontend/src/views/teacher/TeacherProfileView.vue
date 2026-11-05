<template>
  <div class="mx-auto max-w-5xl space-y-7 pb-8">
    <!-- ─── Header ────────────────────────────────────────────────────────── -->
    <header class="flex flex-col gap-3 border-b border-gray-200 pb-6 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <p class="text-xs font-semibold uppercase tracking-[0.18em] text-orange-600">Tài khoản giáo viên</p>
        <h1 class="mt-2 text-3xl font-bold tracking-tight text-gray-900">Thông tin cá nhân</h1>
        <p class="mt-2 max-w-2xl text-sm leading-6 text-gray-500">
          Quản lý thông tin cá nhân, liên hệ, hồ sơ chuyên môn và bảo mật tài khoản giáo viên của bạn.
        </p>
      </div>
      <span class="inline-flex w-fit items-center gap-2 rounded-full bg-orange-50 px-3 py-1.5 text-xs font-semibold text-orange-700 ring-1 ring-inset ring-orange-200">
        <span class="h-2 w-2 rounded-full bg-orange-500"></span>
        Hồ sơ Giáo viên
      </span>
    </header>

    <!-- ─── Load Error Alert ──────────────────────────────────────────────── -->
    <div
      v-if="loadError"
      role="alert"
      class="flex flex-col gap-3 rounded-xl border border-red-200 bg-red-50 px-4 py-4 text-sm text-red-700 sm:flex-row sm:items-center sm:justify-between"
    >
      <span>{{ loadError }}</span>
      <button
        type="button"
        class="w-fit rounded-lg border border-red-200 bg-white px-3 py-2 font-semibold text-red-700 hover:bg-red-100 cursor-pointer"
        @click="loadProfile"
      >
        Tải lại hồ sơ
      </button>
    </div>

    <!-- ─── Loading Skeletons ─────────────────────────────────────────────── -->
    <div v-if="isLoading" class="space-y-5" aria-label="Đang tải hồ sơ">
      <div class="h-48 animate-pulse rounded-2xl bg-gray-100"></div>
      <div class="h-64 animate-pulse rounded-2xl bg-gray-100"></div>
      <div class="h-64 animate-pulse rounded-2xl bg-gray-100"></div>
    </div>

    <!-- ─── Main Content ──────────────────────────────────────────────────── -->
    <template v-else-if="profile">
      <!-- ─── Hero Banner ─────────────────────────────────────────────────── -->
      <section class="overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-sm">
        <div class="bg-gradient-to-r from-orange-600 via-orange-500 to-amber-400 px-6 py-7 text-white sm:px-8">
          <div class="flex flex-col gap-6 sm:flex-row sm:items-center sm:justify-between">
            <div class="flex items-center gap-5">
              <div class="flex h-20 w-20 shrink-0 items-center justify-center overflow-hidden rounded-2xl border-4 border-white/30 bg-white/20 text-2xl font-bold shadow-sm">
                <img
                  v-if="displayedAvatarUrl"
                  :src="displayedAvatarUrl"
                  alt="Ảnh đại diện Giáo viên"
                  class="h-full w-full object-cover"
                />
                <span v-else>{{ initials }}</span>
              </div>
              <div class="min-w-0">
                <p class="text-sm font-medium text-orange-50">Xin chào,</p>
                <h2 class="mt-1 truncate text-2xl font-bold">{{ profile.fullName || 'Giáo viên' }}</h2>
                <div class="mt-1 flex flex-wrap items-center gap-2 text-sm text-orange-50">
                  <span>Giáo viên chính thức</span>
                  <span>•</span>
                  <span>Mã GV: <strong class="text-white font-mono">GV-0{{ teacherId || profile.accountId }}</strong></span>
                </div>
              </div>
            </div>

            <div class="flex flex-col items-start gap-3 sm:items-end">
              <div class="flex flex-wrap items-center gap-2">
                <span class="rounded-lg border border-white/40 bg-white/10 px-3.5 py-2 text-xs font-semibold text-white backdrop-blur-xs">
                  📚 {{ assignedClasses.length }} lớp phụ trách
                </span>
                <span class="rounded-lg border border-white/40 bg-white/10 px-3.5 py-2 text-xs font-semibold text-white backdrop-blur-xs">
                  🎯 {{ teacherSpecialize || 'Toán Tư Duy' }}
                </span>
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

        <!-- Banner status feedback alerts -->
        <div class="px-6 py-4 sm:px-8">
          <p
            v-if="profileMessage"
            role="status"
            class="mb-3 rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700"
          >
            {{ profileMessage }}
          </p>
          <p
            v-if="profileError"
            role="alert"
            class="mb-3 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
          >
            {{ profileError }}
          </p>
          <div class="flex flex-wrap gap-x-6 gap-y-2 text-xs text-gray-500">
            <span>Hồ sơ giáo viên được đồng bộ trực tiếp với hệ thống quản lý đào tạo và thời khóa biểu.</span>
          </div>
        </div>
      </section>

      <!-- ─── Section 1: Thông tin cơ bản ─────────────────────────────────── -->
      <section class="rounded-2xl border border-gray-200 bg-white shadow-sm">
        <div class="flex flex-col gap-3 border-b border-gray-100 px-6 py-5 sm:flex-row sm:items-center sm:justify-between sm:px-8">
          <div>
            <h2 class="text-lg font-semibold text-gray-900">Thông tin cơ bản</h2>
            <p class="mt-1 text-sm text-gray-500">Tên và số điện thoại dùng để đăng nhập và định danh tài khoản.</p>
          </div>
          <span class="w-fit rounded-full bg-orange-50 px-3 py-1 text-xs font-semibold text-orange-700 ring-1 ring-inset ring-orange-200">
            Vai trò: Giáo viên
          </span>
        </div>

        <form class="space-y-6 px-6 py-6 sm:px-8" @submit.prevent="saveProfile">
          <div class="grid gap-5 md:grid-cols-2">
            <div>
              <label for="fullName" class="mb-2 block text-sm font-medium text-gray-700">Họ và tên</label>
              <input
                id="fullName"
                v-model.trim="form.fullName"
                :disabled="!isEditingProfile"
                required
                maxlength="100"
                autocomplete="name"
                class="w-full rounded-xl border border-gray-300 bg-white px-4 py-3 text-sm text-gray-900 outline-none transition placeholder:text-gray-400 focus:border-orange-500 focus:ring-4 focus:ring-orange-100 disabled:border-gray-200 disabled:bg-gray-50 disabled:text-gray-600"
              />
            </div>
            <div>
              <label for="phone" class="mb-2 block text-sm font-medium text-gray-700">Số điện thoại đăng nhập</label>
              <input
                id="phone"
                v-model.trim="form.phone"
                :disabled="true"
                type="tel"
                maxlength="20"
                autocomplete="tel"
                class="w-full rounded-xl border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-600 outline-none cursor-not-allowed"
                title="Số điện thoại tài khoản do quản trị viên cấp"
              />
              <p class="mt-2 text-xs leading-5 text-gray-500">Số điện thoại dùng để đăng nhập hệ thống.</p>
            </div>
          </div>

          <div class="flex flex-wrap items-center gap-3 border-t border-gray-100 pt-5">
            <button
              v-if="!isEditingProfile"
              type="button"
              class="rounded-lg bg-orange-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-orange-700 focus:outline-none focus:ring-4 focus:ring-orange-200 cursor-pointer"
              @click="startEditingProfile"
            >
              Chỉnh sửa thông tin
            </button>
            <template v-else>
              <button
                type="submit"
                :disabled="isSavingProfile"
                class="rounded-lg bg-orange-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-orange-700 focus:outline-none focus:ring-4 focus:ring-orange-200 disabled:cursor-not-allowed disabled:opacity-60 cursor-pointer"
              >
                {{ isSavingProfile ? 'Đang lưu...' : 'Lưu thay đổi' }}
              </button>
              <button
                type="button"
                :disabled="isSavingProfile"
                class="rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm font-semibold text-gray-700 transition hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-60 cursor-pointer"
                @click="cancelEditingProfile"
              >
                Hủy
              </button>
              <span class="text-xs text-gray-500">Thông tin chỉ được cập nhật sau khi lưu.</span>
            </template>
          </div>
        </form>
      </section>

      <!-- ─── Section 2: Email liên hệ ────────────────────────────────────── -->
      <section class="rounded-2xl border border-gray-200 bg-white shadow-sm">
        <div class="border-b border-gray-100 px-6 py-5 sm:px-8">
          <h2 class="text-lg font-semibold text-gray-900">Email liên hệ</h2>
          <p class="mt-1 text-sm text-gray-500">Email nhận thông báo công việc, lịch dạy và thông tin từ trung tâm.</p>
        </div>

        <form class="space-y-5 px-6 py-6 sm:px-8" @submit.prevent="saveEmail">
          <div class="max-w-2xl">
            <label for="email" class="mb-2 block text-sm font-medium text-gray-700">Địa chỉ email</label>
            <input
              id="email"
              v-model.trim="email"
              :disabled="!isEditingEmail"
              required
              type="email"
              maxlength="256"
              autocomplete="email"
              class="w-full rounded-xl border border-gray-300 bg-white px-4 py-3 text-sm text-gray-900 outline-none transition focus:border-orange-500 focus:ring-4 focus:ring-orange-100 disabled:border-gray-200 disabled:bg-gray-50 disabled:text-gray-600"
            />
          </div>

          <p
            v-if="emailMessage"
            role="status"
            class="rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700"
          >
            {{ emailMessage }}
          </p>
          <p
            v-if="emailError"
            role="alert"
            class="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
          >
            {{ emailError }}
          </p>

          <div class="flex flex-wrap items-center gap-3 border-t border-gray-100 pt-5">
            <button
              v-if="!isEditingEmail"
              type="button"
              class="rounded-lg bg-orange-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-orange-700 focus:outline-none focus:ring-4 focus:ring-orange-200 cursor-pointer"
              @click="startEditingEmail"
            >
              Đổi email
            </button>
            <template v-else>
              <button
                type="submit"
                :disabled="isSavingEmail"
                class="rounded-lg bg-orange-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-orange-700 focus:outline-none focus:ring-4 focus:ring-orange-200 disabled:cursor-not-allowed disabled:opacity-60 cursor-pointer"
              >
                {{ isSavingEmail ? 'Đang lưu...' : 'Lưu thay đổi' }}
              </button>
              <button
                type="button"
                :disabled="isSavingEmail"
                class="rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm font-semibold text-gray-700 transition hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-60 cursor-pointer"
                @click="cancelEditingEmail"
              >
                Hủy
              </button>
            </template>
          </div>
        </form>
      </section>

      <!-- ─── Section 3: Hồ sơ chuyên môn & Giảng dạy ──────────────────────── -->
      <section class="rounded-2xl border border-gray-200 bg-white shadow-sm">
        <div class="flex flex-col gap-3 border-b border-gray-100 px-6 py-5 sm:flex-row sm:items-center sm:justify-between sm:px-8">
          <div>
            <h2 class="text-lg font-semibold text-gray-900">Hồ sơ chuyên môn & Giảng dạy</h2>
            <p class="mt-1 text-sm text-gray-500">Lĩnh vực chuyên môn sư phạm và các lớp học được phân công phụ trách.</p>
          </div>
          <span class="w-fit rounded-full bg-emerald-50 px-3 py-1 text-xs font-semibold text-emerald-700 ring-1 ring-inset ring-emerald-200">
            Trạng thái: Đang hoạt động
          </span>
        </div>

        <form class="space-y-6 px-6 py-6 sm:px-8" @submit.prevent="saveSpecialize">
          <div class="grid gap-5 md:grid-cols-2">
            <div>
              <label for="specialize" class="mb-2 block text-sm font-medium text-gray-700">Chuyên môn phụ trách</label>
              <input
                id="specialize"
                v-model.trim="teacherSpecialize"
                :disabled="!isEditingSpecialize"
                placeholder="VD: Toán Tư Duy Tiểu Học, Toán Logic, Hình Học Không Gian"
                class="w-full rounded-xl border border-gray-300 bg-white px-4 py-3 text-sm text-gray-900 outline-none transition placeholder:text-gray-400 focus:border-orange-500 focus:ring-4 focus:ring-orange-100 disabled:border-gray-200 disabled:bg-gray-50 disabled:text-gray-600"
              />
              <p class="mt-2 text-xs leading-5 text-gray-500">Các môn học, chuyên đề toán tư duy bạn chịu trách nhiệm giảng dạy.</p>
            </div>

            <div>
              <label class="mb-2 block text-sm font-medium text-gray-700">Các lớp học đang phụ trách ({{ assignedClasses.length }})</label>
              <div v-if="assignedClasses.length === 0" class="rounded-xl border border-dashed border-gray-300 p-3 text-xs text-gray-400 text-center">
                Chưa có lớp học nào được phân công.
              </div>
              <div v-else class="flex flex-wrap gap-2 pt-1 max-h-32 overflow-y-auto">
                <span
                  v-for="c in assignedClasses"
                  :key="c.id"
                  class="inline-flex items-center gap-1.5 px-3 py-1 rounded-lg bg-orange-50 border border-orange-200 text-orange-900 font-semibold text-xs"
                >
                  <span>🏫</span>
                  <span>{{ c.name }}</span>
                  <span class="text-[10px] text-orange-700 font-normal">({{ c.code || 'Lớp' }})</span>
                </span>
              </div>
              <p class="mt-2 text-xs leading-5 text-gray-500">Danh sách lớp học được phân công bởi Quản trị viên.</p>
            </div>
          </div>

          <p
            v-if="specializeMessage"
            role="status"
            class="rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700"
          >
            {{ specializeMessage }}
          </p>
          <p
            v-if="specializeError"
            role="alert"
            class="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
          >
            {{ specializeError }}
          </p>

          <div class="flex flex-wrap items-center gap-3 border-t border-gray-100 pt-5">
            <button
              v-if="!isEditingSpecialize"
              type="button"
              class="rounded-lg bg-orange-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-orange-700 focus:outline-none focus:ring-4 focus:ring-orange-200 cursor-pointer"
              @click="startEditingSpecialize"
            >
              Chỉnh sửa chuyên môn
            </button>
            <template v-else>
              <button
                type="submit"
                :disabled="isSavingSpecialize"
                class="rounded-lg bg-orange-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-orange-700 focus:outline-none focus:ring-4 focus:ring-orange-200 disabled:cursor-not-allowed disabled:opacity-60 cursor-pointer"
              >
                {{ isSavingSpecialize ? 'Đang lưu...' : 'Lưu thay đổi' }}
              </button>
              <button
                type="button"
                :disabled="isSavingSpecialize"
                class="rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm font-semibold text-gray-700 transition hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-60 cursor-pointer"
                @click="cancelEditingSpecialize"
              >
                Hủy
              </button>
            </template>
          </div>
        </form>
      </section>

      <!-- ─── Section 4: Bảo mật ──────────────────────────────────────────── -->
      <section class="rounded-2xl border border-gray-200 bg-white shadow-sm">
        <div class="border-b border-gray-100 px-6 py-5 sm:px-8">
          <h2 class="text-lg font-semibold text-gray-900">Bảo mật</h2>
          <p class="mt-1 text-sm text-gray-500">Đổi mật khẩu định kỳ để bảo vệ tài khoản giáo viên.</p>
        </div>

        <form class="space-y-5 px-6 py-6 sm:px-8" @submit.prevent="changePassword">
          <template v-if="isEditingPassword">
            <div class="grid gap-5 md:grid-cols-2">
              <div class="md:col-span-2">
                <label for="currentPassword" class="mb-2 block text-sm font-medium text-gray-700">Mật khẩu hiện tại</label>
                <input
                  id="currentPassword"
                  v-model="passwordForm.currentPassword"
                  required
                  type="password"
                  autocomplete="current-password"
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
                  type="password"
                  autocomplete="new-password"
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
                  type="password"
                  autocomplete="new-password"
                  class="w-full rounded-xl border border-gray-300 px-4 py-3 text-sm outline-none transition focus:border-orange-500 focus:ring-4 focus:ring-orange-100"
                />
              </div>
            </div>
          </template>

          <p v-else class="rounded-xl bg-gray-50 px-4 py-3 text-sm text-gray-600">
            Mật khẩu hiện tại được ẩn. Chọn “Đổi mật khẩu” để nhập mật khẩu mới.
          </p>

          <p
            v-if="passwordMessage"
            role="status"
            class="rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700"
          >
            {{ passwordMessage }}
          </p>
          <p
            v-if="passwordError"
            role="alert"
            class="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
          >
            {{ passwordError }}
          </p>

          <div class="flex flex-wrap items-center gap-3 border-t border-gray-100 pt-5">
            <button
              v-if="!isEditingPassword"
              type="button"
              class="rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm font-semibold text-gray-700 transition hover:bg-gray-50 focus:outline-none focus:ring-4 focus:ring-gray-100 cursor-pointer"
              @click="startEditingPassword"
            >
              Đổi mật khẩu
            </button>
            <template v-else>
              <button
                type="submit"
                :disabled="isChangingPassword"
                class="rounded-lg bg-orange-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-orange-700 focus:outline-none focus:ring-4 focus:ring-orange-200 disabled:cursor-not-allowed disabled:opacity-60 cursor-pointer"
              >
                {{ isChangingPassword ? 'Đang cập nhật...' : 'Lưu mật khẩu' }}
              </button>
              <button
                type="button"
                :disabled="isChangingPassword"
                class="rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm font-semibold text-gray-700 transition hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-60 cursor-pointer"
                @click="cancelEditingPassword"
              >
                Hủy
              </button>
            </template>
          </div>
        </form>
      </section>
    </template>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, onUnmounted } from 'vue'
import authService from '@/services/auth.service'
import profileService from '@/services/profile.service'
import teacherService from '@/services/teacher.service'
import classService from '@/services/class.service'
import { getProfileId } from '@/services/auth-storage'

// ─── State ─────────────────────────────────────────────────────────────────
const profile = ref(null)
const teacherId = ref(null)
const teacherSpecialize = ref('')
const assignedClasses = ref([])

const isLoading = ref(true)
const loadError = ref('')

const isEditingProfile = ref(false)
const isEditingEmail = ref(false)
const isEditingSpecialize = ref(false)
const isEditingPassword = ref(false)
const isEditingAvatar = ref(false)
const avatarFile = ref(null)
const avatarPreviewUrl = ref('')
const isAvatarRemovalPending = ref(false)

const form = reactive({ fullName: '', phone: '' })
const email = ref('')
const originalSpecialize = ref('')

const passwordForm = reactive({
  currentPassword: '',
  newPassword: '',
  confirmNewPassword: '',
})

const isSavingProfile = ref(false)
const isSavingEmail = ref(false)
const isSavingSpecialize = ref(false)
const isChangingPassword = ref(false)
const isUploadingAvatar = ref(false)

const profileMessage = ref('')
const profileError = ref('')
const emailMessage = ref('')
const emailError = ref('')
const specializeMessage = ref('')
const specializeError = ref('')
const passwordMessage = ref('')
const passwordError = ref('')

// ─── Computed ──────────────────────────────────────────────────────────────
const displayedAvatarUrl = computed(() => {
  if (isAvatarRemovalPending.value) return ''
  if (avatarPreviewUrl.value) return avatarPreviewUrl.value
  return profile.value?.avatar || ''
})

const initials = computed(() => {
  const words = (form.fullName || 'Giáo viên').trim().split(/\s+/)
  return words.slice(-2).map((w) => w[0]).join('').toUpperCase()
})

function getErrorMessage(error) {
  return (
    error.response?.data?.message ??
    error.response?.data?.title ??
    error.message ??
    'Đã xảy ra lỗi. Vui lòng thử lại.'
  )
}

function applyProfile(value) {
  profile.value = value
  form.fullName = value.fullName ?? ''
  form.phone = value.phone ?? ''
  email.value = value.email ?? ''
}

function notifyProfileUpdated() {
  window.dispatchEvent(new Event('admin-profile-updated'))
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

// ─── Load Data ─────────────────────────────────────────────────────────────
async function loadProfile() {
  isLoading.value = true
  loadError.value = ''
  try {
    const res = await profileService.get()
    applyProfile(res)

    // Lấy profileId của teacher
    const pId = getProfileId('TEACHER') || res.accountId
    teacherId.value = pId

    // Lấy thông tin chi tiết giáo viên (specialize)
    if (pId) {
      try {
        const teacherData = await teacherService.get(pId)
        if (teacherData) {
          teacherSpecialize.value = teacherData.specialize || ''
          originalSpecialize.value = teacherData.specialize || ''
          if (teacherData.email && !email.value) {
            email.value = teacherData.email
          }
        }
      } catch (err) {
        console.warn('Không thể tải chuyên môn giáo viên chi tiết:', err)
      }
    }

    // Lấy danh sách lớp phân công
    try {
      const clsList = await classService.getAssignedClasses(res, pId)
      assignedClasses.value = Array.isArray(clsList) ? clsList : []
    } catch (err) {
      console.warn('Không thể tải lớp phân công của giáo viên:', err)
    }
  } catch (error) {
    loadError.value = getErrorMessage(error)
  } finally {
    isLoading.value = false
  }
}

// ─── Section 1: Save Basic Info ────────────────────────────────────────────
function startEditingProfile() {
  profileMessage.value = ''
  profileError.value = ''
  isEditingProfile.value = true
}

function cancelEditingProfile() {
  form.fullName = profile.value?.fullName ?? ''
  profileMessage.value = ''
  profileError.value = ''
  isEditingProfile.value = false
}

async function saveProfile() {
  isSavingProfile.value = true
  profileMessage.value = ''
  profileError.value = ''
  try {
    const updated = await profileService.update({
      fullName: form.fullName.trim(),
      phone: form.phone.trim(),
    })
    applyProfile(updated)

    // Đồng bộ sang teacher table nếu có teacherId
    if (teacherId.value) {
      try {
        await teacherService.update(teacherId.value, {
          fullName: form.fullName.trim(),
          email: email.value.trim() || null,
          specialize: teacherSpecialize.value.trim() || null,
        })
      } catch (tErr) {
        console.warn('Lỗi đồng bộ bảng giáo viên:', tErr)
      }
    }

    notifyProfileUpdated()
    profileMessage.value = 'Đã cập nhật thông tin cá nhân thành công.'
    isEditingProfile.value = false
  } catch (error) {
    profileError.value = getErrorMessage(error)
  } finally {
    isSavingProfile.value = false
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

// ─── Section 2: Save Email ─────────────────────────────────────────────────
function startEditingEmail() {
  emailMessage.value = ''
  emailError.value = ''
  isEditingEmail.value = true
}

function cancelEditingEmail() {
  email.value = profile.value?.email ?? ''
  emailMessage.value = ''
  emailError.value = ''
  isEditingEmail.value = false
}

async function saveEmail() {
  isSavingEmail.value = true
  emailMessage.value = ''
  emailError.value = ''
  try {
    if (teacherId.value) {
      await teacherService.update(teacherId.value, {
        fullName: form.fullName.trim(),
        email: email.value.trim(),
        specialize: teacherSpecialize.value.trim() || null,
      })
    }
    if (profile.value) {
      profile.value.email = email.value.trim()
    }
    notifyProfileUpdated()
    emailMessage.value = 'Đã cập nhật địa chỉ email liên hệ thành công.'
    isEditingEmail.value = false
  } catch (error) {
    emailError.value = getErrorMessage(error)
  } finally {
    isSavingEmail.value = false
  }
}

// ─── Section 3: Save Specialize ───────────────────────────────────────────
function startEditingSpecialize() {
  specializeMessage.value = ''
  specializeError.value = ''
  isEditingSpecialize.value = true
}

function cancelEditingSpecialize() {
  teacherSpecialize.value = originalSpecialize.value
  specializeMessage.value = ''
  specializeError.value = ''
  isEditingSpecialize.value = false
}

async function saveSpecialize() {
  isSavingSpecialize.value = true
  specializeMessage.value = ''
  specializeError.value = ''
  try {
    if (teacherId.value) {
      await teacherService.update(teacherId.value, {
        fullName: form.fullName.trim(),
        email: email.value.trim() || null,
        specialize: teacherSpecialize.value.trim(),
      })
      originalSpecialize.value = teacherSpecialize.value.trim()
    }
    specializeMessage.value = 'Đã cập nhật hồ sơ chuyên môn giảng dạy.'
    isEditingSpecialize.value = false
  } catch (error) {
    specializeError.value = getErrorMessage(error)
  } finally {
    isSavingSpecialize.value = false
  }
}

// ─── Section 4: Change Password ────────────────────────────────────────────
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

async function changePassword() {
  passwordMessage.value = ''
  passwordError.value = ''
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
