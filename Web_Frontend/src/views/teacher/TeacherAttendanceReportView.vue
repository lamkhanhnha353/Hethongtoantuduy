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
          class="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-orange-100 hover:bg-orange-200 disabled:opacity-50 disabled:cursor-not-allowed text-orange-900 text-xs font-semibold transition-colors cursor-pointer"
          :disabled="isLoadingReport || !selectedClassId || report.marked === 0"
          @click="exportReport"
        >
          <ArrowDownTrayIcon class="w-4 h-4 text-orange-600" />
          Xuất báo cáo chuyên cần
        </button>
      </div>
    </div>

    <!-- ─── Load error ───────────────────────────────────────────────────── -->
    <div
      v-if="loadError"
      class="flex items-center justify-between gap-3 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-xs text-rose-800"
    >
      <span class="font-medium">⚠️ {{ loadError }}</span>
      <button
        type="button"
        @click="loadClasses"
        class="px-3 py-1.5 rounded-lg bg-white border border-rose-200 font-semibold text-rose-700 hover:bg-rose-100 transition-colors cursor-pointer"
      >
        Thử lại
      </button>
    </div>

    <!-- ─── Filter Toolbar ───────────────────────────────────────────────── -->
    <div class="bg-white rounded-2xl border border-orange-100 p-4 shadow-sm">
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <div>
          <label class="block text-xs font-semibold text-gray-600 mb-1">Chọn lớp học</label>
          <select
            v-model="selectedClassId"
            :disabled="isLoadingClasses || classes.length === 0"
            class="w-full text-xs font-semibold bg-orange-50/60 border border-orange-200 text-orange-950 rounded-xl px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 disabled:opacity-60 cursor-pointer"
            @change="loadReport"
          >
            <option v-if="classes.length === 0" :value="null">
              {{ isLoadingClasses ? 'Đang tải danh sách lớp...' : 'Chưa có lớp học được phân công' }}
            </option>
            <option v-for="c in classes" :key="c.id" :value="c.id">
              {{ c.name }} ({{ c.activeStudentsCount || 0 }} học sinh)
            </option>
          </select>
        </div>

        <div>
          <label class="block text-xs font-semibold text-gray-600 mb-1">Thời gian thống kê</label>
          <select
            v-model="timeRange"
            class="w-full text-xs font-semibold bg-white border border-orange-200 text-gray-800 rounded-xl px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 cursor-pointer"
            @change="loadReport"
          >
            <option v-for="opt in timeRangeOptions" :key="opt.value" :value="opt.value">
              {{ opt.label }}
            </option>
          </select>
        </div>
      </div>
    </div>

    <!-- ─── Loading ──────────────────────────────────────────────────────── -->
    <div
      v-if="isLoadingReport"
      class="bg-white rounded-2xl border border-orange-100 p-10 text-center shadow-sm space-y-3"
    >
      <ArrowPathIcon class="w-8 h-8 animate-spin text-orange-500 mx-auto" />
      <p class="text-sm font-semibold text-orange-900">Đang tổng hợp dữ liệu điểm danh từ máy chủ...</p>
    </div>

    <template v-else>
      <!-- ─── Overview Stats Cards ───────────────────────────────────────── -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div class="bg-white rounded-2xl p-4 border border-orange-100 shadow-sm">
          <p class="text-xs text-gray-500 font-medium">Tỷ lệ chuyên cần chung</p>
          <p class="text-2xl font-bold text-orange-600 mt-1">{{ attendanceRateLabel }}</p>
          <p :class="['text-[11px] font-medium mt-0.5', comparison.color]">{{ comparison.text }}</p>
        </div>
        <div class="bg-white rounded-2xl p-4 border border-orange-100 shadow-sm">
          <p class="text-xs text-gray-500 font-medium">Tổng số buổi đã dạy</p>
          <p class="text-2xl font-bold text-gray-800 mt-1">{{ report.lessonsTaught }} Buổi</p>
          <p class="text-[11px] text-gray-400 mt-0.5">Kế hoạch: {{ report.totalLessons }} buổi</p>
        </div>
        <div class="bg-white rounded-2xl p-4 border border-orange-100 shadow-sm">
          <p class="text-xs text-gray-500 font-medium">Lượt vắng có phép</p>
          <p class="text-2xl font-bold text-blue-600 mt-1">{{ report.excused }} Lượt</p>
          <p class="text-[11px] text-gray-400 mt-0.5">Phụ huynh đã báo trước</p>
        </div>
        <div class="bg-white rounded-2xl p-4 border border-orange-100 shadow-sm">
          <p class="text-xs text-gray-500 font-medium">Lượt vắng không phép</p>
          <p class="text-2xl font-bold text-red-500 mt-1">{{ report.absent }} Lượt</p>
          <p class="text-[11px] text-red-600 font-medium mt-0.5">
            {{ report.absent > 0 ? 'Cần liên hệ xác minh' : 'Không có vi phạm' }}
          </p>
        </div>
      </div>

      <!-- ─── Attendance Graph & Warnings ────────────────────────────────── -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Attendance Chart -->
        <div class="lg:col-span-2 bg-white rounded-2xl border border-orange-100 p-5 shadow-sm space-y-4">
          <div class="flex items-center justify-between pb-3 border-b border-orange-100">
            <h3 class="text-sm font-bold text-orange-950">Biểu đồ tỷ lệ có mặt theo từng buổi học</h3>
            <span class="text-xs text-orange-600 font-semibold bg-orange-50 px-2 py-0.5 rounded-md">
              {{ selectedClassName }}
            </span>
          </div>

          <!-- Empty state -->
          <div
            v-if="report.bars.length === 0"
            class="h-44 flex flex-col items-center justify-center gap-2 text-center"
          >
            <div class="w-12 h-12 rounded-full bg-orange-50 flex items-center justify-center text-orange-400">
              <ChartBarIcon class="w-6 h-6" />
            </div>
            <p class="text-sm font-semibold text-gray-700">Chưa có dữ liệu điểm danh</p>
            <p class="text-xs text-gray-400 max-w-sm">
              Chưa có buổi học nào trong khoảng thời gian đã chọn được điểm danh.
            </p>
          </div>

          <!-- Pure CSS Bar Chart -->
          <div v-else class="h-44 flex items-end gap-2 pt-4">
            <div
              v-for="(b, i) in report.bars"
              :key="i"
              class="flex-1 flex flex-col items-center gap-1 group"
              :title="b.title"
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
            Học sinh vắng nhiều (≥ 2 buổi)
          </h3>
          <p class="text-xs text-gray-500">Giáo viên nên liên hệ với phụ huynh để hỗ trợ kèm bài bù.</p>

          <div v-if="report.absentStudents.length === 0" class="pt-3 text-center py-6">
            <div class="w-12 h-12 rounded-full bg-emerald-50 flex items-center justify-center text-emerald-500 mx-auto">
              <CheckBadgeIcon class="w-6 h-6" />
            </div>
            <p class="text-xs font-semibold text-gray-600 mt-2">Không có học sinh vắng nhiều</p>
            <p class="text-[11px] text-gray-400 mt-0.5">Tình hình chuyên cần của lớp đang ổn định.</p>
          </div>

          <div v-else class="space-y-2 pt-2">
            <div
              v-for="st in report.absentStudents"
              :key="st.id"
              class="p-2.5 rounded-xl border border-red-100 bg-red-50/40 flex items-center justify-between text-xs"
            >
              <div>
                <p class="font-bold text-gray-900">{{ st.name }}</p>
                <p class="text-[11px] text-gray-500">
                  Vắng: <strong class="text-red-600">{{ st.absentCount }} buổi</strong>
                  ({{ st.excused }} có phép<span v-if="st.unexcused > 0">, {{ st.unexcused }} không phép</span>)
                </p>
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
    </template>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import {
  ArrowDownTrayIcon,
  ExclamationTriangleIcon,
  ArrowPathIcon,
  ChartBarIcon,
  CheckBadgeIcon,
} from '@heroicons/vue/24/outline'
import { useUserState } from '@/services/user-state'
import { getProfileId } from '@/services/auth-storage'
import classService from '@/services/class.service'
import lessonService from '@/services/lesson.service'
import attendanceService from '@/services/attendance.service'
import teacherService from '@/services/teacher.service'

