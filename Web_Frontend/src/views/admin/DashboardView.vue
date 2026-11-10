<template>
  <div class="min-h-full space-y-6 bg-gray-50 p-4 sm:p-6 lg:p-8">
    <!-- Header -->
    <header class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <p class="text-sm font-medium text-orange-600">Bảng điều khiển</p>
        <h1 class="mt-1 text-2xl font-bold tracking-tight text-gray-900 sm:text-3xl">
          Tổng quan hệ thống
        </h1>
        <p class="mt-1 text-sm text-gray-500">Tình hình hoạt động của trung tâm trong tháng này.</p>
      </div>

      <div class="flex flex-wrap gap-2">
        <RouterLink
          to="/communication/notifications"
          class="inline-flex items-center gap-2 rounded-lg border border-gray-200 bg-white px-3.5 py-2.5 text-sm font-semibold text-gray-700 shadow-sm transition hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-orange-500 focus:ring-offset-2"
        >
          <BellIcon class="h-4 w-4" />
          Gửi thông báo
        </RouterLink>
        <RouterLink
          :to="{ name: 'admin-payroll' }"
          class="inline-flex items-center gap-2 rounded-lg border border-gray-200 bg-white px-3.5 py-2.5 text-sm font-semibold text-gray-700 shadow-sm transition hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-orange-500 focus:ring-offset-2"
        >
          <BanknotesIcon class="h-4 w-4" />
          Chốt lương
        </RouterLink>
      </div>
    </header>

    <!-- KPI Cards -->
    <section aria-label="Chỉ số tổng quan" class="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
      <article
        v-for="kpi in kpis"
        :key="kpi.label"
        class="rounded-xl border border-gray-200 bg-white p-5 shadow-sm transition hover:shadow-md"
      >
        <div class="flex items-start justify-between gap-3">
          <div>
            <p class="text-sm font-medium text-gray-500">{{ kpi.label }}</p>
            <p class="mt-2 text-2xl font-bold tracking-tight text-gray-900">
              {{ sourceLoading[kpi.source] ? 'Đang tải…' : dashboardErrors[kpi.source] ? '—' : kpi.value }}
            </p>
          </div>
          <span :class="['flex h-11 w-11 shrink-0 items-center justify-center rounded-xl', kpi.iconClass]">
            <component :is="kpi.icon" class="h-5 w-5" />
          </span>
        </div>
        <div class="mt-4 flex items-center gap-1.5 text-xs">
          <span
            v-if="kpi.change"
            :class="[
              'inline-flex items-center gap-1 rounded-full px-2 py-1 font-semibold',
              kpi.positive === null
                ? 'bg-gray-100 text-gray-600'
                : kpi.positive ? 'bg-green-50 text-green-700' : 'bg-amber-50 text-amber-700',
            ]"
          >
            <ArrowTrendingUpIcon v-if="kpi.positive === true" class="h-3.5 w-3.5" />
            <ArrowTrendingDownIcon v-else-if="kpi.positive === false" class="h-3.5 w-3.5" />
            {{ kpi.change }}
          </span>
          <span class="text-gray-500">{{ kpi.note }}</span>
        </div>
      </article>
    </section>

    <!-- Charts -->
    <section aria-label="Biểu đồ thống kê" class="grid grid-cols-1 gap-5 xl:grid-cols-2">
      <article class="min-w-0 rounded-xl border border-gray-200 bg-white p-5 shadow-sm sm:p-6">
        <div class="mb-5 flex flex-wrap items-start justify-between gap-3">
          <div>
            <h2 class="text-base font-semibold text-gray-900">Doanh thu &amp; chi phí quỹ lương</h2>
            <p class="mt-1 text-sm text-gray-500">Đơn vị: triệu đồng · 6 tháng gần nhất</p>
          </div>
          <span class="rounded-full bg-gray-100 px-2.5 py-1 text-xs font-medium text-gray-600">Dữ liệu hệ thống</span>
        </div>
        <p v-if="dashboardErrors.revenue || dashboardErrors.payroll" class="mb-3 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">
          {{ dashboardErrors.revenue || dashboardErrors.payroll }}
        </p>
        <div v-if="sourceLoading.revenue || sourceLoading.payroll" class="flex h-64 items-center justify-center text-sm text-gray-500 sm:h-72">
          Đang tải dữ liệu báo cáo…
        </div>
        <div v-else-if="!dashboardErrors.revenue && !dashboardErrors.payroll && revenueChartData.labels.length" class="h-64 sm:h-72">
          <Bar :data="revenueChartData" :options="barChartOptions" />
        </div>
        <p v-else-if="!dashboardErrors.revenue && !dashboardErrors.payroll" class="flex h-64 items-center justify-center text-sm text-gray-500 sm:h-72">
          Chưa có dữ liệu doanh thu hoặc quỹ lương.
        </p>
        <div class="mt-4 flex flex-wrap gap-x-5 gap-y-2 text-xs text-gray-600">
          <span class="inline-flex items-center gap-2">
            <span class="h-2.5 w-2.5 rounded-sm bg-orange-500"></span>Doanh thu học phí
          </span>
          <span class="inline-flex items-center gap-2">
            <span class="h-2.5 w-2.5 rounded-sm bg-sky-400"></span>Chi phí quỹ lương
          </span>
        </div>
      </article>

      <article class="min-w-0 rounded-xl border border-gray-200 bg-white p-5 shadow-sm sm:p-6">
        <div class="mb-5">
          <h2 class="text-base font-semibold text-gray-900">Phân bổ học viên theo lớp</h2>
          <p class="mt-1 text-sm text-gray-500">Lượt ghi danh theo lớp học đang mở</p>
        </div>
        <p v-if="dashboardErrors.students" class="mb-3 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">
          {{ dashboardErrors.students }}
        </p>
        <div v-if="sourceLoading.students" class="flex h-56 items-center justify-center text-sm text-gray-500">
          Đang tải thống kê lớp học…
        </div>
        <p v-else-if="!dashboardErrors.students && !classEnrollmentTotal" class="flex h-56 items-center justify-center text-sm text-gray-500">
          Chưa có dữ liệu ghi danh theo lớp.
        </p>
        <div v-else-if="!dashboardErrors.students" class="flex flex-col items-center gap-6 sm:flex-row sm:justify-center sm:gap-10">
          <div class="h-52 w-52 shrink-0">
            <Doughnut :data="classEnrollmentChartData" :options="doughnutChartOptions" />
          </div>
          <div class="w-full space-y-4 sm:w-auto">
            <div
              v-for="classItem in classEnrollmentBreakdown"
              :key="classItem.label"
              class="flex items-center justify-between gap-8"
            >
              <div class="flex items-center gap-2.5">
                <span :class="['h-3 w-3 rounded-full', classItem.color]"></span>
                <span class="text-sm text-gray-600">{{ classItem.label }}</span>
              </div>
              <div class="text-right">
                <span class="text-sm font-semibold text-gray-900">{{ classItem.count }}</span>
                <span class="ml-1 text-xs text-gray-500">HV</span>
              </div>
            </div>
            <div class="border-t border-gray-100 pt-3 text-sm">
              <span class="text-gray-500">Tổng lượt ghi danh</span>
              <span class="float-right font-semibold text-gray-900">{{ classEnrollmentTotal.toLocaleString('vi-VN') }}</span>
            </div>
          </div>
        </div>
      </article>
    </section>

    <!-- Widgets -->
    <section aria-label="Hoạt động gần đây" class="grid grid-cols-1 gap-5 xl:grid-cols-2">
      <!-- Payroll widget -->
      <article class="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
        <div class="flex items-center justify-between gap-3 border-b border-gray-100 px-5 py-4">
          <div>
            <h2 class="text-base font-semibold text-gray-900">Phiếu lương tháng này</h2>
            <p class="mt-1 text-sm text-gray-500">Tối đa 5 giáo viên từ bảng lương hệ thống</p>
          </div>
          <RouterLink
            :to="{ name: 'admin-payroll' }"
            class="shrink-0 text-sm font-semibold text-orange-600 hover:text-orange-700"
          >
            Xem tất cả
          </RouterLink>
        </div>
        <p v-if="dashboardErrors.payroll" class="m-4 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">
          {{ dashboardErrors.payroll }}
        </p>
        <div v-else-if="sourceLoading.payroll" class="p-8 text-center text-sm text-gray-500">
          Đang tải bảng lương…
        </div>
        <div v-else-if="!recentPayrolls.length" class="p-8 text-center text-sm text-gray-500">
          Chưa có dữ liệu bảng lương tháng này.
        </div>
        <div v-else class="overflow-x-auto">
          <table class="w-full min-w-[440px] text-left text-sm">
            <thead class="bg-gray-50 text-xs uppercase tracking-wide text-gray-500">
              <tr>
                <th scope="col" class="px-5 py-3 font-medium">Giáo viên</th>
                <th scope="col" class="px-5 py-3 font-medium">Số tiền</th>
                <th scope="col" class="px-5 py-3 font-medium">Trạng thái</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100">
              <tr v-for="payroll in recentPayrolls" :key="payroll.payrollId" class="hover:bg-gray-50/70">
                <td class="whitespace-nowrap px-5 py-3.5">
                  <div class="font-medium text-gray-900">{{ payroll.teacherName }}</div>
                  <div class="mt-0.5 text-xs text-gray-500">{{ payroll.month }}/{{ payroll.year }}</div>
                </td>
                <td class="whitespace-nowrap px-5 py-3.5 font-medium text-gray-700">{{ formatCurrency(payroll.finalSalary) }}</td>
                <td class="whitespace-nowrap px-5 py-3.5">
                  <span
                    :class="[
                      'inline-flex rounded-full px-2.5 py-1 text-xs font-medium',
                      payrollStatusClass(payroll.emailStatus),
                    ]"
                  >
                    {{ payrollStatusLabel(payroll.emailStatus) }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </article>

      <!-- Parent conversations widget -->
      <article class="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
        <div class="flex items-center justify-between gap-3 border-b border-gray-100 px-5 py-4">
          <div>
            <h2 class="text-base font-semibold text-gray-900">Tin nhắn phụ huynh cần phản hồi</h2>
            <p class="mt-1 text-sm text-gray-500">Cuộc trò chuyện mới nhất trong hệ thống</p>
          </div>
          <span class="inline-flex items-center gap-1.5 rounded-full bg-gray-100 px-2.5 py-1 text-xs font-semibold text-gray-600">
            {{ pendingParentReplyCount }} hội thoại
          </span>
        </div>
        <p v-if="dashboardErrors.chat" class="m-4 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">
          {{ dashboardErrors.chat }}
        </p>
        <p v-else-if="sourceLoading.chat" class="p-8 text-center text-sm text-gray-500">
          Đang tải hội thoại…
        </p>
        <p v-else-if="!supportMessages.length" class="p-8 text-center text-sm text-gray-500">
          Hiện không có tin nhắn cuối nào từ phụ huynh cần phản hồi.
        </p>
        <ul v-else class="divide-y divide-gray-100">
          <li v-for="message in supportMessages" :key="message.id" class="flex gap-3 px-5 py-4">
            <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-orange-100 text-sm font-semibold text-orange-700">
              {{ message.initials }}
            </div>
            <div class="min-w-0 flex-1">
              <div class="flex flex-wrap items-center justify-between gap-x-3 gap-y-1">
                <p class="text-sm font-semibold text-gray-900">{{ message.name }}</p>
                <span class="text-xs text-gray-400">{{ message.time }}</span>
              </div>
              <p class="mt-1 line-clamp-2 text-sm text-gray-600">{{ message.preview }}</p>
            </div>
          </li>
        </ul>
        <div class="border-t border-gray-100 px-5 py-3 text-xs text-gray-500">
          Nội dung lấy từ tin nhắn cuối cùng do phụ huynh gửi.
        </div>
      </article>
    </section>
  </div>
</template>

<script setup>
import { computed, markRaw, onMounted, ref } from 'vue'
import { Bar, Doughnut } from 'vue-chartjs'
import {
  Chart as ChartJS,
  ArcElement,
  BarElement,
  CategoryScale,
  Filler,
  Legend,
  LinearScale,
  Tooltip,
} from 'chart.js'
import createApiClient from '@/services/api.service'
import adminReportService from '@/services/admin-report.service'
import chatService from '@/services/chat.service'
import {
  AcademicCapIcon,
  ArrowTrendingDownIcon,
  ArrowTrendingUpIcon,
  BanknotesIcon,
  BellIcon,
  ChatBubbleLeftRightIcon,
} from '@heroicons/vue/24/outline'

ChartJS.register(ArcElement, BarElement, CategoryScale, Filler, Legend, LinearScale, Tooltip)

const payrollApi = createApiClient('/api/payrolls')
const revenueReport = ref(null)
const studentReport = ref(null)
const payrollTrend = ref(null)
const recentPayrolls = ref([])
const conversations = ref([])
const dashboardErrors = ref({
  revenue: '',
  students: '',
  payroll: '',
  chat: '',
})
const sourceLoading = ref({
  revenue: true,
  students: true,
  payroll: true,
  chat: true,
})

const barChartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  interaction: { intersect: false, mode: 'index' },
  plugins: {
    legend: { display: false },
    tooltip: {
      callbacks: {
        label: (context) => `${context.dataset.label}: ${context.parsed.y} triệu ₫`,
      },
    },
  },
  scales: {
    x: {
      grid: { display: false },
      border: { display: false },
      ticks: { color: '#6b7280', font: { size: 11 } },
    },
    y: {
      beginAtZero: true,
      border: { display: false, dash: [4, 4] },
      grid: { color: '#f3f4f6' },
      ticks: {
        color: '#9ca3af',
        font: { size: 11 },
        callback: (value) => `${value}`,
      },
    },
  },
}

