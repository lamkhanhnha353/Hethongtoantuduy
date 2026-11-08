<template>
  <div class="space-y-5">
    <header class="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <p class="text-xs font-semibold uppercase tracking-[0.18em] text-orange-500">Báo cáo &amp; thống kê</p>
        <h1 class="mt-1 text-2xl font-bold text-orange-950">Báo cáo doanh thu</h1>
        <p class="mt-1 text-sm text-gray-500">Theo dõi doanh thu học phí và tình hình thanh toán.</p>
      </div>
      <div class="flex flex-wrap gap-2">
        <select v-model="period" aria-label="Khoảng thời gian báo cáo" class="filter-select min-w-40">
          <option value="3">3 tháng gần nhất</option>
          <option value="6">6 tháng gần nhất</option>
          <option value="12">12 tháng gần nhất</option>
        </select>
        <button type="button" class="inline-flex items-center gap-2 rounded-xl bg-orange-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-orange-700 disabled:opacity-60" :disabled="loading || !filteredInvoices.length" @click="exportCsv">
          <ArrowDownTrayIcon class="h-4 w-4" />
          Xuất CSV
        </button>
      </div>
    </header>

    <div v-if="errorMessage" class="flex flex-col gap-3 rounded-xl border border-red-200 bg-red-50 p-4 sm:flex-row sm:items-center sm:justify-between">
      <p class="text-sm text-red-700">{{ errorMessage }}</p>
      <button type="button" class="shrink-0 text-sm font-semibold text-red-700 underline" @click="loadInvoices">Thử lại</button>
    </div>

    <section class="grid grid-cols-1 gap-3 sm:grid-cols-3" aria-label="Tổng quan doanh thu">
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
            <h2 class="font-bold text-orange-950">Doanh thu theo tháng</h2>
            <p class="mt-1 text-xs text-gray-500">Tổng hóa đơn đã thanh toán trong kỳ được chọn</p>
          </div>
          <span class="rounded-full bg-emerald-50 px-3 py-1 text-xs font-semibold text-emerald-700">Đã thanh toán</span>
        </div>
        <div v-if="loading" class="flex h-56 items-center justify-center text-sm text-gray-500">Đang tải dữ liệu doanh thu...</div>
        <div v-else-if="!monthlyRevenue.some(item => item.amount > 0)" class="flex h-56 items-center justify-center text-sm text-gray-500">Chưa có doanh thu trong khoảng thời gian này.</div>
        <div v-else class="flex h-56 items-end gap-2 pt-6 sm:gap-4">
          <div v-for="item in monthlyRevenue" :key="item.key" class="group flex h-full min-w-0 flex-1 flex-col items-center justify-end gap-2">
            <span class="text-[10px] font-semibold text-orange-800 opacity-0 transition group-hover:opacity-100">{{ compactCurrency(item.amount) }}</span>
            <div class="w-full max-w-12 rounded-t-lg bg-gradient-to-t from-orange-500 to-amber-300 transition hover:from-orange-600" :style="{ height: `${barHeight(item.amount)}%` }" :title="`${item.label}: ${formatCurrency(item.amount)}`"></div>
            <span class="text-[10px] text-gray-500">{{ item.label }}</span>
          </div>
        </div>
      </article>

      <article class="rounded-2xl border border-orange-100 bg-white p-5 shadow-sm">
        <div class="border-b border-orange-100 pb-4">
          <h2 class="font-bold text-orange-950">Doanh thu theo lớp</h2>
          <p class="mt-1 text-xs text-gray-500">Top lớp có doanh thu cao nhất trong kỳ</p>
        </div>
        <div v-if="!topClasses.length" class="flex h-56 items-center justify-center text-center text-sm text-gray-500">Chưa có dữ liệu để thống kê.</div>
        <div v-else class="space-y-5 pt-5">
          <div v-for="(item, index) in topClasses" :key="item.name">
            <div class="mb-2 flex items-center justify-between gap-3 text-xs">
              <span class="min-w-0 truncate font-medium text-gray-700"><span class="mr-2 text-orange-400">0{{ index + 1 }}</span>{{ item.name }}</span>
              <span class="shrink-0 font-semibold text-gray-900">{{ compactCurrency(item.amount) }}</span>
            </div>
            <div class="h-2 overflow-hidden rounded-full bg-orange-50">
              <div class="h-full rounded-full bg-orange-400" :style="{ width: `${(item.amount / (topClasses[0]?.amount || 1)) * 100}%` }"></div>
            </div>
          </div>
        </div>
      </article>
    </section>

    <section class="overflow-hidden rounded-2xl border border-orange-100 bg-white shadow-sm">
      <div class="flex flex-wrap items-center justify-between gap-2 border-b border-orange-100 p-4">
        <div>
          <h2 class="font-bold text-orange-950">Giao dịch đã thanh toán</h2>
          <p class="mt-1 text-xs text-gray-500">{{ filteredInvoices.length }} hóa đơn trong kỳ báo cáo</p>
        </div>
        <span class="text-xs text-gray-400">Sắp xếp theo ngày thanh toán mới nhất</span>
      </div>
      <div v-if="loading" class="flex min-h-40 items-center justify-center text-sm text-gray-500">Đang tải danh sách hóa đơn...</div>
      <div v-else-if="!filteredInvoices.length" class="flex min-h-40 items-center justify-center text-sm text-gray-500">Không có hóa đơn phù hợp trong kỳ.</div>
      <div v-else class="overflow-x-auto">
        <table class="w-full min-w-[760px] text-left text-sm">
          <thead class="bg-orange-50/80 text-xs uppercase tracking-wide text-orange-900">
            <tr>
              <th class="px-5 py-3.5 font-semibold">Hóa đơn</th>
              <th class="px-5 py-3.5 font-semibold">Học viên</th>
              <th class="px-5 py-3.5 font-semibold">Lớp học</th>
              <th class="px-5 py-3.5 font-semibold">Ngày thanh toán</th>
              <th class="px-5 py-3.5 text-right font-semibold">Số tiền</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr v-for="invoice in filteredInvoices.slice(0, 10)" :key="invoice.invoiceId" class="hover:bg-orange-50/40">
              <td class="px-5 py-3.5 font-semibold text-gray-800">#{{ invoice.invoiceId }}</td>
              <td class="px-5 py-3.5 text-gray-700">{{ invoice.studentName || '—' }}</td>
              <td class="px-5 py-3.5 text-gray-600">{{ invoice.className || '—' }}</td>
              <td class="whitespace-nowrap px-5 py-3.5 text-gray-600">{{ formatDate(invoice.paidAt || invoice.createdAt) }}</td>
              <td class="whitespace-nowrap px-5 py-3.5 text-right font-semibold text-gray-900">{{ formatCurrency(invoice.amount) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <p v-if="filteredInvoices.length > 10" class="border-t border-orange-50 px-5 py-3 text-xs text-gray-500">Đang hiển thị 10 giao dịch mới nhất. Xuất CSV để xem toàn bộ.</p>
    </section>
  </div>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import {
  ArrowDownTrayIcon,
  BanknotesIcon,
  CheckCircleIcon,
  ClockIcon,
} from '@heroicons/vue/24/outline'
import reportService from '@/services/admin-report.service'

const report = ref(null)
const loading = ref(false)
const errorMessage = ref('')
const period = ref('6')
const currencyFormatter = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 })

