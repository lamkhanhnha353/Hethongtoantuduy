<template>
  <div class="space-y-5">
    <!-- ─── Header ───────────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-stone-900">Thống kê chuyên cần lớp phụ trách</h1>
        <p class="text-xs text-stone-500 mt-0.5">
          Theo dõi tỷ lệ đi học, phát hiện sớm học sinh nghỉ học nhiều để kịp thời liên hệ phụ huynh
        </p>
      </div>
      <div class="flex items-center gap-2">
        <button
          type="button"
          class="inline-flex items-center gap-1.5 h-9 px-3.5 rounded-md bg-white hover:bg-stone-100 text-stone-800 border border-stone-300 hover:border-stone-400 text-xs font-semibold transition-colors cursor-pointer"
          @click="exportReport"
        >
          <ArrowDownTrayIcon class="w-4 h-4 text-orange-600" />
          Xuất báo cáo chuyên cần
        </button>
      </div>
    </div>

    <!-- ─── Filter Toolbar ───────────────────────────────────────────────── -->
    <div class="bg-white rounded-lg border border-stone-300 p-4 shadow-[0_1px_2px_rgba(0,0,0,0.04)]">
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <div>
          <label class="block text-xs font-semibold uppercase tracking-wide text-stone-700 mb-1.5">Chọn lớp học</label>
          <select
            v-model="selectedClassId"
            class="w-full h-10 text-xs font-semibold bg-stone-50/70 border border-stone-300 text-stone-900 rounded-md px-3 focus:bg-white focus:border-orange-600 focus:ring-2 focus:ring-orange-500/20 outline-none"
          >
            <option value="1">Toán Tư Duy - Khối 3A (18 học sinh)</option>
            <option value="2">Hình Học Trực Quan 4B (16 học sinh)</option>
            <option value="3">Toán Nâng Cao 5A (15 học sinh)</option>
          </select>
        </div>

        <div>
          <label class="block text-xs font-semibold uppercase tracking-wide text-stone-700 mb-1.5">Thời gian thống kê</label>
          <select
            v-model="timeRange"
            class="w-full h-10 text-xs font-semibold bg-stone-50/70 border border-stone-300 text-stone-900 rounded-md px-3 focus:bg-white focus:border-orange-600 focus:ring-2 focus:ring-orange-500/20 outline-none"
          >
            <option value="month">Tháng này (Tháng 10/2026)</option>
            <option value="last_month">Tháng trước (Tháng 09/2026)</option>
            <option value="term">Học kỳ 1</option>
          </select>
        </div>
      </div>
    </div>

    <!-- ─── Overview Stats Cards ─────────────────────────────────────────── -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 tabular-nums">
      <div class="bg-white rounded-lg p-4 border border-stone-300 shadow-[0_1px_2px_rgba(0,0,0,0.04)]">
        <p class="text-xs text-stone-500 font-semibold uppercase tracking-wide">Tỷ lệ chuyên cần chung</p>
        <p class="text-2xl font-bold text-orange-600 mt-1">94.8%</p>
        <p class="text-[11px] text-emerald-700 font-medium mt-0.5">↑ Tăng 1.2% so với tháng trước</p>
      </div>
      <div class="bg-white rounded-lg p-4 border border-stone-300 shadow-[0_1px_2px_rgba(0,0,0,0.04)]">
        <p class="text-xs text-stone-500 font-semibold uppercase tracking-wide">Tổng số buổi đã dạy</p>
        <p class="text-2xl font-bold text-stone-900 mt-1">12 Buổi</p>
        <p class="text-[11px] text-stone-500 mt-0.5">Kế hoạch: 24 buổi</p>
      </div>
      <div class="bg-white rounded-lg p-4 border border-stone-300 shadow-[0_1px_2px_rgba(0,0,0,0.04)]">
        <p class="text-xs text-stone-500 font-semibold uppercase tracking-wide">Lượt vắng có phép</p>
        <p class="text-2xl font-bold text-stone-800 mt-1">6 Lượt</p>
        <p class="text-[11px] text-stone-500 mt-0.5">Phụ huynh đã báo trước</p>
      </div>
      <div class="bg-white rounded-lg p-4 border border-stone-300 shadow-[0_1px_2px_rgba(0,0,0,0.04)]">
        <p class="text-xs text-stone-500 font-semibold uppercase tracking-wide">Lượt vắng không phép</p>
        <p class="text-2xl font-bold text-rose-700 mt-1">2 Lượt</p>
        <p class="text-[11px] text-rose-700 font-medium mt-0.5">Cần liên hệ xác minh</p>
      </div>
    </div>

    <!-- ─── Attendance Graph & Warnings ──────────────────────────────────── -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <!-- Attendance Chart -->
      <div class="lg:col-span-2 bg-white rounded-lg border border-stone-300 p-5 shadow-[0_1px_2px_rgba(0,0,0,0.04)] space-y-4">
        <div class="flex items-center justify-between pb-3 border-b border-stone-200">
          <h3 class="text-sm font-bold text-stone-900">Biểu đồ tỷ lệ có mặt theo từng buổi học</h3>
          <span class="text-xs text-orange-800 font-semibold bg-orange-50 border border-orange-300 px-2 py-0.5 rounded">
            Lớp Toán 3A
          </span>
        </div>

        <!-- Pure CSS Bar Chart -->
        <div class="h-44 flex items-end gap-2 pt-4 tabular-nums">
          <div
            v-for="(b, i) in attendanceBars"
            :key="i"
            class="flex-1 flex flex-col items-center gap-1 group"
          >
            <span class="text-[10px] font-bold text-stone-700 opacity-0 group-hover:opacity-100 transition-opacity">
              {{ b.pct }}%
            </span>
            <div
              class="w-full rounded-t bg-orange-500 group-hover:bg-orange-600 transition-colors duration-200"
              :style="{ height: `${b.pct}%` }"
            ></div>
            <span class="text-[11px] text-stone-500 font-medium">{{ b.lesson }}</span>
          </div>
        </div>
      </div>

      <!-- Warning list: Students with multiple absences -->
      <div class="bg-white rounded-lg border border-stone-300 p-5 shadow-[0_1px_2px_rgba(0,0,0,0.04)] space-y-3">
        <h3 class="text-sm font-bold text-stone-900 flex items-center gap-1.5">
          <ExclamationTriangleIcon class="w-4 h-4 text-amber-600" />
          Học sinh vắng nhiều (&gt;2 buổi)
        </h3>
        <p class="text-xs text-stone-500">Giáo viên nên liên hệ với phụ huynh để hỗ trợ kèm bài bù.</p>

        <div class="space-y-2 pt-2">
          <div
            v-for="st in absentStudents"
            :key="st.id"
            class="p-2.5 rounded-md border border-rose-200 bg-rose-50/50 flex items-center justify-between text-xs tabular-nums"
          >
            <div>
              <p class="font-bold text-stone-900">{{ st.name }}</p>
              <p class="text-[11px] text-stone-600">Vắng: <strong class="text-rose-700">{{ st.absentCount }} buổi</strong> ({{ st.excused }} có phép)</p>
            </div>
            <router-link
              to="/teacher/communication/notices"
              class="inline-flex items-center h-7 px-2.5 rounded-md bg-white border border-rose-300 text-rose-700 hover:bg-rose-50 text-[11px] font-semibold transition-colors"
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
