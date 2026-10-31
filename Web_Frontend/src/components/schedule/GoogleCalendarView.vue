<template>
  <div class="bg-white rounded-2xl border border-orange-100 shadow-sm overflow-hidden flex flex-col">
    <!-- ═══════════════════════════════════════════════════════════════════════ -->
    <!-- ─── 1. GOOGLE CALENDAR HEADER ──────────────────────────────────────── -->
    <!-- ═══════════════════════════════════════════════════════════════════════ -->
    <div class="px-5 py-3.5 border-b border-orange-100 flex flex-col md:flex-row md:items-center justify-between gap-3 bg-white">
      <!-- Left controls: Today button, Prev/Next, Month-Year title -->
      <div class="flex items-center gap-3">
        <!-- Toggle Left Sidebar Button -->
        <button
          @click="sidebarOpen = !sidebarOpen"
          :class="[
            'p-2 rounded-xl text-gray-500 hover:text-orange-600 hover:bg-orange-50 transition-colors cursor-pointer',
            sidebarOpen ? 'bg-orange-50 text-orange-600' : ''
          ]"
          title="Thu gọn / Mở rộng bảng điều khiển"
        >
          <Bars3Icon class="w-5 h-5" />
        </button>

        <!-- Today button -->
        <button
          @click="goToToday"
          class="px-3.5 py-1.5 rounded-xl border border-gray-200 text-xs font-bold text-gray-700 hover:bg-orange-50 hover:border-orange-300 hover:text-orange-600 transition-all cursor-pointer shadow-2xs"
        >
          Hôm nay
        </button>

        <!-- Nav arrows -->
        <div class="flex items-center gap-0.5">
          <button
            @click="prevWeek"
            class="p-1.5 rounded-lg text-gray-500 hover:text-gray-800 hover:bg-gray-100 transition-colors cursor-pointer"
            title="Tuần trước"
          >
            <ChevronLeftIcon class="w-4 h-4" />
          </button>
          <button
            @click="nextWeek"
            class="p-1.5 rounded-lg text-gray-500 hover:text-gray-800 hover:bg-gray-100 transition-colors cursor-pointer"
            title="Tuần sau"
          >
            <ChevronRightIcon class="w-4 h-4" />
          </button>
        </div>

        <!-- Month & Year display -->
        <h2 class="text-base sm:text-lg font-bold text-gray-800 tracking-tight ml-1">
          {{ weekRangeTitle }}
        </h2>
      </div>

      <!-- Right controls: Quick filter, Stack badge, Add session -->
      <div class="flex items-center gap-2 flex-wrap">
        <!-- Stack indicator chip -->
        <div
          v-if="stackedGroupsCount > 0"
          class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-xl bg-orange-100/80 text-orange-800 text-xs font-semibold border border-orange-200"
        >
          <Square3Stack3DIcon class="w-4 h-4 text-orange-600" />
          <span>{{ stackedGroupsCount }} ngăn xếp mốc giờ</span>
        </div>

        <!-- View mode dropdown (Tuần / Ngày) -->
        <div class="flex items-center bg-gray-100 p-0.5 rounded-xl text-xs font-semibold text-gray-600">
          <button
            @click="calendarMode = 'week'"
            :class="[
              'px-3 py-1.5 rounded-lg transition-all cursor-pointer',
              calendarMode === 'week' ? 'bg-white text-orange-600 shadow-2xs font-bold' : 'hover:text-gray-900'
            ]"
          >
            Tuần
          </button>
          <button
            @click="calendarMode = 'day'"
            :class="[
              'px-3 py-1.5 rounded-lg transition-all cursor-pointer',
              calendarMode === 'day' ? 'bg-white text-orange-600 shadow-2xs font-bold' : 'hover:text-gray-900'
            ]"
          >
            Ngày
          </button>
        </div>

        <!-- Create Button -->
        <button
          @click="$emit('create')"
          class="inline-flex items-center gap-1.5 px-3.5 py-1.5 bg-orange-500 hover:bg-orange-600 text-white text-xs font-bold rounded-xl shadow-sm transition-all cursor-pointer"
        >
          <PlusIcon class="w-4 h-4" />
          <span>Thêm lịch</span>
        </button>
      </div>
    </div>

    <!-- ═══════════════════════════════════════════════════════════════════════ -->
    <!-- ─── 2. MAIN CALENDAR BODY (SIDEBAR + TIME GRID) ────────────────────── -->
    <!-- ═══════════════════════════════════════════════════════════════════════ -->
    <div class="flex-1 flex overflow-hidden min-h-[640px]">
      <!-- ─── LEFT SIDEBAR (Google Calendar Style) ────────────────────────── -->
      <transition name="slide-left">
        <aside
          v-if="sidebarOpen"
          class="w-64 sm:w-72 border-r border-orange-100 bg-gray-50/50 p-4 space-y-5 flex-shrink-0 overflow-y-auto hidden md:block"
        >
          <!-- Mini Month Calendar -->
          <div class="bg-white p-3 rounded-2xl border border-gray-200/70 shadow-2xs">
            <div class="flex items-center justify-between mb-2">
              <span class="text-xs font-bold text-gray-800">{{ miniCalendarMonthName }}</span>
              <div class="flex items-center gap-1">
                <button
                  @click="changeMiniMonth(-1)"
                  class="p-1 rounded text-gray-400 hover:text-gray-700 hover:bg-gray-100 cursor-pointer"
                >
                  <ChevronLeftIcon class="w-3.5 h-3.5" />
                </button>
                <button
                  @click="changeMiniMonth(1)"
                  class="p-1 rounded text-gray-400 hover:text-gray-700 hover:bg-gray-100 cursor-pointer"
                >
                  <ChevronRightIcon class="w-3.5 h-3.5" />
                </button>
              </div>
            </div>
            <!-- Mini day header (T2..CN) -->
            <div class="grid grid-cols-7 text-center text-[10px] font-bold text-gray-400 mb-1">
              <span>T2</span><span>T3</span><span>T4</span><span>T5</span><span>T6</span><span>T7</span><span>CN</span>
            </div>
            <!-- Mini day cells -->
            <div class="grid grid-cols-7 gap-1 text-center text-xs">
              <button
                v-for="(day, idx) in miniCalendarDays"
                :key="idx"
                @click="selectDateFromMini(day.date)"
                :class="[
                  'w-6 h-6 mx-auto rounded-full text-[11px] font-medium transition-all flex items-center justify-center cursor-pointer',
                  day.isCurrentMonth ? 'text-gray-700' : 'text-gray-300',
                  day.isToday ? 'border border-orange-500 font-bold' : '',
                  day.isSelectedWeek ? 'bg-orange-100 text-orange-800 font-bold' : 'hover:bg-gray-100',
                  day.isExactDate ? 'bg-orange-500 text-white font-bold hover:bg-orange-600' : ''
                ]"
              >
                {{ day.date.getDate() }}
              </button>
            </div>
          </div>

          <!-- Class Filter List ("My Calendars" style) -->
          <div class="space-y-3">
            <div class="flex items-center justify-between">
              <h3 class="text-xs font-bold text-gray-800 uppercase tracking-wider flex items-center gap-1.5">
                <AcademicCapIcon class="w-4 h-4 text-orange-500" />
                Lớp học hiển thị ({{ classes.length }})
              </h3>
              <button
                @click="toggleAllClasses"
                class="text-[11px] text-orange-600 hover:text-orange-800 font-medium cursor-pointer"
              >
                {{ allSelected ? 'Bỏ chọn' : 'Chọn tất cả' }}
              </button>
            </div>

            <!-- Class search filter -->
            <input
              v-model="classSearchQuery"
              type="text"
              placeholder="Lọc tên lớp..."
              class="w-full px-2.5 py-1.5 bg-white border border-gray-200 rounded-xl text-xs placeholder-gray-400 focus:border-orange-400 focus:outline-hidden"
            />

            <!-- Class list with colored checkboxes -->
            <div class="space-y-1.5 max-h-60 overflow-y-auto pr-1">
              <label
                v-for="c in filteredClassList"
                :key="c.id"
                class="flex items-center justify-between p-2 rounded-xl bg-white border border-gray-100 hover:border-orange-200 transition-all cursor-pointer group text-xs"
              >
                <div class="flex items-center gap-2 min-w-0">
                  <input
                    type="checkbox"
                    :value="c.id"
                    v-model="selectedClassIds"
                    class="rounded text-orange-500 focus:ring-orange-400 h-3.5 w-3.5 cursor-pointer"
                  />
                  <span
                    class="w-2.5 h-2.5 rounded-full flex-shrink-0"
                    :class="getClassTheme(c.id).bg"
                  />
                  <span class="truncate font-medium text-gray-700 group-hover:text-gray-900">
                    {{ c.name }}
                  </span>
                </div>
                <button
                  @click.prevent.stop="showClassStackModal(c)"
                  title="Xem tất cả mốc giờ của lớp này (Ngăn xếp)"
                  class="opacity-0 group-hover:opacity-100 p-1 text-orange-600 hover:bg-orange-50 rounded transition-all flex-shrink-0 cursor-pointer"
                >
                  <Square3Stack3DIcon class="w-3.5 h-3.5" />
                </button>
              </label>
            </div>
          </div>

          <!-- Quick Tip -->
          <div class="p-3 bg-orange-100/50 rounded-xl border border-orange-200/60 text-[11px] text-orange-900 leading-relaxed">
            <span class="font-bold flex items-center gap-1 mb-1 text-orange-950">
              <Square3Stack3DIcon class="w-3.5 h-3.5 text-orange-600" />
              Tính năng Ngăn xếp:
            </span>
            Khi nhiều lớp trùng giờ hoặc một lớp có nhiều mốc học, click vào <strong>thẻ ngăn xếp</strong> để xem chi tiết đầy đủ các buổi học.
          </div>
        </aside>
      </transition>

      <!-- ─── TIME GRID VIEW (GOOGLE CALENDAR CORE) ────────────────────────── -->
      <div class="flex-1 flex flex-col overflow-hidden bg-white">
        <!-- Day Columns Header -->
        <div class="flex border-b border-orange-100 bg-white sticky top-0 z-20">
          <!-- Time gutter corner -->
          <div class="w-16 sm:w-20 flex-shrink-0 border-r border-orange-100 p-2 text-center text-[11px] text-gray-400 font-mono">
            GMT+7
          </div>

          <!-- Days headers -->
          <div
            v-for="day in displayedDays"
            :key="day.dayOfWeek"
            :class="[
              'flex-1 min-w-[120px] py-3 px-2 text-center border-r border-orange-50 transition-colors',
              day.isToday ? 'bg-orange-50/50' : ''
            ]"
          >
            <div class="text-[11px] font-bold uppercase tracking-wider text-gray-500">
              {{ day.dayName }}
            </div>
            <div class="mt-1 flex items-center justify-center">
              <span
                :class="[
                  'w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold transition-all',
                  day.isToday
                    ? 'bg-orange-500 text-white shadow-sm'
                    : 'text-gray-800'
                ]"
              >
                {{ day.date.getDate() }}
              </span>
            </div>
            <div class="text-[10px] text-gray-400 mt-0.5">
              {{ getDaySchedules(day.dayOfWeek).length }} buổi
            </div>
          </div>
        </div>

        <!-- Scrollable Time Slots Container -->
        <div
          ref="timeGridScroll"
          class="flex-1 overflow-y-auto relative scroll-smooth"
        >
          <div class="flex relative" :style="{ height: `${TOTAL_HOURS * HOUR_HEIGHT}px` }">
            <!-- ─── Time labels (left gutter) ─── -->
            <div class="w-16 sm:w-20 flex-shrink-0 border-r border-orange-100 select-none bg-white z-10">
              <div
                v-for="hour in HOURS_LIST"
                :key="hour"
                class="relative text-right pr-2 text-[11px] font-mono font-medium text-gray-400 -translate-y-2.5"
                :style="{ height: `${HOUR_HEIGHT}px` }"
              >
                {{ formatHourLabel(hour) }}
              </div>
            </div>

            <!-- ─── Horizontal Grid Lines across all days ─── -->
            <div class="absolute inset-0 pointer-events-none left-16 sm:left-20">
              <div
                v-for="hour in HOURS_LIST"
                :key="'line-' + hour"
                class="border-b border-gray-100 w-full"
                :style="{ height: `${HOUR_HEIGHT}px` }"
              >
                <!-- Half-hour dashed line -->
                <div
                  class="border-b border-dashed border-gray-50/90 w-full"
                  :style="{ height: `${HOUR_HEIGHT / 2}px` }"
                />
              </div>
            </div>

            <!-- ─── Current Time Red Line Indicator ─── -->
            <div
              v-if="currentTimeTop !== null"
              class="absolute left-16 sm:left-20 right-0 z-20 pointer-events-none flex items-center"
              :style="{ top: `${currentTimeTop}px` }"
            >
              <div class="w-3 h-3 rounded-full bg-red-500 -ml-1.5 shadow-sm" />
              <div class="flex-1 border-t-2 border-red-500 shadow-xs" />
            </div>

            <!-- ─── Day Columns & Event Placements ─── -->
            <div
              v-for="day in displayedDays"
              :key="'col-' + day.dayOfWeek"
              :class="[
                'flex-1 min-w-[120px] border-r border-orange-50 relative',
                day.isToday ? 'bg-orange-50/20' : ''
              ]"
            >
              <!-- Overlapping Cluster Groups / Stacked Cards -->
              <div
                v-for="cluster in getClustersForDay(day.dayOfWeek)"
                :key="cluster.key"
                class="absolute left-1 right-1 z-10 transition-all"
                :style="{
                  top: `${cluster.top}px`,
                  height: `${cluster.height}px`,
                }"
              >
                <!-- ════ CASE A: SINGLE EVENT ════ -->
                <div
                  v-if="cluster.schedules.length === 1"
                  @click.stop="handleEventClick(cluster.schedules[0])"
                  :class="[
                    'w-full h-full rounded-xl border p-2 flex flex-col justify-between overflow-hidden shadow-2xs hover:shadow-md transition-all cursor-pointer group',
                    getClassTheme(cluster.schedules[0].classId).lightBg,
                    getClassTheme(cluster.schedules[0].classId).border,
                  ]"
                >
                  <div class="min-w-0">
                    <div class="flex items-center justify-between gap-1 mb-1">
                      <span
                        class="text-[10px] font-bold font-mono px-1.5 py-0.5 rounded-md leading-none shadow-2xs text-white"
                        :class="getClassTheme(cluster.schedules[0].classId).bg"
                      >
                        {{ formatTime(cluster.schedules[0].startTime) }} - {{ formatTime(cluster.schedules[0].endTime) }}
                      </span>
                      <button
                        @click.stop="openEditSchedule(cluster.schedules[0])"
                        title="Chỉnh sửa"
                        class="opacity-0 group-hover:opacity-100 p-0.5 hover:bg-white rounded transition-opacity cursor-pointer"
                      >
                        <PencilSquareIcon class="w-3 h-3 text-gray-600" />
                      </button>
                    </div>
                    <h4
                      class="font-bold text-xs leading-snug line-clamp-2"
                      :class="getClassTheme(cluster.schedules[0].classId).text"
                    >
                      {{ cluster.schedules[0].className }}
                    </h4>
                  </div>

                  <div class="flex items-center justify-between text-[10px] text-gray-500 pt-1">
                    <span class="truncate flex items-center gap-1">
                      <MapPinIcon class="w-3 h-3 text-orange-400" />
                      {{ cluster.schedules[0].room || 'Phòng linh hoạt' }}
                    </span>
                    <button
                      @click.stop="openClassStackBySchedule(cluster.schedules[0])"
                      title="Xem ngăn xếp các mốc giờ của lớp này"
                      class="text-[10px] text-orange-600 hover:text-orange-800 font-semibold flex items-center gap-0.5"
                    >
                      <Square3Stack3DIcon class="w-3 h-3" />
                      <span>Ngăn xếp</span>
                    </button>
                  </div>
                </div>

                <!-- ════ CASE B: STACKED CLUSTER (Nhiều lớp cùng / trùng khung giờ) ════ -->
                <div
                  v-else
                  @click.stop="openStackCluster(cluster, day)"
                  class="relative w-full h-full rounded-xl cursor-pointer group select-none"
                >
                  <!-- Background Stack Layer Effect -->
                  <div class="absolute inset-0 bg-orange-200/50 rounded-xl transform translate-x-1.5 translate-y-1.5 border border-orange-300 pointer-events-none" />
                  <div class="absolute inset-0 bg-amber-100/70 rounded-xl transform translate-x-0.5 translate-y-0.5 border border-amber-300 pointer-events-none" />

                  <!-- Top Card of the Stack -->
                  <div class="relative w-full h-full bg-gradient-to-br from-white to-orange-50 rounded-xl border-2 border-orange-400 p-2.5 shadow-md flex flex-col justify-between overflow-hidden group-hover:scale-[1.01] transition-transform">
                    <div>
                      <!-- Stack Header Badge -->
                      <div class="flex items-center justify-between mb-1">
                        <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-orange-600 text-white font-extrabold text-[10px] shadow-sm animate-pulse">
                          <Square3Stack3DIcon class="w-3 h-3" />
                          Ngăn xếp ({{ cluster.schedules.length }} lớp)
                        </span>
                        <span class="text-[10px] font-mono font-bold text-orange-800">
                          {{ formatTime(cluster.minStart) }} - {{ formatTime(cluster.maxEnd) }}
                        </span>
                      </div>

                      <!-- Preview of classes in stack -->
                      <div class="space-y-1 mt-1.5">
                        <div
                          v-for="(s, sIdx) in cluster.schedules.slice(0, 2)"
                          :key="s.id"
                          class="flex items-center gap-1.5 text-xs text-gray-800 truncate"
                        >
                          <span class="w-2 h-2 rounded-full flex-shrink-0" :class="getClassTheme(s.classId).bg" />
                          <span class="truncate font-semibold text-[11px]">{{ s.className }}</span>
                        </div>
                        <div v-if="cluster.schedules.length > 2" class="text-[10px] text-orange-600 font-bold pl-3.5">
                          +{{ cluster.schedules.length - 2 }} lớp khác trong ngăn xếp...
                        </div>
                      </div>
                    </div>

                    <!-- Call to action -->
                    <div class="pt-1 border-t border-orange-200/80 flex items-center justify-between text-[10px] text-orange-700 font-bold">
                      <span>Ấn để mở chi tiết ngăn xếp</span>
                      <ChevronRightIcon class="w-3.5 h-3.5 group-hover:translate-x-1 transition-transform" />
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ═══════════════════════════════════════════════════════════════════════ -->
    <!-- ─── 3. STACK DETAILS DRAWER / MODAL ("Ngăn xếp lịch học") ─────────── -->
    <!-- ═══════════════════════════════════════════════════════════════════════ -->
    <transition name="drawer-fade">
      <div
        v-if="stackDrawerOpen"
        class="fixed inset-0 z-50 flex items-center justify-end bg-black/40 backdrop-blur-xs"
        @click.self="stackDrawerOpen = false"
      >
        <div class="w-full max-w-md h-full bg-white shadow-2xl flex flex-col border-l border-orange-100 overflow-hidden transform transition-all animate-slide-left">
          <!-- Drawer Header -->
          <div class="p-5 border-b border-orange-100 bg-gradient-to-r from-orange-50 to-amber-50 flex items-center justify-between">
            <div class="flex items-center gap-2.5">
              <div class="w-10 h-10 rounded-xl bg-orange-500 text-white flex items-center justify-center shadow-sm">
                <Square3Stack3DIcon class="w-6 h-6" />
              </div>
              <div>
                <h3 class="text-base font-bold text-orange-950">
                  {{ stackDrawerTitle }}
                </h3>
                <p class="text-xs text-gray-500">
                  {{ stackDrawerSubtitle }}
                </p>
              </div>
            </div>
            <button
              @click="stackDrawerOpen = false"
              class="p-2 rounded-xl text-gray-400 hover:text-gray-700 hover:bg-orange-100/60 transition-colors cursor-pointer"
            >
              <XMarkIcon class="w-5 h-5" />
            </button>
          </div>

          <!-- Drawer Content: List of stacked sessions -->
          <div class="flex-1 overflow-y-auto p-5 space-y-3.5 bg-gray-50/50">
            <div
              v-for="(item, idx) in activeStackItems"
              :key="item.id"
              class="bg-white rounded-2xl border border-orange-100 p-4 shadow-sm hover:border-orange-300 hover:shadow-md transition-all space-y-3 group"
            >
              <!-- Item Header -->
              <div class="flex items-start justify-between gap-2">
                <div class="flex items-center gap-2 min-w-0">
                  <span class="w-3 h-3 rounded-full flex-shrink-0" :class="getClassTheme(item.classId).bg" />
                  <span class="text-xs font-mono font-bold text-orange-800 bg-orange-50 px-2 py-0.5 rounded border border-orange-200">
                    #{{ item.id }}
                  </span>
                  <span class="text-xs font-bold text-gray-700">
                    {{ formatDayOfWeek(item.dayOfWeek) }}
                  </span>
                </div>
                <span class="text-[11px] font-semibold text-gray-400 font-mono">
                  Lớp #{{ item.classId }}
                </span>
              </div>

              <!-- Class Name -->
              <h4 class="font-bold text-sm text-gray-900 group-hover:text-orange-600 transition-colors">
                {{ item.className }}
              </h4>

              <!-- Time & Room info -->
              <div class="grid grid-cols-2 gap-2 text-xs text-gray-600 pt-1 border-t border-gray-100">
                <div class="flex items-center gap-1.5 font-mono font-bold text-orange-700">
                  <ClockIcon class="w-4 h-4 text-orange-500 flex-shrink-0" />
                  <span>{{ formatTime(item.startTime) }} - {{ formatTime(item.endTime) }}</span>
                </div>
                <div class="flex items-center gap-1.5 text-gray-500">
                  <MapPinIcon class="w-4 h-4 text-orange-400 flex-shrink-0" />
                  <span class="truncate">{{ item.room ? item.room : 'Chưa xếp phòng' }}</span>
                </div>
              </div>

              <!-- Quick action buttons -->
              <div class="pt-2 border-t border-orange-50 flex items-center justify-end gap-2">
                <button
                  @click="openViewScheduleFromStack(item)"
                  class="inline-flex items-center gap-1 px-3 py-1.5 rounded-xl text-xs font-semibold text-gray-600 hover:bg-gray-100 transition-colors cursor-pointer"
                >
                  <EyeIcon class="w-3.5 h-3.5" />
                  <span>Chi tiết</span>
                </button>
                <button
                  @click="openEditScheduleFromStack(item)"
                  class="inline-flex items-center gap-1 px-3 py-1.5 rounded-xl text-xs font-semibold text-orange-600 bg-orange-50 hover:bg-orange-100 transition-colors cursor-pointer"
                >
                  <PencilSquareIcon class="w-3.5 h-3.5" />
                  <span>Chỉnh sửa</span>
                </button>
              </div>
            </div>
          </div>

          <!-- Drawer Footer -->
          <div class="p-4 border-t border-orange-100 bg-white flex items-center justify-between">
            <span class="text-xs text-gray-500 font-medium">
              Tổng số: <strong>{{ activeStackItems.length }}</strong> buổi học
            </span>
            <button
              @click="stackDrawerOpen = false"
              class="px-4 py-2 bg-gray-100 hover:bg-gray-200 text-gray-700 text-xs font-bold rounded-xl transition-colors cursor-pointer"
            >
              Đóng
            </button>
          </div>
        </div>
      </div>
    </transition>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import {
  ChevronLeftIcon,
  ChevronRightIcon,
  Bars3Icon,
  PlusIcon,
  ClockIcon,
  MapPinIcon,
  PencilSquareIcon,
  EyeIcon,
  XMarkIcon,
  AcademicCapIcon,
  Square3Stack3DIcon,
} from '@heroicons/vue/24/outline'

