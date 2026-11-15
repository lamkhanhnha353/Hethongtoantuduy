<template>
  <div class="space-y-6 max-w-7xl mx-auto pb-12">
    <!-- ─── Header ───────────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div>
        <nav class="flex items-center gap-2 text-xs font-medium text-stone-500 mb-1">
          <span class="text-orange-600 font-semibold">Giáo viên</span>
          <ChevronRightIcon class="w-3.5 h-3.5 text-stone-400" />
          <span>Thời khóa biểu cá nhân</span>
        </nav>
        <h1 class="text-2xl md:text-3xl font-bold text-stone-900 tracking-tight">Thời khóa biểu giảng dạy</h1>
        <p class="text-xs md:text-sm text-stone-500 mt-0.5">
          Theo dõi lịch dạy, phòng học và thời gian các ca giảng dạy được phân công trong tuần
        </p>
      </div>

      <div class="flex items-center gap-2 flex-wrap">
        <button
          type="button"
          class="inline-flex items-center gap-1.5 h-9 px-3.5 rounded-md bg-white hover:bg-stone-100 text-stone-800 border border-stone-300 hover:border-stone-400 text-xs font-semibold transition-colors cursor-pointer disabled:opacity-50"
          @click="loadTeacherSchedule"
          :disabled="isLoading"
        >
          <ArrowPathIcon class="w-4 h-4 text-orange-600" :class="{ 'animate-spin': isLoading }" />
          <span>Đồng bộ lịch dạy</span>
        </button>

        <router-link
          to="/teacher/classes"
          class="inline-flex items-center gap-1.5 h-9 px-3.5 rounded-md bg-orange-600 hover:bg-orange-700 active:bg-orange-800 text-white border border-orange-700 text-xs font-semibold shadow-xs transition-colors cursor-pointer"
        >
          <BookOpenIcon class="w-4 h-4" />
          <span>Lớp phụ trách</span>
        </router-link>
      </div>
    </div>

    <!-- ─── Week Navigator & Filter Toolbar ──────────────────────────────── -->
    <div class="bg-white rounded-lg border border-stone-300 p-4 shadow-[0_1px_2px_rgba(0,0,0,0.04)] flex flex-col lg:flex-row lg:items-center justify-between gap-4">
      <!-- Week Navigation -->
      <div class="flex items-center gap-2 flex-wrap">
        <button
          class="h-9 w-9 inline-flex items-center justify-center rounded-md border border-stone-300 bg-white text-stone-700 hover:bg-stone-100 hover:border-stone-400 transition-colors cursor-pointer"
          @click="prevWeek"
          title="Tuần trước"
        >
          <ChevronLeftIcon class="w-4 h-4" />
        </button>
        <span class="h-9 inline-flex items-center justify-center text-xs md:text-sm font-bold text-stone-900 px-3 rounded-md bg-stone-50 border border-stone-300 min-w-[240px] tabular-nums">
          {{ currentWeekRange }}
        </span>
        <button
          class="h-9 w-9 inline-flex items-center justify-center rounded-md border border-stone-300 bg-white text-stone-700 hover:bg-stone-100 hover:border-stone-400 transition-colors cursor-pointer"
          @click="nextWeek"
          title="Tuần sau"
        >
          <ChevronRightIcon class="w-4 h-4" />
        </button>
        <button
          class="h-9 px-3.5 rounded-md text-xs font-semibold bg-orange-50 text-orange-800 border border-orange-300 hover:bg-orange-100 transition-colors cursor-pointer"
          @click="goToCurrentWeek"
        >
          Tuần này (Hôm nay)
        </button>
      </div>

      <!-- Filters & View Toggle -->
      <div class="flex items-center gap-2.5 flex-wrap">
        <!-- Class filter -->
        <select
          v-model="selectedClassFilter"
          class="h-9 text-xs font-semibold bg-stone-50/70 border border-stone-300 text-stone-800 rounded-md px-3 focus:bg-white focus:border-orange-600 focus:ring-2 focus:ring-orange-500/20 outline-none max-w-[220px]"
        >
          <option :value="null">Tất cả lớp phụ trách ({{ myClasses.length }})</option>
          <option v-for="cls in myClasses" :key="cls.id" :value="cls.id">
            {{ cls.name }}
          </option>
        </select>

        <!-- View mode -->
        <div class="inline-flex items-center bg-stone-100 border border-stone-300 p-1 rounded-md text-xs font-semibold">
          <button
            @click="viewMode = 'grid'"
            :class="[
              'h-7 px-3 rounded transition-colors cursor-pointer',
              viewMode === 'grid' ? 'bg-orange-600 text-white shadow-xs' : 'text-stone-700 hover:text-stone-900',
            ]"
          >
            Lưới tuần
          </button>
          <button
            @click="viewMode = 'list'"
            :class="[
              'h-7 px-3 rounded transition-colors cursor-pointer',
              viewMode === 'list' ? 'bg-orange-600 text-white shadow-xs' : 'text-stone-700 hover:text-stone-900',
            ]"
          >
            Danh sách
          </button>
        </div>
      </div>
    </div>

    <!-- ─── Loading State ────────────────────────────────────────────────── -->
    <div v-if="isLoading" class="bg-white rounded-lg border border-stone-300 p-12 text-center space-y-3 shadow-[0_1px_2px_rgba(0,0,0,0.04)]">
      <ArrowPathIcon class="w-8 h-8 animate-spin text-orange-600 mx-auto" />
      <p class="text-sm font-semibold text-stone-700">Đang đồng bộ thời khóa biểu từ máy chủ...</p>
    </div>

    <!-- ─── Grid View (Weekly Calendar) ──────────────────────────────────── -->
    <div
      v-else-if="viewMode === 'grid'"
      class="bg-white rounded-lg border border-stone-300 p-4 sm:p-5 shadow-[0_1px_2px_rgba(0,0,0,0.04)] overflow-x-auto"
    >
      <div class="min-w-[900px]">
        <!-- Days header (Mon -> Sun) -->
        <div class="grid grid-cols-7 gap-2 pb-3 border-b border-stone-200 text-center">
          <div
            v-for="day in weekDays"
            :key="day.dateStr"
            :class="[
              'p-2.5 rounded-md text-xs transition-colors tabular-nums border',
              day.isToday
                ? 'bg-orange-600 text-white border-orange-700 font-bold shadow-xs'
                : 'bg-stone-100 text-stone-800 border-stone-300'
            ]"
          >
            <p :class="day.isToday ? 'text-orange-100' : 'text-stone-600'" class="font-bold uppercase tracking-wider text-[11px]">
              {{ day.name }}
            </p>
            <p class="text-sm font-bold mt-0.5">{{ day.formattedDate }}</p>
          </div>
        </div>

        <!-- Schedule cells -->
        <div class="grid grid-cols-7 gap-2 pt-3 min-h-[420px]">
          <div
            v-for="day in weekDays"
            :key="'cell-' + day.dateStr"
            class="space-y-2.5 border-r border-dashed border-stone-200 last:border-r-0 pr-1.5"
          >
            <div
              v-for="session in day.sessions"
              :key="session.id"
              class="p-3 rounded-md border border-stone-300 bg-white hover:bg-orange-50/40 hover:border-orange-400 transition-colors text-left shadow-[0_1px_2px_rgba(0,0,0,0.04)] group flex flex-col justify-between"
            >
              <div>
                <div class="flex items-center justify-between gap-1 text-[11px] font-semibold">
                  <span class="truncate bg-stone-100 border border-stone-300 text-stone-700 px-1.5 py-0.5 rounded text-[10px]">{{ session.room || 'Phòng chính' }}</span>
                  <span class="font-mono text-orange-700 tabular-nums">{{ session.time }}</span>
                </div>

                <h4 class="text-xs font-bold text-stone-900 mt-1.5 line-clamp-2 group-hover:text-orange-600 transition-colors">
                  {{ session.className }}
                </h4>

                <p v-if="session.lessonNumber" class="text-[11px] font-semibold text-stone-600 mt-0.5 tabular-nums">
                  {{ session.lessonNumber }}
                </p>
              </div>

              <div class="flex items-center justify-between text-[11px] text-stone-500 mt-3 pt-2 border-t border-stone-200">
                <span class="tabular-nums font-medium">{{ session.studentsCount || 0 }} HS</span>
                <router-link
                  :to="{ path: '/teacher/attendance', query: { lessonId: session.id, classId: session.classId } }"
                  class="text-[11px] font-semibold text-orange-700 hover:bg-orange-50 bg-white px-2 py-0.5 rounded border border-orange-300 transition-colors"
                >
                  Điểm danh
                </router-link>
              </div>
            </div>

            <!-- Empty slot marker -->
            <div
              v-if="day.sessions.length === 0"
              class="h-32 rounded-md border border-dashed border-stone-300 flex items-center justify-center text-center p-2"
            >
              <span class="text-xs text-stone-400 font-medium">Không có ca dạy</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ─── List View ────────────────────────────────────────────────────── -->
    <div v-else class="space-y-3">
      <div
        v-if="allFilteredSessions.length === 0"
        class="bg-white rounded-lg border border-stone-300 p-12 text-center shadow-[0_1px_2px_rgba(0,0,0,0.04)]"
      >
        <EmptyState
          :icon="CalendarDaysIcon"
          message="Không có lịch dạy trong tuần này"
          sub="Khi các lớp học được xếp lịch và phân công, lịch dạy của bạn sẽ hiển thị chi tiết tại đây."
        />
      </div>

      <div
        v-for="session in allFilteredSessions"
        :key="session.id"
        class="bg-white rounded-lg border border-stone-300 p-4 sm:p-5 shadow-[0_1px_2px_rgba(0,0,0,0.04)] hover:border-stone-400 transition-colors flex flex-col sm:flex-row sm:items-center justify-between gap-4"
      >
        <div class="flex items-start gap-3.5">
          <div class="w-14 h-14 rounded-md bg-orange-50 text-orange-900 font-bold text-xs flex flex-col items-center justify-center flex-shrink-0 border border-orange-200 tabular-nums">
            <span class="text-[11px] font-bold uppercase text-orange-700">{{ session.dayName }}</span>
            <span class="text-sm font-bold text-stone-900 mt-0.5">{{ session.dateStr }}</span>
          </div>

          <div>
            <div class="flex items-center gap-2 flex-wrap">
              <h3 class="text-sm font-bold text-stone-900">{{ session.className }}</h3>
              <span class="text-[11px] font-semibold px-2 py-0.5 rounded border border-stone-300 bg-stone-100 text-stone-700">
                {{ session.room || 'Phòng học chính' }}
              </span>
              <span v-if="session.lessonNumber" class="text-[11px] font-semibold px-2 py-0.5 rounded border border-emerald-300 bg-emerald-50 text-emerald-800 tabular-nums">
                {{ session.lessonNumber }}
              </span>
            </div>

            <p class="text-xs text-stone-500 mt-1 flex items-center gap-3 flex-wrap tabular-nums">
              <span>Thời gian: <strong class="text-stone-800">{{ session.time }}</strong></span>
              <span class="text-stone-300">•</span>
              <span>Sĩ số: <strong class="text-stone-800">{{ session.studentsCount || 0 }} học sinh</strong></span>
            </p>
          </div>
        </div>

        <div class="flex items-center gap-2 self-end sm:self-center">
          <router-link
            :to="{ path: '/teacher/attendance', query: { lessonId: session.id, classId: session.classId } }"
            class="inline-flex items-center h-9 px-3.5 rounded-md bg-orange-600 hover:bg-orange-700 text-white border border-orange-700 text-xs font-semibold shadow-xs transition-colors cursor-pointer"
          >
            Điểm danh
          </router-link>
          <router-link
            :to="{ path: '/teacher/students', query: { classId: session.classId } }"
            class="inline-flex items-center h-9 px-3 rounded-md bg-white border border-stone-300 text-stone-800 hover:bg-stone-100 hover:border-stone-400 text-xs font-semibold transition-colors cursor-pointer"
          >
            Học sinh
          </router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import classService from '@/services/class.service'
