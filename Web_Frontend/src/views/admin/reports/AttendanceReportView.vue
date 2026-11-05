<template>
  <div class="space-y-5">
    <!-- ─── Header ───────────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-orange-950">Thống kê chuyên cần toàn hệ thống</h1>
        <p class="text-xs text-gray-500 mt-0.5">
          Theo dõi tỷ lệ đi học theo lớp, phát hiện học sinh vắng nhiều để kịp thời phối hợp phụ huynh
        </p>
      </div>
      <div class="flex items-center gap-2">
        <button
          type="button"
          class="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-orange-100 hover:bg-orange-200 disabled:opacity-50 disabled:cursor-not-allowed text-orange-900 text-xs font-semibold transition-colors cursor-pointer"
          :disabled="isLoadingReport || report.marked === 0"
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
        @click="loadData"
        class="px-3 py-1.5 rounded-lg bg-white border border-rose-200 font-semibold text-rose-700 hover:bg-rose-100 transition-colors cursor-pointer"
      >
        Thử lại
      </button>
    </div>

    <!-- ─── Filter Toolbar ───────────────────────────────────────────────── -->
    <div class="bg-white rounded-2xl border border-orange-100 p-4 shadow-sm">
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <div>
          <label class="block text-xs font-semibold text-gray-600 mb-1">Phạm vi lớp học</label>
          <select
            v-model="selectedClassId"
            :disabled="isLoading || classes.length === 0"
            class="w-full text-xs font-semibold bg-orange-50/60 border border-orange-200 text-orange-950 rounded-xl px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 disabled:opacity-60 cursor-pointer"
            @change="loadReport"
          >
            <option :value="null">
              {{ isLoading ? 'Đang tải danh sách lớp...' : 'Tất cả lớp học' }}
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
      v-if="isLoading || isLoadingReport"
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
          <div class="flex items-center justify-between gap-2 pb-3 border-b border-orange-100">
            <h3 class="text-sm font-bold text-orange-950">Tỷ lệ chuyên cần theo thứ trong tuần</h3>
            <span class="text-xs text-orange-600 font-semibold bg-orange-50 px-2 py-0.5 rounded-md truncate max-w-[50%]">
              {{ selectedScopeLabel }}
            </span>
          </div>

          <!-- Empty state -->
          <div
            v-if="report.lessonsWithAttendance === 0"
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
              v-for="(bar, i) in report.bars"
              :key="i"
              class="flex-1 flex flex-col items-center gap-1 group"
              :title="bar.title"
            >
              <span class="text-[9px] font-bold text-orange-800 opacity-0 group-hover:opacity-100 transition-opacity">
                {{ bar.pct !== null ? `${bar.pct}%` : '—' }}
              </span>
              <div
                class="w-full rounded-t-md transition-all duration-300"
                :class="bar.pct !== null
                  ? 'bg-gradient-to-t from-orange-400 to-amber-300 group-hover:from-orange-500 group-hover:to-amber-400'
                  : 'bg-orange-100'"
                :style="{ height: bar.pct !== null ? `${Math.max(bar.pct, 6)}%` : '6px' }"
              ></div>
              <span class="text-[10px] text-gray-400 font-medium">{{ bar.label }}</span>
            </div>
          </div>

          <p class="text-[11px] text-gray-400">
            * {{ report.lessonsWithAttendance }} buổi đã điểm danh · {{ report.marked }} lượt ghi nhận
          </p>
        </div>

        <!-- Warning list: Students with multiple absences -->
        <div class="bg-white rounded-2xl border border-orange-100 p-5 shadow-sm space-y-3">
          <h3 class="text-sm font-bold text-orange-950 flex items-center gap-1.5">
            <ExclamationTriangleIcon class="w-4 h-4 text-amber-500" />
            Học sinh vắng nhiều (≥ 2 buổi)
          </h3>
          <p class="text-xs text-gray-500">Ưu tiên liên hệ phụ huynh để hỗ trợ kèm bài bù.</p>

          <div v-if="report.absentStudents.length === 0" class="pt-3 text-center py-6">
            <div class="w-12 h-12 rounded-full bg-emerald-50 flex items-center justify-center text-emerald-500 mx-auto">
              <CheckBadgeIcon class="w-6 h-6" />
            </div>
            <p class="text-xs font-semibold text-gray-600 mt-2">Không có học sinh vắng nhiều</p>
            <p class="text-[11px] text-gray-400 mt-0.5">Tình hình chuyên cần đang ổn định.</p>
          </div>

          <div v-else class="space-y-2 pt-2">
            <div
              v-for="st in visibleAbsentStudents"
              :key="st.id"
              class="p-2.5 rounded-xl border border-red-100 bg-red-50/40 flex items-center justify-between gap-2 text-xs"
            >
              <div class="min-w-0">
                <p class="font-bold text-gray-900 truncate">{{ st.name }}</p>
                <p class="text-[11px] text-gray-500 truncate" :title="st.classNames">
                  {{ st.classNames || 'Chưa xác định lớp' }}
                </p>
                <p class="text-[11px] text-gray-500">
                  Vắng: <strong class="text-red-600">{{ st.absentCount }} buổi</strong>
                  ({{ st.excused }} có phép<span v-if="st.unexcused > 0">, {{ st.unexcused }} không phép</span>)
                </p>
              </div>
              <router-link
                to="/users/students"
                class="px-2 py-1 rounded-lg bg-white border border-red-200 text-red-700 hover:bg-red-50 text-[11px] font-bold flex-shrink-0"
              >
                Hồ sơ
              </router-link>
            </div>
            <p v-if="report.absentStudents.length > visibleAbsentStudents.length" class="text-[11px] text-gray-400 text-center pt-1">
              + {{ report.absentStudents.length - visibleAbsentStudents.length }} học sinh khác
            </p>
          </div>
        </div>
      </div>

      <!-- ─── Per-class breakdown ────────────────────────────────────────── -->
      <div class="bg-white rounded-2xl border border-orange-100 shadow-sm overflow-hidden">
        <div class="flex items-center justify-between px-5 py-4 border-b border-orange-100">
          <div>
            <h3 class="text-sm font-bold text-orange-950">Chuyên cần theo lớp học</h3>
            <p class="text-xs text-gray-500 mt-0.5">Các lớp có tỷ lệ chuyên cần thấp được ưu tiên hiển thị trước</p>
          </div>
          <span class="text-xs bg-orange-100 text-orange-600 px-2 py-1 rounded-full font-medium hidden sm:inline">
            {{ report.classBreakdown.length }} lớp
          </span>
        </div>

        <div v-if="report.classBreakdown.length === 0" class="py-10 text-center">
          <div class="w-12 h-12 rounded-full bg-orange-50 flex items-center justify-center text-orange-400 mx-auto">
            <AcademicCapIcon class="w-6 h-6" />
          </div>
          <p class="text-sm font-semibold text-gray-700 mt-2">Chưa có dữ liệu lớp học</p>
          <p class="text-xs text-gray-400 mt-0.5">Không có lớp học nào trong khoảng thời gian đã chọn.</p>
        </div>

        <div v-else class="overflow-x-auto">
          <table class="w-full text-left border-collapse">
            <thead>
              <tr class="bg-orange-50/70 border-b border-orange-100 text-[11px] font-bold text-orange-950 uppercase tracking-wider">
                <th class="py-3.5 px-4">Lớp học</th>
                <th class="py-3.5 px-4">Giáo viên</th>
                <th class="py-3.5 px-4 text-center">Sĩ số</th>
                <th class="py-3.5 px-4 text-center">Buổi đã điểm danh</th>
                <th class="py-3.5 px-4 text-center">Tỷ lệ chuyên cần</th>
                <th class="py-3.5 px-4 text-center">Có phép</th>
                <th class="py-3.5 px-4 text-center">Không phép</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 text-xs text-gray-700">
              <tr
                v-for="row in report.classBreakdown"
                :key="row.id"
                class="hover:bg-orange-50/40 transition-colors"
              >
                <td class="py-3 px-4">
                  <router-link
                    :to="{ name: 'class-detail', params: { id: row.id } }"
                    class="font-bold text-gray-900 hover:text-orange-600 transition-colors"
                  >
                    {{ row.name }}
                  </router-link>
                </td>
                <td class="py-3 px-4 text-gray-600">
                  {{ row.teacherName || 'Chưa phân công' }}
                </td>
                <td class="py-3 px-4 text-center">{{ row.studentsCount }}</td>
                <td class="py-3 px-4 text-center">
                  {{ row.lessonsWithAttendance }}<span class="text-gray-400">/{{ row.lessons }}</span>
                </td>
                <td class="py-3 px-4 text-center">
                  <span :class="['font-bold', rateClass(row.rate)]">
                    {{ row.rate === null ? '—' : `${row.rate.toFixed(1)}%` }}
                  </span>
                </td>
                <td class="py-3 px-4 text-center text-blue-600 font-semibold">{{ row.excused }}</td>
                <td class="py-3 px-4 text-center text-red-500 font-semibold">{{ row.absent }}</td>
              </tr>
            </tbody>
          </table>
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
  AcademicCapIcon,
} from '@heroicons/vue/24/outline'
import classService from '@/services/class.service'
import lessonService from '@/services/lesson.service'
import attendanceService from '@/services/attendance.service'

