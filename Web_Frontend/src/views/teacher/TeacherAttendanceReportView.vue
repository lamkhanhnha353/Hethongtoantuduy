<template>
  <div class="space-y-5">
    <!-- ─── Header ───────────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-orange-950">Thống kê chuyên cần lớp phụ trách</h1>
        <p class="text-xs text-gray-500 mt-0.5">
          Theo dõi tỷ lệ đi học, phát hiện sớm học sinh nghỉ học nhiều để kịp thời liên hệ phụ huynh
        </p>
      </div>
      <div class="flex items-center gap-2">
        <button
          type="button"
          class="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-orange-100 hover:bg-orange-200 text-orange-900 text-xs font-semibold transition-colors cursor-pointer"
          @click="exportReport"
        >
          <ArrowDownTrayIcon class="w-4 h-4 text-orange-600" />
          Xuất báo cáo chuyên cần
        </button>
      </div>
    </div>

    <!-- ─── Filter Toolbar ───────────────────────────────────────────────── -->
    <div class="bg-white rounded-2xl border border-orange-100 p-4 shadow-sm">
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <div>
          <label class="block text-xs font-semibold text-gray-600 mb-1">Chọn lớp học</label>
          <select
            v-model="selectedClassId"
            class="w-full text-xs font-semibold bg-orange-50/60 border border-orange-200 text-orange-950 rounded-xl px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
          >
            <option value="1">Toán Tư Duy - Khối 3A (18 học sinh)</option>
            <option value="2">Hình Học Trực Quan 4B (16 học sinh)</option>
            <option value="3">Toán Nâng Cao 5A (15 học sinh)</option>
          </select>
        </div>

        <div>
          <label class="block text-xs font-semibold text-gray-600 mb-1">Thời gian thống kê</label>
          <select
            v-model="timeRange"
            class="w-full text-xs font-semibold bg-white border border-orange-200 text-gray-800 rounded-xl px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
          >
            <option value="month">Tháng này (Tháng 10/2026)</option>
            <option value="last_month">Tháng trước (Tháng 09/2026)</option>
            <option value="term">Học kỳ 1</option>
          </select>
        </div>
      </div>
    </div>

    <!-- ─── Overview Stats Cards ─────────────────────────────────────────── -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <div class="bg-white rounded-2xl p-4 border border-orange-100 shadow-sm">
        <p class="text-xs text-gray-500 font-medium">Tỷ lệ chuyên cần chung</p>
        <p class="text-2xl font-bold text-orange-600 mt-1">94.8%</p>
        <p class="text-[11px] text-emerald-600 font-medium mt-0.5">↑ Tăng 1.2% so với tháng trước</p>
      </div>
      <div class="bg-white rounded-2xl p-4 border border-orange-100 shadow-sm">
        <p class="text-xs text-gray-500 font-medium">Tổng số buổi đã dạy</p>
        <p class="text-2xl font-bold text-gray-800 mt-1">12 Buổi</p>
        <p class="text-[11px] text-gray-400 mt-0.5">Kế hoạch: 24 buổi</p>
      </div>
      <div class="bg-white rounded-2xl p-4 border border-orange-100 shadow-sm">
        <p class="text-xs text-gray-500 font-medium">Lượt vắng có phép</p>
        <p class="text-2xl font-bold text-blue-600 mt-1">6 Lượt</p>
        <p class="text-[11px] text-gray-400 mt-0.5">Phụ huynh đã báo trước</p>
      </div>
      <div class="bg-white rounded-2xl p-4 border border-orange-100 shadow-sm">
        <p class="text-xs text-gray-500 font-medium">Lượt vắng không phép</p>
        <p class="text-2xl font-bold text-red-500 mt-1">2 Lượt</p>
        <p class="text-[11px] text-red-600 font-medium mt-0.5">Cần liên hệ xác minh</p>
      </div>
    </div>

    <!-- ─── Attendance Graph & Warnings ──────────────────────────────────── -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <!-- Attendance Chart -->
      <div class="lg:col-span-2 bg-white rounded-2xl border border-orange-100 p-5 shadow-sm space-y-4">
        <div class="flex items-center justify-between pb-3 border-b border-orange-100">
          <h3 class="text-sm font-bold text-orange-950">Biểu đồ tỷ lệ có mặt theo từng buổi học</h3>
          <span class="text-xs text-orange-600 font-semibold bg-orange-50 px-2 py-0.5 rounded-md">
            Lớp Toán 3A
          </span>
        </div>

        <!-- Pure CSS Bar Chart -->
        <div class="h-44 flex items-end gap-2 pt-4">
          <div
            v-for="(b, i) in attendanceBars"
            :key="i"
            class="flex-1 flex flex-col items-center gap-1 group"
          >
            <span class="text-[9px] font-bold text-orange-800 opacity-0 group-hover:opacity-100 transition-opacity">
              {{ b.pct }}%
            </span>
            <div
              class="w-full rounded-t-md bg-gradient-to-t from-orange-400 to-amber-300 group-hover:from-orange-500 group-hover:to-amber-400 transition-all duration-300"
              :style="{ height: `${b.pct}%` }"
            ></div>
            <span class="text-[10px] text-gray-400 font-medium">{{ b.lesson }}</span>
          </div>
        </div>
      </div>

      <!-- Warning list: Students with multiple absences -->
      <div class="bg-white rounded-2xl border border-orange-100 p-5 shadow-sm space-y-3">
        <h3 class="text-sm font-bold text-orange-950 flex items-center gap-1.5">
          <ExclamationTriangleIcon class="w-4 h-4 text-amber-500" />
          Học sinh vắng nhiều (&gt;2 buổi)
        </h3>
        <p class="text-xs text-gray-500">Giáo viên nên liên hệ với phụ huynh để hỗ trợ kèm bài bù.</p>

        <div class="space-y-2 pt-2">
          <div
            v-for="st in absentStudents"
            :key="st.id"
            class="p-2.5 rounded-xl border border-red-100 bg-red-50/40 flex items-center justify-between text-xs"
          >
            <div>
              <p class="font-bold text-gray-900">{{ st.name }}</p>
              <p class="text-[11px] text-gray-500">Vắng: <strong class="text-red-600">{{ st.absentCount }} buổi</strong> ({{ st.excused }} có phép)</p>
            </div>
            <router-link
              to="/teacher/communication/notices"
              class="px-2 py-1 rounded-lg bg-white border border-red-200 text-red-700 hover:bg-red-50 text-[11px] font-bold"
            >
              Nhắc nhở
            </router-link>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import {
  ArrowDownTrayIcon,
  ExclamationTriangleIcon,
} from '@heroicons/vue/24/outline'

const selectedClassId = ref('1')
const timeRange = ref('month')

const attendanceBars = [
  { lesson: 'B1', pct: 100 },
  { lesson: 'B2', pct: 94 },
  { lesson: 'B3', pct: 89 },
  { lesson: 'B4', pct: 94 },
  { lesson: 'B5', pct: 100 },
  { lesson: 'B6', pct: 88 },
  { lesson: 'B7', pct: 94 },
  { lesson: 'B8', pct: 100 },
]

const absentStudents = ref([
  { id: 1, name: 'Vũ Đức Minh', absentCount: 3, excused: 2 },
  { id: 2, name: 'Lê Hoàng Phúc', absentCount: 2, excused: 1 },
])

function exportReport() {
  alert('Đang trích xuất báo cáo chuyên cần lớp học sang file Excel...')
}
</script>