const { fetchProfile } = useUserState()

const classes = ref([])
const selectedClassId = ref(null)
const timeRange = ref('month')

const isLoadingClasses = ref(false)
const isLoadingReport = ref(false)
const loadError = ref('')

const report = ref({
  rate: null,
  marked: 0,
  present: 0,
  late: 0,
  absent: 0,
  excused: 0,
  lessonsTaught: 0,
  totalLessons: 0,
  lessonsWithAttendance: 0,
  bars: [],
  absentStudents: [],
})

const previousRate = ref(null)
const previousMarked = ref(0)

let reportRequestId = 0

// ─── Computed labels ─────────────────────────────────────────────────────────
const selectedClass = computed(() => classes.value.find((c) => c.id === selectedClassId.value) || null)
const selectedClassName = computed(() => selectedClass.value?.name || 'Chưa chọn lớp')

const timeRangeOptions = computed(() => {
  const now = new Date()
  const year = now.getFullYear()
  const month = now.getMonth()
  const prev = new Date(year, month - 1, 1)
  const term = getRangeFor('term')

  return [
    { value: 'month', label: `Tháng này (Tháng ${month + 1}/${year})` },
    { value: 'last_month', label: `Tháng trước (Tháng ${prev.getMonth() + 1}/${prev.getFullYear()})` },
    { value: 'term', label: `Học kỳ 1 (${term.start.getFullYear()} - ${term.end.getFullYear()})` },
  ]
})

