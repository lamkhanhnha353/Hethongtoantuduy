<template>
  <div class="space-y-6 max-w-6xl mx-auto pb-12">
    <!-- ─── Breadcrumb & Header ────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
      <div>
        <nav class="flex items-center gap-2 text-xs font-medium text-orange-600 mb-1">
          <span>Tài khoản của tôi</span>
          <ChevronRightIcon class="w-3.5 h-3.5 text-orange-400" />
          <span class="text-orange-950 font-semibold">Thông tin cá nhân</span>
        </nav>
        <h1 class="text-2xl md:text-3xl font-bold text-gray-900 tracking-tight">Hồ sơ cá nhân</h1>
        <p class="text-sm text-gray-500 mt-0.5">Quản lý thông tin cá nhân, hình ảnh đại diện và tùy chọn tài khoản</p>
      </div>

      <!-- Quick Tab Navigation Switcher -->
      <div class="flex items-center bg-white p-1 rounded-2xl border border-orange-200/80 shadow-xs self-start sm:self-auto">
        <router-link
          to="/account/profile"
          class="flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-semibold bg-orange-500 text-white shadow-xs transition-all"
        >
          <UserIcon class="w-3.5 h-3.5" />
          <span>Thông tin cá nhân</span>
        </router-link>
        <router-link
          to="/account/password"
          class="flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-medium text-gray-600 hover:text-orange-700 hover:bg-orange-50 transition-all"
        >
          <ShieldCheckIcon class="w-3.5 h-3.5 text-orange-500" />
          <span>Đổi mật khẩu / PIN</span>
        </router-link>
      </div>
    </div>

    <!-- ─── Notification Alerts ────────────────────────────────────────── -->
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

    <!-- ─── Hero Profile Header Card ───────────────────────────────────── -->
    <div class="relative overflow-hidden bg-white rounded-3xl border border-orange-100 shadow-sm">
      <!-- Decorative Header Banner -->
      <div class="h-32 sm:h-40 bg-gradient-to-r from-orange-400 via-amber-400 to-orange-500 relative">
        <div class="absolute inset-0 bg-[radial-gradient(ellipse_at_top_right,_var(--tw-gradient-stops))] from-white/20 via-transparent to-transparent"></div>
        <div class="absolute -right-10 -bottom-10 w-48 h-48 bg-white/10 rounded-full blur-2xl pointer-events-none"></div>
      </div>

      <!-- Avatar & Basic Bio Container -->
      <div class="px-6 pb-6 sm:px-8">
        <div class="relative flex flex-col sm:flex-row sm:items-end justify-between gap-4 -mt-16 sm:-mt-20">
          <!-- Avatar With Upload Action -->
          <div class="flex items-end gap-5">
            <div class="relative group">
              <!-- Avatar Display -->
              <div class="w-28 h-28 sm:w-32 sm:h-32 rounded-3xl ring-4 ring-white bg-white shadow-md overflow-hidden flex items-center justify-center">
                <img
                  v-if="avatarPreview || profile.avatar"
                  :src="avatarPreview || profile.avatar"
                  :alt="profile.fullName"
                  class="w-full h-full object-cover"
                />
                <div
                  v-else
                  class="w-full h-full bg-gradient-to-br from-orange-400 to-amber-500 text-white font-bold text-3xl flex items-center justify-center shadow-inner"
                >
                  {{ initials }}
                </div>
              </div>

              <!-- Upload overlay button -->
              <label
                for="avatar-upload"
                class="absolute bottom-1 right-1 w-9 h-9 rounded-2xl bg-orange-600 hover:bg-orange-700 text-white flex items-center justify-center shadow-md cursor-pointer transition-all hover:scale-105 ring-2 ring-white"
                title="Thay đổi ảnh đại diện"
              >
                <CameraIcon class="w-4 h-4" />
                <input
                  id="avatar-upload"
                  type="file"
                  accept="image/png, image/jpeg, image/webp"
                  class="sr-only"
                  @change="handleAvatarChange"
                  :disabled="isUploadingAvatar"
                />
              </label>
            </div>

            <!-- Name and Roles -->
            <div class="mb-2">
              <div class="flex items-center gap-2.5 flex-wrap">
                <h2 class="text-xl sm:text-2xl font-bold text-gray-900">{{ profile.fullName || 'Người dùng' }}</h2>
                <span class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-bold uppercase tracking-wider bg-orange-100 text-orange-800 border border-orange-200">
                  <ShieldCheckIcon class="w-3.5 h-3.5 text-orange-600" />
                  {{ profile.role || 'Admin' }}
                </span>
                <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[11px] font-medium bg-emerald-50 text-emerald-700 border border-emerald-200">
                  <span class="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
                  Đang hoạt động
                </span>
              </div>
              <p class="text-xs sm:text-sm text-gray-500 font-mono mt-1">
                Số điện thoại: {{ profile.phone || 'Chưa thiết lập' }} &bull; ID: #{{ profile.accountId || '001' }}
              </p>
            </div>
          </div>

          <!-- Avatar Actions (if custom avatar exists) -->
          <div class="flex items-center gap-2 self-start sm:self-auto mb-2">
            <button
              v-if="avatarPreview"
              type="button"
              @click="saveAvatar"
              :disabled="isUploadingAvatar"
              class="flex items-center gap-1.5 px-3.5 py-2 rounded-xl text-xs font-semibold bg-emerald-600 hover:bg-emerald-700 text-white shadow-xs transition-all cursor-pointer"
            >
              <CheckIcon class="w-3.5 h-3.5" />
              <span>{{ isUploadingAvatar ? 'Đang tải...' : 'Lưu ảnh mới' }}</span>
            </button>
            <button
              v-if="avatarPreview"
              type="button"
              @click="cancelAvatarPreview"
              class="px-3 py-2 rounded-xl text-xs font-medium bg-gray-100 hover:bg-gray-200 text-gray-700 transition-all cursor-pointer"
            >
              Hủy
            </button>
            <button
              v-if="!avatarPreview && profile.avatar"
              type="button"
              @click="confirmDeleteAvatar"
              class="flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs font-medium text-rose-600 hover:bg-rose-50 border border-rose-200 transition-all cursor-pointer"
            >
              <TrashIcon class="w-3.5 h-3.5" />
              <span>Xóa ảnh</span>
            </button>
          </div>
        </div>

        <!-- Quick Info Ribbon -->
        <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 mt-6 pt-5 border-t border-gray-100 text-xs">
          <div class="p-3 bg-orange-50/50 rounded-2xl border border-orange-100/60">
            <p class="text-gray-500 font-medium">Vai trò hệ thống</p>
            <p class="text-orange-900 font-bold mt-0.5">{{ profile.role === 'Admin' ? 'Quản trị viên cấp cao' : 'Giảng viên' }}</p>
          </div>
          <div class="p-3 bg-orange-50/50 rounded-2xl border border-orange-100/60">
            <p class="text-gray-500 font-medium">Bảo mật mã PIN</p>
            <p :class="profile.hasPin ? 'text-emerald-700 font-bold' : 'text-amber-700 font-bold'" class="mt-0.5">
              {{ profile.hasPin ? 'Đã kích hoạt' : 'Chưa thiết lập' }}
            </p>
          </div>
          <div class="p-3 bg-orange-50/50 rounded-2xl border border-orange-100/60">
            <p class="text-gray-500 font-medium">Xác thực Email</p>
            <p :class="profile.emailConfirm ? 'text-emerald-700 font-bold' : 'text-gray-700 font-semibold'" class="mt-0.5">
              {{ profile.emailConfirm ? 'Đã xác minh' : 'Chưa xác minh' }}
            </p>
          </div>
          <div class="p-3 bg-orange-50/50 rounded-2xl border border-orange-100/60">
            <p class="text-gray-500 font-medium">Phiên làm việc</p>
            <p class="text-gray-800 font-bold mt-0.5">Bảo vệ qua JWT</p>
          </div>
        </div>
      </div>
    </div>

    <!-- ─── Main Details Grid ──────────────────────────────────────────── -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <!-- Col 1 & 2: Edit Information Form -->
      <div class="lg:col-span-2 space-y-6">
        <div class="bg-white rounded-3xl border border-orange-100 shadow-sm p-6 sm:p-8">
          <div class="flex items-center justify-between pb-5 border-b border-gray-100 mb-6">
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-2xl bg-orange-100 text-orange-600 flex items-center justify-center">
                <IdentificationIcon class="w-5 h-5" />
              </div>
              <div>
                <h3 class="text-lg font-bold text-gray-900">Thông tin cơ bản</h3>
                <p class="text-xs text-gray-500">Cập nhật họ tên hiển thị và email liên hệ của bạn</p>
              </div>
            </div>
            <button
              v-if="!isEditing"
              type="button"
              @click="enableEdit"
              class="flex items-center gap-1.5 px-3.5 py-2 rounded-xl text-xs font-semibold text-orange-700 bg-orange-50 hover:bg-orange-100 transition-colors cursor-pointer"
            >
              <PencilSquareIcon class="w-4 h-4" />
              <span>Chỉnh sửa</span>
            </button>
          </div>

          <form @submit.prevent="handleSaveProfile" class="space-y-5">
            <!-- Full Name -->
            <div>
              <label class="block text-xs font-bold uppercase tracking-wider text-gray-700 mb-1.5">
                Họ và tên <span class="text-rose-500">*</span>
              </label>
              <div class="relative">
                <input
                  v-model="form.fullName"
                  type="text"
                  required
                  :disabled="!isEditing || isSubmitting"
                  placeholder="Nhập họ và tên đầy đủ..."
                  :class="[
                    'w-full px-4 py-2.5 rounded-xl border text-sm transition-all focus:outline-none',
                    !isEditing
                      ? 'bg-gray-50/70 border-gray-200 text-gray-800 cursor-not-allowed'
                      : 'bg-white border-orange-300 focus:border-orange-500 focus:ring-3 focus:ring-orange-100 text-gray-900'
                  ]"
                />
                <UserIcon class="w-4 h-4 text-gray-400 absolute right-3.5 top-3 pointer-events-none" />
              </div>
              <p v-if="nameError" class="text-xs text-rose-600 mt-1">{{ nameError }}</p>
            </div>

            <!-- Phone Number (Username - Read only) -->
            <div>
              <div class="flex items-center justify-between mb-1.5">
                <label class="block text-xs font-bold uppercase tracking-wider text-gray-700">
                  Số điện thoại đăng nhập
                </label>
                <span class="text-[11px] font-medium text-gray-400 flex items-center gap-1">
                  <LockClosedIcon class="w-3 h-3" /> Cố định
                </span>
              </div>
              <div class="relative">
                <input
                  :value="profile.phone || '0901234567'"
                  disabled
                  type="text"
                  class="w-full px-4 py-2.5 rounded-xl border border-gray-200 bg-gray-50/80 text-gray-600 font-mono text-sm cursor-not-allowed focus:outline-none"
                />
                <PhoneIcon class="w-4 h-4 text-gray-400 absolute right-3.5 top-3 pointer-events-none" />
              </div>
              <p class="text-[11px] text-gray-400 mt-1">Số điện thoại được dùng làm tên đăng nhập tài khoản hệ thống.</p>
            </div>

            <!-- Email Address -->
            <div>
              <div class="flex items-center justify-between mb-1.5">
                <label class="block text-xs font-bold uppercase tracking-wider text-gray-700">
                  Địa chỉ Email
                </label>
                <span
                  v-if="profile.emailConfirm"
                  class="inline-flex items-center gap-1 text-[11px] font-semibold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-full"
                >
                  <CheckCircleIcon class="w-3 h-3" /> Đã xác thực
                </span>
                <span
                  v-else
                  class="inline-flex items-center gap-1 text-[11px] font-semibold text-amber-700 bg-amber-50 px-2 py-0.5 rounded-full"
                >
                  Chưa xác thực
                </span>
              </div>
              <div class="relative">
                <input
                  v-model="form.email"
                  type="email"
                  :disabled="!isEditing || isSubmitting"
                  placeholder="admin@toantuduy.edu.vn"
                  :class="[
                    'w-full px-4 py-2.5 rounded-xl border text-sm transition-all focus:outline-none',
                    !isEditing
                      ? 'bg-gray-50/70 border-gray-200 text-gray-800 cursor-not-allowed'
                      : 'bg-white border-orange-300 focus:border-orange-500 focus:ring-3 focus:ring-orange-100 text-gray-900'
                  ]"
                />
                <EnvelopeIcon class="w-4 h-4 text-gray-400 absolute right-3.5 top-3 pointer-events-none" />
              </div>
            </div>

            <!-- Role & Account Level -->
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-2">
              <div>
                <label class="block text-xs font-bold uppercase tracking-wider text-gray-700 mb-1.5">
                  Phân quyền hệ thống
                </label>
                <div class="flex items-center gap-2 p-2.5 rounded-xl bg-orange-50/50 border border-orange-100">
                  <ShieldCheckIcon class="w-5 h-5 text-orange-600" />
                  <div>
                    <p class="text-xs font-bold text-orange-950">{{ profile.role || 'Admin' }}</p>
                    <p class="text-[11px] text-orange-700">Quyền truy cập toàn diện bảng điều khiển</p>
                  </div>
                </div>
              </div>

              <div>
                <label class="block text-xs font-bold uppercase tracking-wider text-gray-700 mb-1.5">
                  Trạng thái bảo mật
                </label>
                <div class="flex items-center gap-2 p-2.5 rounded-xl bg-emerald-50/60 border border-emerald-100">
                  <CheckCircleIcon class="w-5 h-5 text-emerald-600" />
                  <div>
                    <p class="text-xs font-bold text-emerald-950">Được bảo vệ an toàn</p>
                    <p class="text-[11px] text-emerald-700">Mật khẩu mã hóa & phiên xác thực</p>
                  </div>
                </div>
              </div>
            </div>

            <!-- Form Actions -->
            <div v-if="isEditing" class="flex items-center justify-end gap-3 pt-5 border-t border-gray-100">
              <button
                type="button"
                @click="cancelEdit"
                class="px-4 py-2.5 rounded-xl text-xs font-semibold text-gray-600 hover:bg-gray-100 transition-colors cursor-pointer"
                :disabled="isSubmitting"
              >
                Hủy bỏ
              </button>
              <button
                type="submit"
                :disabled="isSubmitting"
                class="flex items-center gap-2 px-5 py-2.5 rounded-xl text-xs font-semibold text-white bg-orange-500 hover:bg-orange-600 shadow-sm transition-all cursor-pointer disabled:opacity-50"
              >
                <ArrowPathIcon v-if="isSubmitting" class="w-4 h-4 animate-spin" />
                <CheckIcon v-else class="w-4 h-4" />
                <span>{{ isSubmitting ? 'Đang lưu...' : 'Lưu thay đổi' }}</span>
              </button>
            </div>
          </form>
        </div>

        <!-- Notification Preferences Card -->
        <div class="bg-white rounded-3xl border border-orange-100 shadow-sm p-6 sm:p-8">
          <div class="flex items-center gap-3 pb-4 border-b border-gray-100 mb-5">
            <div class="w-10 h-10 rounded-2xl bg-amber-100 text-amber-600 flex items-center justify-center">
              <BellAlertIcon class="w-5 h-5" />
            </div>
            <div>
              <h3 class="text-lg font-bold text-gray-900">Tùy chọn thông báo</h3>
              <p class="text-xs text-gray-500">Tùy chỉnh các thông báo bạn muốn nhận về máy</p>
            </div>
          </div>

          <div class="space-y-4">
            <label class="flex items-start justify-between gap-4 p-3.5 rounded-2xl border border-gray-100 hover:bg-orange-50/30 transition-colors cursor-pointer">
              <div class="flex items-start gap-3">
                <input type="checkbox" v-model="settings.notifNewStudent" class="mt-1 w-4 h-4 rounded text-orange-600 focus:ring-orange-500" />
                <div>
                  <p class="text-sm font-semibold text-gray-800">Thông báo học viên mới</p>
                  <p class="text-xs text-gray-500">Nhận thông báo khi có học sinh mới đăng ký hồ sơ hoặc ghi danh khóa học</p>
                </div>
              </div>
            </label>

            <label class="flex items-start justify-between gap-4 p-3.5 rounded-2xl border border-gray-100 hover:bg-orange-50/30 transition-colors cursor-pointer">
              <div class="flex items-start gap-3">
                <input type="checkbox" v-model="settings.notifTuition" class="mt-1 w-4 h-4 rounded text-orange-600 focus:ring-orange-500" />
                <div>
                  <p class="text-sm font-semibold text-gray-800">Thông báo học phí & hóa đơn</p>
                  <p class="text-xs text-gray-500">Nhận cập nhật thanh toán và nhắc nhở học phí định kỳ</p>
                </div>
              </div>
            </label>

            <label class="flex items-start justify-between gap-4 p-3.5 rounded-2xl border border-gray-100 hover:bg-orange-50/30 transition-colors cursor-pointer">
              <div class="flex items-start gap-3">
                <input type="checkbox" v-model="settings.notifSecurity" class="mt-1 w-4 h-4 rounded text-orange-600 focus:ring-orange-500" />
                <div>
                  <p class="text-sm font-semibold text-gray-800">Cảnh báo bảo mật tài khoản</p>
                  <p class="text-xs text-gray-500">Gửi email cảnh báo khi có lượt đăng nhập từ thiết bị mới hoặc đổi mật khẩu</p>
                </div>
              </div>
            </label>
          </div>
        </div>
      </div>

      <!-- Col 3: Side Widget - Security & Actions -->
      <div class="space-y-6">
        <!-- Security Overview Widget -->
        <div class="bg-gradient-to-br from-orange-500 to-amber-600 rounded-3xl p-6 text-white shadow-md relative overflow-hidden">
          <div class="absolute -right-6 -bottom-6 w-36 h-36 bg-white/10 rounded-full blur-xl pointer-events-none"></div>

          <div class="flex items-center justify-between mb-4">
            <span class="text-xs uppercase font-bold tracking-wider text-orange-100">Bảo mật tài khoản</span>
            <span class="px-2.5 py-0.5 rounded-full text-xs font-bold bg-white/20 text-white backdrop-blur-xs">
              95/100 An toàn
            </span>
          </div>

          <h4 class="text-xl font-bold mb-2">Tài khoản được bảo vệ</h4>
          <p class="text-xs text-orange-100 leading-relaxed mb-5">
            Tài khoản quản trị của bạn đang hoạt động an toàn. Khuyến nghị cập nhật mật khẩu định kỳ 3 tháng một lần.
          </p>

          <router-link
            to="/account/password"
            class="w-full flex items-center justify-center gap-2 py-2.5 px-4 rounded-xl text-xs font-bold bg-white text-orange-600 hover:bg-orange-50 transition-colors shadow-sm"
          >
            <KeyIcon class="w-4 h-4" />
            <span>Đổi mật khẩu / PIN</span>
          </router-link>
        </div>

        <!-- Quick Access Checklist -->
        <div class="bg-white rounded-3xl border border-orange-100 shadow-sm p-6">
          <h4 class="text-sm font-bold text-gray-900 mb-4 flex items-center gap-2">
            <ClipboardDocumentCheckIcon class="w-4 h-4 text-orange-500" />
            <span>Hạng mục bảo vệ</span>
          </h4>

          <ul class="space-y-3 text-xs">
            <li class="flex items-center justify-between p-2.5 rounded-xl bg-orange-50/50">
              <span class="font-medium text-gray-700">Độ phức tạp mật khẩu</span>
              <span class="font-bold text-emerald-600 flex items-center gap-1">
                <CheckIcon class="w-3.5 h-3.5" /> Tiêu chuẩn cao
              </span>
            </li>
            <li class="flex items-center justify-between p-2.5 rounded-xl bg-orange-50/50">
              <span class="font-medium text-gray-700">Số điện thoại 2FA</span>
              <span class="font-bold text-emerald-600 flex items-center gap-1">
                <CheckIcon class="w-3.5 h-3.5" /> Đã kết nối
              </span>
            </li>
            <li class="flex items-center justify-between p-2.5 rounded-xl bg-orange-50/50">
              <span class="font-medium text-gray-700">Mã PIN giao dịch</span>
              <span :class="profile.hasPin ? 'text-emerald-600' : 'text-amber-600'" class="font-bold flex items-center gap-1">
                <span v-if="profile.hasPin"><CheckIcon class="w-3.5 h-3.5" /> Đã bật</span>
                <span v-else>Chưa bật</span>
              </span>
            </li>
          </ul>
        </div>

        <!-- Help Support Card -->
        <div class="bg-white rounded-3xl border border-orange-100 shadow-sm p-6 text-center">
          <div class="w-12 h-12 rounded-2xl bg-orange-100 text-orange-600 mx-auto flex items-center justify-center mb-3">
            <QuestionMarkCircleIcon class="w-6 h-6" />
          </div>
          <h5 class="text-sm font-bold text-gray-900">Cần hỗ trợ kỹ thuật?</h5>
          <p class="text-xs text-gray-500 mt-1 mb-4 leading-relaxed">
            Nếu bạn gặp khó khăn trong việc cập nhật hồ sơ hoặc quản lý quyền, hãy liên hệ đội ngũ phát triển.
          </p>
          <a
            href="mailto:hotro@toantuduy.edu.vn"
            class="inline-flex items-center gap-1.5 text-xs font-semibold text-orange-600 hover:text-orange-700 transition-colors"
          >
            <EnvelopeIcon class="w-3.5 h-3.5" />
            <span>hotro@toantuduy.edu.vn</span>
          </a>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, watch, onMounted } from 'vue'