const classEnrollmentBreakdown = computed(() => {
  const classes = [...(studentReport.value?.classSummaries ?? [])]
    .filter((item) => Number(item.count) > 0)
    .sort((left, right) => right.count - left.count)
  const topClasses = classes.slice(0, 4).map((item, index) => ({
    label: item.name,
    count: Number(item.count),
    color: ['bg-orange-500', 'bg-sky-500', 'bg-violet-500', 'bg-emerald-500'][index],
  }))
  const otherCount = classes.slice(4).reduce((total, item) => total + Number(item.count), 0)
  if (otherCount) topClasses.push({ label: 'Các lớp khác', count: otherCount, color: 'bg-gray-400' })
  return topClasses
})
const classEnrollmentTotal = computed(() =>
  classEnrollmentBreakdown.value.reduce((total, item) => total + item.count, 0),
)

const revenueChartData = computed(() => {
  const revenueByMonth = new Map(
    (revenueReport.value?.monthlyRevenue ?? []).map((item) => [monthKey(item.month), Number(item.amount) || 0]),
  )
  const payrollByMonth = new Map(
    (payrollTrend.value?.monthlyPayroll ?? []).map((item) => [monthKey(item.month), Number(item.amount) || 0]),
  )
  const months = revenueReport.value?.monthlyRevenue ?? []

  return {
    labels: months.map((item) => formatMonth(item.month)),
    datasets: [
      {
        label: 'Doanh thu học phí',
        data: months.map((item) => (revenueByMonth.get(monthKey(item.month)) ?? 0) / 1_000_000),
        backgroundColor: '#f97316',
        borderRadius: 5,
        maxBarThickness: 28,
      },
      {
        label: 'Chi phí quỹ lương',
        data: months.map((item) => (payrollByMonth.get(monthKey(item.month)) ?? 0) / 1_000_000),
        backgroundColor: '#38bdf8',
        borderRadius: 5,
        maxBarThickness: 28,
      },
    ],
  }
})