const props = defineProps({
  schedules: { type: Array, default: () => [] },
  classes: { type: Array, default: () => [] },
})

const emit = defineEmits(['view', 'edit', 'create'])

// ─── Constants: Time Grid Config ─────────────────────────────────────────────
const START_HOUR = 7  // 07:00
const END_HOUR   = 21 // 21:00
const TOTAL_HOURS = END_HOUR - START_HOUR + 1
const HOUR_HEIGHT = 68 // pixels per hour

const HOURS_LIST = computed(() => {
  const hours = []
  for (let h = START_HOUR; h <= END_HOUR; h++) {
    hours.push(h)
  }
  return hours
})

// ─── Color Themes Palette (Google Calendar Vibe) ─────────────────────────────
const CLASS_THEMES = [
  { bg: 'bg-orange-500', lightBg: 'bg-orange-50/90', border: 'border-orange-300', text: 'text-orange-950' },
  { bg: 'bg-blue-600',   lightBg: 'bg-blue-50/90',   border: 'border-blue-300',   text: 'text-blue-950' },
  { bg: 'bg-emerald-600',lightBg: 'bg-emerald-50/90',border: 'border-emerald-300',text: 'text-emerald-950' },
  { bg: 'bg-purple-600', lightBg: 'bg-purple-50/90', border: 'border-purple-300', text: 'text-purple-950' },
  { bg: 'bg-rose-500',   lightBg: 'bg-rose-50/90',   border: 'border-rose-300',   text: 'text-rose-950' },
  { bg: 'bg-amber-500',  lightBg: 'bg-amber-50/90',  border: 'border-amber-300',  text: 'text-amber-950' },
  { bg: 'bg-teal-600',   lightBg: 'bg-teal-50/90',   border: 'border-teal-300',   text: 'text-teal-950' },
  { bg: 'bg-indigo-600', lightBg: 'bg-indigo-50/90', border: 'border-indigo-300', text: 'text-indigo-950' },
  { bg: 'bg-cyan-600',   lightBg: 'bg-cyan-50/90',   border: 'border-cyan-300',   text: 'text-cyan-950' },
]