import { useUserState } from '@/services/user-state'
import {
  UserIcon,
  ShieldCheckIcon,
  IdentificationIcon,
  LockClosedIcon,
  PhoneIcon,
  EnvelopeIcon,
  CheckIcon,
  PencilSquareIcon,
  ArrowPathIcon,
  CameraIcon,
  TrashIcon,
  BellAlertIcon,
  KeyIcon,
  ClipboardDocumentCheckIcon,
  QuestionMarkCircleIcon,
  CheckCircleIcon,
  ExclamationCircleIcon,
  XMarkIcon,
  ChevronRightIcon,
} from '@heroicons/vue/24/outline'

const { profile, initials, fetchProfile, updateProfile, uploadAvatar, removeAvatar } = useUserState()

// ─── Local State ─────────────────────────────────────────────────────────────
const isEditing = ref(false)
const isSubmitting = ref(false)
const isUploadingAvatar = ref(false)
const avatarPreview = ref(null)
const selectedAvatarFile = ref(null)
const nameError = ref('')

const form = reactive({
  fullName: '',
  email: '',
})

const settings = reactive({
  notifNewStudent: true,
  notifTuition: true,
  notifSecurity: true,
})

const feedback = reactive({
  message: '',
  type: 'success', // 'success' | 'error'
})

