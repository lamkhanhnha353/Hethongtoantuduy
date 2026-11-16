<template>
  <div class="space-y-6 max-w-7xl mx-auto pb-12">
    <!-- ─── Header ───────────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div>
        <nav class="flex items-center gap-2 text-xs font-medium text-stone-500 mb-1">
          <span class="text-orange-600 font-semibold">Giáo viên</span>
          <ChevronRightIcon class="w-3.5 h-3.5 text-stone-400" />
          <span>Lớp học đang phụ trách</span>
        </nav>
        <h1 class="text-2xl md:text-3xl font-bold text-stone-900 tracking-tight">Lớp học đang phụ trách</h1>
        <p class="text-xs md:text-sm text-stone-500 mt-0.5">
          Quản lý tiến độ giảng dạy, thời khóa biểu, sĩ số và các hoạt động của từng lớp được phân công
        </p>
      </div>
      <div class="flex items-center gap-2 flex-wrap">
        <button
          type="button"
          @click="loadAssignedClasses"
          :disabled="isLoading"
          class="inline-flex items-center gap-1.5 h-9 px-3.5 rounded-md bg-white hover:bg-stone-100 text-stone-800 border border-stone-300 hover:border-stone-400 text-xs font-semibold transition-colors cursor-pointer disabled:opacity-50"
        >
          <ArrowPathIcon class="w-4 h-4 text-orange-600" :class="{ 'animate-spin': isLoading }" />
          <span>Làm mới</span>
        </button>

        <router-link
          to="/teacher/students"
          class="inline-flex items-center gap-1.5 h-9 px-3.5 rounded-md bg-orange-600 hover:bg-orange-700 active:bg-orange-800 text-white border border-orange-700 text-xs font-semibold shadow-xs transition-colors cursor-pointer"
        >
          <UserGroupIcon class="w-4 h-4" />
          <span>Tất cả học sinh</span>
        </router-link>
      </div>
    </div>

    <!-- ─── Filter & Search Bar ─────────────────────────────────────────── -->
    <div class="bg-white rounded-lg border border-stone-300 p-4 shadow-[0_1px_2px_rgba(0,0,0,0.04)] space-y-3">
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
            class="h-9 text-xs font-semibold bg-stone-50/70 border border-stone-300 text-stone-800 rounded-md px-3 focus:bg-white focus:border-orange-600 focus:ring-2 focus:ring-orange-500/20 outline-none"
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
    <div class="flex items-center justify-between text-xs text-stone-500 tabular-nums">
      <span>
        Tìm thấy <strong class="text-stone-900">{{ totalCount }}</strong> / {{ classesList.length }} lớp học được phân công
      </span>
      <span v-if="search || statusFilter !== 'all'" class="text-orange-600 font-semibold">
        Đang lọc kết quả
      </span>
    </div>

    <!-- ─── Loading State ────────────────────────────────────────────────── -->
    <div v-if="isLoading" class="bg-white rounded-lg border border-stone-300 p-12 text-center space-y-3 shadow-[0_1px_2px_rgba(0,0,0,0.04)]">
      <ArrowPathIcon class="w-8 h-8 animate-spin text-orange-600 mx-auto" />
      <p class="text-sm font-semibold text-stone-700">Đang đồng bộ danh sách lớp học từ máy chủ...</p>
    </div>

    <!-- ─── Empty State ──────────────────────────────────────────────────── -->
    <div
      v-else-if="displayedClasses.length === 0"
      class="bg-white rounded-lg border border-stone-300 p-12 text-center shadow-[0_1px_2px_rgba(0,0,0,0.04)]"
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
        v-for="cls in displayedClasses"
        :key="cls.id"
        class="bg-white rounded-lg border border-stone-300 p-5 shadow-[0_1px_2px_rgba(0,0,0,0.04)] hover:border-stone-400 transition-colors flex flex-col justify-between group"
      >
        <div>
          <!-- Card Header: Code & Status badge -->
          <div class="flex items-center justify-between gap-2 mb-3">
            <span class="font-mono text-xs font-bold text-orange-800 bg-orange-50 px-2.5 py-0.5 rounded border border-orange-200 tabular-nums">
              Lớp #{{ cls.id }}
            </span>
            <span
              :class="[
                'px-2 py-0.5 rounded border text-[11px] font-semibold inline-block',
                cls.statusNormalized === 'active' ? 'border-emerald-300 bg-emerald-50 text-emerald-800' :
                cls.statusNormalized === 'upcoming' ? 'border-amber-300 bg-amber-50 text-amber-800' :
                'border-stone-300 bg-stone-100 text-stone-700'
              ]"
            >
              {{ cls.statusLabel }}
            </span>
          </div>

          <!-- Class Title & Course Info -->
          <h3 class="text-base font-bold text-stone-900 group-hover:text-orange-600 transition-colors line-clamp-1">
            {{ cls.name }}
          </h3>
          <p class="text-xs text-stone-500 mt-1 line-clamp-1 font-medium">
            {{ cls.courseLevelName || cls.courseName || 'Khóa học Toán Tư Duy' }}
          </p>

          <!-- Class Meta: Room, Schedule, Students -->
          <div class="mt-4 space-y-2 text-xs text-stone-600 bg-stone-50 p-3 rounded-md border border-stone-200">
            <div class="flex items-center justify-between">
              <span class="text-stone-500">Phòng học:</span>
              <span class="font-bold text-stone-900">{{ cls.room || 'Phòng học chính' }}</span>
            </div>
            <div class="flex items-center justify-between">
              <span class="text-stone-500">Lịch học trong tuần:</span>
              <span class="font-semibold text-stone-800 text-right truncate max-w-[170px]" :title="cls.scheduleSummary">
                {{ cls.scheduleSummary || 'Chưa xếp lịch' }}
              </span>
            </div>
            <div class="flex items-center justify-between">
              <span class="text-stone-500">Sĩ số học sinh:</span>
              <span class="font-bold text-emerald-700 tabular-nums">
                {{ cls.activeStudentsCount || 0 }} / {{ cls.maxCapacity || 20 }} học sinh
              </span>
            </div>
          </div>

          <!-- Program Progress Bar -->
          <div class="mt-4">
            <div class="flex items-center justify-between text-xs text-stone-500 mb-1.5 tabular-nums">
              <span>Tiến độ giảng dạy</span>
              <span class="font-bold text-stone-900">
                {{ cls.completedLessons }} / {{ cls.totalLessons || 24 }} buổi
              </span>
            </div>
            <div class="w-full bg-stone-200 rounded h-1.5 overflow-hidden">
              <div
                class="bg-orange-600 h-1.5 rounded transition-all duration-300"
                :style="{ width: `${cls.progressPercent}%` }"
              ></div>
            </div>
          </div>
        </div>

        <!-- Card Footer Actions -->
        <div class="mt-5 pt-3.5 border-t border-stone-200 flex items-center justify-between gap-2">
          <router-link
            :to="{ path: '/teacher/students', query: { classId: cls.id } }"
            class="text-xs font-semibold text-stone-700 hover:text-stone-900 h-8 px-2.5 rounded-md hover:bg-stone-100 transition-colors flex items-center gap-1 tabular-nums"
          >
            <UserGroupIcon class="w-3.5 h-3.5 text-orange-600" />
            <span>Học sinh ({{ cls.activeStudentsCount || 0 }})</span>
          </router-link>

          <div class="flex items-center gap-1.5">
            <router-link
              :to="{ path: '/teacher/schedule', query: { classId: cls.id } }"
              class="inline-flex items-center h-8 px-2.5 rounded-md bg-white border border-stone-300 text-stone-800 hover:bg-stone-100 hover:border-stone-400 text-xs font-semibold transition-colors"
              title="Xem thời khóa biểu lớp này"
            >
              Lịch học
            </router-link>
            <router-link
              :to="{ path: '/teacher/attendance', query: { classId: cls.id } }"
              class="inline-flex items-center h-8 px-3 rounded-md bg-orange-600 hover:bg-orange-700 text-white border border-orange-700 text-xs font-semibold shadow-xs transition-colors"
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
        v-for="cls in displayedClasses"
        :key="cls.id"
        class="bg-white rounded-lg border border-stone-300 p-4 sm:p-5 shadow-[0_1px_2px_rgba(0,0,0,0.04)] hover:border-stone-400 transition-colors flex flex-col md:flex-row md:items-center justify-between gap-4"
      >
        <div class="flex items-start gap-4">
          <div class="w-12 h-12 rounded-md bg-orange-600 border border-orange-700 text-white flex items-center justify-center font-mono font-bold text-sm flex-shrink-0 tabular-nums">
            #{{ cls.id }}
          </div>
          <div>
            <div class="flex items-center gap-2 flex-wrap">
              <h3 class="text-base font-bold text-stone-900">{{ cls.name }}</h3>
              <span
                :class="[
                  'px-2 py-0.5 rounded border text-[11px] font-semibold inline-block',
                  cls.statusNormalized === 'active' ? 'border-emerald-300 bg-emerald-50 text-emerald-800' :
                  cls.statusNormalized === 'upcoming' ? 'border-amber-300 bg-amber-50 text-amber-800' :
                  'border-stone-300 bg-stone-100 text-stone-700'
                ]"
              >
                {{ cls.statusLabel }}
              </span>
            </div>
            <p class="text-xs text-stone-500 mt-1">
              {{ cls.courseLevelName || cls.courseName || 'Khóa học' }} &bull; Phòng: <span class="text-stone-900 font-semibold">{{ cls.room || 'Chính' }}</span> &bull; Lịch: <span class="font-medium text-stone-800">{{ cls.scheduleSummary || 'Chưa xếp' }}</span>
            </p>
          </div>
        </div>

        <div class="flex items-center justify-between md:justify-end gap-5 border-t md:border-t-0 pt-3 md:pt-0 border-stone-200">
          <div class="text-right text-xs tabular-nums">
            <p class="font-bold text-stone-900">{{ cls.activeStudentsCount || 0 }} / {{ cls.maxCapacity || 20 }} học sinh</p>
            <p class="text-xs text-orange-600 font-semibold">{{ cls.completedLessons }}/{{ cls.totalLessons || 24 }} buổi đã dạy ({{ cls.progressPercent }}%)</p>
          </div>

          <div class="flex items-center gap-2">
            <router-link
              :to="{ path: '/teacher/students', query: { classId: cls.id } }"
              class="inline-flex items-center h-8 px-3 rounded-md bg-white border border-stone-300 text-stone-800 hover:bg-stone-100 hover:border-stone-400 text-xs font-semibold transition-colors"
            >
              Học sinh
            </router-link>
            <router-link
              :to="{ path: '/teacher/attendance', query: { classId: cls.id } }"
              class="inline-flex items-center h-8 px-3 rounded-md bg-orange-600 hover:bg-orange-700 text-white border border-orange-700 text-xs font-semibold shadow-xs transition-colors"
            >
              Điểm danh
            </router-link>
          </div>
        </div>
      </div>
    </div>

    <div v-if="totalCount > 0" class="overflow-hidden rounded-lg border border-stone-300 bg-white shadow-[0_1px_2px_rgba(0,0,0,0.04)]">
      <PaginationBar
        v-model:page="page"
        v-model:page-size="pageSize"
        :page-size-options="pageSizeOptions"
        :total-count="totalCount"
        :total-pages="totalPages"
        :disabled="isLoading"
        :bordered="false"
        item-label="lớp học"
      />
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, watch } from 'vue'
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
import PaginationBar from '@/components/common/PaginationBar.vue'
import SearchBar from '@/components/common/SearchBar.vue'
import ViewToggle from '@/components/common/ViewToggle.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import { usePagination } from '@/composables/usePagination'

