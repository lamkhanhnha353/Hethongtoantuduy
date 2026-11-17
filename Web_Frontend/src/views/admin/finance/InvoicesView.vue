<template>
  <div class="space-y-5">
    <header class="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <p class="text-xs font-semibold uppercase tracking-[0.18em] text-orange-600">Tài chính</p>
        <h1 class="mt-1 text-2xl font-bold text-stone-900">Hóa đơn &amp; Biên lai</h1>
        <p class="mt-1 text-sm text-stone-500">Theo dõi thanh toán học phí và tra cứu biên lai của học viên.</p>
      </div>
      <button
        type="button"
        class="inline-flex h-9 items-center justify-center gap-2 rounded-md border border-stone-300 bg-white px-3.5 text-sm font-semibold text-stone-800 transition hover:border-stone-400 hover:bg-stone-100 disabled:opacity-60"
        :disabled="loading"
        @click="loadInvoices"
      >
        <ArrowPathIcon class="h-4 w-4 text-stone-600" :class="{ 'animate-spin': loading }" />
        Làm mới
      </button>
    </header>

    <section class="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4" aria-label="Thống kê hóa đơn">
      <article
        v-for="stat in stats"
        :key="stat.label"
        class="flex items-center gap-3 rounded-lg border border-stone-300 bg-white p-4 shadow-[0_1px_2px_rgba(0,0,0,0.04)]"
      >
        <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-md border border-stone-200" :class="stat.iconBg">
          <component :is="stat.icon" class="h-5 w-5" :class="stat.iconColor" />
        </div>
        <div class="min-w-0">
          <p class="text-xs font-semibold uppercase tracking-wide text-stone-500">{{ stat.label }}</p>
          <p class="mt-1 truncate text-lg font-bold text-stone-900 tabular-nums">{{ stat.value }}</p>
        </div>
      </article>
    </section>

    <section class="overflow-hidden rounded-lg border border-stone-300 bg-white shadow-[0_1px_2px_rgba(0,0,0,0.04)]">
      <div class="flex flex-col gap-3 border-b border-stone-200 p-4 lg:flex-row lg:items-center lg:justify-between">
        <div class="relative w-full lg:max-w-md">
          <MagnifyingGlassIcon class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-stone-400" />
          <input
            v-model="search"
            type="search"
            maxlength="100"
            placeholder="Tìm mã hóa đơn, phụ huynh, học viên..."
            aria-label="Tìm kiếm hóa đơn"
            class="h-9 w-full rounded-md border border-stone-300 bg-stone-50/70 pl-9 pr-3 text-sm text-stone-900 outline-none transition placeholder:text-stone-400 focus:border-orange-600 focus:bg-white focus:ring-2 focus:ring-orange-500/20"
          />
        </div>
        <div class="flex flex-col gap-2 sm:flex-row sm:items-center">
          <label for="invoice-status" class="text-sm font-medium text-stone-600">Trạng thái</label>
          <select
            id="invoice-status"
            v-model="statusFilter"
            class="filter-select min-w-44"
          >
            <option value="ALL">Tất cả hóa đơn</option>
            <option value="PENDING">Chờ thanh toán</option>
            <option value="PAID">Đã thanh toán</option>
            <option value="FAILED">Thất bại</option>
            <option value="CANCELLED">Đã hủy</option>
            <option value="REFUNDED">Đã hoàn tiền</option>
          </select>
        </div>
      </div>

      <div v-if="errorMessage" class="m-4 flex flex-col items-start gap-3 rounded-md border border-rose-200 bg-rose-50 p-4 sm:flex-row sm:items-center sm:justify-between">
        <p class="text-sm text-rose-700">{{ errorMessage }}</p>
        <button type="button" class="shrink-0 text-sm font-semibold text-rose-700 underline" @click="loadInvoices">
          Thử lại
        </button>
      </div>

      <div v-if="loading" class="flex min-h-56 items-center justify-center gap-3 text-sm text-stone-600">
        <ArrowPathIcon class="h-5 w-5 animate-spin text-orange-600" />
        Đang tải danh sách hóa đơn...
      </div>
      <div v-else-if="!errorMessage && invoices.length === 0" class="flex min-h-56 flex-col items-center justify-center px-4 text-center">
        <DocumentTextIcon class="h-10 w-10 text-stone-400" />
        <p class="mt-3 font-semibold text-stone-800">Không tìm thấy hóa đơn phù hợp</p>
        <p class="mt-1 text-sm text-stone-500">Thử đổi từ khóa hoặc trạng thái lọc.</p>
      </div>

      <template v-else-if="!errorMessage">
        <div class="border-b border-stone-200 bg-stone-50/70 px-4 py-2.5 text-xs text-stone-600">
          Tìm thấy <span class="font-semibold text-stone-900 tabular-nums">{{ totalCount.toLocaleString('vi-VN') }}</span> hóa đơn phù hợp
        </div>
        <div class="overflow-x-auto">
          <table class="w-full min-w-[900px] text-left text-sm">
            <thead class="border-b-2 border-stone-300 bg-stone-100 text-xs font-bold uppercase tracking-wider text-stone-700">
              <tr>
                <th scope="col" class="px-5 py-3.5">Hóa đơn</th>
                <th scope="col" class="px-5 py-3.5">Phụ huynh / Học viên</th>
                <th scope="col" class="px-5 py-3.5">Lớp học</th>
                <th scope="col" class="px-5 py-3.5">Ngày tạo</th>
                <th scope="col" class="px-5 py-3.5 text-right">Số tiền</th>
                <th scope="col" class="px-5 py-3.5">Trạng thái</th>
                <th scope="col" class="px-5 py-3.5 text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-stone-200">
              <tr v-for="invoice in invoices" :key="invoice.invoiceId" class="transition hover:bg-orange-50/50">
                <td class="px-5 py-4">
                  <p class="font-semibold text-stone-900 tabular-nums">#{{ invoice.invoiceId }}</p>
                  <p class="mt-1 font-mono text-xs text-stone-500 tabular-nums">{{ invoice.txnRef || '—' }}</p>
                </td>
                <td class="px-5 py-4">
                  <p class="font-medium text-stone-900">{{ invoice.parentName || 'Chưa cập nhật' }}</p>
                  <p class="mt-1 text-xs text-stone-500">{{ invoice.studentName || 'Chưa có học viên' }}</p>
                </td>
                <td class="px-5 py-4">
                  <p class="font-medium text-stone-800">{{ invoice.className || '—' }}</p>
                  <p class="mt-1 text-xs text-stone-500">{{ invoice.courseLevelName || '—' }}</p>
                </td>
                <td class="whitespace-nowrap px-5 py-4 text-stone-600 tabular-nums">{{ formatDate(invoice.createdAt) }}</td>
                <td class="whitespace-nowrap px-5 py-4 text-right font-semibold text-stone-900 tabular-nums">{{ formatCurrency(invoice.amount) }}</td>
                <td class="px-5 py-4">
                  <span class="inline-flex items-center gap-1.5 rounded border px-2 py-0.5 text-xs font-semibold" :class="statusClass(invoice.status)">
                    <span class="h-1.5 w-1.5 rounded-full" :class="statusDotClass(invoice.status)"></span>
                    {{ statusLabel(invoice.status) }}
                  </span>
                </td>
                <td class="px-5 py-4 text-right">
                  <button
                    type="button"
                    class="inline-flex h-8 items-center gap-1.5 rounded-md border border-stone-300 bg-white px-2.5 text-xs font-semibold text-stone-800 transition hover:border-stone-400 hover:bg-stone-100"
                    :aria-label="invoice.status === 'PAID' ? `Xem biên lai hóa đơn ${invoice.invoiceId}` : `Xem hóa đơn ${invoice.invoiceId}`"
                    @click="selectedInvoice = invoice"
                  >
                    <ReceiptPercentIcon class="h-4 w-4 text-stone-600" />
                    {{ invoice.status === 'PAID' ? 'Biên lai' : 'Chi tiết' }}
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <PaginationBar
          v-model:page="page"
          v-model:page-size="pageSize"
          :total-count="totalCount"
          :total-pages="totalPages"
          :disabled="loading"
          item-label="hóa đơn"
          @change="loadInvoices"
        />
      </template>
    </section>

    <div v-if="selectedInvoice" class="fixed inset-0 z-50 flex items-center justify-center bg-stone-950/50 p-4" @click.self="selectedInvoice = null">
      <section
        class="receipt-print w-full max-w-xl overflow-hidden rounded-lg border border-stone-300 bg-white shadow-2xl"
        role="dialog"
        aria-modal="true"
        :aria-label="selectedInvoice.status === 'PAID' ? 'Biên lai thanh toán' : 'Chi tiết hóa đơn'"
      >
        <div class="no-print flex items-center justify-between border-b border-stone-200 bg-stone-50 px-5 py-4">
          <div>
            <p class="text-xs font-semibold uppercase tracking-wide text-orange-600">
              {{ selectedInvoice.status === 'PAID' ? 'Biên lai thanh toán' : 'Chi tiết hóa đơn' }}
            </p>
            <h2 class="mt-0.5 text-lg font-bold text-stone-900 tabular-nums">Hóa đơn #{{ selectedInvoice.invoiceId }}</h2>
          </div>
          <button type="button" class="rounded-md p-1.5 text-stone-500 transition hover:bg-stone-200/70 hover:text-stone-800" aria-label="Đóng" @click="selectedInvoice = null">
            <XMarkIcon class="h-5 w-5" />
          </button>
        </div>

        <div class="space-y-5 p-5 sm:p-6">
          <div class="flex items-start justify-between gap-4 border-b border-dashed border-stone-300 pb-4">
            <div>
              <p class="text-lg font-bold text-orange-700">EduAdmin</p>
              <p class="mt-1 text-xs text-stone-500">Hệ thống đào tạo Toán Tư Duy</p>
            </div>
            <span class="rounded border px-2.5 py-0.5 text-xs font-semibold" :class="statusClass(selectedInvoice.status)">
              {{ statusLabel(selectedInvoice.status) }}
            </span>
          </div>

          <dl class="grid grid-cols-1 gap-x-6 gap-y-4 text-sm sm:grid-cols-2">
            <div>
              <dt class="text-xs text-stone-500">Mã giao dịch</dt>
              <dd class="mt-1 break-all font-mono font-medium text-stone-900 tabular-nums">{{ selectedInvoice.txnRef || '—' }}</dd>
            </div>
            <div>
              <dt class="text-xs text-stone-500">Ngày lập</dt>
              <dd class="mt-1 font-medium text-stone-900 tabular-nums">{{ formatDateTime(selectedInvoice.createdAt) }}</dd>
            </div>
            <div>
              <dt class="text-xs text-stone-500">Phụ huynh</dt>
              <dd class="mt-1 font-medium text-stone-900">{{ selectedInvoice.parentName || '—' }}</dd>
              <dd v-if="selectedInvoice.parentPhone" class="mt-0.5 text-xs text-stone-500 tabular-nums">{{ selectedInvoice.parentPhone }}</dd>
              <dd v-if="selectedInvoice.parentEmail" class="mt-0.5 break-all text-xs text-stone-500">{{ selectedInvoice.parentEmail }}</dd>
            </div>
            <div>
              <dt class="text-xs text-stone-500">Học viên</dt>
              <dd class="mt-1 font-medium text-stone-900">{{ selectedInvoice.studentName || '—' }}</dd>
            </div>
            <div class="sm:col-span-2">
              <dt class="text-xs text-stone-500">Nội dung</dt>
              <dd class="mt-1 font-medium text-stone-900">{{ selectedInvoice.className || '—' }} · {{ selectedInvoice.courseLevelName || '—' }}</dd>
            </div>
            <div>
              <dt class="text-xs text-stone-500">Phương thức thanh toán</dt>
              <dd class="mt-1 font-medium text-stone-900">{{ paymentMethodLabel(selectedInvoice.paymentMethod) }}</dd>
            </div>
            <div v-if="selectedInvoice.transactionId">
              <dt class="text-xs text-stone-500">Mã giao dịch thanh toán</dt>
              <dd class="mt-1 break-all font-mono text-xs font-medium text-stone-900 tabular-nums">{{ selectedInvoice.transactionId }}</dd>
            </div>
            <div v-if="selectedInvoice.paidAt" class="sm:col-span-2">
              <dt class="text-xs text-stone-500">Thời điểm thanh toán</dt>
              <dd class="mt-1 font-medium text-stone-900 tabular-nums">{{ formatDateTime(selectedInvoice.paidAt) }}</dd>
            </div>
          </dl>

          <div class="flex items-center justify-between border-t border-dashed border-stone-300 pt-4">
            <p class="font-semibold text-stone-700">Tổng thanh toán</p>
            <p class="text-xl font-bold text-orange-700 tabular-nums">{{ formatCurrency(selectedInvoice.amount) }}</p>
          </div>
          <p v-if="selectedInvoice.status !== 'PAID'" class="no-print rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-800">
            Biên lai chỉ có hiệu lực khi hóa đơn đã được thanh toán.
          </p>
        </div>

        <div class="no-print flex justify-end gap-2 border-t border-stone-200 bg-stone-50 px-5 py-3.5">
          <button type="button" class="h-9 rounded-md border border-stone-300 bg-white px-4 text-sm font-semibold text-stone-800 transition hover:border-stone-400 hover:bg-stone-100" @click="selectedInvoice = null">
            Đóng
          </button>
          <button
            v-if="selectedInvoice.status === 'PAID'"
            type="button"
            class="inline-flex h-9 items-center gap-2 rounded-md border border-orange-700 bg-orange-600 px-4 text-sm font-semibold text-white shadow-xs transition hover:bg-orange-700"
            @click="printReceipt"
          >
            <PrinterIcon class="h-4 w-4" />
            In biên lai
          </button>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import {
  ArrowPathIcon,
  BanknotesIcon,
  CheckCircleIcon,
  ClockIcon,
  DocumentTextIcon,
  MagnifyingGlassIcon,
  PrinterIcon,
  ReceiptPercentIcon,
  XMarkIcon,
} from '@heroicons/vue/24/outline'
import PaginationBar from '@/components/common/PaginationBar.vue'
import { usePagination } from '@/composables/usePagination'
import adminInvoiceService from '@/services/admin-invoice.service'