function getClassTheme(classId) {
  const idx = Math.abs(Number(classId) || 0) % CLASS_THEMES.length
  return CLASS_THEMES[idx]
}

// ─── State ───────────────────────────────────────────────────────────────────
const sidebarOpen = ref(true)
const calendarMode = ref('week') // 'week' | 'day'
const currentDate = ref(new Date())
const miniCalendarDate = ref(new Date())
const timeGridScroll = ref(null)

// Class filter state
const selectedClassIds = ref([])
const classSearchQuery = ref('')

// Stack Drawer State
const stackDrawerOpen = ref(false)
const stackDrawerTitle = ref('')
const stackDrawerSubtitle = ref('')
const activeStackItems = ref([])

// Initialize all classes selected
watch(
  () => props.classes,
  (newClasses) => {
    if (newClasses && newClasses.length > 0 && selectedClassIds.value.length === 0) {
      selectedClassIds.value = newClasses.map(c => c.id)
    }
  },
  { immediate: true }
)

const allSelected = computed(() =>
  props.classes.length > 0 && selectedClassIds.value.length === props.classes.length
)

function toggleAllClasses() {
  if (allSelected.value) {
    selectedClassIds.value = []
  } else {
    selectedClassIds.value = props.classes.map(c => c.id)
  }
}

