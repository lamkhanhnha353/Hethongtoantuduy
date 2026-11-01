<template>
  <div class="space-y-6 max-w-5xl mx-auto pb-12">
    <!-- ─── Breadcrumb & Header ────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
      <div>
        <nav class="flex items-center gap-2 text-xs font-medium text-orange-600 mb-1">
          <span>Tài khoản giáo viên</span>
          <span>›</span>
          <span class="text-orange-950 font-semibold">Hồ sơ cá nhân & Chuyên môn</span>
        </nav>
        <h1 class="text-2xl font-bold text-gray-900 tracking-tight">Hồ sơ & Chuyên môn giáo viên</h1>
        <p class="text-xs text-gray-500 mt-0.5">Quản lý thông tin cá nhân, hồ sơ chuyên môn và môn học giảng dạy</p>
      </div>

      <!-- Tab Switcher -->
      <div class="flex items-center bg-white p-1 rounded-2xl border border-orange-200/80 shadow-xs self-start sm:self-auto text-xs">
        <router-link
          to="/teacher/account/profile"
          class="flex items-center gap-2 px-3.5 py-2 rounded-xl font-semibold bg-orange-400 text-white shadow-xs"
        >
          <UserIcon class="w-3.5 h-3.5" />
          <span>Hồ sơ & Chuyên môn</span>
        </router-link>
        <router-link
          to="/teacher/account/password"
          class="flex items-center gap-2 px-3.5 py-2 rounded-xl font-medium text-gray-600 hover:text-orange-700 hover:bg-orange-50 transition-all"
        >
          <ShieldCheckIcon class="w-3.5 h-3.5 text-orange-500" />
          <span>Đổi mật khẩu / PIN</span>
        </router-link>
      </div>
    </div>

    <!-- ─── Profile Overview Card ──────────────────────────────────────── -->
    <div class="bg-white rounded-3xl border border-orange-100 p-6 shadow-sm flex flex-col md:flex-row items-center gap-6">
      <div class="relative">
        <div class="w-24 h-24 rounded-full bg-gradient-to-br from-orange-400 to-amber-500 text-white font-extrabold text-3xl flex items-center justify-center shadow-md ring-4 ring-orange-100">
          {{ teacherInitials }}
        </div>
        <button
          type="button"
          class="absolute bottom-0 right-0 p-2 rounded-full bg-orange-500 text-white hover:bg-orange-600 shadow-sm transition-all"
          title="Đổi ảnh đại diện"
        >
          <CameraIcon class="w-4 h-4" />
        </button>
      </div>

      <div class="flex-1 text-center md:text-left">
        <div class="flex flex-wrap items-center justify-center md:justify-start gap-2">
          <h2 class="text-xl font-bold text-gray-900">{{ form.fullName }}</h2>
          <span class="text-xs font-bold text-orange-800 bg-orange-100 px-2.5 py-0.5 rounded-full">
            Giáo viên chính thức
          </span>
        </div>
        <p class="text-xs text-gray-500 mt-1">Mã GV: <span class="font-mono text-orange-700 font-bold">GV-TOAN08</span> · Khoa Toán Tư Duy Tiểu Học</p>
        <div class="flex flex-wrap items-center justify-center md:justify-start gap-4 text-xs text-gray-600 mt-3">
          <span>📧 {{ form.email }}</span>
          <span>📞 {{ form.phone }}</span>
          <span>📍 Chi nhánh Hà Đông, Hà Nội</span>
        </div>
      </div>
    </div>

    <!-- ─── Form Details ────────────────────────────────────────────────── -->
    <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
      <!-- Left: Thông tin cá nhân -->
      <div class="bg-white rounded-3xl border border-orange-100 p-6 shadow-sm space-y-4">
        <h3 class="text-sm font-bold text-orange-950 pb-2 border-b border-orange-100 flex items-center gap-2">
          <IdentificationIcon class="w-4 h-4 text-orange-500" />
          Thông tin cơ bản
        </h3>

        <div class="space-y-3 text-xs">
          <div>
            <label class="block font-semibold text-gray-700 mb-1">Họ và tên</label>
            <input type="text" v-model="form.fullName" class="w-full rounded-xl border border-orange-200 p-2.5" />
          </div>

          <div>
            <label class="block font-semibold text-gray-700 mb-1">Địa chỉ Email</label>
            <input type="email" v-model="form.email" class="w-full rounded-xl border border-orange-200 p-2.5" />
          </div>

          <div>
            <label class="block font-semibold text-gray-700 mb-1">Số điện thoại</label>
            <input type="tel" v-model="form.phone" class="w-full rounded-xl border border-orange-200 p-2.5" />
          </div>

          <div>
            <label class="block font-semibold text-gray-700 mb-1">Giới thiệu ngắn / Triết lý giảng dạy</label>
            <textarea
              v-model="form.bio"
              rows="3"
              class="w-full rounded-xl border border-orange-200 p-2.5"
              placeholder="Chia sẻ phương pháp giảng dạy..."
            ></textarea>
          </div>
        </div>
      </div>

      <!-- Right: Chuyên môn & Bằng cấp -->
      <div class="bg-white rounded-3xl border border-orange-100 p-6 shadow-sm space-y-4">
        <h3 class="text-sm font-bold text-orange-950 pb-2 border-b border-orange-100 flex items-center gap-2">
          <AcademicCapIcon class="w-4 h-4 text-orange-500" />
          Hồ sơ Chuyên môn & Bằng cấp
        </h3>

        <div class="space-y-3 text-xs">
          <div>
            <label class="block font-semibold text-gray-700 mb-1">Trình độ học vấn cao nhất</label>
            <input type="text" v-model="form.degree" class="w-full rounded-xl border border-orange-200 p-2.5" />
          </div>

          <div>
            <label class="block font-semibold text-gray-700 mb-1">Chứng chỉ sư phạm / Chuyên ngành</label>
            <input type="text" v-model="form.certificate" class="w-full rounded-xl border border-orange-200 p-2.5" />
          </div>

          <div>
            <label class="block font-semibold text-gray-700 mb-1">Môn học phụ trách chính</label>
            <div class="flex flex-wrap gap-2 pt-1">
              <span
                v-for="sub in form.specialties"
                :key="sub"
                class="px-2.5 py-1 rounded-lg bg-orange-50 border border-orange-200 text-orange-900 font-semibold text-[11px]"
              >
                {{ sub }}
              </span>
            </div>
          </div>

          <div>
            <label class="block font-semibold text-gray-700 mb-1">Số năm kinh nghiệm</label>
            <input type="text" v-model="form.experience" class="w-full rounded-xl border border-orange-200 p-2.5" />
          </div>
        </div>
      </div>
    </div>

    <!-- ─── Action Button ────────────────────────────────────────────────── -->
    <div class="flex items-center justify-end">
      <button
        type="button"
        class="px-5 py-2.5 rounded-xl bg-orange-400 hover:bg-orange-500 text-white text-xs font-bold shadow-xs transition-all cursor-pointer"
        @click="updateProfile"
      >
        Lưu thay đổi hồ sơ
      </button>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import {
  UserIcon,
  ShieldCheckIcon,
  IdentificationIcon,
  AcademicCapIcon,
  CameraIcon,
} from '@heroicons/vue/24/outline'

const form = ref({
  fullName: 'Cô Lê Hoàng Mai',
  email: 'hoangmai.toantuduy@gmail.com',
  phone: '0988776655',
  bio: 'Tốt nghiệp ĐH Sư Phạm Hà Nội khoa Sư phạm Toán. 5 năm kinh nghiệm giảng dạy Toán tư duy và kích thích trí thông minh logic cho trẻ từ 6 - 11 tuổi.',
  degree: 'Cử nhân Sư phạm Toán - ĐH Sư Phạm Hà Nội',
  certificate: 'Chứng chỉ Phương pháp Giáo dục Toán Tư Duy Singapore',
  experience: '5 năm giảng dạy',
  specialties: ['Toán Tư Duy Tiểu Học', 'Hình Học Không Gian Sơ Cấp', 'Toán Logic'],
})

const teacherInitials = computed(() => {
  const parts = form.value.fullName.trim().split(' ')
  return parts.length >= 2
    ? (parts[parts.length - 2][0] + parts[parts.length - 1][0]).toUpperCase()
    : 'GV'
})

function updateProfile() {
  alert('Đã cập nhật hồ sơ cá nhân và chuyên môn thành công!')
}
</script>