const { page, pageSize, totalCount, totalPages, applyPagedResult, resetPage } = usePagination({
  initialPageSize: 20,
})

const invoices = ref([])
const loading = ref(false)
const errorMessage = ref('')
const search = ref('')
const statusFilter = ref('ALL')
const selectedInvoice = ref(null)
const statsSummary = ref({
  totalAllCount: 0,
  paidCount: 0,
  pendingCount: 0,
  paidTotalAmount: 0,
})

let searchTimer = null
let requestToken = 0

const stats = computed(() => [
  { label: 'Tổng hóa đơn', value: statsSummary.value.totalAllCount.toLocaleString('vi-VN'), icon: DocumentTextIcon, iconBg: 'bg-orange-100', iconColor: 'text-orange-600' },
  { label: 'Đã thanh toán', value: statsSummary.value.paidCount.toLocaleString('vi-VN'), icon: CheckCircleIcon, iconBg: 'bg-emerald-100', iconColor: 'text-emerald-600' },
  { label: 'Chờ thanh toán', value: statsSummary.value.pendingCount.toLocaleString('vi-VN'), icon: ClockIcon, iconBg: 'bg-amber-100', iconColor: 'text-amber-600' },
  { label: 'Doanh thu đã thu', value: formatCurrency(statsSummary.value.paidTotalAmount), icon: BanknotesIcon, iconBg: 'bg-stone-100', iconColor: 'text-stone-700' },
])