const selectedRangeLabel = computed(() => {
  return timeRangeOptions.value.find((o) => o.value === timeRange.value)?.label || ''
})

const attendanceRateLabel = computed(() => {
  return report.value.rate === null ? '—' : `${report.value.rate.toFixed(1)}%`
})

const comparison = computed(() => {
  if (timeRange.value === 'term') {
    return { text: 'Số liệu tích lũy học kỳ 1', color: 'text-gray-400' }
  }
  if (report.value.rate === null || previousRate.value === null || previousMarked.value === 0) {
    return { text: 'Chưa có dữ liệu so sánh kỳ trước', color: 'text-gray-400' }
  }

  const diff = Math.round((report.value.rate - previousRate.value) * 10) / 10
  if (diff === 0) {
    return { text: 'Không đổi so với kỳ trước', color: 'text-gray-500' }
  }

  const comparisonRange = getComparisonRange(timeRange.value)
  const label = comparisonRange?.label || 'kỳ trước'
  return diff > 0
    ? { text: `↑ Tăng ${diff.toFixed(1)}% so với ${label}`, color: 'text-emerald-600' }
    : { text: `↓ Giảm ${Math.abs(diff).toFixed(1)}% so với ${label}`, color: 'text-red-500' }
})

// ─── Load classes ────────────────────────────────────────────────────────────
onMounted(async () => {
  await loadClasses()
})

async function loadClasses() {
  isLoadingClasses.value = true
  loadError.value = ''

  try {
    const userProfile = await fetchProfile()
    let teacherProfileId = getProfileId()
    const role = (userProfile?.role || '').toLowerCase()
    const teacherFullName = (userProfile?.fullName || '').trim().toLowerCase()
    const teacherPhone = (userProfile?.phone || '').trim()

    if (!teacherProfileId && role === 'teacher') {
      try {
        const teachersData = await teacherService.getAll()
        const rawTeachers = Array.isArray(teachersData) ? teachersData : (teachersData.items || [])
        const matched = rawTeachers.find((t) =>
          (teacherPhone && t.phone === teacherPhone) ||
          (teacherFullName && t.fullName && t.fullName.trim().toLowerCase() === teacherFullName) ||
          (userProfile?.accountId && t.accountId === userProfile.accountId)
        )
        if (matched) teacherProfileId = matched.id
      } catch (e) {
        console.warn('Không thể tra cứu Teacher Profile:', e)
      }
    }

    const assigned = await classService.getAssignedClasses(userProfile, teacherProfileId)
    classes.value = Array.isArray(assigned) ? assigned : []

    if (classes.value.length > 0) {
      selectedClassId.value = classes.value[0].id
      await loadReport()
    } else {
      resetReport()
    }
  } catch (err) {
    console.error('Lỗi khi tải danh sách lớp học:', err)
    loadError.value = 'Không thể tải danh sách lớp học. Vui lòng kiểm tra kết nối và thử lại.'
  } finally {
    isLoadingClasses.value = false
  }
}