const WEEKDAY_LABELS = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN']
const ABSENT_PREVIEW_LIMIT = 8

// ─── State ──────────────────────────────────────────────────────────────────
const classes = ref([])
const allLessons = ref([])
const selectedClassId = ref(null)
const timeRange = ref('month')

const isLoading = ref(false)
const isLoadingReport = ref(false)
const loadError = ref('')

const previousRate = ref(null)
const previousMarked = ref(0)

let reportRequestId = 0

function emptyReport() {
  return {
    rate: null,
    marked: 0,
    present: 0,
    late: 0,
    absent: 0,
    excused: 0,
    lessonsTaught: 0,
    totalLessons: 0,
    lessonsWithAttendance: 0,
    bars: WEEKDAY_LABELS.map((label) => ({
      label,
      pct: null,
      lessons: 0,
      title: `${label}: chưa có dữ liệu`,
    })),
    absentStudents: [],
    classBreakdown: [],
  }
}

const report = ref(emptyReport())

// ─── Computed ───────────────────────────────────────────────────────────────
const selectedClass = computed(
  () => classes.value.find((c) => Number(c.id) === Number(selectedClassId.value)) || null
)

const selectedScopeLabel = computed(() => {
  return selectedClass.value ? selectedClass.value.name : 'Tất cả lớp học'
})

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
  return timeRangeOptions.value.find((option) => option.value === timeRange.value)?.label || ''
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

