<template>
  <div class="space-y-5">
    <header class="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <p class="text-xs font-semibold uppercase tracking-[0.18em] text-orange-500">Báo cáo &amp; thống kê</p>
        <h1 class="mt-1 text-2xl font-bold text-orange-950">Thống kê chuyên cần</h1>
        <p class="mt-1 text-sm text-gray-500">Tổng hợp tình hình đi học theo lớp và từng buổi học.</p>
      </div>
      <div>
        <select v-model="period" aria-label="Khoảng thời gian báo cáo" class="filter-select min-w-40">
          <option value="30">30 ngày gần nhất</option>
          <option value="90">90 ngày gần nhất</option>
          <option value="365">12 tháng gần nhất</option>
        </select>
      </div>
    </header>

    <div v-if="errorMessage" class="flex flex-col gap-3 rounded-xl border border-red-200 bg-red-50 p-4 sm:flex-row sm:items-center sm:justify-between">
      <p class="text-sm text-red-700">{{ errorMessage }}</p>
      <button type="button" class="shrink-0 text-sm font-semibold text-red-700 underline" @click="loadReport">Thử lại</button>
    </div>

    <section class="rounded-2xl border border-orange-100 bg-white p-4 shadow-sm">
      <label for="attendance-class" class="mb-1.5 block text-xs font-semibold text-gray-600">Lọc theo lớp</label>
      <select id="attendance-class" v-model="selectedClassId" class="filter-select max-w-xl">
        <option value="">Tất cả lớp học</option>
        <option v-for="item in classOptions" :key="item.id" :value="String(item.id)">{{ item.name }}</option>
      </select>
    </section>

    <section class="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4" aria-label="Tổng quan chuyên cần">
      <article v-for="stat in stats" :key="stat.label" class="flex items-center gap-3 rounded-2xl border border-orange-100 bg-white p-4 shadow-sm">
        <div class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl" :class="stat.bg">
          <component :is="stat.icon" class="h-5 w-5" :class="stat.color" />
        </div>
        <div class="min-w-0">
          <p class="text-xs font-medium text-gray-500">{{ stat.label }}</p>
          <p class="mt-1 truncate text-lg font-bold text-gray-900">{{ stat.value }}</p>
          <p class="mt-0.5 text-xs text-gray-400">{{ stat.caption }}</p>
        </div>
      </article>
    </section>

    <section class="grid grid-cols-1 gap-5 xl:grid-cols-3">
      <article class="rounded-2xl border border-orange-100 bg-white p-5 shadow-sm xl:col-span-2">
        <div class="flex flex-wrap items-start justify-between gap-2 border-b border-orange-100 pb-4">
          <div>
            <h2 class="font-bold text-orange-950">Tỷ lệ có mặt theo buổi</h2>
            <p class="mt-1 text-xs text-gray-500">Tỷ lệ học viên có mặt trên tổng sĩ số được điểm danh</p>
          </div>
          <span class="rounded-full bg-orange-50 px-3 py-1 text-xs font-semibold text-orange-700">{{ report?.lessonCount ?? 0 }} buổi học</span>
        </div>
        <div v-if="loading" class="flex h-56 items-center justify-center text-sm text-gray-500">Đang tổng hợp dữ liệu điểm danh...</div>
        <div v-else-if="!chartLessons.length" class="flex h-56 items-center justify-center text-sm text-gray-500">Chưa có dữ liệu điểm danh trong kỳ.</div>
        <div v-else class="flex h-56 items-end gap-2 overflow-x-auto pt-6 sm:gap-3">
          <div v-for="item in chartLessons" :key="item.lessonId" class="group flex h-full min-w-8 flex-1 flex-col items-center justify-end gap-2">
            <span class="text-[10px] font-semibold text-orange-800 opacity-0 transition group-hover:opacity-100">{{ item.rate }}%</span>
            <div class="w-full max-w-10 rounded-t-lg bg-gradient-to-t from-orange-500 to-amber-300 transition hover:from-orange-600" :style="{ height: `${Math.max(item.rate, 3)}%` }" :title="`${item.className}: ${item.rate}% có mặt`"></div>
            <span class="text-[9px] text-gray-500">{{ formatShortDate(item.lessonDate) }}</span>
          </div>
        </div>
      </article>

      <article class="rounded-2xl border border-orange-100 bg-white p-5 shadow-sm">
        <div class="border-b border-orange-100 pb-4">
          <h2 class="font-bold text-orange-950">Tình trạng điểm danh</h2>
          <p class="mt-1 text-xs text-gray-500">Tổng lượt ghi nhận trong kỳ</p>
        </div>
        <div v-if="!attendanceBreakdown.total" class="flex h-56 items-center justify-center text-center text-sm text-gray-500">Chưa có lượt điểm danh.</div>
        <div v-else class="space-y-5 pt-5">
          <div v-for="item in attendanceBreakdown.items" :key="item.label">
            <div class="mb-2 flex items-center justify-between text-xs">
              <span class="flex items-center gap-2 text-gray-600"><span class="h-2.5 w-2.5 rounded-full" :class="item.dot"></span>{{ item.label }}</span>
              <span class="font-semibold text-gray-900">{{ item.count }} <span class="font-normal text-gray-400">({{ item.percent }}%)</span></span>
            </div>
            <div class="h-2 overflow-hidden rounded-full bg-gray-100">
              <div class="h-full rounded-full" :class="item.bar" :style="{ width: `${item.percent}%` }"></div>
            </div>
          </div>
        </div>
      </article>
    </section>

    <section class="overflow-hidden rounded-2xl border border-orange-100 bg-white shadow-sm">
      <div class="border-b border-orange-100 p-4">
        <h2 class="font-bold text-orange-950">Chi tiết buổi học</h2>
        <p class="mt-1 text-xs text-gray-500">{{ filteredLessons.length }} buổi học trong kỳ báo cáo</p>
      </div>
      <div v-if="loading" class="flex min-h-40 items-center justify-center text-sm text-gray-500">Đang tải danh sách buổi học...</div>
      <div v-else-if="!filteredLessons.length" class="flex min-h-40 items-center justify-center text-sm text-gray-500">Không có buổi học phù hợp trong kỳ.</div>
      <div v-else class="overflow-x-auto">
        <table class="w-full min-w-[720px] text-left text-sm">
          <thead class="bg-orange-50/80 text-xs uppercase tracking-wide text-orange-900">
            <tr>
              <th class="px-5 py-3.5 font-semibold">Ngày học</th>
              <th class="px-5 py-3.5 font-semibold">Lớp</th>
              <th class="px-5 py-3.5 text-center font-semibold">Có mặt</th>
              <th class="px-5 py-3.5 text-center font-semibold">Vắng</th>
              <th class="px-5 py-3.5 text-center font-semibold">Đi muộn</th>
              <th class="px-5 py-3.5 text-right font-semibold">Tỷ lệ có mặt</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr v-for="item in filteredLessons.slice(0, 15)" :key="item.lessonId" class="hover:bg-orange-50/40">
              <td class="whitespace-nowrap px-5 py-3.5 text-gray-600">{{ formatDate(item.lessonDate) }}</td>
              <td class="px-5 py-3.5 font-medium text-gray-800">{{ item.className }}</td>
              <td class="px-5 py-3.5 text-center text-emerald-700">{{ item.presentCount }}</td>
              <td class="px-5 py-3.5 text-center text-red-600">{{ item.absentCount + item.excusedCount }}</td>
              <td class="px-5 py-3.5 text-center text-amber-600">{{ item.lateCount }}</td>
              <td class="px-5 py-3.5 text-right font-semibold text-gray-900">{{ item.rate }}%</td>
            </tr>
          </tbody>
        </table>
      </div>
      <p v-if="filteredLessons.length > 15" class="border-t border-orange-50 px-5 py-3 text-xs text-gray-500">Đang hiển thị 15 buổi mới nhất.</p>
    </section>
  </div>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import {
  CalendarDaysIcon,
  CheckCircleIcon,
  UserGroupIcon,
  UserMinusIcon,
} from '@heroicons/vue/24/outline'
import reportService from '@/services/admin-report.service'