const classEnrollmentChartData = computed(() => ({
  labels: classEnrollmentBreakdown.value.map((item) => item.label),
  datasets: [
    {
      data: classEnrollmentBreakdown.value.map((item) => item.count),
      backgroundColor: ['#f97316', '#0ea5e9', '#8b5cf6', '#10b981', '#9ca3af'],
      borderWidth: 0,
      hoverOffset: 5,
    },
  ],
}))

const doughnutChartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  cutout: '72%',
  plugins: {
    legend: { display: false },
    tooltip: {
      callbacks: {
        label: (context) => `${context.label}: ${context.parsed.toLocaleString('vi-VN')} học viên`,
      },
    },
  },
}

const supportMessages = computed(() =>
  conversations.value
    .filter((conversation) =>
      String(conversation.lastMessageSenderType ?? '').toUpperCase() === 'PARENT'
      && conversation.lastMessage,
    )
    .slice(0, 4)
    .map((conversation) => ({
      id: conversation.id,
      name: conversation.partnerName,
      initials: getInitials(conversation.partnerName),
      time: formatDateTime(conversation.lastMessageAt),
      preview: conversation.lastMessage,
    })),
)
const pendingParentReplyCount = computed(() =>
  conversations.value.filter((conversation) =>
    String(conversation.lastMessageSenderType ?? '').toUpperCase() === 'PARENT'
    && conversation.lastMessage,
  ).length,
)

