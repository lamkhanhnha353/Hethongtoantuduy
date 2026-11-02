<template>
  <div class="space-y-6 max-w-7xl mx-auto pb-12">
    <!-- ─── Header ───────────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div>
        <nav class="flex items-center gap-2 text-xs font-medium text-orange-600 mb-1">
          <span>Giáo viên</span>
          <ChevronRightIcon class="w-3.5 h-3.5 text-orange-400" />
          <span>Thời khóa biểu cá nhân</span>
        </nav>
        <h1 class="text-2xl md:text-3xl font-bold text-gray-900 tracking-tight">Thời khóa biểu giảng dạy</h1>
        <p class="text-xs md:text-sm text-gray-500 mt-0.5">
          Theo dõi lịch dạy, phòng học và thời gian các ca giảng dạy được phân công trong tuần
        </p>
      </div>

      <div class="flex items-center gap-2 flex-wrap">
        <button
          type="button"
          class="inline-flex items-center gap-1.5 px-3.5 py-2.5 rounded-xl bg-orange-100 hover:bg-orange-200/80 text-orange-800 text-xs font-semibold transition-colors cursor-pointer disabled:opacity-50"
          @click="loadTeacherSchedule"
          :disabled="isLoading"
        >
          <ArrowPathIcon class="w-4 h-4 text-orange-600" :class="{ 'animate-spin': isLoading }" />
          <span>Đồng bộ lịch dạy</span>
        </button>

        <router-link
          to="/teacher/classes"
          class="inline-flex items-center gap-1.5 px-4 py-2.5 rounded-xl bg-orange-500 hover:bg-orange-600 text-white text-xs font-bold shadow-xs transition-colors cursor-pointer"
        >
          <BookOpenIcon class="w-4 h-4" />
          <span>Lớp phụ trách</span>
        </router-link>
      </div>
    </div>

    <!-- ─── Week Navigator & Filter Toolbar ──────────────────────────────── -->
    <div class="bg-white rounded-3xl border border-orange-100 p-4 shadow-sm flex flex-col lg:flex-row lg:items-center justify-between gap-4">
      <!-- Week Navigation -->
      <div class="flex items-center gap-2 flex-wrap">
        <button
          class="p-2 rounded-xl border border-orange-200 text-orange-700 hover:bg-orange-50 transition-colors cursor-pointer"
          @click="prevWeek"
          title="Tuần trước"
        >
          <ChevronLeftIcon class="w-4 h-4" />
        </button>
        <span class="text-xs md:text-sm font-bold text-gray-900 px-3 py-1.5 rounded-xl bg-orange-50/70 border border-orange-100 min-w-[240px] text-center">
          {{ currentWeekRange }}
        </span>
        <button
          class="p-2 rounded-xl border border-orange-200 text-orange-700 hover:bg-orange-50 transition-colors cursor-pointer"
          @click="nextWeek"
          title="Tuần sau"
        >
          <ChevronRightIcon class="w-4 h-4" />
        </button>
        <button
          class="px-3.5 py-1.5 rounded-xl text-xs font-bold bg-orange-100/80 text-orange-900 border border-orange-200 hover:bg-orange-200 transition-colors cursor-pointer"
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
          class="text-xs font-semibold bg-white border border-orange-200 text-gray-800 rounded-xl px-3 py-2 focus:outline-none focus:ring-2 focus:ring-orange-300 max-w-[200px]"
        >
          <option :value="null">Tất cả lớp phụ trách ({{ myClasses.length }})</option>
          <option v-for="cls in myClasses" :key="cls.id" :value="cls.id">
            {{ cls.name }}
          </option>
        </select>

        <!-- View mode -->
        <div class="flex items-center bg-orange-100/70 p-1 rounded-xl text-xs font-semibold">
          <button
            @click="viewMode = 'grid'"
            :class="[
              'px-3 py-1.5 rounded-lg transition-all cursor-pointer',
              viewMode === 'grid' ? 'bg-orange-500 text-white shadow-xs' : 'text-orange-900 hover:text-orange-950',
            ]"
          >
            Lưới tuần
          </button>
          <button
            @click="viewMode = 'list'"
            :class="[
              'px-3 py-1.5 rounded-lg transition-all cursor-pointer',
              viewMode === 'list' ? 'bg-orange-500 text-white shadow-xs' : 'text-orange-900 hover:text-orange-950',
            ]"
          >
            Danh sách
          </button>
        </div>
      </div>
    </div>

    <!-- ─── Loading State ────────────────────────────────────────────────── -->
    <div v-if="isLoading" class="bg-white rounded-3xl border border-orange-100 p-12 text-center space-y-3 shadow-sm">
      <ArrowPathIcon class="w-8 h-8 animate-spin text-orange-500 mx-auto" />
      <p class="text-sm font-semibold text-orange-900">Đang đồng bộ thời khóa biểu từ máy chủ...</p>
    </div>

    <!-- ─── Grid View (Weekly Calendar) ──────────────────────────────────── -->
    <div
      v-else-if="viewMode === 'grid'"
      class="bg-white rounded-3xl border border-orange-100 p-4 sm:p-5 shadow-sm overflow-x-auto"
    >
      <div class="min-w-[900px]">
        <!-- Days header (Mon -> Sun) -->
        <div class="grid grid-cols-7 gap-2 pb-3 border-b border-orange-100 text-center">
          <div
            v-for="day in weekDays"
            :key="day.dateStr"
            :class="[
              'p-2.5 rounded-2xl text-xs transition-colors',
              day.isToday
                ? 'bg-orange-500 text-white font-bold shadow-xs'
                : 'bg-orange-50/50 text-gray-700 border border-orange-100/60'
            ]"
          >
            <p :class="day.isToday ? 'text-orange-100' : 'text-orange-700'" class="font-bold uppercase tracking-wider text-[11px]">
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
            class="space-y-2.5 border-r border-dashed border-orange-100/80 last:border-r-0 pr-1.5"
          >
            <div
              v-for="session in day.sessions"
              :key="session.id"
              class="p-3 rounded-2xl border border-orange-200/80 bg-gradient-to-br from-orange-50/70 to-amber-50/50 hover:bg-orange-100/80 hover:shadow-xs transition-all text-left shadow-2xs group flex flex-col justify-between"
            >
              <div>
                <div class="flex items-center justify-between gap-1 text-[10px] font-bold text-orange-800">
                  <span class="truncate bg-orange-100/80 px-1.5 py-0.5 rounded text-[9px]">{{ session.room || 'Phòng chính' }}</span>
                  <span class="font-mono">{{ session.time }}</span>
                </div>

                <h4 class="text-xs font-bold text-gray-900 mt-1.5 line-clamp-2 group-hover:text-orange-600 transition-colors">
                  {{ session.className }}
                </h4>

                <p v-if="session.lessonNumber" class="text-[10px] font-semibold text-orange-700 mt-0.5">
                  {{ session.lessonNumber }}
                </p>
              </div>

              <div class="flex items-center justify-between text-[11px] text-gray-500 mt-3 pt-2 border-t border-orange-100/80">
                <span>{{ session.studentsCount || 0 }} HS</span>
                <router-link
                  :to="{ path: '/teacher/attendance', query: { lessonId: session.id, classId: session.classId } }"
                  class="text-[10px] font-bold text-orange-600 hover:text-orange-800 bg-white px-2 py-0.5 rounded-lg border border-orange-200 transition-colors"
                >
                  Điểm danh
                </router-link>
              </div>
            </div>

            <!-- Empty slot marker -->
            <div
              v-if="day.sessions.length === 0"
              class="h-32 rounded-2xl border border-dashed border-orange-100/80 flex items-center justify-center text-center p-2"
            >
              <span class="text-[11px] text-gray-300 font-medium">Không có ca dạy</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ─── List View ────────────────────────────────────────────────────── -->
    <div v-else class="space-y-3">
      <div
        v-if="allFilteredSessions.length === 0"
        class="bg-white rounded-3xl border border-orange-100 p-12 text-center shadow-sm"
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
        class="bg-white rounded-3xl border border-orange-100 p-4 sm:p-5 shadow-sm hover:border-orange-300 transition-all flex flex-col sm:flex-row sm:items-center justify-between gap-4"
      >
        <div class="flex items-start gap-3.5">
          <div class="w-14 h-14 rounded-2xl bg-orange-100 text-orange-950 font-bold text-xs flex flex-col items-center justify-center flex-shrink-0 shadow-2xs border border-orange-200">
            <span class="text-[11px] font-bold uppercase text-orange-800">{{ session.dayName }}</span>
            <span class="text-sm font-bold text-gray-900 mt-0.5">{{ session.dateStr }}</span>
          </div>

          <div>
            <div class="flex items-center gap-2 flex-wrap">
              <h3 class="text-sm font-bold text-gray-900">{{ session.className }}</h3>
              <span class="text-[11px] font-bold px-2 py-0.5 rounded-full bg-orange-100 text-orange-800">
                {{ session.room || 'Phòng học chính' }}
              </span>
              <span v-if="session.lessonNumber" class="text-[11px] font-bold px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-800">
                {{ session.lessonNumber }}
              </span>
            </div>

            <p class="text-xs text-gray-500 mt-1 flex items-center gap-3 flex-wrap">
              <span>⏰ Thời gian: <strong class="text-gray-800">{{ session.time }}</strong></span>
              <span>👥 Sĩ số: <strong class="text-gray-800">{{ session.studentsCount || 0 }} học sinh</strong></span>
            </p>
          </div>
        </div>

        <div class="flex items-center gap-2 self-end sm:self-center">
          <router-link
            :to="{ path: '/teacher/attendance', query: { lessonId: session.id, classId: session.classId } }"
            class="px-4 py-2 rounded-xl bg-orange-500 hover:bg-orange-600 text-white text-xs font-bold shadow-xs transition-colors cursor-pointer"
          >
            Điểm danh
          </router-link>
          <router-link
            :to="{ path: '/teacher/students', query: { classId: session.classId } }"
            class="px-3 py-2 rounded-xl border border-orange-200 text-orange-800 hover:bg-orange-50 text-xs font-semibold transition-colors cursor-pointer"
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
