<template>
  <div class="space-y-4">
    <!-- ─── Page header & Top Switcher ────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-orange-900">Quản lý Lịch học & Thời khóa biểu</h1>
        <p class="text-sm text-gray-500 mt-0.5">
          Giao diện trực quan phong cách Google Calendar kết hợp ngăn xếp mốc giờ
        </p>
      </div>

      <div class="flex items-center gap-2.5 flex-wrap">
        <!-- View mode tabs: Google Calendar vs Table List -->
        <div class="flex items-center bg-orange-100/70 p-1 rounded-xl text-xs font-semibold">
          <button
            @click="mainDisplayMode = 'calendar'"
            :class="[
              'inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg transition-all cursor-pointer',
              mainDisplayMode === 'calendar'
                ? 'bg-white text-orange-600 font-bold shadow-2xs'
                : 'text-orange-800 hover:text-orange-950'
            ]"
          >
            <CalendarDaysIcon class="w-4 h-4 text-orange-500" />
            <span>Lịch Google Calendar</span>
          </button>
          <button
            @click="mainDisplayMode = 'table'"
            :class="[
              'inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg transition-all cursor-pointer',
              mainDisplayMode === 'table'
                ? 'bg-white text-orange-600 font-bold shadow-2xs'
                : 'text-orange-800 hover:text-orange-950'
            ]"
          >
            <Bars3Icon class="w-4 h-4 text-orange-500" />
            <span>Bảng danh sách</span>
          </button>
        </div>

        <button
          @click="openCreateModal"
          class="inline-flex items-center gap-2 px-4 py-2 bg-orange-500 hover:bg-orange-600
                 text-white text-xs font-bold rounded-xl shadow-sm transition-colors flex-shrink-0 cursor-pointer"
        >
          <PlusIcon class="w-4 h-4" />
          <span>Thêm lịch học</span>
        </button>
      </div>
    </div>

    <!-- ─── Error Alert ───────────────────────────────────────────────────── -->
    <div
      v-if="errorMessage"
      class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl flex items-center justify-between gap-3 text-sm"
    >
      <div class="flex items-center gap-2">
        <ExclamationCircleIcon class="w-5 h-5 flex-shrink-0 text-red-500" />
        <span>{{ errorMessage }}</span>
      </div>
      <button
        @click="loadAllData"
        class="px-3 py-1 bg-red-600 hover:bg-red-700 text-white rounded-lg text-xs font-medium transition-colors cursor-pointer"
      >
        Thử lại
      </button>
    </div>

    <!-- ─── Loading State ─────────────────────────────────────────────────── -->
    <LoadingSpinner v-if="loading" label="Đang tải dữ liệu thời khóa biểu từ backend..." />

    <!-- ═══════════════════════════════════════════════════════════════════════ -->
    <!-- ─── MODE 1: GOOGLE CALENDAR VIEW (PRIMARY & HIGH VISUAL) ───────────── -->
    <!-- ═══════════════════════════════════════════════════════════════════════ -->
    <div v-else-if="mainDisplayMode === 'calendar'">
      <GoogleCalendarView
        :schedules="schedules"
        :classes="classes"
        @view="openViewModal"
        @edit="openEditModal"
        @create="openCreateModal"
      />
    </div>

    <!-- ═══════════════════════════════════════════════════════════════════════ -->
    <!-- ─── MODE 2: TABLE LIST VIEW (ADMIN / TABULAR) ──────────────────────── -->
    <!-- ═══════════════════════════════════════════════════════════════════════ -->
    <div v-else class="space-y-4">
      <!-- Toolbar for Table View -->
      <div class="bg-white rounded-2xl border border-orange-100 shadow-sm p-4 space-y-3">
        <div class="grid grid-cols-1 md:grid-cols-12 gap-3">
          <div class="md:col-span-5">
            <label class="block text-[11px] font-bold text-gray-500 uppercase tracking-wider mb-1">
              Lớp học
            </label>
            <select
              v-model="selectedClassId"
              class="w-full px-3.5 py-2.5 bg-gray-50 border border-orange-200/80 rounded-xl text-xs font-semibold text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden cursor-pointer"
            >
              <option value="all">-- Tất cả các lớp học --</option>
              <option
                v-for="c in classes"
                :key="c.id"
                :value="c.id"
              >
                {{ c.name }} (ID: {{ c.id }})
              </option>
            </select>
          </div>

          <div class="md:col-span-4">
            <label class="block text-[11px] font-bold text-gray-500 uppercase tracking-wider mb-1">
              Tìm kiếm nhanh
            </label>
            <SearchBar
              v-model="tableSearch"
              placeholder="Tìm theo tên lớp, phòng học, giờ..."
            />
          </div>

          <div class="md:col-span-3">
            <label class="block text-[11px] font-bold text-gray-500 uppercase tracking-wider mb-1">
              Thứ trong tuần
            </label>
            <select
              v-model="tableFilterDay"
              class="w-full px-3 py-2 bg-gray-50 border border-orange-200/80 rounded-xl text-xs font-semibold text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden"
            >
              <option value="">Tất cả các ngày</option>
              <option :value="1">Thứ 2</option>
              <option :value="2">Thứ 3</option>
              <option :value="3">Thứ 4</option>
              <option :value="4">Thứ 5</option>
              <option :value="5">Thứ 6</option>
              <option :value="6">Thứ 7</option>
              <option :value="0">Chủ nhật</option>
            </select>
          </div>
        </div>
      </div>

      <!-- Table Card -->
      <div class="bg-white rounded-2xl border border-orange-100 shadow-sm overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-left border-collapse">
            <thead>
              <tr class="bg-orange-50/70 border-b border-orange-100 text-[11px] font-bold text-orange-950 uppercase tracking-wider">
                <th class="py-3.5 px-4 w-16 text-center">ID</th>
                <th class="py-3.5 px-4">Lớp học</th>
                <th class="py-3.5 px-4 w-32">Thứ trong tuần</th>
                <th class="py-3.5 px-4 w-44">Khung giờ học</th>
                <th class="py-3.5 px-4 w-36">Phòng học</th>
                <th class="py-3.5 px-4 w-40">Ngày tạo</th>
                <th class="py-3.5 px-4 w-28 text-center">Thao tác</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 text-xs text-gray-700">
              <tr
                v-for="s in filteredTableSchedules"
                :key="s.id"
                class="hover:bg-orange-50/40 transition-colors cursor-pointer"
                @click="openViewModal(s)"
              >
                <!-- ID -->
                <td class="py-3 px-4 text-center font-mono font-bold text-orange-700">
                  #{{ s.id }}
                </td>

                <!-- Class Name -->
                <td class="py-3 px-4">
                  <p class="font-bold text-gray-900 hover:text-orange-600 transition-colors">
                    {{ s.className }}
                  </p>
                  <p class="text-[11px] text-gray-400 mt-0.5 font-mono">
                    Mã lớp: ID {{ s.classId }}
                  </p>
                </td>

                <!-- Day of week badge -->
                <td class="py-3 px-4">
                  <span :class="['px-2.5 py-1 text-xs font-bold rounded-lg border inline-block', getDayBadgeClass(s.dayOfWeek)]">
                    {{ formatDayOfWeek(s.dayOfWeek) }}
                  </span>
                </td>

                <!-- Time range -->
                <td class="py-3 px-4">
                  <span class="font-mono font-bold text-gray-800 text-xs">
                    {{ formatTime(s.startTime) }} - {{ formatTime(s.endTime) }}
                  </span>
                </td>

                <!-- Room -->
                <td class="py-3 px-4">
                  <span v-if="s.room" class="font-medium text-gray-800 bg-gray-100 px-2 py-0.5 rounded text-[11px]">
                    {{ s.room }}
                  </span>
                  <span v-else class="text-gray-400 italic text-[11px]">Chưa xếp phòng</span>
                </td>

                <!-- Created At -->
                <td class="py-3 px-4 text-gray-500 text-[11px]">
                  {{ formatDate(s.createdAt) }}
                </td>

                <!-- Actions -->
                <td class="py-3 px-4 text-center" @click.stop>
                  <div class="flex items-center justify-center gap-1.5">
                    <button
                      @click="openViewModal(s)"
                      title="Xem chi tiết"
                      class="p-1.5 rounded-lg text-gray-500 hover:text-orange-600 hover:bg-orange-50 transition-colors cursor-pointer"
                    >
                      <EyeIcon class="w-4 h-4" />
                    </button>
                    <button
                      @click="openEditModal(s)"
                      title="Chỉnh sửa lịch"
                      class="p-1.5 rounded-lg text-orange-500 hover:text-orange-700 hover:bg-orange-100 transition-colors cursor-pointer"
                    >
                      <PencilSquareIcon class="w-4 h-4" />
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- ─── Schedule Modal (Chi tiết & Chỉnh sửa & Thêm mới) ────────────────── -->
    <ScheduleModal
      :show="showModal"
      :schedule="activeSchedule"
      :classes="classes"
      :initial-mode="modalMode"
      :default-class-id="selectedClassId"
      @close="showModal = false"
      @saved="handleScheduleSaved"
      @deleted="handleScheduleDeleted"
    />
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import {
  PlusIcon,
  CalendarDaysIcon,
  Bars3Icon,
  EyeIcon,
  PencilSquareIcon,
  ExclamationCircleIcon,
} from '@heroicons/vue/24/outline'

