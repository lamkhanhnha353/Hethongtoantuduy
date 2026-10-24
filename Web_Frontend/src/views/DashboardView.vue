<template>
  <div class="space-y-6">
    <!-- ─── Stats cards ──────────────────────────────────────────────────── -->
    <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4">
      <div
        v-for="stat in stats"
        :key="stat.label"
        class="bg-white rounded-2xl p-5 shadow-sm border border-orange-100 flex items-center gap-4 hover:shadow-md transition-shadow"
      >
        <div :class="['w-12 h-12 rounded-xl flex items-center justify-center flex-shrink-0', stat.bg]">
          <component :is="stat.icon" class="w-6 h-6 text-white" />
        </div>
        <div>
          <p class="text-xs text-gray-500 font-medium">{{ stat.label }}</p>
          <p class="text-2xl font-bold text-gray-800 mt-0.5">{{ stat.value }}</p>
          <p :class="['text-xs mt-0.5 font-medium', stat.trendUp ? 'text-green-500' : 'text-red-400']">
            {{ stat.trendUp ? '▲' : '▼' }} {{ stat.trend }} so với tháng trước
          </p>
        </div>
      </div>
    </div>

    <!-- ─── Charts row ───────────────────────────────────────────────────── -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-4">
      <!-- Attendance chart placeholder -->
      <div class="bg-white rounded-2xl p-5 shadow-sm border border-orange-100">
        <div class="flex items-center justify-between mb-4">
          <h2 class="font-semibold text-orange-900">Biểu đồ Chuyên cần</h2>
          <span class="text-xs bg-orange-100 text-orange-600 px-2 py-1 rounded-full font-medium">Tháng 10</span>
        </div>
        <div class="h-44 flex items-end gap-2">
          <div
            v-for="(bar, i) in attendanceBars"
            :key="i"
            class="flex-1 flex flex-col items-center gap-1"
          >
            <div
              class="w-full rounded-t-md bg-gradient-to-t from-orange-400 to-orange-300 transition-all duration-500"
              :style="{ height: bar.pct + '%' }"
            ></div>
            <span class="text-[10px] text-gray-400">{{ bar.day }}</span>
          </div>
        </div>
        <p class="text-xs text-gray-400 mt-3">* Tỷ lệ chuyên cần trung bình: <span class="font-semibold text-orange-600">92.4%</span></p>
      </div>

      <!-- Renewal rate chart placeholder -->
      <div class="bg-white rounded-2xl p-5 shadow-sm border border-orange-100">
        <div class="flex items-center justify-between mb-4">
          <h2 class="font-semibold text-orange-900">Tỷ lệ Gia hạn Khóa học</h2>
          <span class="text-xs bg-green-100 text-green-600 px-2 py-1 rounded-full font-medium">↑ 4.2%</span>
        </div>
        <!-- Donut chart (pure CSS) -->
        <div class="flex items-center gap-6">
          <div class="relative w-36 h-36 flex-shrink-0">
            <svg viewBox="0 0 36 36" class="w-full h-full -rotate-90">
              <circle cx="18" cy="18" r="15.9" fill="none" stroke="#fed7aa" stroke-width="3.5" />
              <circle
                cx="18" cy="18" r="15.9" fill="none"
                stroke="#fb923c" stroke-width="3.5"
                stroke-dasharray="76 24"
                stroke-linecap="round"
              />
            </svg>
            <div class="absolute inset-0 flex flex-col items-center justify-center">
              <span class="text-2xl font-bold text-orange-600">76%</span>
              <span class="text-[10px] text-gray-400">gia hạn</span>
            </div>
          </div>
          <div class="space-y-3 text-sm">
            <div class="flex items-center gap-2">
              <span class="w-3 h-3 rounded-full bg-orange-400 flex-shrink-0"></span>
              <span class="text-gray-600">Đã gia hạn: <strong class="text-gray-800">76%</strong></span>
            </div>
            <div class="flex items-center gap-2">
              <span class="w-3 h-3 rounded-full bg-orange-200 flex-shrink-0"></span>
              <span class="text-gray-600">Chưa gia hạn: <strong class="text-gray-800">24%</strong></span>
            </div>
            <p class="text-xs text-gray-400 pt-1 leading-relaxed">
              Tổng số học viên hết hạn:<br/>
              <strong class="text-gray-700">248 học viên</strong>
            </p>
          </div>
        </div>
      </div>
    </div>

    <!-- ─── Recent activity ──────────────────────────────────────────────── -->
    <div class="bg-white rounded-2xl shadow-sm border border-orange-100 overflow-hidden">
      <div class="flex items-center justify-between px-5 py-4 border-b border-orange-100">
        <h2 class="font-semibold text-orange-900">Hoạt động gần đây</h2>
        <button class="text-xs text-orange-500 hover:text-orange-700 font-medium transition-colors">Xem tất cả</button>
      </div>
      <div class="divide-y divide-orange-50">
        <div
          v-for="activity in recentActivities"
          :key="activity.id"
          class="flex items-center gap-4 px-5 py-3 hover:bg-orange-50/50 transition-colors"
        >
          <div :class="['w-9 h-9 rounded-full flex items-center justify-center flex-shrink-0', activity.color]">
            <component :is="activity.icon" class="w-4 h-4 text-white" />
          </div>
          <div class="flex-1 min-w-0">
            <p class="text-sm text-gray-700 leading-snug">{{ activity.message }}</p>
            <p class="text-xs text-gray-400 mt-0.5">{{ activity.time }}</p>
          </div>
          <span
            :class="[
              'text-xs px-2 py-0.5 rounded-full font-medium flex-shrink-0',
              activity.badgeClass,
            ]"
          >{{ activity.badge }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import {
  CurrencyDollarIcon,
  UserPlusIcon,
  AcademicCapIcon,
  BookOpenIcon,
  CheckCircleIcon,
  ExclamationTriangleIcon,
  ArrowRightOnRectangleIcon,
} from '@heroicons/vue/24/outline'

// ─── Stats ───────────────────────────────────────────────────────────────────
const stats = [
  {
    label: 'Doanh thu tháng 10',
    value: '128.4M đ',
    trend: '+12.3%',
    trendUp: true,
    icon: CurrencyDollarIcon,
    bg: 'bg-gradient-to-br from-orange-400 to-orange-500',
  },
  {
    label: 'Học viên mới',
    value: '47',
    trend: '+8 học viên',
    trendUp: true,
    icon: UserPlusIcon,
    bg: 'bg-gradient-to-br from-blue-400 to-blue-500',
  },
  {
    label: 'Lớp học đang mở',
    value: '32',
    trend: '-2 lớp',
    trendUp: false,
    icon: BookOpenIcon,
    bg: 'bg-gradient-to-br from-green-400 to-green-500',
  },
]

// ─── Attendance bar chart ─────────────────────────────────────────────────────
const attendanceBars = [
  { day: 'T2', pct: 88 },
  { day: 'T3', pct: 95 },
  { day: 'T4', pct: 91 },
  { day: 'T5', pct: 97 },
  { day: 'T6', pct: 85 },
  { day: 'T7', pct: 93 },
  { day: 'CN', pct: 78 },
]

// ─── Recent activities ────────────────────────────────────────────────────────
const recentActivities = [
  {
    id: 1,
    message: 'Học viên Trần Minh Khoa đăng ký khóa Toán 10 – Nâng cao',
    time: '2 phút trước',
    icon: UserPlusIcon,
    color: 'bg-green-400',
    badge: 'Mới',
    badgeClass: 'bg-green-100 text-green-600',
  },
  {
    id: 2,
    message: 'Giáo viên Lê Thị Hoa cập nhật điểm lớp Toán 8B',
    time: '18 phút trước',
    icon: AcademicCapIcon,
    color: 'bg-blue-400',
    badge: 'Cập nhật',
    badgeClass: 'bg-blue-100 text-blue-600',
  },
  {
    id: 3,
    message: 'Thanh toán học phí tháng 10 – Lớp Lý 11A hoàn tất',
    time: '1 giờ trước',
    icon: CheckCircleIcon,
    color: 'bg-orange-400',
    badge: 'Thành công',
    badgeClass: 'bg-orange-100 text-orange-600',
  },
  {
    id: 4,
    message: 'Phụ huynh Nguyễn Văn A yêu cầu xem lại lịch học lớp Hóa 12',
    time: '3 giờ trước',
    icon: ExclamationTriangleIcon,
    color: 'bg-yellow-400',
    badge: 'Chờ xử lý',
    badgeClass: 'bg-yellow-100 text-yellow-600',
  },
]
</script>