function showFeedback(message, type = 'success') {
  feedback.message = message
  feedback.type = type
  setTimeout(() => {
    if (feedback.message === message) {
      clearFeedback()
    }
  }, 4000)
}

function clearFeedback() {
  feedback.message = ''
}

// ─── Sync Form with Profile ──────────────────────────────────────────────────
function syncForm() {
  form.fullName = profile.value.fullName || ''
  form.email = profile.value.email || ''
}

watch(profile, () => {
  if (!isEditing.value) {
    syncForm()
  }
}, { immediate: true })

onMounted(async () => {
  await fetchProfile()
  syncForm()
})

// ─── Edit Mode Actions ───────────────────────────────────────────────────────
function enableEdit() {
  syncForm()
  nameError.value = ''
  isEditing.value = true
}

function cancelEdit() {
  syncForm()
  nameError.value = ''
  isEditing.value = false
}

async function handleSaveProfile() {
  if (!form.fullName.trim()) {
    nameError.value = 'Họ và tên không được để trống.'
    return
  }

  isSubmitting.value = true
  nameError.value = ''

  try {
    await updateProfile({
      fullName: form.fullName.trim(),
      email: form.email.trim(),
    })
    isEditing.value = false
    showFeedback('Cập nhật thông tin cá nhân thành công!', 'success')
  } catch (err) {
    const errorMsg = err.response?.data?.message || err.message || 'Không thể cập nhật thông tin lúc này.'
    showFeedback(errorMsg, 'error')
  } finally {
    isSubmitting.value = false
  }
}