import lessonService from '@/services/lesson.service'
import classScheduleService from '@/services/classSchedule.service'
import teacherService from '@/services/teacher.service'
import { useUserState } from '@/services/user-state'
import { getProfileId } from '@/services/auth-storage'
import {
  CalendarDaysIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
  ArrowPathIcon,
  ChevronRightIcon as ChevronRightIconSmall,
  BookOpenIcon,
} from '@heroicons/vue/24/outline'
import EmptyState from '@/components/common/EmptyState.vue'

const route = useRoute()
const { profile, fetchProfile } = useUserState()

const viewMode = ref('grid')
const isLoading = ref(false)
const weekOffset = ref(0)
const myClasses = ref([])
const allLessons = ref([])
const selectedClassFilter = ref(null)

onMounted(async () => {
  if (route.query.classId) {
    const cId = parseInt(route.query.classId, 10)
    if (!isNaN(cId)) selectedClassFilter.value = cId
  }
  await loadTeacherSchedule()
})

async function loadTeacherSchedule() {
  isLoading.value = true
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
        const matched = rawTeachers.find(t =>
          (teacherPhone && t.phone === teacherPhone) ||
          (teacherFullName && t.fullName && t.fullName.trim().toLowerCase() === teacherFullName) ||
          (userProfile?.accountId && t.accountId === userProfile.accountId)
        )
        if (matched) teacherProfileId = matched.id
      } catch (e) {
        console.warn('Không thể tra cứu Teacher Profile:', e)
      }
    }

    // 1. Fetch assigned classes
    const assigned = await classService.getAssignedClasses(userProfile, teacherProfileId)
    myClasses.value = Array.isArray(assigned) ? assigned : []

    // 2. Fetch all lessons and recurring schedules for these classes
    const classMap = new Map()
    myClasses.value.forEach(c => classMap.set(c.id, c))

    const lessonPromises = myClasses.value.map(async (cls) => {
      try {
        const lessonsData = await lessonService.getByClass(cls.id)
        const list = Array.isArray(lessonsData) ? lessonsData : (lessonsData.items || [])

        // Sort ascending by date to number lessons properly
        const sorted = [...list].sort((a, b) => new Date(a.lessonDate).getTime() - new Date(b.lessonDate).getTime())

        return sorted.map((les, idx) => ({
          ...les,
          lessonNumber: `Buổi ${idx + 1}`,
          className: cls.name,
          room: cls.room || 'Phòng chính',
          studentsCount: cls.activeStudentsCount || 0,
        }))
      } catch {
        return []
      }
    })

    const lessonResults = await Promise.all(lessonPromises)
    allLessons.value = lessonResults.flat()
  } catch (err) {
    console.error('Lỗi khi tải thời khóa biểu giáo viên:', err)
  } finally {
    isLoading.value = false
  }
}

