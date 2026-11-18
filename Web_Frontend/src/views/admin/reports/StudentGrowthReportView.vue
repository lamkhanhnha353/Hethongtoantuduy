<template>
  <div class="space-y-5">
    <header class="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <p class="text-xs font-semibold uppercase tracking-[0.18em] text-orange-600">Báo cáo &amp; thống kê</p>
        <h1 class="mt-1 text-2xl font-bold text-stone-900">Tăng trưởng học viên</h1>
        <p class="mt-1 text-sm text-stone-500">Theo dõi lượt ghi danh và quy mô học viên theo thời gian.</p>
      </div>
      <div>
        <select v-model="period" aria-label="Khoảng thời gian báo cáo" class="filter-select min-w-40">
          <option value="6">6 tháng gần nhất</option>
          <option value="12">12 tháng gần nhất</option>
        </select>
      </div>
    </header>

    <div v-if="errorMessage" class="flex flex-col gap-3 rounded-md border border-rose-200 bg-rose-50 p-4 sm:flex-row sm:items-center sm:justify-between">
      <p class="text-sm text-rose-700">{{ errorMessage }}</p>
      <button type="button" class="shrink-0 text-sm font-semibold text-rose-700 underline" @click="loadGrowth">Thử lại</button>
    </div>

    <section class="grid grid-cols-1 gap-3 sm:grid-cols-3" aria-label="Tổng quan học viên">
      <article v-for="stat in stats" :key="stat.label" class="flex items-center gap-3 rounded-lg border border-stone-300 bg-white p-4 shadow-[0_1px_2px_rgba(0,0,0,0.04)]">
        <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-md border border-stone-200" :class="stat.bg">
          <component :is="stat.icon" class="h-5 w-5" :class="stat.color" />
        </div>
        <div class="min-w-0">
          <p class="text-xs font-semibold uppercase tracking-wide text-stone-500">{{ stat.label }}</p>
          <p class="mt-1 truncate text-lg font-bold text-stone-900 tabular-nums">{{ stat.value }}</p>
          <p class="mt-0.5 text-xs text-stone-500">{{ stat.caption }}</p>
        </div>
      </article>
    </section>

    <section class="grid grid-cols-1 gap-5 xl:grid-cols-3">
      <article class="rounded-lg border border-stone-300 bg-white p-5 shadow-[0_1px_2px_rgba(0,0,0,0.04)] xl:col-span-2">
        <div class="flex flex-wrap items-start justify-between gap-2 border-b border-stone-200 pb-4">
          <div>
            <h2 class="font-bold text-stone-900">Lượt ghi danh mới</h2>
            <p class="mt-1 text-xs text-stone-500">Dựa trên ngày học viên tham gia lớp</p>
          </div>
          <span class="rounded border border-orange-300 bg-orange-50 px-2.5 py-0.5 text-xs font-semibold text-orange-800 tabular-nums">{{ period }} tháng gần nhất</span>
        </div>
        <div v-if="loading" class="flex h-56 items-center justify-center text-sm text-stone-500">Đang tổng hợp dữ liệu ghi danh...</div>
        <div v-else-if="!monthlyEnrollments.some(item => item.count)" class="flex h-56 items-center justify-center text-sm text-stone-500">Chưa có lượt ghi danh trong khoảng thời gian này.</div>
        <div v-else class="flex h-56 items-end gap-2 pt-6 sm:gap-4">
          <div v-for="item in monthlyEnrollments" :key="item.key" class="group flex h-full min-w-0 flex-1 flex-col items-center justify-end gap-2">
            <span class="text-xs font-semibold text-stone-800 tabular-nums opacity-0 transition group-hover:opacity-100">{{ item.count }}</span>
            <div class="w-full max-w-12 rounded-t bg-orange-500 transition hover:bg-orange-600" :style="{ height: `${barHeight(item.count)}%` }" :title="`${item.label}: ${item.count} lượt ghi danh`"></div>
            <span class="text-[10px] text-stone-500 tabular-nums">{{ item.label }}</span>
          </div>
        </div>
      </article>

      <article class="rounded-lg border border-stone-300 bg-white p-5 shadow-[0_1px_2px_rgba(0,0,0,0.04)]">
        <div class="border-b border-stone-200 pb-4">
          <h2 class="font-bold text-stone-900">Quy mô theo lớp</h2>
          <p class="mt-1 text-xs text-stone-500">Số học viên đang có trong từng lớp</p>
        </div>
        <div v-if="!classSummaries.length" class="flex h-56 items-center justify-center text-center text-sm text-stone-500">Chưa có dữ liệu lớp học.</div>
        <div v-else class="space-y-4 pt-5">
          <div v-for="item in classSummaries.slice(0, 5)" :key="item.id">
            <div class="mb-2 flex items-center justify-between gap-3 text-xs">
              <span class="min-w-0 truncate font-medium text-stone-700">{{ item.name }}</span>
              <span class="shrink-0 font-semibold text-stone-900 tabular-nums">{{ item.count }}</span>
            </div>
            <div class="h-2 overflow-hidden rounded bg-stone-100">
              <div class="h-full rounded bg-orange-500" :style="{ width: `${(item.count / maxClassSize) * 100}%` }"></div>
            </div>
          </div>
        </div>
      </article>
    </section>

    <section class="overflow-hidden rounded-lg border border-stone-300 bg-white shadow-[0_1px_2px_rgba(0,0,0,0.04)]">
      <div class="border-b border-stone-200 bg-stone-50 p-4">
        <h2 class="font-bold text-stone-900">Lớp học có lượt ghi danh gần đây</h2>
        <p class="mt-1 text-xs text-stone-500 tabular-nums">{{ report?.newEnrollmentCount ?? 0 }} lượt ghi danh trong kỳ báo cáo</p>
      </div>
      <div v-if="loading" class="flex min-h-40 items-center justify-center text-sm text-stone-500">Đang tải danh sách...</div>
      <div v-else-if="!recentEnrollments.length" class="flex min-h-40 items-center justify-center text-sm text-stone-500">Không tìm thấy lượt ghi danh trong kỳ.</div>
      <div v-else class="overflow-x-auto">
        <table class="w-full min-w-[640px] text-left text-sm">
          <thead class="border-b-2 border-stone-300 bg-stone-100 text-xs font-bold uppercase tracking-wider text-stone-700">
            <tr>
              <th class="px-5 py-3.5">Học viên</th>
              <th class="px-5 py-3.5">Lớp học</th>
              <th class="px-5 py-3.5">Ngày ghi danh</th>
              <th class="px-5 py-3.5">Trạng thái</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-stone-200">
            <tr v-for="item in recentEnrollments.slice(0, 10)" :key="`${item.classId}-${item.studentId}`" class="transition hover:bg-orange-50/50">
              <td class="px-5 py-3.5 font-semibold text-stone-900">{{ item.studentName }}</td>
              <td class="px-5 py-3.5 text-stone-700">{{ item.className }}</td>
              <td class="whitespace-nowrap px-5 py-3.5 text-stone-600 tabular-nums">{{ formatDate(item.joinedAt) }}</td>
              <td class="px-5 py-3.5"><span class="rounded border px-2 py-0.5 text-xs font-semibold" :class="String(item.status).toUpperCase() === 'PENDING' ? 'border-amber-300 bg-amber-50 text-amber-800' : 'border-emerald-300 bg-emerald-50 text-emerald-800'">{{ String(item.status).toUpperCase() === 'PENDING' ? 'Chờ xác nhận' : 'Đang học' }}</span></td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { UserGroupIcon, UserPlusIcon, BuildingLibraryIcon } from '@heroicons/vue/24/outline'