const visibleAbsentStudents = computed(() => {
  return report.value.absentStudents.slice(0, ABSENT_PREVIEW_LIMIT)
})

// ─── Lifecycle ──────────────────────────────────────────────────────────────
onMounted(() => {
  loadData()
})

// ─── Data loading ───────────────────────────────────────────────────────────
async function loadData() {
  isLoading.value = true
  loadError.value = ''

  try {
    const [classesData, lessonsData] = await Promise.all([
      classService.getAll(),
      lessonService.getAll(),
    ])

    classes.value = normalizeList(classesData).map((cls) => ({
      ...cls,
      id: Number(cls.id),
      activeStudentsCount: Number(cls.activeStudentsCount) || 0,
    }))

    allLessons.value = normalizeList(lessonsData).map((lesson) => ({
      ...lesson,
      id: Number(lesson.id),
      classId: Number(lesson.classId),
      attendancesCount: Number(lesson.attendancesCount) || 0,
    }))

    if (
      selectedClassId.value !== null &&
      !classes.value.some((cls) => cls.id === Number(selectedClassId.value))
    ) {
      selectedClassId.value = null
    }

    await loadReport()
  } catch (err) {
    console.error('Lỗi khi tải dữ liệu thống kê chuyên cần:', err)
    loadError.value = 'Không thể tải dữ liệu thống kê. Vui lòng kiểm tra kết nối và thử lại.'
    resetReport()
  } finally {
    isLoading.value = false
  }
}