const filteredClassList = computed(() => {
  const q = classSearchQuery.value.toLowerCase().trim()
  if (!q) return props.classes
  return props.classes.filter(c => c.name?.toLowerCase().includes(q))
})

// ─── Date & Week Calculation ──────────────────────────────────────────────────
// Compute start of the currently viewed week (Monday as day 1)
const currentWeekStart = computed(() => {
  const d = new Date(currentDate.value)
  const day = d.getDay()
  // In JS getDay(): 0 is Sunday. In Vietnam: Monday is day 1.
  const diff = d.getDate() - (day === 0 ? 6 : day - 1)
  const monday = new Date(d.setDate(diff))
  monday.setHours(0, 0, 0, 0)
  return monday
})

// 7 Days of the week displayed
const displayedDays = computed(() => {
  const dayNames = ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'Chủ nhật']
  const days = []
  const start = new Date(currentWeekStart.value)
  const today = new Date()

  if (calendarMode.value === 'day') {
    // Single day mode
    const d = new Date(currentDate.value)
    const jsDay = d.getDay()
    // map JS Day (0..6) to our convention: 1 = T2.. 6 = T7, 0 = CN
    const ourDayOfWeek = jsDay
    const dayName = ourDayOfWeek === 0 ? 'Chủ nhật' : `Thứ ${ourDayOfWeek + 1}`
    const isToday = d.toDateString() === today.toDateString()
    return [{
      dayOfWeek: ourDayOfWeek,
      dayName,
      date: d,
      isToday,
    }]
  }

  // Week mode (7 days)
  for (let i = 0; i < 7; i++) {
    const d = new Date(start)
    d.setDate(start.getDate() + i)
    // i: 0 -> Thứ 2 (1), 1 -> Thứ 3 (2), ..., 5 -> Thứ 7 (6), 6 -> CN (0)
    const ourDayOfWeek = i === 6 ? 0 : i + 1
    const isToday = d.toDateString() === today.toDateString()
    days.push({
      dayOfWeek: ourDayOfWeek,
      dayName: dayNames[i],
      date: d,
      isToday,
    })
  }
  return days
})