// ─── Avatar Upload & Actions ─────────────────────────────────────────────────
function handleAvatarChange(event) {
  const file = event.target.files?.[0]
  if (!file) return

  // Validate format and size (max 5MB)
  if (!file.type.startsWith('image/')) {
    showFeedback('Vui lòng chọn tệp hình ảnh hợp lệ (PNG, JPG, WebP).', 'error')
    return
  }

  if (file.size > 5 * 1024 * 1024) {
    showFeedback('Kích thước ảnh không được vượt quá 5MB.', 'error')
    return
  }

  selectedAvatarFile.value = file
  avatarPreview.value = URL.createObjectURL(file)
}

function cancelAvatarPreview() {
  if (avatarPreview.value) {
    URL.revokeObjectURL(avatarPreview.value)
  }
  avatarPreview.value = null
  selectedAvatarFile.value = null
}

async function saveAvatar() {
  if (!selectedAvatarFile.value) return

  isUploadingAvatar.value = true
  try {
    await uploadAvatar(selectedAvatarFile.value)
    showFeedback('Cập nhật ảnh đại diện thành công!', 'success')
    cancelAvatarPreview()
  } catch (err) {
    const msg = err.response?.data?.message || 'Tải ảnh đại diện thất bại. Vui lòng thử lại.'
    showFeedback(msg, 'error')
  } finally {
    isUploadingAvatar.value = false
  }
}

async function confirmDeleteAvatar() {
  if (!confirm('Bạn có chắc chắn muốn xóa ảnh đại diện hiện tại?')) return

  try {
    await removeAvatar()
    showFeedback('Đã gỡ ảnh đại diện thành công.', 'success')
  } catch (err) {
    showFeedback('Xóa ảnh đại diện thất bại.', 'error')
  }
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