async function loadReport() {
  const requestId = ++reportRequestId
  isLoadingReport.value = true
  loadError.value = ''

  try {
    const currentRange = getRangeFor(timeRange.value)
    const comparisonRange = getComparisonRange(timeRange.value)

    const lessonsInScope = allLessons.value.filter(
      (lesson) => selectedClassId.value === null || Number(lesson.classId) === Number(selectedClassId.value)
    )

    const currentLessons = lessonsInScope.filter(
      (lesson) => !isNonTeaching(lesson) && isInRange(lesson, currentRange)
    )
    const previousLessons = comparisonRange
      ? lessonsInScope.filter((lesson) => !isNonTeaching(lesson) && isInRange(lesson, comparisonRange))
      : []

    const lessonIds = [
      ...new Set(
        [...currentLessons, ...previousLessons]
          .filter((lesson) => lesson.attendancesCount > 0)
          .map((lesson) => lesson.id)
      ),
    ]

    const detailMap = await fetchLessonDetails(lessonIds)

    if (requestId !== reportRequestId) return

    const summary = summarizeRange(currentLessons, detailMap)
    const now = new Date()

    report.value = {
      ...summary,
      totalLessons: currentLessons.length,
      lessonsTaught: currentLessons.filter((lesson) => isTaught(lesson, now)).length,
    }

    if (comparisonRange) {
      const previousSummary = summarizeRange(previousLessons, detailMap)
      previousRate.value = previousSummary.rate
      previousMarked.value = previousSummary.marked
    } else {
      previousRate.value = null
      previousMarked.value = 0
    }
  } catch (err) {
    if (requestId !== reportRequestId) return
    console.error('Lỗi khi tải báo cáo chuyên cần:', err)
    loadError.value = 'Không thể tải dữ liệu chuyên cần. Vui lòng thử lại.'
    resetReport()
  } finally {
    if (requestId === reportRequestId) isLoadingReport.value = false
  }
}

function resetReport() {
  report.value = emptyReport()
  previousRate.value = null
  previousMarked.value = 0
}

async function fetchLessonDetails(lessonIds, concurrency = 6) {
  const detailMap = new Map()

  for (let i = 0; i < lessonIds.length; i += concurrency) {
    const batch = lessonIds.slice(i, i + concurrency)
    const results = await Promise.all(
      batch.map(async (lessonId) => {
        try {
          return [lessonId, await attendanceService.getByLesson(lessonId)]
        } catch {
          return [lessonId, null]
        }
      })
    )
    results.forEach(([lessonId, detail]) => detailMap.set(lessonId, detail))
  }

  return detailMap
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
    classBreakdown: [],
  }

  const studentMap = new Map()
  const classMap = new Map()
  const buckets = WEEKDAY_LABELS.map((label) => ({ label, attended: 0, marked: 0, lessons: 0 }))
  const classById = new Map(classes.value.map((cls) => [cls.id, cls]))

  lessons.forEach((lesson) => {
    const classId = Number(lesson.classId)
    const cls = classById.get(classId)

    const classEntry = classMap.get(classId) || {
      id: classId,
      name: cls?.name || lesson.className || `Lớp #${classId}`,
      teacherName: cls?.teacherName || '',
      studentsCount: Number(cls?.activeStudentsCount) || 0,
      lessons: 0,
      lessonsWithAttendance: 0,
      marked: 0,
      present: 0,
      late: 0,
      absent: 0,
      excused: 0,
      rate: null,
    }

    classEntry.lessons += 1
    classMap.set(classId, classEntry)

    const detail = detailMap.get(lesson.id)
    if (!detail) return

    const present = Number(detail.presentCount) || 0
    const late = Number(detail.lateCount) || 0
    const absent = Number(detail.absentCount) || 0
    const excused = Number(detail.excusedCount) || 0
    const marked = present + late + absent + excused

    if (marked === 0) return

    summary.lessonsWithAttendance += 1
    summary.present += present
    summary.late += late
    summary.absent += absent
    summary.excused += excused
    summary.marked += marked

    classEntry.lessonsWithAttendance += 1
    classEntry.present += present
    classEntry.late += late
    classEntry.absent += absent
    classEntry.excused += excused
    classEntry.marked += marked

    const date = toDateSafe(lesson.lessonDate)
    if (date) {
      const index = (date.getDay() + 6) % 7
      buckets[index].attended += present + late
      buckets[index].marked += marked
      buckets[index].lessons += 1
    }

    ;(detail.students || []).forEach((student) => {
      const status = String(student.status || '').toUpperCase()
      if (status !== 'ABSENT' && status !== 'EXCUSED') return

      const key = student.studentId ?? student.studentName ?? 'unknown'
      const entry = studentMap.get(key) || {
        id: key,
        name: student.studentName || 'Học sinh',
        classNames: new Set(),
        absentCount: 0,
        excused: 0,
        unexcused: 0,
      }

      entry.absentCount += 1
      if (status === 'EXCUSED') entry.excused += 1
      else entry.unexcused += 1

      const className = cls?.name || lesson.className
      if (className) entry.classNames.add(className)

      studentMap.set(key, entry)
    })
  })

  summary.rate = summary.marked > 0
    ? Math.round(((summary.present + summary.late) / summary.marked) * 1000) / 10
    : null

  summary.bars = buckets.map((bucket) => ({
    label: bucket.label,
    pct: bucket.marked > 0 ? Math.round((bucket.attended / bucket.marked) * 100) : null,
    lessons: bucket.lessons,
    title: bucket.marked > 0
      ? `${bucket.label}: ${bucket.attended}/${bucket.marked} lượt có mặt · ${bucket.lessons} buổi`
      : `${bucket.label}: chưa có dữ liệu`,
  }))

  summary.absentStudents = [...studentMap.values()]
    .filter((student) => student.absentCount >= 2)
    .map((student) => ({
      ...student,
      classNames: [...student.classNames].join(', '),
    }))
    .sort((a, b) => b.absentCount - a.absentCount || a.name.localeCompare(b.name, 'vi'))

  summary.classBreakdown = [...classMap.values()]
    .map((entry) => ({
      ...entry,
      rate: entry.marked > 0
        ? Math.round(((entry.present + entry.late) / entry.marked) * 1000) / 10
        : null,
    }))
    .sort((a, b) => {
      if (a.rate === null && b.rate === null) return a.name.localeCompare(b.name, 'vi')
      if (a.rate === null) return 1
      if (b.rate === null) return -1
      return a.rate - b.rate || a.name.localeCompare(b.name, 'vi')
    })

  return summary
}