import reportService from '@/services/admin-report.service'

const report = ref(null)
const loading = ref(false)
const errorMessage = ref('')
const period = ref('6')
let requestId = 0
const recentEnrollments = computed(() => report.value?.recentEnrollments ?? [])
const monthlyEnrollments = computed(() => (report.value?.monthlyEnrollments ?? []).map(item => ({
  key: item.month,
  label: formatMonth(item.month),
  count: Number(item.count || 0),
})))
const classSummaries = computed(() => report.value?.classSummaries ?? [])
const maxClassSize = computed(() => Math.max(...classSummaries.value.map(item => item.count), 1))
const stats = computed(() => [
  { label: 'Học viên đang theo học', value: Number(report.value?.activeStudentCount ?? 0).toLocaleString('vi-VN'), caption: 'Học viên duy nhất có ghi danh hoạt động', icon: UserGroupIcon, bg: 'bg-orange-50', color: 'text-orange-600' },
  { label: 'Lượt ghi danh mới', value: Number(report.value?.newEnrollmentCount ?? 0).toLocaleString('vi-VN'), caption: `Trong ${period.value} tháng gần nhất`, icon: UserPlusIcon, bg: 'bg-emerald-50', color: 'text-emerald-600' },
  { label: 'Lớp đang thống kê', value: Number(report.value?.classCount ?? 0).toLocaleString('vi-VN'), caption: 'Có dữ liệu từ hệ thống lớp học', icon: BuildingLibraryIcon, bg: 'bg-stone-100', color: 'text-stone-700' },
])

function barHeight(value) {
  const max = Math.max(...monthlyEnrollments.value.map(item => item.count), 1)
  return value ? Math.max(8, (value / max) * 100) : 3
}

function formatDate(value) {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleDateString('vi-VN')
}

function formatMonth(value) {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleDateString('vi-VN', { month: 'short' })
}

async function loadGrowth() {
  const activeRequest = ++requestId
  loading.value = true
  errorMessage.value = ''
  try {
    const result = await reportService.getStudentGrowth(Number(period.value))
    if (activeRequest === requestId) report.value = result
  } catch (error) {
    if (activeRequest !== requestId) return
    console.error('Không thể tải dữ liệu báo cáo tăng trưởng học viên:', error)
    errorMessage.value = error.response?.data?.message || 'Không thể tải dữ liệu ghi danh. Vui lòng thử lại.'
    report.value = null
  } finally {
    if (activeRequest === requestId) loading.value = false
  }
}

watch(period, loadGrowth, { immediate: true })
</script>