const weekRangeTitle = computed(() => {
  const start = currentWeekStart.value
  const end = new Date(start)
  end.setDate(start.getDate() + 6)
  const mStart = start.getMonth() + 1
  const mEnd = end.getMonth() + 1
  const y = end.getFullYear()
  if (mStart === mEnd) {
    return `Tháng ${mStart}, ${y}`
  }
  return `Tháng ${mStart} - ${mEnd}, ${y}`
})

// ─── Current Time Indicator (Red line) ────────────────────────────────────────
const currentTimeTop = computed(() => {
  const now = new Date()
  const hours = now.getHours()
  const mins = now.getMinutes()
  if (hours < START_HOUR || hours > END_HOUR) return null
  const totalMins = (hours - START_HOUR) * 60 + mins
  return (totalMins / 60) * HOUR_HEIGHT
})

// ─── Sched Filtered by Selected Classes ───────────────────────────────────────
const activeSchedules = computed(() => {
  return props.schedules.filter(s => selectedClassIds.value.includes(Number(s.classId)))
})

function getDaySchedules(dayOfWeek) {
  return activeSchedules.value.filter(s => Number(s.dayOfWeek) === Number(dayOfWeek))
}

// ─── Stacking / Overlap Clustering Algorithm ──────────────────────────────────
// Returns clusters for a given day. If overlapping, grouped into a stack!
function getClustersForDay(dayOfWeek) {
  const items = getDaySchedules(dayOfWeek)
  if (items.length === 0) return []

  // Convert each schedule to start/end in minutes from START_HOUR (07:00)
  const parsed = items.map(s => {
    const [sH, sM] = String(s.startTime || '00:00').split(':').map(Number)
    const [eH, eM] = String(s.endTime || '00:00').split(':').map(Number)
    const startMin = sH * 60 + (sM || 0)
    const endMin = eH * 60 + (eM || 0)
    return {
      schedule: s,
      startMin,
      endMin,
    }
  }).sort((a, b) => a.startMin - b.startMin)

  // Group into overlapping clusters
  const clusters = []
  parsed.forEach(item => {
    // Check if overlaps with current open cluster
    const lastCluster = clusters[clusters.length - 1]
    if (lastCluster && item.startMin < lastCluster.maxEndMin) {
      // Overlap detected: add to stack
      lastCluster.schedules.push(item.schedule)
      lastCluster.minStartMin = Math.min(lastCluster.minStartMin, item.startMin)
      lastCluster.maxEndMin = Math.max(lastCluster.maxEndMin, item.endMin)
    } else {
      // New cluster
      clusters.push({
        key: `cluster-${dayOfWeek}-${item.schedule.id}-${clusters.length}`,
        schedules: [item.schedule],
        minStartMin: item.startMin,
        maxEndMin: item.endMin,
        minStart: item.schedule.startTime,
        maxEnd: item.schedule.endTime,
      })
    }
  })

  // Calculate pixel top & height for each cluster
  const gridStartMinutes = START_HOUR * 60
  return clusters.map(c => {
    const topMinutes = Math.max(0, c.minStartMin - gridStartMinutes)
    const durationMinutes = Math.max(35, c.maxEndMin - c.minStartMin)
    const top = (topMinutes / 60) * HOUR_HEIGHT
    const height = Math.max(50, (durationMinutes / 60) * HOUR_HEIGHT)
    return {
      ...c,
      top,
      height,
    }
  })
}