// ─── Week Calculation & Navigation ───────────────────────────────────────────
function getStartOfWeek(date, offsetWeeks = 0) {
  const d = new Date(date)
  // Day of week: 0 = Sun, 1 = Mon, ..., 6 = Sat
  const day = d.getDay()
  const diff = d.getDate() - day + (day === 0 ? -6 : 1) // adjust when day is sunday
  const monday = new Date(d.setDate(diff))
  monday.setHours(0, 0, 0, 0)
  if (offsetWeeks !== 0) {
    monday.setDate(monday.getDate() + offsetWeeks * 7)
  }
  return monday
}

const weekStartDate = computed(() => {
  return getStartOfWeek(new Date(), weekOffset.value)
})

const weekEndDate = computed(() => {
  const end = new Date(weekStartDate.value)
  end.setDate(end.getDate() + 6)
  end.setHours(23, 59, 59, 999)
  return end
})

const currentWeekRange = computed(() => {
  const start = weekStartDate.value
  const end = weekEndDate.value
  const startStr = formatDateDM(start)
  const endStr = formatDateDMY(end)
  return `${startStr} - ${endStr}`
})

const weekDays = computed(() => {
  const dayNames = ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'Chủ Nhật']
  const todayStr = formatYMD(new Date())

  return dayNames.map((name, idx) => {
    const currentDay = new Date(weekStartDate.value)
    currentDay.setDate(currentDay.getDate() + idx)
    const dateStr = formatYMD(currentDay)
    const formattedDate = formatDateDM(currentDay)
    const isToday = dateStr === todayStr

    // Filter sessions occurring on this date
    let sessions = allLessons.value.filter(l => {
      const lesDateStr = l.lessonDate ? l.lessonDate.slice(0, 10) : ''
      return lesDateStr === dateStr
    })

    // Filter by selectedClassFilter if any
    if (selectedClassFilter.value) {
      sessions = sessions.filter(s => s.classId === selectedClassFilter.value)
    }

    // Map time string
    const mappedSessions = sessions.map(s => {
      const timeStr = formatLessonTime(s.startTime, s.endTime)
      return {
        ...s,
        time: timeStr || 'Theo lịch lớp',
      }
    })

    return {
      name,
      dateStr,
      formattedDate,
      isToday,
      sessions: mappedSessions,
    }
  })
})