// ─── Load attendance report ──────────────────────────────────────────────────
async function loadReport() {
  if (!selectedClassId.value) {
    resetReport()
    return
  }

  const requestId = ++reportRequestId
  isLoadingReport.value = true
  loadError.value = ''

  try {
    const lessonsData = await lessonService.getByClass(selectedClassId.value)
    const lessons = (Array.isArray(lessonsData) ? lessonsData : (lessonsData?.items || []))
      .sort((a, b) => lessonTimeValue(a) - lessonTimeValue(b))

    const currentRange = getRangeFor(timeRange.value)
    const comparisonRange = getComparisonRange(timeRange.value)

    const rangeLessons = lessons.filter((l) => isInRange(l, currentRange))
    const previousLessons = comparisonRange ? lessons.filter((l) => isInRange(l, comparisonRange)) : []

    // Gộp các buổi cần lấy điểm danh (kỳ hiện tại + kỳ so sánh) để tránh gọi trùng
    const lessonIds = [...new Set([...rangeLessons, ...previousLessons].map((l) => l.id))]
    const detailMap = new Map()

    await Promise.all(
      lessonIds.map(async (lessonId) => {
        try {
          detailMap.set(lessonId, await attendanceService.getByLesson(lessonId))
        } catch {
          detailMap.set(lessonId, null)
        }
      })
    )

    // Bỏ kết quả cũ nếu người dùng đã đổi lớp/khoảng thời gian trong lúc chờ
    if (requestId !== reportRequestId) return

    const summary = summarizeRange(rangeLessons, detailMap)
    const now = new Date()

    report.value = {
      ...summary,
      totalLessons: rangeLessons.filter((l) => !['CANCELED', 'HOLIDAY'].includes(String(l.status || '').toUpperCase())).length,
      lessonsTaught: rangeLessons.filter((l) => isTaught(l, now)).length,
    }

    if (comparisonRange) {
      const prevSummary = summarizeRange(previousLessons, detailMap)
      previousRate.value = prevSummary.rate
      previousMarked.value = prevSummary.marked
    } else {
      previousRate.value = null
      previousMarked.value = 0
    }
  } catch (err) {
    if (requestId !== reportRequestId) return
    console.error('Lỗi khi tải báo cáo chuyên cần:', err)
    loadError.value = 'Không thể tải dữ liệu chuyên cần của lớp. Vui lòng thử lại.'
    resetReport()
  } finally {
    if (requestId === reportRequestId) isLoadingReport.value = false
  }
}

function resetReport() {
  report.value = {
    rate: null,
    marked: 0,
    present: 0,
    late: 0,
    absent: 0,
    excused: 0,
    lessonsTaught: 0,
    totalLessons: 0,
    lessonsWithAttendance: 0,
    bars: [],
    absentStudents: [],
  }
  previousRate.value = null
  previousMarked.value = 0
}

function summarizeRange(lessons, detailMap) {
  const summary = {
    marked: 0,
    present: 0,
    late: 0,
    absent: 0,
    excused: 0,
    lessonsWithAttendance: 0,
    bars: [],
    absentStudents: [],
  }

  const studentMap = new Map()

  lessons.forEach((lesson) => {
    const detail = detailMap.get(lesson.id)
    if (!detail) return

    const present = detail.presentCount || 0
    const absent = detail.absentCount || 0
    const late = detail.lateCount || 0
    const excused = detail.excusedCount || 0
    const marked = present + absent + late + excused

    if (marked === 0) return

    summary.lessonsWithAttendance += 1
    summary.present += present
    summary.absent += absent
    summary.late += late
    summary.excused += excused
    summary.marked += marked

    const attended = present + late
    summary.bars.push({
      lesson: `B${summary.bars.length + 1}`,
      pct: Math.round((attended / marked) * 100),
      title: `${formatDate(lesson.lessonDate)} · ${attended}/${marked} học sinh có mặt`,
    })

    ;(detail.students || []).forEach((student) => {
      const status = String(student.status || '').toUpperCase()
      if (status !== 'ABSENT' && status !== 'EXCUSED') return

      const entry = studentMap.get(student.studentId) || {
        id: student.studentId,
        name: student.studentName || 'Học sinh',
        absentCount: 0,
        excused: 0,
        unexcused: 0,
      }

      entry.absentCount += 1
      if (status === 'EXCUSED') entry.excused += 1
      else entry.unexcused += 1

      studentMap.set(student.studentId, entry)
    })
  })

  summary.rate = summary.marked > 0
    ? Math.round(((summary.present + summary.late) / summary.marked) * 1000) / 10
    : null

  summary.absentStudents = [...studentMap.values()]
    .filter((s) => s.absentCount >= 2)
    .sort((a, b) => b.absentCount - a.absentCount || a.name.localeCompare(b.name, 'vi'))

  return summary
}