const stackedGroupsCount = computed(() => {
  let count = 0
  displayedDays.value.forEach(day => {
    const clusters = getClustersForDay(day.dayOfWeek)
    count += clusters.filter(c => c.schedules.length > 1).length
  })
  return count
})

// ─── Format Helpers ───────────────────────────────────────────────────────────
function formatHourLabel(h) {
  return `${String(h).padStart(2, '0')}:00`
}

function formatTime(t) {
  if (!t) return '--:--'
  const parts = String(t).split(':')
  if (parts.length >= 2) return `${parts[0].padStart(2, '0')}:${parts[1].padStart(2, '0')}`
  return t
}

const DAYS_NAME_MAP = {
  1: 'Thứ 2', 2: 'Thứ 3', 3: 'Thứ 4', 4: 'Thứ 5', 5: 'Thứ 6', 6: 'Thứ 7', 0: 'Chủ nhật'
}
function formatDayOfWeek(d) {
  return DAYS_NAME_MAP[Number(d)] ?? `Thứ ${d}`
}

// ─── Navigation ───────────────────────────────────────────────────────────────
function goToToday() {
  currentDate.value = new Date()
  miniCalendarDate.value = new Date()
}

function prevWeek() {
  const d = new Date(currentDate.value)
  d.setDate(d.getDate() - 7)
  currentDate.value = d
}