watch(search, () => {
  if (searchTimer) clearTimeout(searchTimer)
  searchTimer = setTimeout(() => {
    resetPage()
    loadInvoices()
  }, 300)
})

watch(statusFilter, () => {
  resetPage()
  loadInvoices()
})

onBeforeUnmount(() => {
  if (searchTimer) clearTimeout(searchTimer)
})

async function loadInvoices() {
  const token = ++requestToken
  loading.value = true
  errorMessage.value = ''
  try {
    const response = await adminInvoiceService.getPaged({
      page: page.value,
      pageSize: pageSize.value,
      keyword: search.value,
      status: statusFilter.value,
    })
    if (token !== requestToken) return

    const items = applyPagedResult(response)
    if (items.length === 0 && page.value > 1 && totalPages.value > 0 && page.value > totalPages.value) {
      page.value = totalPages.value
      await loadInvoices()
      return
    }
    invoices.value = items
    statsSummary.value = {
      totalAllCount: Number(response?.totalAllCount ?? response?.totalCount ?? items.length),
      paidCount: Number(response?.paidCount ?? 0),
      pendingCount: Number(response?.pendingCount ?? 0),
      paidTotalAmount: Number(response?.paidTotalAmount ?? 0),
    }
  } catch (error) {
    if (token !== requestToken) return
    errorMessage.value = error.response?.data?.message || error.message || 'Không thể tải danh sách hóa đơn.'
  } finally {
    if (token === requestToken) {
      loading.value = false
    }
  }
}