import SearchBar          from '@/components/common/SearchBar.vue'
import LoadingSpinner     from '@/components/common/LoadingSpinner.vue'
import GoogleCalendarView from '@/components/schedule/GoogleCalendarView.vue'
import ScheduleModal      from '@/components/schedule/ScheduleModal.vue'

import classScheduleService from '@/services/classSchedule.service'
import classService         from '@/services/class.service'

const route = useRoute()

// ─── State ───────────────────────────────────────────────────────────────────
const loading         = ref(false)
const errorMessage    = ref('')
const classes         = ref([])
const schedules       = ref([])
const mainDisplayMode = ref('calendar') // 'calendar' | 'table'

// Table specific filter state
const selectedClassId = ref('all')
const tableFilterDay  = ref('')
const tableSearch     = ref('')

// Modal state
const showModal      = ref(false)
const modalMode      = ref('view') // 'view' | 'edit' | 'create'
const activeSchedule = ref(null)

// ─── Day of Week helpers ─────────────────────────────────────────────────────
const DAYS_OF_WEEK = [
  { value: 1, label: 'Thứ 2', color: 'border-blue-200 bg-blue-50 text-blue-700' },
  { value: 2, label: 'Thứ 3', color: 'border-emerald-200 bg-emerald-50 text-emerald-700' },
  { value: 3, label: 'Thứ 4', color: 'border-amber-200 bg-amber-50 text-amber-700' },
  { value: 4, label: 'Thứ 5', color: 'border-purple-200 bg-purple-50 text-purple-700' },
  { value: 5, label: 'Thứ 6', color: 'border-rose-200 bg-rose-50 text-rose-700' },
  { value: 6, label: 'Thứ 7', color: 'border-cyan-200 bg-cyan-50 text-cyan-700' },
  { value: 0, label: 'Chủ nhật', color: 'border-red-200 bg-red-50 text-red-700' },
]