const allFilteredSessions = computed(() => {
  return weekDays.value.flatMap(day =>
    day.sessions.map(s => ({
      ...s,
      dayName: day.name,
      dateStr: day.formattedDate,
    }))
  )
})

function prevWeek() {
  weekOffset.value--
}
function nextWeek() {
  weekOffset.value++
}
function goToCurrentWeek() {
  weekOffset.value = 0
}

// ─── Format Helpers ──────────────────────────────────────────────────────────
function formatYMD(d) {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

function formatDateDM(d) {
  const day = String(d.getDate()).padStart(2, '0')
  const month = String(d.getMonth() + 1).padStart(2, '0')
  return `${day}/${month}`
}

function formatDateDMY(d) {
  const day = String(d.getDate()).padStart(2, '0')
  const month = String(d.getMonth() + 1).padStart(2, '0')
  const year = d.getFullYear()
  return `${day}/${month}/${year}`
}

function formatLessonTime(start, end) {
  if (!start) return ''
  const s = new Date(start)
  const startFormatted = !isNaN(s.getTime())
    ? s.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', hour12: false })
    : String(start).slice(0, 5)

  if (!end) return startFormatted
  const e = new Date(end)
  const endFormatted = !isNaN(e.getTime())
    ? e.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', hour12: false })
    : String(end).slice(0, 5)

  return `${startFormatted} - ${endFormatted}`
}
</script>