function formatCurrency(value) {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 })
    .format(Number(value) || 0)
}

function formatDate(value) {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '—' : new Intl.DateTimeFormat('vi-VN').format(date)
}

function formatDateTime(value) {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '—' : new Intl.DateTimeFormat('vi-VN', { dateStyle: 'medium', timeStyle: 'short' }).format(date)
}

function statusLabel(status) {
  return ({
    PAID: 'Đã thanh toán',
    PENDING: 'Chờ thanh toán',
    FAILED: 'Thất bại',
    CANCELLED: 'Đã hủy',
    REFUNDED: 'Đã hoàn tiền',
  })[status] || status || 'Không xác định'
}

function statusClass(status) {
  return ({
    PAID: 'border-emerald-300 bg-emerald-50 text-emerald-800',
    PENDING: 'border-amber-300 bg-amber-50 text-amber-800',
    FAILED: 'border-rose-300 bg-rose-50 text-rose-800',
    CANCELLED: 'border-stone-300 bg-stone-100 text-stone-700',
    REFUNDED: 'border-violet-300 bg-violet-50 text-violet-800',
  })[status] || 'border-stone-300 bg-stone-100 text-stone-700'
}

function statusDotClass(status) {
  return ({
    PAID: 'bg-emerald-500',
    PENDING: 'bg-amber-500',
    FAILED: 'bg-rose-500',
    CANCELLED: 'bg-stone-400',
    REFUNDED: 'bg-violet-500',
  })[status] || 'bg-stone-400'
}

function paymentMethodLabel(method) {
  if (!method) return 'Chưa thanh toán'
  return ({ VNPAY: 'VNPay', CASH: 'Tiền mặt' })[method.toUpperCase()] || method
}

function printReceipt() {
  window.print()
}

onMounted(loadInvoices)
</script>

<style scoped>
@media print {
  :global(body *) {
    visibility: hidden !important;
  }

  .receipt-print,
  .receipt-print * {
    visibility: visible !important;
  }

  .receipt-print {
    position: absolute !important;
    inset: 0 !important;
    width: 100% !important;
    max-width: none !important;
    box-shadow: none !important;
  }

  .no-print {
    display: none !important;
  }
}
</style>
