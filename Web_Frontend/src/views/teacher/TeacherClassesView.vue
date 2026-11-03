<template>
  <div class="space-y-6 max-w-7xl mx-auto pb-12">
    <!-- ─── Header ───────────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div>
        <nav class="flex items-center gap-2 text-xs font-medium text-orange-600 mb-1">
          <span>Giáo viên</span>
          <ChevronRightIcon class="w-3.5 h-3.5 text-orange-400" />
          <span>Lớp học đang phụ trách</span>
        </nav>
        <h1 class="text-2xl md:text-3xl font-bold text-gray-900 tracking-tight">Lớp học đang phụ trách</h1>
        <p class="text-xs md:text-sm text-gray-500 mt-0.5">
          Quản lý tiến độ giảng dạy, thời khóa biểu, sĩ số và các hoạt động của từng lớp được phân công
        </p>
      </div>
      <div class="flex items-center gap-2 flex-wrap">
        <button
          type="button"
          @click="loadAssignedClasses"
          :disabled="isLoading"
          class="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-orange-100 hover:bg-orange-200/80 text-orange-900 text-xs font-semibold transition-colors cursor-pointer disabled:opacity-50"
        >
          <ArrowPathIcon class="w-4 h-4 text-orange-600" :class="{ 'animate-spin': isLoading }" />
          <span>Làm mới</span>
        </button>

        <router-link
          to="/teacher/students"
          class="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-orange-500 hover:bg-orange-600 text-white text-xs font-semibold shadow-xs transition-colors cursor-pointer"
        >
          <UserGroupIcon class="w-4 h-4" />
          <span>Tất cả học sinh</span>
        </router-link>
      </div>
    </div>

    <!-- ─── Filter & Search Bar ─────────────────────────────────────────── -->
    <div class="bg-white rounded-3xl border border-orange-100 p-4 shadow-sm space-y-3">
      <div class="flex flex-col sm:flex-row gap-3">
        <SearchBar
          v-model="search"
          placeholder="Tìm theo tên lớp, khóa học hoặc phòng học..."
          class="flex-1"
        />

        <div class="flex items-center gap-2 flex-wrap">
          <!-- Status filter -->
          <select
            v-model="statusFilter"
            class="text-xs font-semibold bg-white border border-orange-200 text-gray-700 rounded-xl px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
          >
            <option value="all">Tất cả trạng thái</option>
            <option value="active">Đang giảng dạy (Mở lớp)</option>
            <option value="upcoming">Sắp khai giảng</option>
            <option value="completed">Đã kết thúc</option>
          </select>

          <!-- View toggle -->
          <ViewToggle v-model="viewMode" />
        </div>
      </div>
    </div>

    <!-- ─── Summary ──────────────────────────────────────────────────────── -->
    <div class="flex items-center justify-between text-xs text-gray-500">
      <span>
        Hiển thị <strong class="text-orange-950">{{ filteredClasses.length }}</strong> / {{ classesList.length }} lớp học được phân công
      </span>
      <span v-if="search || statusFilter !== 'all'" class="text-orange-600 font-medium">
        Đang lọc kết quả
      </span>
    </div>

    <!-- ─── Loading State ────────────────────────────────────────────────── -->
    <div v-if="isLoading" class="bg-white rounded-3xl border border-orange-100 p-12 text-center space-y-3 shadow-sm">
      <ArrowPathIcon class="w-8 h-8 animate-spin text-orange-500 mx-auto" />
      <p class="text-sm font-semibold text-orange-900">Đang đồng bộ danh sách lớp học từ máy chủ...</p>
    </div>

    <!-- ─── Empty State ──────────────────────────────────────────────────── -->
    <div
      v-else-if="filteredClasses.length === 0"
      class="bg-white rounded-3xl border border-orange-100 p-12 text-center shadow-sm"
    >
      <EmptyState
        :icon="BookOpenIcon"
        message="Không tìm thấy lớp học nào"
        sub="Bạn chưa có lớp học nào phù hợp với bộ lọc, hoặc chưa được ban đào tạo phân công lớp mới."
      />
    </div>

    <!-- ─── Grid View ────────────────────────────────────────────────────── -->
    <div
      v-else-if="viewMode === 'grid'"
      class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5"
    >
      <div
        v-for="cls in filteredClasses"
        :key="cls.id"
        class="bg-white rounded-3xl border border-orange-100 p-5 shadow-sm hover:shadow-md hover:border-orange-300 transition-all flex flex-col justify-between group"
      >
        <div>
          <!-- Card Header: Code & Status badge -->
          <div class="flex items-center justify-between gap-2 mb-3">
            <span class="font-mono text-xs font-bold text-orange-800 bg-orange-100/80 px-2.5 py-0.5 rounded-lg border border-orange-200">
              Lớp #{{ cls.id }}
            </span>
            <span
              :class="[
                'px-2.5 py-0.5 rounded-full text-[11px] font-bold inline-block',
                cls.statusNormalized === 'active' ? 'bg-emerald-100 text-emerald-800' :
                cls.statusNormalized === 'upcoming' ? 'bg-blue-100 text-blue-800' :
                'bg-gray-100 text-gray-700'
              ]"
            >
              {{ cls.statusLabel }}
            </span>
          </div>

          <!-- Class Title & Course Info -->
          <h3 class="text-base font-bold text-gray-900 group-hover:text-orange-600 transition-colors line-clamp-1">
            {{ cls.name }}
          </h3>
          <p class="text-xs text-gray-500 mt-1 line-clamp-1 font-medium">
            {{ cls.courseLevelName || cls.courseName || 'Khóa học Toán Tư Duy' }}
          </p>

          <!-- Class Meta: Room, Schedule, Students -->
          <div class="mt-4 space-y-2 text-xs text-gray-600 bg-orange-50/40 p-3 rounded-2xl border border-orange-100/60">
            <div class="flex items-center justify-between">
              <span class="text-gray-500">Phòng học:</span>
              <span class="font-bold text-orange-950">{{ cls.room || 'Phòng học chính' }}</span>
            </div>
            <div class="flex items-center justify-between">
              <span class="text-gray-500">Lịch học trong tuần:</span>
              <span class="font-semibold text-gray-800 text-right truncate max-w-[170px]" :title="cls.scheduleSummary">
                {{ cls.scheduleSummary || 'Chưa xếp lịch' }}
              </span>
            </div>
            <div class="flex items-center justify-between">
              <span class="text-gray-500">Sĩ số học sinh:</span>
              <span class="font-bold text-emerald-800">
                {{ cls.activeStudentsCount || 0 }} / {{ cls.maxCapacity || 20 }} học sinh
              </span>
            </div>
          </div>

          <!-- Program Progress Bar -->
          <div class="mt-4">
            <div class="flex items-center justify-between text-[11px] text-gray-500 mb-1">
              <span>Tiến độ giảng dạy</span>
              <span class="font-bold text-orange-700">
                {{ cls.completedLessons }} / {{ cls.totalLessons || 24 }} buổi
              </span>
            </div>
            <div class="w-full bg-orange-100 rounded-full h-1.5 overflow-hidden">
              <div
                class="bg-gradient-to-r from-orange-400 to-amber-500 h-1.5 rounded-full transition-all duration-300"
                :style="{ width: `${cls.progressPercent}%` }"
              ></div>
            </div>
          </div>
        </div>

        <!-- Card Footer Actions -->
        <div class="mt-5 pt-3.5 border-t border-orange-100 flex items-center justify-between gap-2">
          <router-link
            :to="{ path: '/teacher/students', query: { classId: cls.id } }"
            class="text-xs font-semibold text-orange-800 hover:text-orange-950 px-2.5 py-1.5 rounded-xl hover:bg-orange-50 transition-colors flex items-center gap-1"
          >
            <UserGroupIcon class="w-3.5 h-3.5 text-orange-600" />
            <span>Học sinh ({{ cls.activeStudentsCount || 0 }})</span>
          </router-link>

          <div class="flex items-center gap-1.5">
            <router-link
              :to="{ path: '/teacher/schedule', query: { classId: cls.id } }"
              class="px-2.5 py-1.5 rounded-xl border border-orange-200 text-orange-800 hover:bg-orange-50 text-xs font-semibold transition-colors"
              title="Xem thời khóa biểu lớp này"
            >
              Lịch học
            </router-link>
            <router-link
              :to="{ path: '/teacher/attendance', query: { classId: cls.id } }"
              class="px-3.5 py-1.5 rounded-xl bg-orange-500 hover:bg-orange-600 text-white text-xs font-bold shadow-xs transition-colors"
            >
              Điểm danh
            </router-link>
          </div>
        </div>
      </div>
    </div>

    <!-- ─── List View ────────────────────────────────────────────────────── -->
    <div v-else class="space-y-3">
      <div
        v-for="cls in filteredClasses"
        :key="cls.id"
        class="bg-white rounded-3xl border border-orange-100 p-4 sm:p-5 shadow-sm hover:border-orange-300 transition-all flex flex-col md:flex-row md:items-center justify-between gap-4"
      >
        <div class="flex items-start gap-4">
          <div class="w-12 h-12 rounded-2xl bg-orange-500 text-white flex items-center justify-center font-mono font-bold text-sm flex-shrink-0 shadow-xs">
            #{{ cls.id }}
          </div>
          <div>
            <div class="flex items-center gap-2 flex-wrap">
              <h3 class="text-base font-bold text-gray-900">{{ cls.name }}</h3>
              <span
                :class="[
                  'px-2.5 py-0.5 rounded-full text-[10px] font-bold inline-block',
                  cls.statusNormalized === 'active' ? 'bg-emerald-100 text-emerald-800' :
                  cls.statusNormalized === 'upcoming' ? 'bg-blue-100 text-blue-800' :
                  'bg-gray-100 text-gray-700'
                ]"
              >
                {{ cls.statusLabel }}
              </span>
            </div>
            <p class="text-xs text-gray-500 mt-1">
              {{ cls.courseLevelName || cls.courseName || 'Khóa học' }} &bull; Phòng: <span class="text-orange-950 font-bold">{{ cls.room || 'Chính' }}</span> &bull; Lịch: <span class="font-medium text-gray-800">{{ cls.scheduleSummary || 'Chưa xếp' }}</span>
            </p>
          </div>
        </div>

        <div class="flex items-center justify-between md:justify-end gap-5 border-t md:border-t-0 pt-3 md:pt-0 border-orange-50">
          <div class="text-right text-xs">
            <p class="font-bold text-gray-900">{{ cls.activeStudentsCount || 0 }} / {{ cls.maxCapacity || 20 }} học sinh</p>
            <p class="text-[11px] text-orange-600 font-medium">{{ cls.completedLessons }}/{{ cls.totalLessons || 24 }} buổi đã dạy ({{ cls.progressPercent }}%)</p>
          </div>

          <div class="flex items-center gap-2">
            <router-link
              :to="{ path: '/teacher/students', query: { classId: cls.id } }"
              class="px-3 py-1.5 rounded-xl border border-orange-200 text-orange-800 hover:bg-orange-50 text-xs font-semibold transition-colors"
            >
              Học sinh
            </router-link>
            <router-link
              :to="{ path: '/teacher/attendance', query: { classId: cls.id } }"
              class="px-3.5 py-1.5 rounded-xl bg-orange-500 hover:bg-orange-600 text-white text-xs font-bold shadow-xs transition-colors"
            >
              Điểm danh
            </router-link>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import classService from '@/services/class.service'