const filteredInvoices = computed(() => report.value?.paidInvoices ?? [])
const monthlyRevenue = computed(() => (report.value?.monthlyRevenue ?? []).map(item => ({
  key: item.month,
  label: formatMonth(item.month),
  amount: Number(item.amount || 0),
})))
const topClasses = computed(() => report.value?.topClasses ?? [])
const stats = computed(() => {
  return [
    { label: 'Tổng doanh thu', value: formatCurrency(report.value?.totalRevenue), caption: `Trong ${period.value} tháng gần nhất`, icon: BanknotesIcon, bg: 'bg-orange-50', color: 'text-orange-600' },
    { label: 'Hóa đơn đã thanh toán', value: Number(report.value?.paidInvoiceCount ?? 0).toLocaleString('vi-VN'), caption: 'Trong khoảng thời gian đã chọn', icon: CheckCircleIcon, bg: 'bg-emerald-50', color: 'text-emerald-600' },
    { label: 'Đang chờ thanh toán', value: Number(report.value?.pendingInvoiceCount ?? 0).toLocaleString('vi-VN'), caption: 'Tất cả hóa đơn chưa hoàn tất', icon: ClockIcon, bg: 'bg-amber-50', color: 'text-amber-600' },
  ]
})

function formatCurrency(value) {
  return currencyFormatter.format(Number(value) || 0)
}

function compactCurrency(value) {
  if (value >= 1_000_000) return `${(value / 1_000_000).toLocaleString('vi-VN', { maximumFractionDigits: 1 })} tr`
  if (value >= 1_000) return `${(value / 1_000).toLocaleString('vi-VN', { maximumFractionDigits: 0 })} k`
  return String(value)
}

function formatDate(value) {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleDateString('vi-VN')
}

function formatMonth(value) {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleDateString('vi-VN', { month: 'short' })
}

function barHeight(value) {
  const max = Math.max(...monthlyRevenue.value.map(item => item.amount), 1)
  return value ? Math.max(8, (value / max) * 100) : 3
}

function exportCsv() {
  const rows = [
    ['Ma hoa don', 'Hoc vien', 'Lop hoc', 'Ngay thanh toan', 'So tien (VND)'],
    ...filteredInvoices.value.map(invoice => [
      invoice.invoiceId,
      invoice.studentName || '',
      invoice.className || '',
      formatDate(invoice.paidAt || invoice.createdAt),
      invoice.amount || 0,
    ]),
  ]
  const csv = `\uFEFF${rows.map(row => row.map(value => `"${String(value).replaceAll('"', '""')}"`).join(',')).join('\r\n')}`
  const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }))
  const link = document.createElement('a')
  link.href = url
  link.download = 'bao-cao-doanh-thu.csv'
  link.click()
  URL.revokeObjectURL(url)
}

async function loadInvoices() {
  loading.value = true
  errorMessage.value = ''
  try {
    report.value = await reportService.getRevenue(Number(period.value))
  } catch (error) {
    console.error('Không thể tải dữ liệu báo cáo doanh thu:', error)
    errorMessage.value = error.response?.data?.message || 'Không thể tải dữ liệu doanh thu. Vui lòng thử lại.'
    report.value = null
  } finally {
    loading.value = false
  }
}

watch(period, loadInvoices, { immediate: true })
</script>