const { profile, fetchProfile } = useUserState()
const { page, pageSize, totalCount, totalPages, pageSizeOptions, applyClientList, resetPage } = usePagination({
  initialPageSize: 9,
  pageSizeOptions: [9, 18, 36, 72],
})

const search = ref('')
const statusFilter = ref('all')
const viewMode = ref('grid')
const isLoading = ref(false)
const classesList = ref([])
const enrichmentById = reactive({})

onMounted(async () => {
  await loadAssignedClasses()
})

function normalizeClassStatus(rawStatusValue) {
  const rawStatus = (rawStatusValue || '').toUpperCase()
  if (rawStatus === 'UPCOMING' || rawStatus === 'DRAFT' || rawStatus === 'PLANNING') {
    return { statusNormalized: 'upcoming', statusLabel: 'Sắp khai giảng' }
  }
  if (rawStatus === 'COMPLETED' || rawStatus === 'CLOSED' || rawStatus === 'FINISHED') {
    return { statusNormalized: 'completed', statusLabel: 'Đã kết thúc' }
  }
  return { statusNormalized: 'active', statusLabel: 'Đang giảng dạy' }
}

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

    classesList.value = rawList.map((c) => ({
      ...c,
      ...normalizeClassStatus(c.status),
    }))
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

const pagedRawClasses = computed(() => applyClientList(filteredClasses.value))

