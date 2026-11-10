<template>
  <div class="space-y-5">
    <header class="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <p class="text-xs font-semibold uppercase tracking-[0.18em] text-orange-500">Tài chính</p>
        <h1 class="mt-1 text-2xl font-bold text-orange-950">Hóa đơn &amp; Biên lai</h1>
        <p class="mt-1 text-sm text-gray-500">Theo dõi thanh toán học phí và tra cứu biên lai của học viên.</p>
      </div>
      <button
        type="button"
        class="inline-flex items-center justify-center gap-2 rounded-xl border border-orange-200 bg-white px-4 py-2.5 text-sm font-semibold text-orange-700 shadow-sm transition hover:bg-orange-50 disabled:opacity-60"
        :disabled="loading"
        @click="loadInvoices"
      >
        <ArrowPathIcon class="h-4 w-4" :class="{ 'animate-spin': loading }" />
        Làm mới
      </button>
    </header>

    <section class="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4" aria-label="Thống kê hóa đơn">
      <article
        v-for="stat in stats"
        :key="stat.label"
        class="flex items-center gap-3 rounded-2xl border border-orange-100 bg-white p-4 shadow-sm"
      >
        <div class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl" :class="stat.iconBg">
          <component :is="stat.icon" class="h-5 w-5" :class="stat.iconColor" />
        </div>
        <div class="min-w-0">
          <p class="text-xs font-medium text-gray-500">{{ stat.label }}</p>
          <p class="mt-1 truncate text-lg font-bold text-gray-900">{{ stat.value }}</p>
        </div>
      </article>
    </section>

    <section class="overflow-hidden rounded-2xl border border-orange-100 bg-white shadow-sm">
      <div class="flex flex-col gap-3 border-b border-orange-100 p-4 lg:flex-row lg:items-center lg:justify-between">
        <div class="relative w-full lg:max-w-md">
          <MagnifyingGlassIcon class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-gray-400" />
          <input
            v-model="search"
            type="search"
            maxlength="100"
            placeholder="Tìm mã hóa đơn, phụ huynh, học viên..."
            aria-label="Tìm kiếm hóa đơn"
            class="w-full rounded-xl border border-orange-200 bg-white py-2.5 pl-9 pr-3 text-sm text-gray-700 outline-none transition placeholder:text-gray-400 focus:border-orange-400 focus:ring-2 focus:ring-orange-100"
          />
        </div>
        <div class="flex flex-col gap-2 sm:flex-row sm:items-center">
          <label for="invoice-status" class="text-sm font-medium text-gray-600">Trạng thái</label>
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

      <div v-if="errorMessage" class="m-4 flex flex-col items-start gap-3 rounded-xl border border-red-200 bg-red-50 p-4 sm:flex-row sm:items-center sm:justify-between">
        <p class="text-sm text-red-700">{{ errorMessage }}</p>
        <button type="button" class="shrink-0 text-sm font-semibold text-red-700 underline" @click="loadInvoices">
          Thử lại
        </button>
      </div>

      <div v-if="loading" class="flex min-h-56 items-center justify-center gap-3 text-sm text-orange-700">
        <ArrowPathIcon class="h-5 w-5 animate-spin" />
        Đang tải danh sách hóa đơn...
      </div>
      <div v-else-if="!errorMessage && filteredInvoices.length === 0" class="flex min-h-56 flex-col items-center justify-center px-4 text-center">
        <DocumentTextIcon class="h-10 w-10 text-orange-300" />
        <p class="mt-3 font-semibold text-gray-700">Không tìm thấy hóa đơn phù hợp</p>
        <p class="mt-1 text-sm text-gray-500">Thử đổi từ khóa hoặc trạng thái lọc.</p>
      </div>

      <template v-else-if="!errorMessage">
        <div class="border-b border-orange-50 px-4 py-3 text-xs text-gray-500">
          Hiển thị <span class="font-semibold text-orange-700">{{ filteredInvoices.length }}</span> / {{ invoices.length }} hóa đơn
        </div>
        <div class="overflow-x-auto">
          <table class="w-full min-w-[900px] text-left text-sm">
            <thead class="bg-orange-50/80 text-xs uppercase tracking-wide text-orange-900">
              <tr>
                <th scope="col" class="px-5 py-3.5 font-semibold">Hóa đơn</th>
                <th scope="col" class="px-5 py-3.5 font-semibold">Phụ huynh / Học viên</th>
                <th scope="col" class="px-5 py-3.5 font-semibold">Lớp học</th>
                <th scope="col" class="px-5 py-3.5 font-semibold">Ngày tạo</th>
                <th scope="col" class="px-5 py-3.5 text-right font-semibold">Số tiền</th>
                <th scope="col" class="px-5 py-3.5 font-semibold">Trạng thái</th>
                <th scope="col" class="px-5 py-3.5 text-right font-semibold">Thao tác</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100">
              <tr v-for="invoice in filteredInvoices" :key="invoice.invoiceId" class="transition hover:bg-orange-50/40">
                <td class="px-5 py-4">
                  <p class="font-semibold text-gray-900">#{{ invoice.invoiceId }}</p>
                  <p class="mt-1 font-mono text-xs text-gray-400">{{ invoice.txnRef || '—' }}</p>
                </td>
                <td class="px-5 py-4">
                  <p class="font-medium text-gray-800">{{ invoice.parentName || 'Chưa cập nhật' }}</p>
                  <p class="mt-1 text-xs text-gray-500">{{ invoice.studentName || 'Chưa có học viên' }}</p>
                </td>
                <td class="px-5 py-4">
                  <p class="font-medium text-gray-700">{{ invoice.className || '—' }}</p>
                  <p class="mt-1 text-xs text-gray-500">{{ invoice.courseLevelName || '—' }}</p>
                </td>
                <td class="whitespace-nowrap px-5 py-4 text-gray-600">{{ formatDate(invoice.createdAt) }}</td>
                <td class="whitespace-nowrap px-5 py-4 text-right font-semibold text-gray-900">{{ formatCurrency(invoice.amount) }}</td>
                <td class="px-5 py-4">
                  <span class="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold" :class="statusClass(invoice.status)">
                    <span class="h-1.5 w-1.5 rounded-full" :class="statusDotClass(invoice.status)"></span>
                    {{ statusLabel(invoice.status) }}
                  </span>
                </td>
                <td class="px-5 py-4 text-right">
                  <button
                    type="button"
                    class="inline-flex items-center gap-1.5 rounded-lg px-2.5 py-2 text-xs font-semibold text-orange-700 transition hover:bg-orange-100"
                    :aria-label="invoice.status === 'PAID' ? `Xem biên lai hóa đơn ${invoice.invoiceId}` : `Xem hóa đơn ${invoice.invoiceId}`"
                    @click="selectedInvoice = invoice"
                  >
                    <ReceiptPercentIcon class="h-4 w-4" />
                    {{ invoice.status === 'PAID' ? 'Biên lai' : 'Chi tiết' }}
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </template>
    </section>

    <div v-if="selectedInvoice" class="fixed inset-0 z-50 flex items-center justify-center bg-gray-950/50 p-4" @click.self="selectedInvoice = null">
      <section
        class="receipt-print w-full max-w-xl overflow-hidden rounded-2xl bg-white shadow-2xl"
        role="dialog"
        aria-modal="true"
        :aria-label="selectedInvoice.status === 'PAID' ? 'Biên lai thanh toán' : 'Chi tiết hóa đơn'"
      >
        <div class="no-print flex items-center justify-between border-b border-orange-100 bg-orange-50 px-5 py-4">
          <div>
            <p class="text-xs font-semibold uppercase tracking-wide text-orange-600">
              {{ selectedInvoice.status === 'PAID' ? 'Biên lai thanh toán' : 'Chi tiết hóa đơn' }}
            </p>
            <h2 class="mt-1 text-lg font-bold text-orange-950">Hóa đơn #{{ selectedInvoice.invoiceId }}</h2>
          </div>
          <button type="button" class="rounded-lg p-2 text-gray-500 hover:bg-white hover:text-gray-800" aria-label="Đóng" @click="selectedInvoice = null">
            <XMarkIcon class="h-5 w-5" />
          </button>
        </div>

        <div class="space-y-5 p-5 sm:p-6">
          <div class="flex items-start justify-between gap-4 border-b border-dashed border-gray-200 pb-4">
            <div>
              <p class="text-lg font-bold text-orange-800">EduAdmin</p>
              <p class="mt-1 text-xs text-gray-500">Hệ thống đào tạo Toán Tư Duy</p>
            </div>
            <span class="rounded-full px-3 py-1 text-xs font-semibold" :class="statusClass(selectedInvoice.status)">
              {{ statusLabel(selectedInvoice.status) }}
            </span>
          </div>

          <dl class="grid grid-cols-1 gap-x-6 gap-y-4 text-sm sm:grid-cols-2">
            <div>
              <dt class="text-xs text-gray-500">Mã giao dịch</dt>
              <dd class="mt-1 break-all font-mono font-medium text-gray-800">{{ selectedInvoice.txnRef || '—' }}</dd>
            </div>
            <div>
              <dt class="text-xs text-gray-500">Ngày lập</dt>
              <dd class="mt-1 font-medium text-gray-800">{{ formatDateTime(selectedInvoice.createdAt) }}</dd>
            </div>
            <div>
              <dt class="text-xs text-gray-500">Phụ huynh</dt>
              <dd class="mt-1 font-medium text-gray-800">{{ selectedInvoice.parentName || '—' }}</dd>
              <dd v-if="selectedInvoice.parentPhone" class="mt-0.5 text-xs text-gray-500">{{ selectedInvoice.parentPhone }}</dd>
              <dd v-if="selectedInvoice.parentEmail" class="mt-0.5 break-all text-xs text-gray-500">{{ selectedInvoice.parentEmail }}</dd>
            </div>
            <div>
              <dt class="text-xs text-gray-500">Học viên</dt>
              <dd class="mt-1 font-medium text-gray-800">{{ selectedInvoice.studentName || '—' }}</dd>
            </div>
            <div class="sm:col-span-2">
              <dt class="text-xs text-gray-500">Nội dung</dt>
              <dd class="mt-1 font-medium text-gray-800">{{ selectedInvoice.className || '—' }} · {{ selectedInvoice.courseLevelName || '—' }}</dd>
            </div>
            <div>
              <dt class="text-xs text-gray-500">Phương thức thanh toán</dt>
              <dd class="mt-1 font-medium text-gray-800">{{ paymentMethodLabel(selectedInvoice.paymentMethod) }}</dd>
            </div>
            <div v-if="selectedInvoice.transactionId">
              <dt class="text-xs text-gray-500">Mã giao dịch thanh toán</dt>
              <dd class="mt-1 break-all font-mono text-xs font-medium text-gray-800">{{ selectedInvoice.transactionId }}</dd>
            </div>
            <div v-if="selectedInvoice.paidAt" class="sm:col-span-2">
              <dt class="text-xs text-gray-500">Thời điểm thanh toán</dt>
              <dd class="mt-1 font-medium text-gray-800">{{ formatDateTime(selectedInvoice.paidAt) }}</dd>
            </div>
          </dl>

          <div class="flex items-center justify-between border-t border-dashed border-gray-200 pt-4">
            <p class="font-semibold text-gray-700">Tổng thanh toán</p>
            <p class="text-xl font-bold text-orange-700">{{ formatCurrency(selectedInvoice.amount) }}</p>
          </div>
          <p v-if="selectedInvoice.status !== 'PAID'" class="no-print rounded-xl bg-amber-50 px-3 py-2 text-xs text-amber-800">
            Biên lai chỉ có hiệu lực khi hóa đơn đã được thanh toán.
          </p>
        </div>

        <div class="no-print flex justify-end gap-2 border-t border-gray-100 bg-gray-50 px-5 py-4">
          <button type="button" class="rounded-xl border border-gray-200 bg-white px-4 py-2 text-sm font-semibold text-gray-600 hover:bg-gray-100" @click="selectedInvoice = null">
            Đóng
          </button>
          <button
            v-if="selectedInvoice.status === 'PAID'"
            type="button"
            class="inline-flex items-center gap-2 rounded-xl bg-orange-500 px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-orange-600"
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
import { computed, onMounted, ref } from 'vue'
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
import adminInvoiceService from '@/services/admin-invoice.service'

