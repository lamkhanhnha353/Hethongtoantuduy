<template>
  <div class="space-y-5">
    <!-- ─── Header ───────────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-orange-950">Thời khóa biểu cá nhân</h1>
        <p class="text-xs text-gray-500 mt-0.5">
          Theo dõi lịch dạy, phòng học và thời gian các ca giảng dạy được phân công
        </p>
      </div>
      <div class="flex items-center gap-2">
        <button
          type="button"
          class="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-orange-100 hover:bg-orange-200/80 text-orange-800 text-xs font-semibold transition-colors cursor-pointer"
          @click="syncSchedule"
        >
          <ArrowPathIcon class="w-4 h-4 text-orange-600" :class="{ 'animate-spin': isSyncing }" />
          Đồng bộ lịch
        </button>
        <button
          type="button"
          class="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-orange-400 hover:bg-orange-500 text-white text-xs font-semibold shadow-xs transition-colors cursor-pointer"
          @click="exportPdf"
        >
          <ArrowDownTrayIcon class="w-4 h-4" />
          Xuất PDF lịch dạy
        </button>
      </div>
    </div>

    <!-- ─── Week Navigator Toolbar ───────────────────────────────────────── -->
    <div class="bg-white rounded-2xl border border-orange-100 p-4 shadow-sm flex flex-col md:flex-row md:items-center justify-between gap-3">
      <div class="flex items-center gap-2">
        <button
          class="p-2 rounded-xl border border-orange-200 text-orange-700 hover:bg-orange-50 transition-colors cursor-pointer"
          @click="prevWeek"
          title="Tuần trước"
        >
          <ChevronLeftIcon class="w-4 h-4" />
        </button>
        <span class="text-xs md:text-sm font-bold text-gray-800 px-2 min-w-[210px] text-center">
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
          class="px-3 py-1.5 rounded-xl text-xs font-semibold bg-orange-50 text-orange-700 border border-orange-200 hover:bg-orange-100 ml-1 transition-colors cursor-pointer"
          @click="goToCurrentWeek"
        >
          Hôm nay
        </button>
      </div>

      <div class="flex items-center gap-2">
        <div class="flex items-center bg-orange-100/70 p-1 rounded-xl text-xs font-semibold">
          <button
            @click="viewMode = 'grid'"
            :class="[
              'px-3 py-1.5 rounded-lg transition-all cursor-pointer',
              viewMode === 'grid' ? 'bg-orange-400 text-white shadow-xs' : 'text-orange-800 hover:text-orange-950',
            ]"
          >
            Lưới tuần
          </button>
          <button
            @click="viewMode = 'list'"
            :class="[
              'px-3 py-1.5 rounded-lg transition-all cursor-pointer',
              viewMode === 'list' ? 'bg-orange-400 text-white shadow-xs' : 'text-orange-800 hover:text-orange-950',
            ]"
          >
            Danh sách
          </button>
        </div>
      </div>
    </div>

    <!-- ─── Grid View (Weekly Calendar) ──────────────────────────────────── -->
    <div v-if="viewMode === 'grid'" class="bg-white rounded-2xl border border-orange-100 p-4 shadow-sm overflow-x-auto">
      <div class="min-w-[800px]">
        <!-- Days header -->
        <div class="grid grid-cols-7 gap-2 pb-3 border-b border-orange-100 text-center">
          <div
            v-for="day in weekDays"
            :key="day.dateStr"
            :class="[
              'p-2 rounded-xl text-xs transition-colors',
              day.isToday ? 'bg-orange-100 text-orange-950 font-bold border border-orange-300' : 'text-gray-600',
            ]"
          >
            <p class="font-semibold uppercase tracking-wider text-[11px]">{{ day.name }}</p>
            <p class="text-sm mt-0.5">{{ day.formattedDate }}</p>
          </div>
        </div>

        <!-- Schedule cells -->
        <div class="grid grid-cols-7 gap-2 pt-3 min-h-[380px]">
          <div
            v-for="day in weekDays"
            :key="'cell-' + day.dateStr"
            class="space-y-2 border-r border-dashed border-orange-100/70 last:border-r-0 pr-1.5"
          >
            <div
              v-for="session in day.sessions"
              :key="session.id"
              class="p-2.5 rounded-xl border border-orange-200 bg-orange-50/70 hover:bg-orange-100/80 transition-all text-left shadow-2xs group cursor-pointer"
            >
              <div class="flex items-center justify-between gap-1 text-[10px] font-bold text-orange-800">
                <span class="truncate">{{ session.room }}</span>
                <span>{{ session.time }}</span>
              </div>
              <h4 class="text-xs font-bold text-gray-900 mt-1 line-clamp-2 group-hover:text-orange-600">
                {{ session.className }}
              </h4>
              <div class="flex items-center justify-between text-[11px] text-gray-500 mt-1.5">
                <span>{{ session.studentsCount }} HS</span>
                <router-link
                  :to="{ path: '/teacher/attendance', query: { lessonId: session.id, classId: session.classId } }"
                  class="text-[10px] font-semibold text-orange-600 hover:underline"
                >
                  Điểm danh
                </router-link>
              </div>
            </div>

            <!-- Empty slot marker -->
            <div
              v-if="day.sessions.length === 0"
              class="h-28 rounded-xl border border-dashed border-orange-100/80 flex items-center justify-center text-center p-2"
            >
              <span class="text-[11px] text-gray-400">Không có tiết</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ─── List View ────────────────────────────────────────────────────── -->
    <div v-else class="space-y-3">
      <div
        v-if="allSessions.length === 0"
        class="bg-white rounded-2xl border border-orange-100 p-8 text-center"
      >
        <EmptyState
          :icon="CalendarDaysIcon"
          message="Không có lịch dạy trong tuần này"
          sub="Khi ban đào tạo xếp thời khóa biểu, các ca dạy của thầy/cô sẽ hiển thị ở đây."
        />
      </div>

      <div
        v-for="session in allSessions"
        :key="session.id"
        class="bg-white rounded-2xl border border-orange-100 p-4 shadow-sm hover:border-orange-300 transition-colors flex flex-col sm:flex-row sm:items-center justify-between gap-3"
      >
        <div class="flex items-start gap-3">
          <div class="w-12 h-12 rounded-xl bg-orange-100 text-orange-900 font-bold text-xs flex flex-col items-center justify-center flex-shrink-0">
            <span>{{ session.dayName }}</span>
            <span class="text-[10px] font-normal text-orange-700">{{ session.dateStr }}</span>
          </div>
          <div>
            <div class="flex items-center gap-2">
              <span class="text-sm font-bold text-gray-900">{{ session.className }}</span>
              <span class="text-[11px] font-semibold px-2 py-0.5 rounded-full bg-orange-100 text-orange-800">
                {{ session.room }}
              </span>
            </div>
            <p class="text-xs text-gray-500 mt-0.5 flex items-center gap-3">
              <span>⏰ {{ session.time }}</span>
              <span>👥 Sĩ số: {{ session.studentsCount }} học sinh</span>
            </p>
          </div>
        </div>

        <div class="flex items-center gap-2 self-end sm:self-center">
          <router-link
            :to="{ path: '/teacher/attendance', query: { lessonId: session.id, classId: session.classId } }"
            class="px-3 py-1.5 rounded-xl bg-orange-400 hover:bg-orange-500 text-white text-xs font-semibold shadow-xs cursor-pointer"
          >
            Điểm danh
          </router-link>
          <router-link
            to="/teacher/classes"
            class="px-3 py-1.5 rounded-xl border border-orange-200 text-orange-800 hover:bg-orange-50 text-xs font-semibold cursor-pointer"
          >
            Chi tiết lớp
          </router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import {
  CalendarDaysIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
  ArrowPathIcon,
  ArrowDownTrayIcon,
} from '@heroicons/vue/24/outline'
import EmptyState from '@/components/common/EmptyState.vue'