function formatDayOfWeek(val) {
  const item = DAYS_OF_WEEK.find(d => d.value === Number(val))
  return item ? item.label : `Thứ ${val}`
}

function getDayBadgeClass(val) {
  const item = DAYS_OF_WEEK.find(d => d.value === Number(val))
  return item ? item.color : 'border-gray-200 bg-gray-50 text-gray-700'
}

function formatTime(t) {
  if (!t) return '--:--'
  const parts = String(t).split(':')
  if (parts.length >= 2) return `${parts[0].padStart(2, '0')}:${parts[1].padStart(2, '0')}`
  return t
}

function formatDate(d) {
  if (!d) return '--'
  return new Date(d).toLocaleDateString('vi-VN')
}

// ─── Filtered Table Schedules ────────────────────────────────────────────────
const filteredTableSchedules = computed(() => {
  let list = [...schedules.value]

  // Filter by selected class
  if (selectedClassId.value !== 'all') {
    list = list.filter(s => Number(s.classId) === Number(selectedClassId.value))
  }

  // Filter by day of week
  if (tableFilterDay.value !== '' && tableFilterDay.value != null) {
    list = list.filter(s => Number(s.dayOfWeek) === Number(tableFilterDay.value))
  }

  // Filter by search query
  const q = tableSearch.value.toLowerCase().trim()
  if (q) {
    list = list.filter(s =>
      s.className?.toLowerCase().includes(q) ||
      s.room?.toLowerCase().includes(q) ||
      s.startTime?.includes(q) ||
      s.endTime?.includes(q) ||
      String(s.id).includes(q)
    )
  }

  // Sort
  list.sort((a, b) => {
    const dayA = a.dayOfWeek === 0 ? 7 : a.dayOfWeek
    const dayB = b.dayOfWeek === 0 ? 7 : b.dayOfWeek
    if (dayA !== dayB) return dayA - dayB
    return (a.startTime || '').localeCompare(b.startTime || '')
  })

  return list
})

