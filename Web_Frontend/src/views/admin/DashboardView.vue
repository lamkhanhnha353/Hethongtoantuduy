<template>
  <div class="space-y-6">
    <!-- ─── Page header ──────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-orange-950">Tổng quan hệ thống</h1>
        <p class="text-xs text-gray-500 mt-0.5">
          Số liệu được tổng hợp trực tiếp từ lớp học, giáo viên và dữ liệu điểm danh
        </p>
      </div>
      <div class="flex items-center gap-2">
        <span
          v-if="isLoading"
          class="inline-flex items-center gap-1.5 px-3 py-2 rounded-xl bg-orange-100 text-orange-800 text-xs font-semibold"
        >
          <ArrowPathIcon class="w-4 h-4 animate-spin" />
          Đang đồng bộ...
        </span>
        <button
          type="button"
          class="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-orange-100 hover:bg-orange-200 disabled:opacity-50 disabled:cursor-not-allowed text-orange-900 text-xs font-semibold transition-colors cursor-pointer"
          :disabled="isLoading"
          @click="loadDashboard"
        >
          <ArrowPathIcon :class="['w-4 h-4 text-orange-600', isLoading ? 'animate-spin' : '']" />
          Làm mới
        </button>
        <router-link
          to="/reports/attendance"
          class="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-orange-500 hover:bg-orange-600 text-white text-xs font-semibold shadow-sm transition-colors"
        >
          <ChartBarIcon class="w-4 h-4" />
          Báo cáo chuyên cần
        </router-link>
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
        @click="loadDashboard"
        class="px-3 py-1.5 rounded-lg bg-white border border-rose-200 font-semibold text-rose-700 hover:bg-rose-100 transition-colors cursor-pointer"
      >
        Thử lại
      </button>
    </div>

    <!-- ─── Stats cards ──────────────────────────────────────────────────── -->
    <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
      <div
        v-for="stat in stats"
        :key="stat.label"
        class="bg-white rounded-2xl p-5 shadow-sm border border-orange-100 flex items-center gap-4 hover:shadow-md transition-shadow"
      >
        <div :class="['w-12 h-12 rounded-xl flex items-center justify-center flex-shrink-0', stat.bg]">
          <component :is="stat.icon" class="w-6 h-6 text-white" />
        </div>
        <div class="min-w-0">
          <p class="text-xs text-gray-500 font-medium">{{ stat.label }}</p>
          <p class="text-2xl font-bold text-gray-800 mt-0.5">{{ stat.value }}</p>
          <p :class="['text-xs mt-0.5 font-medium truncate', stat.subClass]" :title="stat.sub">
            {{ stat.sub }}
          </p>
        </div>
      </div>
    </div>

    <!-- ─── Charts row ───────────────────────────────────────────────────── -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-4">
      <!-- Attendance chart -->
      <div class="bg-white rounded-2xl p-5 shadow-sm border border-orange-100">
        <div class="flex items-center justify-between mb-4">
          <h2 class="font-semibold text-orange-900">Biểu đồ Chuyên cần</h2>
          <span class="text-xs bg-orange-100 text-orange-600 px-2 py-1 rounded-full font-medium">
            Tháng {{ currentMonth }}
          </span>
        </div>

        <!-- Empty state -->
        <div
          v-if="attendanceSummary.lessons === 0"
          class="h-44 flex flex-col items-center justify-center gap-2 text-center"
        >
          <div class="w-12 h-12 rounded-full bg-orange-50 flex items-center justify-center text-orange-400">
            <ChartBarIcon class="w-6 h-6" />
          </div>
          <p class="text-sm font-semibold text-gray-700">Chưa có dữ liệu điểm danh</p>
          <p class="text-xs text-gray-400 max-w-sm">
            Chưa có buổi học nào trong tháng này được điểm danh.
          </p>
        </div>

        <!-- Pure CSS Bar Chart -->
        <div v-else class="h-44 flex items-end gap-2 pt-4">
          <div
            v-for="(bar, i) in attendanceSummary.weekdayBars"
            :key="i"
            class="flex-1 flex flex-col items-center gap-1 group"
            :title="bar.title"
          >
            <span class="text-[9px] font-bold text-orange-800 opacity-0 group-hover:opacity-100 transition-opacity">
              {{ bar.pct !== null ? `${bar.pct}%` : '—' }}
            </span>
            <div
              class="w-full rounded-t-md transition-all duration-500"
              :class="bar.pct !== null
                ? 'bg-gradient-to-t from-orange-400 to-amber-300 group-hover:from-orange-500 group-hover:to-amber-400'
                : 'bg-orange-100'"
              :style="{ height: bar.pct !== null ? `${Math.max(bar.pct, 6)}%` : '6px' }"
            ></div>
            <span class="text-[10px] text-gray-400">{{ bar.label }}</span>
          </div>
        </div>

        <p class="text-xs text-gray-400 mt-3">
          * Tỷ lệ chuyên cần trung bình:
          <span class="font-semibold text-orange-600">{{ attendanceRateLabel }}</span>
          <span v-if="attendanceSummary.lessons > 0">
            · {{ attendanceSummary.lessons }} buổi đã điểm danh
          </span>
        </p>
      </div>

      <!-- Class status distribution -->
      <div class="bg-white rounded-2xl p-5 shadow-sm border border-orange-100">
        <div class="flex items-center justify-between mb-4">
          <h2 class="font-semibold text-orange-900">Phân bố Lớp học</h2>
          <span class="text-xs bg-blue-100 text-blue-600 px-2 py-1 rounded-full font-medium">
            {{ statusTotal }} lớp
          </span>
        </div>

        <div v-if="statusTotal === 0" class="h-44 flex flex-col items-center justify-center gap-2 text-center">
          <div class="w-12 h-12 rounded-full bg-orange-50 flex items-center justify-center text-orange-400">
            <BookOpenIcon class="w-6 h-6" />
          </div>
          <p class="text-sm font-semibold text-gray-700">Chưa có lớp học nào</p>
          <p class="text-xs text-gray-400 max-w-sm">Mở lớp mới để bắt đầu theo dõi tình hình đào tạo.</p>
        </div>

        <div v-else class="flex items-center gap-6">
          <div class="relative w-36 h-36 flex-shrink-0">
            <svg viewBox="0 0 36 36" class="w-full h-full -rotate-90">
              <circle cx="18" cy="18" r="15.9" fill="none" stroke="#ffedd5" stroke-width="3.5" />
              <circle
                v-for="seg in statusSegments"
                :key="seg.key"
                cx="18" cy="18" r="15.9" fill="none"
                :stroke="seg.color" stroke-width="3.5"
                :stroke-dasharray="seg.dash"
                :stroke-dashoffset="seg.offset"
              />
            </svg>
            <div class="absolute inset-0 flex flex-col items-center justify-center">
              <span class="text-2xl font-bold text-orange-600">{{ statusTotal }}</span>
              <span class="text-[10px] text-gray-400">lớp học</span>
            </div>
          </div>
          <div class="space-y-2.5 text-xs flex-1 min-w-0">
            <div
              v-for="group in statusGroups"
              :key="group.key"
              class="flex items-center justify-between gap-2"
            >
              <span class="flex items-center gap-2 text-gray-600 min-w-0">
                <span class="w-3 h-3 rounded-full flex-shrink-0" :style="{ backgroundColor: group.color }"></span>
                <span class="truncate">{{ group.label }}</span>
              </span>
              <span class="font-semibold text-gray-800 flex-shrink-0">
                {{ group.count }}
                <span class="text-gray-400 font-normal">({{ statusPercent(group.count) }}%)</span>
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ─── Recent activity ──────────────────────────────────────────────── -->
    <div class="bg-white rounded-2xl shadow-sm border border-orange-100 overflow-hidden">
      <div class="flex items-center justify-between px-5 py-4 border-b border-orange-100">
        <h2 class="font-semibold text-orange-900">Hoạt động gần đây</h2>
        <router-link
          to="/reports/attendance"
          class="text-xs text-orange-500 hover:text-orange-700 font-medium transition-colors"
        >
          Xem báo cáo
        </router-link>
      </div>

      <div v-if="recentActivities.length === 0" class="py-10 text-center flex flex-col items-center justify-center gap-2">
        <div class="w-12 h-12 rounded-full bg-orange-50 flex items-center justify-center text-orange-400">
          <ClockIcon class="w-6 h-6" />
        </div>
        <p class="text-sm font-semibold text-gray-700">Chưa có hoạt động nào</p>
        <p class="text-xs text-gray-400 max-w-sm">
          Hoạt động điểm danh, lịch học và lớp mới sẽ xuất hiện tại đây.
        </p>
      </div>

      <div v-else class="divide-y divide-orange-50">
        <div
          v-for="activity in recentActivities"
          :key="activity.id"
          class="flex items-center gap-4 px-5 py-3 hover:bg-orange-50/50 transition-colors"
        >
          <div :class="['w-9 h-9 rounded-full flex items-center justify-center flex-shrink-0', activity.color]">
            <component :is="activity.icon" class="w-4 h-4 text-white" />
          </div>
          <div class="flex-1 min-w-0">
            <p class="text-sm text-gray-700 leading-snug">{{ activity.message }}</p>
            <p class="text-xs text-gray-400 mt-0.5">{{ activity.time }}</p>
          </div>
          <span
            :class="[
              'text-xs px-2 py-0.5 rounded-full font-medium flex-shrink-0',
              activity.badgeClass,
            ]"
          >{{ activity.badge }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import {
  BookOpenIcon,
  UserGroupIcon,
  AcademicCapIcon,
  ChartBarIcon,
  ClipboardDocumentCheckIcon,
  CalendarDaysIcon,
  PlusCircleIcon,
  XCircleIcon,
  ClockIcon,
  ArrowPathIcon,
} from '@heroicons/vue/24/outline'
import classService from '@/services/class.service'
import teacherService from '@/services/teacher.service'
import lessonService from '@/services/lesson.service'
import attendanceService from '@/services/attendance.service'

const WEEKDAY_LABELS = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN']
const ATTENDANCE_DETAIL_LIMIT = 60

// ─── State ──────────────────────────────────────────────────────────────────
const isLoading = ref(false)
const loadError = ref('')
const classes = ref([])
const teachers = ref([])
const allLessons = ref([])
const previousMonthRate = ref(null)

const emptySummary = () => ({
  marked: 0,
  present: 0,
  late: 0,
  absent: 0,
  excused: 0,
  lessons: 0,
  rate: null,
  weekdayBars: WEEKDAY_LABELS.map((label) => ({ label, pct: null, title: `${label}: chưa có dữ liệu` })),
})

const attendanceSummary = ref(emptySummary())

let dashboardRequestId = 0

// ─── Computed ───────────────────────────────────────────────────────────────
const currentMonth = computed(() => new Date().getMonth() + 1)

const attendanceRateLabel = computed(() => {
  return attendanceSummary.value.rate === null ? '—' : `${attendanceSummary.value.rate.toFixed(1)}%`
})

const stats = computed(() => {
  const totalClasses = classes.value.length
  const openClasses = classes.value.filter((c) => ['active', 'full'].includes(normalizeStatus(c.status))).length
  const studentCount = classes.value.reduce((sum, c) => sum + (Number(c.activeStudentsCount) || 0), 0)
  const assignedTeachers = new Set(
    classes.value.map((c) => (c.teacherName || '').trim()).filter(Boolean)
  ).size
  const teacherCount = teachers.value.length || assignedTeachers

  let attendanceSub = 'Chưa có dữ liệu điểm danh tháng này'
  let attendanceSubClass = 'text-gray-400'

  if (attendanceSummary.value.rate !== null) {
    if (previousMonthRate.value !== null) {
      const diff = Math.round((attendanceSummary.value.rate - previousMonthRate.value) * 10) / 10
      if (diff > 0) {
        attendanceSub = `↑ Tăng ${diff.toFixed(1)}% so với tháng trước`
        attendanceSubClass = 'text-green-500'
      } else if (diff < 0) {
        attendanceSub = `↓ Giảm ${Math.abs(diff).toFixed(1)}% so với tháng trước`
        attendanceSubClass = 'text-red-400'
      } else {
        attendanceSub = 'Không đổi so với tháng trước'
        attendanceSubClass = 'text-gray-500'
      }
    } else {
      attendanceSub = `Tháng này · ${attendanceSummary.value.lessons} buổi đã điểm danh`
      attendanceSubClass = 'text-orange-600'
    }
  }

  return [
    {
      label: 'Lớp học đang mở',
      value: `${openClasses} Lớp`,
      sub: `${totalClasses} lớp trong hệ thống`,
      subClass: 'text-orange-600',
      icon: BookOpenIcon,
      bg: 'bg-gradient-to-br from-orange-400 to-orange-500',
    },
    {
      label: 'Tổng sĩ số học viên',
      value: `${studentCount} HS`,
      sub: `${classes.value.filter((c) => (Number(c.activeStudentsCount) || 0) > 0).length} lớp có học viên`,
      subClass: 'text-blue-600',
      icon: UserGroupIcon,
      bg: 'bg-gradient-to-br from-blue-400 to-indigo-500',
    },
    {
      label: 'Giáo viên',
      value: `${teacherCount} GV`,
      sub: assignedTeachers > 0 ? `${assignedTeachers} giáo viên đang phụ trách lớp` : 'Chưa phân công giáo viên',
      subClass: 'text-emerald-600',
      icon: AcademicCapIcon,
      bg: 'bg-gradient-to-br from-emerald-400 to-teal-500',
    },
    {
      label: 'Tỷ lệ chuyên cần tháng này',
      value: attendanceRateLabel.value,
      sub: attendanceSub,
      subClass: attendanceSubClass,
      icon: ChartBarIcon,
      bg: 'bg-gradient-to-br from-purple-400 to-pink-500',
    },
  ]
})

const statusGroups = computed(() => {
  const counts = { active: 0, full: 0, upcoming: 0, inactive: 0 }

  classes.value.forEach((c) => {
    const status = normalizeStatus(c.status)
    if (status === 'active') counts.active += 1
    else if (status === 'full') counts.full += 1
    else if (status === 'upcoming') counts.upcoming += 1
    else counts.inactive += 1
  })

  return [
    { key: 'active', label: 'Đang mở', color: '#22c55e', count: counts.active },
    { key: 'full', label: 'Đã đầy', color: '#fb923c', count: counts.full },
    { key: 'upcoming', label: 'Sắp mở', color: '#3b82f6', count: counts.upcoming },
    { key: 'inactive', label: 'Tạm dừng / khác', color: '#9ca3af', count: counts.inactive },
  ]
})

const statusTotal = computed(() => statusGroups.value.reduce((sum, group) => sum + group.count, 0))

const statusSegments = computed(() => {
  const total = statusTotal.value
  if (total === 0) return []

  let offset = 0
  return statusGroups.value
    .filter((group) => group.count > 0)
    .map((group) => {
      const percent = (group.count / total) * 100
      const segment = {
        ...group,
        percent: Math.round(percent * 10) / 10,
        dash: `${percent} ${100 - percent}`,
        offset: -offset,
      }
      offset += percent
      return segment
    })
})

const recentActivities = computed(() => {
  const items = []
  const now = Date.now()
  const upcomingWindow = now + 7 * 24 * 60 * 60 * 1000

  allLessons.value.forEach((lesson) => {
    const lessonTime = lessonTimeValue(lesson)
    if (!lessonTime) return

    const status = normalizeStatus(lesson.status)
    const attendanceCount = Number(lesson.attendancesCount) || 0

    if (attendanceCount > 0) {
      items.push({
        id: `lesson-${lesson.id}`,
        sortTime: lessonTime,
        message: `Điểm danh lớp ${lesson.className || 'học'}: ${attendanceCount} học sinh đã được ghi nhận`,
        time: relativeTime(lessonTime),
        icon: ClipboardDocumentCheckIcon,
        color: 'bg-orange-400',
        badge: 'Điểm danh',
        badgeClass: 'bg-orange-100 text-orange-600',
      })
    } else if (status === 'canceled') {
      items.push({
        id: `lesson-${lesson.id}`,
        sortTime: lessonTime,
        message: `Buổi học của lớp ${lesson.className || 'học'} đã bị hủy`,
        time: relativeTime(lessonTime),
        icon: XCircleIcon,
        color: 'bg-gray-400',
        badge: 'Đã hủy',
        badgeClass: 'bg-gray-100 text-gray-500',
      })
    } else if (lessonTime > now && lessonTime <= upcomingWindow && status !== 'holiday') {
      items.push({
        id: `lesson-${lesson.id}`,
        sortTime: lessonTime,
        message: `Lớp ${lesson.className || 'học'} có buổi học ${formatUpcoming(lesson)}`,
        time: 'Sắp diễn ra',
        icon: CalendarDaysIcon,
        color: 'bg-blue-400',
        badge: 'Sắp tới',
        badgeClass: 'bg-blue-100 text-blue-600',
      })
    }
  })

  classes.value.forEach((cls) => {
    const created = toDateSafe(cls.createdAt)
    if (!created) return

    items.push({
      id: `class-${cls.id}`,
      sortTime: created.getTime(),
      message: `Lớp ${cls.name} vừa được tạo${cls.teacherName ? ` · GV ${cls.teacherName}` : ''}`,
      time: relativeTime(created.getTime()),
      icon: PlusCircleIcon,
      color: 'bg-green-400',
      badge: 'Lớp mới',
      badgeClass: 'bg-green-100 text-green-600',
    })
  })

  // Ưu tiên các hoạt động gần thời điểm hiện tại nhất (quá khứ gần hoặc sắp tới gần)
  return items
    .sort((a, b) => Math.abs(a.sortTime - now) - Math.abs(b.sortTime - now))
    .slice(0, 6)
})

// ─── Lifecycle ──────────────────────────────────────────────────────────────
onMounted(() => {
  loadDashboard()
})

// ─── Data loading ───────────────────────────────────────────────────────────
async function loadDashboard() {
  const requestId = ++dashboardRequestId
  isLoading.value = true
  loadError.value = ''

  try {
    const results = await Promise.allSettled([
      classService.getAll(),
      teacherService.getAll(),
      lessonService.getAll(),
    ])

    if (requestId !== dashboardRequestId) return

    const [classesResult, teachersResult, lessonsResult] = results

    if (classesResult.status === 'rejected') {
      throw classesResult.reason
    }

    classes.value = normalizeList(classesResult.value)
    teachers.value = teachersResult.status === 'fulfilled' ? normalizeList(teachersResult.value) : []
    allLessons.value = lessonsResult.status === 'fulfilled' ? normalizeList(lessonsResult.value) : []

    if (lessonsResult.status === 'rejected') {
      console.warn('Không thể tải danh sách buổi học:', lessonsResult.reason)
    }

    try {
      await loadAttendanceSummary()
    } catch (err) {
      console.warn('Không thể tổng hợp dữ liệu điểm danh:', err)
    }
  } catch (err) {
    if (requestId !== dashboardRequestId) return
    console.error('Lỗi khi tải dữ liệu tổng quan admin:', err)
    loadError.value = 'Không thể tải dữ liệu tổng quan. Vui lòng kiểm tra kết nối và thử lại.'
  } finally {
    if (requestId === dashboardRequestId) isLoading.value = false
  }
}

async function loadAttendanceSummary() {
  const currentRange = monthRange(0)
  const previousRange = monthRange(-1)

  const byNewest = (a, b) => lessonTimeValue(b) - lessonTimeValue(a)

  const currentLessons = allLessons.value
    .filter((lesson) => {
      const status = normalizeStatus(lesson.status)
      return status !== 'canceled' && status !== 'holiday' && isInRange(lesson, currentRange) && (Number(lesson.attendancesCount) || 0) > 0
    })
    .sort(byNewest)
    .slice(0, ATTENDANCE_DETAIL_LIMIT)

  const previousLessons = allLessons.value
    .filter((lesson) => {
      const status = normalizeStatus(lesson.status)
      return status !== 'canceled' && status !== 'holiday' && isInRange(lesson, previousRange) && (Number(lesson.attendancesCount) || 0) > 0
    })
    .sort(byNewest)
    .slice(0, ATTENDANCE_DETAIL_LIMIT)

  const lessonIds = [...new Set([...currentLessons, ...previousLessons].map((lesson) => lesson.id))]
  const detailMap = await fetchLessonDetails(lessonIds)

  attendanceSummary.value = summarizeLessons(currentLessons, detailMap)
  previousMonthRate.value = summarizeLessons(previousLessons, detailMap).rate
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

function summarizeLessons(lessons, detailMap) {
  const summary = emptySummary()
  const buckets = WEEKDAY_LABELS.map((label) => ({ label, attended: 0, marked: 0, lessons: 0 }))

  lessons.forEach((lesson) => {
    const detail = detailMap.get(lesson.id)
    if (!detail) return

    const present = Number(detail.presentCount) || 0
    const late = Number(detail.lateCount) || 0
    const absent = Number(detail.absentCount) || 0
    const excused = Number(detail.excusedCount) || 0
    const marked = present + late + absent + excused

    if (marked === 0) return

    summary.lessons += 1
    summary.present += present
    summary.late += late
    summary.absent += absent
    summary.excused += excused
    summary.marked += marked

    const date = toDateSafe(lesson.lessonDate)
    if (date) {
      const index = (date.getDay() + 6) % 7
      buckets[index].attended += present + late
      buckets[index].marked += marked
      buckets[index].lessons += 1
    }
  })

  summary.rate = summary.marked > 0
    ? Math.round(((summary.present + summary.late) / summary.marked) * 1000) / 10
    : null

  summary.weekdayBars = buckets.map((bucket) => ({
    label: bucket.label,
    pct: bucket.marked > 0 ? Math.round((bucket.attended / bucket.marked) * 100) : null,
    title: bucket.marked > 0
      ? `${bucket.label}: ${bucket.attended}/${bucket.marked} lượt có mặt · ${bucket.lessons} buổi`
      : `${bucket.label}: chưa có dữ liệu`,
  }))

  return summary
}

// ─── Helpers ────────────────────────────────────────────────────────────────
function normalizeList(data) {
  if (Array.isArray(data)) return data
  return data?.items ?? data?.data ?? []
}

function normalizeStatus(status) {
  return String(status || '').toLowerCase()
}

function monthRange(offset = 0) {
  const now = new Date()
  return {
    start: new Date(now.getFullYear(), now.getMonth() + offset, 1),
    end: new Date(now.getFullYear(), now.getMonth() + offset + 1, 1),
  }
}

function isInRange(lesson, range) {
  const date = toDateSafe(lesson.lessonDate)
  return Boolean(date && date >= range.start && date < range.end)
}

function toDateSafe(value) {
  if (!value) return null
  const date = new Date(value)
  return isNaN(date.getTime()) ? null : date
}

function lessonTimeValue(lesson) {
  const date = toDateSafe(lesson.startTime) || toDateSafe(lesson.lessonDate)
  return date ? date.getTime() : 0
}

function statusPercent(count) {
  const total = statusTotal.value
  return total > 0 ? Math.round((count / total) * 100) : 0
}

function relativeTime(timestamp) {
  const diff = Date.now() - timestamp

  if (diff < 0) return 'Sắp tới'
  const minutes = Math.floor(diff / 60000)
  if (minutes < 1) return 'Vừa xong'
  if (minutes < 60) return `${minutes} phút trước`

  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours} giờ trước`

  const days = Math.floor(hours / 24)
  if (days < 7) return `${days} ngày trước`

  const date = new Date(timestamp)
  return date.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function formatUpcoming(lesson) {
  const start = toDateSafe(lesson.startTime)
  const date = start || toDateSafe(lesson.lessonDate)
  if (!date) return 'sắp tới'

  const today = new Date()
  const isToday = date.toDateString() === today.toDateString()
  const dateLabel = isToday
    ? 'hôm nay'
    : `vào ${date.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' })}`

  if (!start) return dateLabel

  const time = start.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', hour12: false })
  return `${dateLabel} lúc ${time}`
}
</script>