// ─── Date range helpers ──────────────────────────────────────────────────────
function getRangeFor(key) {
  const now = new Date()
  const year = now.getFullYear()
  const month = now.getMonth()

  if (key === 'last_month') {
    return { start: new Date(year, month - 1, 1), end: new Date(year, month, 1) }
  }

  if (key === 'term') {
    // Năm học bắt đầu từ tháng 9; học kỳ 1 kéo dài đến giữa tháng 1 năm sau
    const startYear = month >= 7 ? year : year - 1
    return { start: new Date(startYear, 8, 1), end: new Date(startYear + 1, 0, 16) }
  }

  return { start: new Date(year, month, 1), end: new Date(year, month + 1, 1) }
}

function getComparisonRange(key) {
  const now = new Date()
  const year = now.getFullYear()
  const month = now.getMonth()

  if (key === 'month') {
    const start = new Date(year, month - 1, 1)
    return { start, end: new Date(year, month, 1), label: `tháng ${start.getMonth() + 1}/${start.getFullYear()}` }
  }

  if (key === 'last_month') {
    const start = new Date(year, month - 2, 1)
    return { start, end: new Date(year, month - 1, 1), label: `tháng ${start.getMonth() + 1}/${start.getFullYear()}` }
  }

  return null
}

function isInRange(lesson, range) {
  const d = toDateSafe(lesson.lessonDate)
  return Boolean(d && d >= range.start && d < range.end)
}

function isTaught(lesson, now) {
  const status = String(lesson.status || '').toUpperCase()
  if (status === 'CANCELED' || status === 'HOLIDAY') return false
  if (status === 'COMPLETED') return true

  const end = toDateSafe(lesson.endTime) || toDateSafe(lesson.lessonDate)
  return Boolean(end && end < now)
}

function toDateSafe(value) {
  if (!value) return null
  const d = new Date(value)
  return isNaN(d.getTime()) ? null : d
}

function lessonTimeValue(lesson) {
  const d = toDateSafe(lesson.startTime) || toDateSafe(lesson.lessonDate)
  return d ? d.getTime() : 0
}

function formatDate(value) {
  const d = toDateSafe(value)
  return d ? d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' }) : ''
}

// ─── Export CSV (mở được bằng Excel) ─────────────────────────────────────────
function exportReport() {
  if (!selectedClassId.value || report.value.marked === 0) return

  const rows = [
    ['Báo cáo chuyên cần lớp học'],
    ['Lớp', selectedClassName.value],
    ['Thời gian', selectedRangeLabel.value],
    ['Tỷ lệ chuyên cần', attendanceRateLabel.value],
    ['Số buổi đã dạy', `${report.value.lessonsTaught}/${report.value.totalLessons}`],
    ['Tổng lượt điểm danh', report.value.marked],
    ['Có mặt', report.value.present],
    ['Đi trễ', report.value.late],
    ['Vắng có phép', report.value.excused],
    ['Vắng không phép', report.value.absent],
    [],
    ['STT', 'Buổi', 'Ngày / Thông tin', 'Tỷ lệ có mặt (%)'],
    ...report.value.bars.map((b, idx) => [idx + 1, b.lesson, b.title, b.pct]),
    [],
    ['STT', 'Học sinh', 'Số buổi vắng', 'Có phép', 'Không phép'],
    ...report.value.absentStudents.map((s, idx) => [idx + 1, s.name, s.absentCount, s.excused, s.unexcused]),
  ]

  const csv = rows.map((row) => row.map(csvCell).join(',')).join('\r\n')
  const blob = new Blob(['\ufeff' + csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')

  link.href = url
  link.download = `bao-cao-chuyen-can-lop-${selectedClassId.value}-${timeRange.value}.csv`
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)
}

function csvCell(value) {
  const text = value === null || value === undefined ? '' : String(value)
  return `"${text.replace(/"/g, '""')}"`
}
</script>