// ─── Date range helpers ─────────────────────────────────────────────────────
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
  const date = toDateSafe(lesson.lessonDate)
  return Boolean(date && date >= range.start && date < range.end)
}

function isNonTeaching(lesson) {
  const status = String(lesson.status || '').toUpperCase()
  return status === 'CANCELED' || status === 'HOLIDAY'
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
  const date = new Date(value)
  return isNaN(date.getTime()) ? null : date
}

// ─── Small helpers ──────────────────────────────────────────────────────────
function normalizeList(data) {
  if (Array.isArray(data)) return data
  return data?.items ?? data?.data ?? []
}

function rateClass(rate) {
  if (rate === null || rate === undefined) return 'text-gray-400'
  if (rate >= 90) return 'text-emerald-600'
  if (rate >= 80) return 'text-amber-600'
  return 'text-red-500'
}

// ─── Export CSV (mở được bằng Excel) ─────────────────────────────────────────
function exportReport() {
  if (report.value.marked === 0) return

  const rows = [
    ['Báo cáo chuyên cần toàn hệ thống'],
    ['Phạm vi', selectedScopeLabel.value],
    ['Thời gian', selectedRangeLabel.value],
    ['Tỷ lệ chuyên cần', attendanceRateLabel.value],
    ['Số buổi đã dạy', `${report.value.lessonsTaught}/${report.value.totalLessons}`],
    ['Tổng lượt điểm danh', report.value.marked],
    ['Có mặt', report.value.present],
    ['Đi trễ', report.value.late],
    ['Vắng có phép', report.value.excused],
    ['Vắng không phép', report.value.absent],
    [],
    ['STT', 'Lớp', 'Giáo viên', 'Sĩ số', 'Buổi đã điểm danh', 'Tỷ lệ (%)', 'Có mặt', 'Đi trễ', 'Có phép', 'Không phép'],
    ...report.value.classBreakdown.map((row, idx) => [
      idx + 1,
      row.name,
      row.teacherName || 'Chưa phân công',
      row.studentsCount,
      `${row.lessonsWithAttendance}/${row.lessons}`,
      row.rate === null ? '' : row.rate,
      row.present,
      row.late,
      row.excused,
      row.absent,
    ]),
    [],
    ['STT', 'Thứ', 'Số buổi', 'Tỷ lệ có mặt (%)'],
    ...report.value.bars.map((bar, idx) => [idx + 1, bar.label, bar.lessons, bar.pct === null ? '' : bar.pct]),
    [],
    ['STT', 'Học sinh', 'Lớp', 'Số buổi vắng', 'Có phép', 'Không phép'],
    ...report.value.absentStudents.map((student, idx) => [
      idx + 1,
      student.name,
      student.classNames,
      student.absentCount,
      student.excused,
      student.unexcused,
    ]),
  ]

  const csv = rows.map((row) => row.map(csvCell).join(',')).join('\r\n')
  const blob = new Blob(['\ufeff' + csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')

  const scope = selectedClassId.value === null ? 'toan-he-thong' : `lop-${selectedClassId.value}`
  link.href = url
  link.download = `bao-cao-chuyen-can-${scope}-${timeRange.value}.csv`
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