const invoices = ref([])
const loading = ref(false)
const errorMessage = ref('')
const search = ref('')
const statusFilter = ref('ALL')
const selectedInvoice = ref(null)

const filteredInvoices = computed(() => {
  const keyword = search.value.trim().toLocaleLowerCase('vi')
  return invoices.value.filter((invoice) => {
    const matchesStatus = statusFilter.value === 'ALL' || invoice.status === statusFilter.value
    const searchableText = [
      invoice.invoiceId,
      invoice.txnRef,
      invoice.parentName,
      invoice.parentEmail,
      invoice.parentPhone,
      invoice.studentName,
      invoice.className,
      invoice.courseLevelName,
    ].join(' ').toLocaleLowerCase('vi')
    return matchesStatus && (!keyword || searchableText.includes(keyword))
  })
})

const stats = computed(() => {
  const paidInvoices = invoices.value.filter((invoice) => invoice.status === 'PAID')
  const pendingCount = invoices.value.filter((invoice) => invoice.status === 'PENDING').length
  const paidTotal = paidInvoices.reduce((total, invoice) => total + (Number(invoice.amount) || 0), 0)
  return [
    { label: 'Tổng hóa đơn', value: invoices.value.length.toLocaleString('vi-VN'), icon: DocumentTextIcon, iconBg: 'bg-orange-100', iconColor: 'text-orange-600' },
    { label: 'Đã thanh toán', value: paidInvoices.length.toLocaleString('vi-VN'), icon: CheckCircleIcon, iconBg: 'bg-emerald-100', iconColor: 'text-emerald-600' },
    { label: 'Chờ thanh toán', value: pendingCount.toLocaleString('vi-VN'), icon: ClockIcon, iconBg: 'bg-amber-100', iconColor: 'text-amber-600' },
    { label: 'Doanh thu đã thu', value: formatCurrency(paidTotal), icon: BanknotesIcon, iconBg: 'bg-sky-100', iconColor: 'text-sky-600' },
  ]
})

async function loadInvoices() {
  loading.value = true
  errorMessage.value = ''
  try {
    const response = await adminInvoiceService.getAll()
    if (!Array.isArray(response)) {
      throw new Error('Dữ liệu hóa đơn trả về không hợp lệ.')
    }
    invoices.value = response
  } catch (error) {
    errorMessage.value = error.response?.data?.message || error.message || 'Không thể tải danh sách hóa đơn.'
  } finally {
    loading.value = false
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
    PAID: 'bg-emerald-50 text-emerald-700',
    PENDING: 'bg-amber-50 text-amber-700',
    FAILED: 'bg-red-50 text-red-700',
    CANCELLED: 'bg-gray-100 text-gray-600',
    REFUNDED: 'bg-violet-50 text-violet-700',
  })[status] || 'bg-gray-100 text-gray-600'
}

function statusDotClass(status) {
  return ({
    PAID: 'bg-emerald-500',
    PENDING: 'bg-amber-500',
    FAILED: 'bg-red-500',
    CANCELLED: 'bg-gray-400',
    REFUNDED: 'bg-violet-500',
  })[status] || 'bg-gray-400'
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