import classScheduleService from '@/services/classSchedule.service'
import lessonService from '@/services/lesson.service'
import teacherService from '@/services/teacher.service'
import { useUserState } from '@/services/user-state'
import { getProfileId } from '@/services/auth-storage'
import {
  BookOpenIcon,
  UserGroupIcon,
  ArrowPathIcon,
  ChevronRightIcon,
} from '@heroicons/vue/24/outline'
import SearchBar from '@/components/common/SearchBar.vue'
import ViewToggle from '@/components/common/ViewToggle.vue'
import EmptyState from '@/components/common/EmptyState.vue'

const { profile, fetchProfile } = useUserState()

const search = ref('')
const statusFilter = ref('all')
const viewMode = ref('grid')
const isLoading = ref(false)
const classesList = ref([])

onMounted(async () => {
  await loadAssignedClasses()
})

async function loadAssignedClasses() {
  isLoading.value = true
  try {
    const userProfile = await fetchProfile()
    let teacherProfileId = getProfileId()
    const role = (userProfile?.role || '').toLowerCase()
    const teacherFullName = (userProfile?.fullName || '').trim().toLowerCase()
    const teacherPhone = (userProfile?.phone || '').trim()

    // Resolve teacherProfileId if not in storage
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

    const assigned = await classService.getAssignedClasses(userProfile, teacherProfileId)
    const rawList = Array.isArray(assigned) ? assigned : []

    // Enrich each class with schedules and lesson progress
    const enriched = await Promise.all(
      rawList.map(async (c) => {
        let scheduleSummary = ''
        let completedLessons = 0
        let totalLessons = 24

        // 1. Fetch class recurring schedule
        try {
          const schedules = await classScheduleService.getByClass(c.id)
          const schList = Array.isArray(schedules) ? schedules : []
          if (schList.length > 0) {
            const dayNames = schList
              .map(s => formatDayOfWeek(s.dayOfWeek))
              .filter((v, i, a) => a.indexOf(v) === i)
            const timeSlot = formatTimeRange(schList[0].startTime, schList[0].endTime)
            const room = schList.find(s => s.room)?.room || c.room || ''
            scheduleSummary = `${dayNames.join(' & ')} (${timeSlot})`
            if (room && !c.room) c.room = room
          }
        } catch {
          // ignore schedule error
        }

        // 2. Fetch lessons to compute progress
        try {
          const lessonsData = await lessonService.getByClass(c.id)
          const lessonList = Array.isArray(lessonsData) ? lessonsData : (lessonsData.items || [])
          if (lessonList.length > 0) {
            totalLessons = lessonList.length
            completedLessons = lessonList.filter(l => l.status === 'COMPLETED').length
          }
        } catch {
          // ignore lessons error
        }

        // Status normalization
        const rawStatus = (c.status || '').toUpperCase()
        let statusNormalized = 'active'
        let statusLabel = 'Đang giảng dạy'

        if (rawStatus === 'UPCOMING' || rawStatus === 'DRAFT' || rawStatus === 'PLANNING') {
          statusNormalized = 'upcoming'
          statusLabel = 'Sắp khai giảng'
        } else if (rawStatus === 'COMPLETED' || rawStatus === 'CLOSED' || rawStatus === 'FINISHED') {
          statusNormalized = 'completed'
          statusLabel = 'Đã kết thúc'
        }

        const progressPercent = totalLessons > 0 ? Math.round((completedLessons / totalLessons) * 100) : 0

        return {
          ...c,
          scheduleSummary,
          completedLessons,
          totalLessons,
          progressPercent,
          statusNormalized,
          statusLabel,
        }
      })
    )

    classesList.value = enriched
  } catch (err) {
    console.error('Lỗi khi tải danh sách lớp học:', err)
  } finally {
    isLoading.value = false
  }
}

const filteredClasses = computed(() => {
  return classesList.value.filter((cls) => {
    const q = search.value.toLowerCase().trim()
    const matchSearch =
      !q ||
      cls.name.toLowerCase().includes(q) ||
      (cls.courseLevelName && cls.courseLevelName.toLowerCase().includes(q)) ||
      (cls.courseName && cls.courseName.toLowerCase().includes(q)) ||
      (cls.room && cls.room.toLowerCase().includes(q)) ||
      String(cls.id).includes(q)

    const matchStatus = statusFilter.value === 'all' || cls.statusNormalized === statusFilter.value
    return matchSearch && matchStatus
  })
})

function formatDayOfWeek(dow) {
  const map = {
    0: 'CN',
    1: 'T2',
    2: 'T3',
    3: 'T4',
    4: 'T5',
    5: 'T6',
    6: 'T7',
  }
  return map[dow] || `T${dow}`
}

function formatTimeRange(start, end) {
  if (!start) return ''
  const s = String(start).slice(0, 5)
  const e = end ? String(end).slice(0, 5) : ''
  return e ? `${s} - ${e}` : s
}
</script>