// ─── Modal Triggers ───────────────────────────────────────────────────────────
function openViewModal(schedule) {
  activeSchedule.value = schedule
  modalMode.value = 'view'
  showModal.value = true
}

function openEditModal(schedule) {
  activeSchedule.value = schedule
  modalMode.value = 'edit'
  showModal.value = true
}

function openCreateModal() {
  activeSchedule.value = null
  modalMode.value = 'create'
  showModal.value = true
}

function handleScheduleSaved() {
  loadAllData()
}

function handleScheduleDeleted(deletedId) {
  schedules.value = schedules.value.filter(s => s.id !== deletedId)
}

// ─── Data Loading ─────────────────────────────────────────────────────────────
async function loadAllData() {
  loading.value = true
  errorMessage.value = ''

  try {
    // 1. Load classes
    const classData = await classService.getAll()
    const rawClasses = Array.isArray(classData) ? classData : (classData.items ?? classData.data ?? [])
    classes.value = rawClasses

    // 2. Load schedules for all classes concurrently
    if (rawClasses.length > 0) {
      const results = await Promise.allSettled(
        rawClasses.map(c => classScheduleService.getByClass(c.id))
      )

      const all = []
      results.forEach((res, idx) => {
        if (res.status === 'fulfilled' && Array.isArray(res.value)) {
          const className = rawClasses[idx]?.name
          res.value.forEach(item => {
            all.push({
              ...item,
              className: item.className || className || `Lớp #${item.classId}`,
            })
          })
        }
      })
      schedules.value = all
    } else {
      schedules.value = []
    }
  } catch (err) {
    console.error('Không thể tải dữ liệu lịch học:', err)
    errorMessage.value = 'Không thể kết nối đến máy chủ backend (cổng 5114) để lấy dữ liệu lịch học.'
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  await loadAllData()

  // Preselect from query if provided (e.g. /training/schedule?classId=1)
  const queryClassId = route.query.classId
  if (queryClassId && classes.value.some(c => String(c.id) === String(queryClassId))) {
    selectedClassId.value = Number(queryClassId)
  }
})
</script>

<style scoped>
/* Scoped styles */
</style>