function nextWeek() {
  const d = new Date(currentDate.value)
  d.setDate(d.getDate() + 7)
  currentDate.value = d
}

// ─── Mini Calendar Navigation ─────────────────────────────────────────────────
const miniCalendarMonthName = computed(() => {
  const m = miniCalendarDate.value.getMonth() + 1
  const y = miniCalendarDate.value.getFullYear()
  return `Tháng ${m}, ${y}`
})

function changeMiniMonth(delta) {
  const d = new Date(miniCalendarDate.value)
  d.setMonth(d.getMonth() + delta)
  miniCalendarDate.value = d
}

function selectDateFromMini(d) {
  currentDate.value = new Date(d)
}

const miniCalendarDays = computed(() => {
  const year = miniCalendarDate.value.getFullYear()
  const month = miniCalendarDate.value.getMonth()
  const firstDay = new Date(year, month, 1)
  const lastDay = new Date(year, month + 1, 0)

  // Start with Monday before the first day
  const startDayOfWeek = firstDay.getDay()
  const mondayOffset = startDayOfWeek === 0 ? 6 : startDayOfWeek - 1
  const start = new Date(firstDay)
  start.setDate(start.getDate() - mondayOffset)

  const days = []
  const today = new Date().toDateString()
  const currentWeekStartTime = currentWeekStart.value.getTime()
  const currentWeekEndTime = currentWeekStartTime + 7 * 86400000

  for (let i = 0; i < 35; i++) {
    const d = new Date(start)
    d.setDate(start.getDate() + i)
    const time = d.getTime()
    days.push({
      date: d,
      isCurrentMonth: d.getMonth() === month,
      isToday: d.toDateString() === today,
      isSelectedWeek: time >= currentWeekStartTime && time < currentWeekEndTime,
      isExactDate: d.toDateString() === currentDate.value.toDateString(),
    })
  }
  return days
})