const period = ref('90')
const selectedClassId = ref('')
const report = ref(null)
const loading = ref(false)
const errorMessage = ref('')
let requestId = 0

const classOptions = computed(() => report.value?.classes ?? [])
const filteredLessons = computed(() => report.value?.lessons ?? [])
const chartLessons = computed(() => [...filteredLessons.value].slice(0, 12).reverse())
const attendanceBreakdown = computed(() => {
  const result = report.value ?? { presentCount: 0, absentCount: 0, excusedCount: 0, lateCount: 0, unmarkedCount: 0 }
  const total = result.presentCount + result.absentCount + result.excusedCount + result.lateCount + result.unmarkedCount
  const items = [
    { label: 'Có mặt', count: result.presentCount, dot: 'bg-emerald-500', bar: 'bg-emerald-500' },
    { label: 'Vắng không phép', count: result.absentCount, dot: 'bg-rose-400', bar: 'bg-rose-400' },
    { label: 'Vắng có phép', count: result.excusedCount, dot: 'bg-blue-400', bar: 'bg-blue-400' },
    { label: 'Đi muộn', count: result.lateCount, dot: 'bg-amber-400', bar: 'bg-amber-400' },
    { label: 'Chưa điểm danh', count: result.unmarkedCount, dot: 'bg-gray-300', bar: 'bg-gray-300' },
  ].map(item => ({ ...item, percent: total ? Math.round((item.count / total) * 100) : 0 }))
  return { total, items }
})
const stats = computed(() => {
  const total = report.value?.totalStudents ?? 0
  const present = report.value?.presentCount ?? 0
  const absent = (report.value?.absentCount ?? 0) + (report.value?.excusedCount ?? 0)
  const late = report.value?.lateCount ?? 0
  return [
    { label: 'Tỷ lệ có mặt', value: `${total ? Math.round((present / total) * 100) : 0}%`, caption: 'Trên tổng sĩ số các buổi học', icon: CheckCircleIcon, bg: 'bg-emerald-50', color: 'text-emerald-600' },
    { label: 'Buổi học đã thống kê', value: Number(report.value?.lessonCount ?? 0).toLocaleString('vi-VN'), caption: 'Trong khoảng thời gian đã chọn', icon: CalendarDaysIcon, bg: 'bg-orange-50', color: 'text-orange-600' },
    { label: 'Lượt vắng', value: absent.toLocaleString('vi-VN'), caption: 'Bao gồm lượt vắng có phép', icon: UserMinusIcon, bg: 'bg-rose-50', color: 'text-rose-600' },
    { label: 'Lượt đi muộn', value: late.toLocaleString('vi-VN'), caption: 'Theo dữ liệu điểm danh', icon: UserGroupIcon, bg: 'bg-amber-50', color: 'text-amber-600' },
  ]
})

function formatDate(value) {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleDateString('vi-VN')
}

function formatShortDate(value) {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' })
}

async function loadReport() {
  const activeRequest = ++requestId
  loading.value = true
  errorMessage.value = ''
  try {
    const result = await reportService.getAttendance(Number(period.value), selectedClassId.value || null)
    if (activeRequest === requestId) report.value = result
  } catch (error) {
    if (activeRequest !== requestId) return
    console.error('Không thể tải dữ liệu báo cáo chuyên cần:', error)
    errorMessage.value = error.response?.data?.message || 'Không thể tải dữ liệu chuyên cần. Vui lòng thử lại.'
    report.value = null
  } finally {
    if (activeRequest === requestId) loading.value = false
  }
}

watch([period, selectedClassId], loadReport, { immediate: true })
</script>