const currentMonthKey = monthKey(new Date())
const previousMonthDate = new Date()
previousMonthDate.setMonth(previousMonthDate.getMonth() - 1)
const previousMonthKey = monthKey(previousMonthDate)

const currentRevenue = computed(() => monthlyValue(revenueReport.value?.monthlyRevenue, currentMonthKey))
const previousRevenue = computed(() => monthlyValue(revenueReport.value?.monthlyRevenue, previousMonthKey))
const currentPayroll = computed(() => monthlyValue(payrollTrend.value?.monthlyPayroll, currentMonthKey))
const previousPayroll = computed(() => monthlyValue(payrollTrend.value?.monthlyPayroll, previousMonthKey))
const currentMonthEnrollments = computed(() =>
  monthlyValue(studentReport.value?.monthlyEnrollments, currentMonthKey, 'count'),
)

const kpis = computed(() => [
  {
    label: 'Doanh thu tháng này',
    value: formatCurrency(currentRevenue.value),
    change: formatChange(currentRevenue.value, previousRevenue.value),
    positive: compare(currentRevenue.value, previousRevenue.value),
    note: 'so với tháng trước',
    source: 'revenue',
    icon: markRaw(BanknotesIcon),
    iconClass: 'bg-orange-50 text-orange-600',
  },
  {
    label: 'Quỹ lương tháng này',
    value: formatCurrency(currentPayroll.value),
    change: formatChange(currentPayroll.value, previousPayroll.value),
    positive: compare(currentPayroll.value, previousPayroll.value),
    note: 'so với tháng trước',
    source: 'payroll',
    icon: markRaw(AcademicCapIcon),
    iconClass: 'bg-sky-50 text-sky-600',
  },
  {
    label: 'Tổng học viên đang hoạt động',
    value: (studentReport.value?.activeStudentCount ?? 0).toLocaleString('vi-VN'),
    change: `${currentMonthEnrollments.value.toLocaleString('vi-VN')} mới`,
    positive: null,
    note: 'tuyển mới tháng này',
    source: 'students',
    icon: markRaw(AcademicCapIcon),
    iconClass: 'bg-violet-50 text-violet-600',
  },
  {
    label: 'Tin nhắn phụ huynh cần phản hồi',
    value: pendingParentReplyCount.value.toLocaleString('vi-VN'),
    change: null,
    positive: null,
    note: 'tin nhắn cuối do phụ huynh gửi',
    source: 'chat',
    icon: markRaw(ChatBubbleLeftRightIcon),
    iconClass: 'bg-amber-50 text-amber-600',
  },
])