// ─── Modal / Stack Triggers ───────────────────────────────────────────────────
function handleEventClick(schedule) {
  emit('view', schedule)
}

function openEditSchedule(schedule) {
  emit('edit', schedule)
}

// Khi người dùng bấm vào một Ngăn xếp nhiều lớp cùng mốc giờ
function openStackCluster(cluster, day) {
  stackDrawerTitle.value = `Ngăn xếp mốc giờ (${cluster.schedules.length} lớp)`
  stackDrawerSubtitle.value = `${day.dayName} · Khung giờ: ${formatTime(cluster.minStart)} - ${formatTime(cluster.maxEnd)}`
  activeStackItems.value = cluster.schedules
  stackDrawerOpen.value = true
}

// Khi người dùng bấm vào nút "Ngăn xếp của lớp này"
function openClassStackBySchedule(schedule) {
  const classItem = props.classes.find(c => c.id === schedule.classId)
  showClassStackModal(classItem || { id: schedule.classId, name: schedule.className })
}

// Xem toàn bộ mốc giờ trong tuần của 1 lớp
function showClassStackModal(classItem) {
  const classSchedules = props.schedules.filter(s => s.classId === classItem.id)
  stackDrawerTitle.value = `Ngăn xếp lịch học của lớp`
  stackDrawerSubtitle.value = `${classItem.name} (Gồm ${classSchedules.length} mốc học trong tuần)`
  activeStackItems.value = classSchedules
  stackDrawerOpen.value = true
}

function openViewScheduleFromStack(item) {
  emit('view', item)
}

function openEditScheduleFromStack(item) {
  emit('edit', item)
}

// Scroll to 8:00 AM upon mounting
onMounted(() => {
  if (timeGridScroll.value) {
    const scrollTarget = 1 * HOUR_HEIGHT // Scroll to ~08:00
    timeGridScroll.value.scrollTop = scrollTarget
  }
})
</script>

<style scoped>
.slide-left-enter-active,
.slide-left-leave-active {
  transition: all 0.25s ease;
}
.slide-left-enter-from,
.slide-left-leave-to {
  width: 0;
  opacity: 0;
  padding: 0;
  margin: 0;
}

.drawer-fade-enter-active,
.drawer-fade-leave-active {
  transition: opacity 0.2s ease;
}
.drawer-fade-enter-from,
.drawer-fade-leave-to {
  opacity: 0;
}

@keyframes slideLeft {
  from {
    transform: translateX(100%);
  }
  to {
    transform: translateX(0);
  }
}

.animate-slide-left {
  animation: slideLeft 0.25s cubic-bezier(0.16, 1, 0.3, 1);
}
</style>