const viewMode = ref('grid')
const isSyncing = ref(false)
const weekOffset = ref(0)

const currentWeekRange = computed(() => {
  return 'Tuần 44 (28/10/2026 - 03/11/2026)'
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

function syncSchedule() {
  isSyncing.value = true
  setTimeout(() => {
    isSyncing.value = false
  }, 800)
}

function exportPdf() {
  alert('Đang tạo và tải file PDF thời khóa biểu tuần...')
}

const weekDays = ref([
  {
    name: 'Thứ 2',
    dateStr: '2026-10-28',
    formattedDate: '28/10',
    isToday: false,
    sessions: [
      { id: 1, className: 'Toán Tư Duy - Lớp 3A', room: 'P201', time: '08:00 - 09:30', studentsCount: 18 },
      { id: 2, className: 'Hình Học Trực Quan - Lớp 4B', room: 'P305', time: '14:00 - 15:30', studentsCount: 16 },
    ],
  },
  {
    name: 'Thứ 3',
    dateStr: '2026-10-29',
    formattedDate: '29/10',
    isToday: false,
    sessions: [],
  },
  {
    name: 'Thứ 4',
    dateStr: '2026-10-30',
    formattedDate: '30/10',
    isToday: false,
    sessions: [
      { id: 3, className: 'Toán Nâng Cao - Lớp 5A', room: 'P102', time: '09:45 - 11:15', studentsCount: 15 },
    ],
  },
  {
    name: 'Thứ 5',
    dateStr: '2026-10-31',
    formattedDate: '31/10',
    isToday: false,
    sessions: [
      { id: 4, className: 'Toán Tư Duy - Lớp 3A', room: 'P201', time: '08:00 - 09:30', studentsCount: 18 },
    ],
  },
  {
    name: 'Thứ 6',
    dateStr: '2026-11-01',
    formattedDate: '01/11',
    isToday: true,
    sessions: [
      { id: 5, className: 'Hình Học Trực Quan - Lớp 4B', room: 'P305', time: '14:00 - 15:30', studentsCount: 16 },
    ],
  },
  {
    name: 'Thứ 7',
    dateStr: '2026-11-02',
    formattedDate: '02/11',
    isToday: false,
    sessions: [
      { id: 6, className: 'Bồi Dưỡng Học Sinh Giỏi', room: 'P401', time: '08:30 - 11:00', studentsCount: 12 },
    ],
  },
  {
    name: 'Chủ Nhật',
    dateStr: '2026-11-03',
    formattedDate: '03/11',
    isToday: false,
    sessions: [],
  },
])

const allSessions = computed(() => {
  return weekDays.value.flatMap(day =>
    day.sessions.map(s => ({
      ...s,
      dayName: day.name,
      dateStr: day.formattedDate,
    }))
  )
})
</script>