function monthKey(value) {
  const match = String(value ?? '').match(/^(\d{4})-(\d{2})/)
  if (match) return `${match[1]}-${match[2]}`
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? ''
    : `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`
}

function formatMonth(value) {
  const key = monthKey(value)
  if (!key) return ''
  const [year, month] = key.split('-')
  return `T${Number(month)}`
}

function monthlyValue(rows, key, valueField = 'amount') {
  const row = (rows ?? []).find((item) => monthKey(item.month) === key)
  return Number(row?.[valueField] ?? 0)
}

function formatCurrency(value) {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value ?? 0)
}

function formatChange(current, previous) {
  if (previous === null || previous === undefined) return 'Chưa có dữ liệu'
  if (!previous) return current ? 'Mới' : '0%'
  const percentage = ((current - previous) / previous) * 100
  const sign = percentage > 0 ? '+' : ''
  return `${sign}${percentage.toFixed(1)}%`
}

function compare(current, previous) {
  if (previous === null || previous === undefined) return null
  return current >= previous
}

function getInitials(name) {
  return String(name ?? '')
    .trim()
    .split(/\s+/)
    .slice(-2)
    .map((part) => part[0] ?? '')
    .join('')
    .toUpperCase()
}

function formatDateTime(value) {
  if (!value) return 'Không rõ thời gian'
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? 'Không rõ thời gian'
    : new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' }).format(date)
}