const displayedClasses = computed(() =>
  pagedRawClasses.value.map((cls) => {
    const cached = enrichmentById[cls.id]
    if (!cached) {
      return {
        ...cls,
        scheduleSummary: '',
        completedLessons: 0,
        totalLessons: 24,
        progressPercent: 0,
      }
    }
    return {
      ...cls,
      ...cached,
      room: cached.room || cls.room,
    }
  })
)

watch([search, statusFilter], () => {
  resetPage()
})

watch(
  pagedRawClasses,
  async (currentSlice) => {
    const pending = currentSlice.filter((c) => !enrichmentById[c.id])
    if (pending.length === 0) return

    await Promise.all(
      pending.map(async (c) => {
        let scheduleSummary = ''
        let completedLessons = 0
        let totalLessons = 24
        let roomOverride = ''

        try {
          const schedules = await classScheduleService.getByClass(c.id)
          const schList = Array.isArray(schedules) ? schedules : []
          if (schList.length > 0) {
            const dayNames = schList
              .map(s => formatDayOfWeek(s.dayOfWeek))
              .filter((v, i, a) => a.indexOf(v) === i)
            const timeSlot = formatTimeRange(schList[0].startTime, schList[0].endTime)
            roomOverride = schList.find(s => s.room)?.room || ''
            scheduleSummary = `${dayNames.join(' & ')} (${timeSlot})`
          }
        } catch {
          // ignore schedule error
        }

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

        const progressPercent = totalLessons > 0 ? Math.round((completedLessons / totalLessons) * 100) : 0
        enrichmentById[c.id] = {
          scheduleSummary,
          completedLessons,
          totalLessons,
          progressPercent,
          room: roomOverride,
        }
      })
    )
  },
  { immediate: true }
)

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