function payrollStatusLabel(status) {
  const normalized = String(status ?? '').toLowerCase()
  if (normalized === 'sent') return 'Đã gửi phiếu lương'
  if (['pending', 'queued', 'processing'].includes(normalized)) return 'Đang chờ gửi'
  if (normalized === 'failed') return 'Gửi thất bại'
  return 'Chưa gửi'
}

function payrollStatusClass(status) {
  const normalized = String(status ?? '').toLowerCase()
  if (normalized === 'sent') return 'bg-green-50 text-green-700'
  if (normalized === 'failed') return 'bg-red-50 text-red-700'
  return 'bg-amber-50 text-amber-700'
}

function getErrorMessage(error, fallback) {
  return error?.response?.data?.detail
    ?? error?.response?.data?.message
    ?? error?.message
    ?? fallback
}

async function loadRevenue() {
  try {
    revenueReport.value = await adminReportService.getRevenue(6)
  } catch (error) {
    dashboardErrors.value.revenue = getErrorMessage(error, 'Không thể tải báo cáo doanh thu.')
  } finally {
    sourceLoading.value.revenue = false
  }
}

async function loadStudents() {
  try {
    studentReport.value = await adminReportService.getStudentGrowth(6)
  } catch (error) {
    dashboardErrors.value.students = getErrorMessage(error, 'Không thể tải số liệu học viên.')
  } finally {
    sourceLoading.value.students = false
  }
}

async function loadPayroll() {
  try {
    const now = new Date()
    const response = await payrollApi.get('/admin/summary', {
      params: { month: now.getMonth() + 1, year: now.getFullYear() },
    })
    recentPayrolls.value = (Array.isArray(response.data) ? response.data : []).slice(0, 5)
    payrollTrend.value = await adminReportService.getPayrollTrend(6)
  } catch (error) {
    dashboardErrors.value.payroll = getErrorMessage(error, 'Không thể tải dữ liệu bảng lương.')
  } finally {
    sourceLoading.value.payroll = false
  }
}

async function loadConversations() {
  try {
    conversations.value = await chatService.getAdminConversations()
  } catch (error) {
    dashboardErrors.value.chat = getErrorMessage(error, 'Không thể tải tin nhắn phụ huynh.')
  } finally {
    sourceLoading.value.chat = false
  }
}

onMounted(() => {
  void Promise.all([
    loadRevenue(),
    loadStudents(),
    loadPayroll(),
    loadConversations(),
  ])
})
</script>
