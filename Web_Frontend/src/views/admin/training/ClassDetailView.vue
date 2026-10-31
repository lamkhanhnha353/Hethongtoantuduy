<template>
  <div class="space-y-6">
    <!-- ─── Loading State ─────────────────────────────────────────────────── -->
    <LoadingSpinner v-if="loading" label="Đang tải thông tin chi tiết lớp học..." />

    <!-- ─── Content ───────────────────────────────────────────────────────── -->
    <template v-else-if="cls">
      <!-- ─── Breadcrumb ───────────────────────────────────────────────── -->
      <nav class="flex items-center gap-2 text-sm text-gray-400 flex-wrap">
        <router-link to="/training/classes" class="hover:text-orange-600 transition-colors flex items-center gap-1 font-medium">
          <UserGroupIcon class="w-4 h-4 text-orange-500" />
          Danh sách Lớp học
        </router-link>
        <ChevronRightIcon class="w-4 h-4" />
        <router-link
          v-if="course"
          :to="{ name: 'course-detail', params: { id: course.id } }"
          class="hover:text-orange-600 transition-colors flex items-center gap-1 font-medium"
        >
          <BookOpenIcon class="w-4 h-4 text-orange-500" />
          {{ course.name }}
        </router-link>
        <ChevronRightIcon v-if="course" class="w-4 h-4" />
        <span class="text-orange-950 font-bold truncate">{{ cls.name }}</span>
      </nav>

      <!-- ─── Class Hero Banner & Action Header ────────────────────────── -->
      <div class="bg-white rounded-2xl border border-orange-100 shadow-sm overflow-hidden">
        <div class="h-2 w-full bg-gradient-to-r from-orange-400 via-amber-500 to-orange-500" />
        <div class="p-6">
          <div class="flex flex-col lg:flex-row lg:items-center justify-between gap-6">
            <div class="flex-1 min-w-0">
              <!-- Badges row -->
              <div class="flex flex-wrap items-center gap-2 mb-2">
                <span class="text-xs font-mono font-bold text-orange-700 bg-orange-100 px-3 py-1 rounded-full border border-orange-200">
                  {{ cls.code ?? `LH-${cls.id}` }}
                </span>
                <StatusBadge :status="cls.status" />
                <span v-if="cls.courseLevelName" class="text-xs text-orange-800 bg-orange-50 px-2.5 py-0.5 rounded-full border border-orange-200 font-semibold">
                  Cấp độ: {{ cls.courseLevelName }}
                </span>
              </div>

              <!-- Class Name -->
              <h1 class="text-2xl sm:text-3xl font-extrabold text-orange-950 mb-2 leading-tight">
                {{ cls.name }}
              </h1>

              <!-- Teacher & Room quick info -->
              <div class="flex flex-wrap items-center gap-4 text-xs text-gray-500">
                <div class="flex items-center gap-1.5 font-medium">
                  <AcademicCapIcon class="w-4 h-4 text-orange-500" />
                  <span>Giảng viên: <strong class="text-gray-800">{{ cls.teacherName || 'Chưa phân công' }}</strong></span>
                </div>
                <div v-if="cls.startDate" class="flex items-center gap-1.5 font-medium">
                  <CalendarDaysIcon class="w-4 h-4 text-orange-500" />
                  <span>Khai giảng: <strong class="text-gray-800">{{ formatDate(cls.startDate) }}</strong></span>
                </div>
                <div v-if="cls.endDate" class="flex items-center gap-1.5 font-medium">
                  <ClockIcon class="w-4 h-4 text-orange-500" />
                  <span>Bế giảng dự kiến: <strong class="text-gray-800">{{ formatDate(cls.endDate) }}</strong></span>
                </div>
              </div>
            </div>

            <!-- Top Actions -->
            <div class="flex items-center gap-2.5 flex-wrap flex-shrink-0">
              <button
                @click="openEditClassModal"
                class="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold text-orange-700 bg-orange-100 hover:bg-orange-200 rounded-xl transition-colors cursor-pointer"
              >
                <PencilSquareIcon class="w-4 h-4" />
                <span>Chỉnh sửa lớp học</span>
              </button>

              <button
                @click="openAddScheduleModal"
                class="inline-flex items-center gap-1.5 px-4 py-2 bg-orange-500 hover:bg-orange-600 text-white text-xs font-bold rounded-xl shadow-sm transition-colors cursor-pointer"
              >
                <PlusIcon class="w-4 h-4" />
                <span>Thêm lịch học</span>
              </button>

              <router-link
                :to="{ name: 'schedule', query: { classId: cls.id } }"
                class="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs font-semibold text-gray-600 hover:text-orange-600 hover:bg-orange-50 border border-gray-200 rounded-xl transition-colors cursor-pointer"
              >
                <CalendarIcon class="w-4 h-4 text-orange-500" />
                <span>Lịch toàn trường</span>
              </router-link>
            </div>
          </div>

          <!-- Quick Metrics Banner -->
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-4 mt-6 pt-5 border-t border-orange-100">
            <!-- Capacity -->
            <div class="p-3.5 bg-orange-50/60 rounded-xl border border-orange-100">
              <div class="flex items-center justify-between text-xs text-gray-500 mb-1">
                <span class="flex items-center gap-1">
                  <UserGroupIcon class="w-4 h-4 text-orange-500" />
                  Sĩ số lớp
                </span>
                <span class="font-bold text-orange-600">{{ Math.round(capacityFillPct) }}%</span>
              </div>
              <p class="text-base font-extrabold text-gray-900">
                {{ activeCount }} / {{ maxCapacityDisplay }} <span class="text-xs font-normal text-gray-500">học viên</span>
              </p>
              <div class="h-1.5 bg-orange-100 rounded-full overflow-hidden mt-2">
                <div
                  :class="['h-full rounded-full transition-all duration-500', capacityFillPct >= 90 ? 'bg-red-500' : 'bg-orange-500']"
                  :style="{ width: capacityFillPct + '%' }"
                />
              </div>
            </div>

            <!-- Weekly Sessions -->
            <div class="p-3.5 bg-blue-50/60 rounded-xl border border-blue-100">
              <div class="flex items-center justify-between text-xs text-gray-500 mb-1">
                <span class="flex items-center gap-1 text-blue-700">
                  <CalendarDaysIcon class="w-4 h-4 text-blue-500" />
                  Số buổi học/tuần
                </span>
              </div>
              <p class="text-base font-extrabold text-blue-950">
                {{ schedules.length }} <span class="text-xs font-normal text-gray-500">buổi trong tuần</span>
              </p>
              <p class="text-[11px] text-blue-600 mt-1 truncate">
                {{ weeklyScheduleSummary }}
              </p>
            </div>

            <!-- Fee & Level -->
            <div class="p-3.5 bg-green-50/60 rounded-xl border border-green-100">
              <div class="flex items-center justify-between text-xs text-gray-500 mb-1">
                <span class="flex items-center gap-1 text-green-700">
                  <CurrencyDollarIcon class="w-4 h-4 text-green-600" />
                  Học phí khóa học
                </span>
              </div>
              <p class="text-base font-extrabold text-green-700">
                {{ level?.fee ? formatFee(level.fee) : 'Liên hệ' }}
              </p>
              <p class="text-[11px] text-green-600 mt-1">
                {{ level?.totalSessions ?? 0 }} buổi đào tạo
              </p>
            </div>

            <!-- Teacher -->
            <div class="p-3.5 bg-purple-50/60 rounded-xl border border-purple-100">
              <div class="flex items-center justify-between text-xs text-gray-500 mb-1">
                <span class="flex items-center gap-1 text-purple-700">
                  <AcademicCapIcon class="w-4 h-4 text-purple-500" />
                  Giáo viên
                </span>
              </div>
              <p class="text-sm font-bold text-purple-950 truncate">
                {{ cls.teacherName || 'Chưa phân công' }}
              </p>
              <p class="text-[11px] text-purple-600 mt-1 truncate">
                {{ teacher?.phone || teacher?.email || 'Tài khoản giảng dạy' }}
              </p>
            </div>
          </div>
        </div>
      </div>

      <!-- ─── Grid: Course Info (Left) + Schedule Info (Right) ──────────── -->
      <div class="grid grid-cols-1 lg:grid-cols-12 gap-6">
        <!-- ─── LEFT: Associated Course & Level Info (5 cols) ──────────── -->
        <div class="lg:col-span-5 space-y-6">
          <div class="bg-white rounded-2xl border border-orange-100 shadow-sm p-6 space-y-4">
            <div class="flex items-center justify-between pb-3 border-b border-orange-100">
              <h2 class="font-bold text-orange-950 text-base flex items-center gap-2">
                <BookOpenIcon class="w-5 h-5 text-orange-500" />
                Khóa học & Cấp độ Đào tạo
              </h2>
              <router-link
                v-if="course"
                :to="{ name: 'course-detail', params: { id: course.id } }"
                class="text-xs font-semibold text-orange-600 hover:text-orange-700 hover:underline flex items-center gap-1"
              >
                <span>Xem chi tiết</span>
                <ChevronRightIcon class="w-3.5 h-3.5" />
              </router-link>
            </div>

            <!-- Course Cover & Title -->
            <div v-if="course" class="space-y-3">
              <div v-if="course.imageUrl" class="w-full h-36 rounded-xl overflow-hidden bg-orange-50 border border-orange-100">
                <img :src="course.imageUrl" :alt="course.name" class="w-full h-full object-cover" />
              </div>

              <div>
                <div class="flex items-center gap-2 mb-1">
                  <span class="text-[11px] font-mono font-bold text-orange-700 bg-orange-100 px-2 py-0.5 rounded">
                    {{ course.code }}
                  </span>
                  <StatusBadge :status="course.status" />
                </div>
                <h3 class="font-bold text-gray-900 text-base leading-snug">
                  {{ course.name }}
                </h3>
                <p v-if="course.description" class="text-xs text-gray-500 mt-1.5 leading-relaxed line-clamp-3">
                  {{ course.description }}
                </p>
              </div>

              <!-- Level Detail Card -->
              <div v-if="level" class="p-4 bg-orange-50/50 rounded-xl border border-orange-100 space-y-2.5">
                <div class="flex items-center justify-between">
                  <span class="text-xs font-bold text-orange-900 flex items-center gap-1.5">
                    <AcademicCapIcon class="w-4 h-4 text-orange-500" />
                    Cấp độ: {{ level.name }}
                  </span>
                  <span class="text-[11px] font-mono font-bold text-orange-600 bg-white px-2 py-0.5 rounded border border-orange-200">
                    {{ level.code }}
                  </span>
                </div>
                <div class="grid grid-cols-2 gap-2 text-xs text-gray-600 pt-1 border-t border-orange-100">
                  <div>
                    <span class="text-gray-400 block text-[10px]">Học phí</span>
                    <strong class="text-orange-600 font-bold">{{ formatFee(level.fee) }}</strong>
                  </div>
                  <div>
                    <span class="text-gray-400 block text-[10px]">Thời lượng</span>
                    <strong>{{ level.totalSessions || 0 }} buổi học</strong>
                  </div>
                  <div v-if="course.minAge || course.maxAge" class="col-span-2">
                    <span class="text-gray-400 block text-[10px]">Độ tuổi phù hợp</span>
                    <strong>{{ course.minAge }} - {{ course.maxAge }} tuổi</strong>
                  </div>
                </div>
              </div>

              <!-- Benefits if any -->
              <div v-if="courseBenefits.length > 0" class="pt-2">
                <h4 class="text-xs font-bold text-gray-700 mb-2 flex items-center gap-1.5">
                  <SparklesIcon class="w-4 h-4 text-amber-500" />
                  Mục tiêu & Điểm nổi bật
                </h4>
                <ul class="space-y-1.5 text-xs text-gray-600">
                  <li v-for="(b, idx) in courseBenefits.slice(0, 4)" :key="idx" class="flex items-start gap-2">
                    <CheckCircleIcon class="w-4 h-4 text-green-500 flex-shrink-0 mt-0.5" />
                    <span>{{ b }}</span>
                  </li>
                </ul>
              </div>
            </div>

            <!-- Fallback if course data not loaded -->
            <div v-else class="text-xs text-gray-400 py-4 text-center">
              Đang liên kết dữ liệu khóa học...
            </div>
          </div>

          <!-- Teacher Details Card -->
          <div class="bg-white rounded-2xl border border-orange-100 shadow-sm p-6 space-y-3">
            <h2 class="font-bold text-orange-950 text-base flex items-center gap-2 pb-3 border-b border-orange-100">
              <AcademicCapIcon class="w-5 h-5 text-orange-500" />
              Giáo viên Phụ trách
            </h2>
            <div v-if="cls.teacherName" class="flex items-center gap-3.5">
              <div class="w-12 h-12 rounded-2xl bg-gradient-to-tr from-orange-400 to-amber-500 text-white font-bold flex items-center justify-center text-lg shadow-sm">
                {{ cls.teacherName.charAt(0) }}
              </div>
              <div class="min-w-0 flex-1">
                <h4 class="font-bold text-gray-900 text-sm truncate">{{ cls.teacherName }}</h4>
                <p class="text-xs text-gray-500 mt-0.5">Giảng viên bộ môn Toán Tư Duy</p>
                <p v-if="teacher?.phone" class="text-xs text-orange-600 font-mono mt-0.5">
                  SĐT: {{ teacher.phone }}
                </p>
              </div>
            </div>
            <div v-else class="p-4 bg-gray-50 rounded-xl text-center text-xs text-gray-400">
              Lớp học chưa được chỉ định giáo viên phụ trách.
              <button
                @click="openEditClassModal"
                class="block mx-auto mt-2 text-orange-600 font-bold hover:underline"
              >
                + Phân công ngay
              </button>
            </div>
          </div>
        </div>

        <!-- ─── RIGHT: Schedules & Timetable (7 cols) ────────────────────── -->
        <div class="lg:col-span-7 space-y-6">
          <!-- Schedules List Card -->
          <div class="bg-white rounded-2xl border border-orange-100 shadow-sm p-6 space-y-4">
            <div class="flex items-center justify-between pb-3 border-b border-orange-100">
              <div>
                <h2 class="font-bold text-orange-950 text-base flex items-center gap-2">
                  <CalendarDaysIcon class="w-5 h-5 text-orange-500" />
                  Lịch học trong tuần của lớp
                </h2>
                <p class="text-xs text-gray-500 mt-0.5">
                  Thiết lập các buổi học diễn ra hàng tuần và phòng học tương ứng
                </p>
              </div>

              <button
                @click="openAddScheduleModal"
                class="inline-flex items-center gap-1.5 px-3.5 py-2 bg-orange-500 hover:bg-orange-600 text-white text-xs font-bold rounded-xl shadow-sm transition-colors cursor-pointer"
              >
                <PlusIcon class="w-3.5 h-3.5" />
                <span>Thêm lịch</span>
              </button>
            </div>

            <!-- Loading schedules -->
            <LoadingSpinner v-if="loadingSchedules" label="Đang tải danh sách lịch học..." />

            <!-- Empty Schedules -->
            <EmptyState
              v-else-if="schedules.length === 0"
              :icon="CalendarDaysIcon"
              message="Lớp học chưa được xếp lịch học"
              sub="Bấm nút 'Thêm lịch' phía trên để thiết lập các ngày và khung giờ học trong tuần"
            />

            <!-- Schedules Table / List -->
            <div v-else class="space-y-3">
              <div
                v-for="item in sortedSchedules"
                :key="item.id"
                class="p-4 rounded-xl border border-orange-100 bg-white hover:border-orange-300 hover:shadow-xs transition-all flex flex-col sm:flex-row sm:items-center justify-between gap-3"
              >
                <!-- Day & Time -->
                <div class="flex items-center gap-3.5">
                  <!-- Day badge -->
                  <div :class="['w-16 h-12 rounded-xl flex flex-col items-center justify-center font-bold text-xs border flex-shrink-0', getDayBadge(item.dayOfWeek).color]">
                    <span>{{ getDayBadge(item.dayOfWeek).label }}</span>
                    <span class="text-[9px] font-normal opacity-80">{{ getDayBadge(item.dayOfWeek).full }}</span>
                  </div>

                  <div>
                    <!-- Time range -->
                    <div class="flex items-center gap-2">
                      <ClockIcon class="w-4 h-4 text-orange-500 flex-shrink-0" />
                      <span class="font-mono font-extrabold text-sm text-gray-900">
                        {{ formatTime(item.startTime) }} → {{ formatTime(item.endTime) }}
                      </span>
                      <span class="text-[10px] text-orange-700 bg-orange-100 font-semibold px-2 py-0.5 rounded-full">
                        {{ calcDuration(item.startTime, item.endTime) }}
                      </span>
                    </div>

                    <!-- Room -->
                    <div class="flex items-center gap-1.5 text-xs text-gray-500 mt-1">
                      <MapPinIcon class="w-3.5 h-3.5 text-orange-400" />
                      <span>Phòng: <strong class="text-gray-700">{{ item.room || 'Chưa xếp phòng' }}</strong></span>
                    </div>
                  </div>
                </div>

                <!-- Schedule Actions -->
                <div class="flex items-center gap-1.5 self-end sm:self-center">
                  <button
                    @click="openEditScheduleModal(item)"
                    class="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-semibold text-gray-600 hover:text-orange-600 hover:bg-orange-50 rounded-lg transition-colors cursor-pointer"
                    title="Chỉnh sửa lịch học này"
                  >
                    <PencilSquareIcon class="w-3.5 h-3.5 text-orange-500" />
                    <span>Sửa</span>
                  </button>

                  <button
                    @click="confirmDeleteSchedule(item)"
                    class="inline-flex items-center gap-1 px-2.5 py-1.5 text-xs font-semibold text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors cursor-pointer"
                    title="Xóa lịch học này"
                  >
                    <TrashIcon class="w-3.5 h-3.5" />
                  </button>
                </div>
              </div>
            </div>
          </div>

          <!-- Weekly Schedule Visual Map (Thứ 2 -> Chủ nhật) -->
          <div class="bg-white rounded-2xl border border-orange-100 shadow-sm p-6 space-y-3">
            <h2 class="font-bold text-orange-950 text-base flex items-center gap-2">
              <CalendarIcon class="w-5 h-5 text-orange-500" />
              Thời khóa biểu tuần trực quan
            </h2>
            <p class="text-xs text-gray-500">
              Tổng quan các mốc giờ học được phân bổ trong tuần của lớp
            </p>

            <div class="grid grid-cols-2 sm:grid-cols-4 lg:grid-cols-7 gap-2 pt-2">
              <div
                v-for="d in DAYS_OF_WEEK"
                :key="d.value"
                :class="[
                  'p-3 rounded-xl border text-center transition-all flex flex-col justify-between min-h-[110px]',
                  hasScheduleOnDay(d.value)
                    ? 'border-orange-300 bg-orange-50/70 shadow-2xs'
                    : 'border-gray-100 bg-gray-50/50 opacity-60'
                ]"
              >
                <div>
                  <span class="block text-xs font-bold text-gray-700">{{ d.label }}</span>
                  <span class="block text-[10px] text-gray-400">{{ d.full }}</span>
                </div>

                <div v-if="hasScheduleOnDay(d.value)" class="my-2 space-y-1">
                  <div
                    v-for="sch in getSchedulesOnDay(d.value)"
                    :key="sch.id"
                    @click="openEditScheduleModal(sch)"
                    class="bg-white border border-orange-200 p-1.5 rounded-lg text-left shadow-2xs hover:border-orange-400 cursor-pointer transition-all"
                  >
                    <p class="text-[10px] font-mono font-bold text-orange-700 leading-tight">
                      {{ formatTime(sch.startTime) }}
                    </p>
                    <p v-if="sch.room" class="text-[9px] text-gray-500 truncate mt-0.5">
                      {{ sch.room }}
                    </p>
                  </div>
                </div>
                <div v-else class="text-[10px] text-gray-400 italic my-auto">
                  Nghỉ
                </div>

                <div class="pt-1 border-t border-gray-100/80">
                  <span
                    v-if="hasScheduleOnDay(d.value)"
                    class="text-[10px] font-bold text-orange-600"
                  >
                    {{ getSchedulesOnDay(d.value).length }} ca
                  </span>
                  <span v-else class="text-[10px] text-gray-300">-</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- ─── Add / Edit Class Modal ───────────────────────────────────── -->
      <ClassModal
        :show="showClassModal"
        :cls="cls"
        @close="showClassModal = false"
        @saved="loadAllData"
        @deleted="handleClassDeleted"
      />

      <!-- ─── Add / Edit Schedule Modal ────────────────────────────────── -->
      <ScheduleModal
        :show="showScheduleModal"
        :schedule="selectedSchedule"
        :classes="[cls]"
        :default-class-id="cls.id"
        :initial-mode="scheduleModalMode"
        @close="showScheduleModal = false"
        @saved="loadSchedules"
        @deleted="loadSchedules"
      />
    </template>

    <!-- ─── Not Found / Error ─────────────────────────────────────────────── -->
    <div v-else class="flex flex-col items-center justify-center py-20 gap-3 bg-white rounded-2xl border border-orange-100">
      <ExclamationCircleIcon class="w-12 h-12 text-red-400" />
      <p class="text-gray-700 font-bold text-lg">Không tìm thấy thông tin lớp học</p>
      <p class="text-gray-400 text-sm">Lớp học không tồn tại hoặc đã bị xóa khỏi hệ thống.</p>
      <router-link to="/training/classes" class="mt-2 text-sm text-orange-600 hover:text-orange-700 underline font-semibold">
        ← Quay lại danh sách lớp học
      </router-link>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  UserGroupIcon, ChevronRightIcon, BookOpenIcon, AcademicCapIcon,
  CalendarDaysIcon, CalendarIcon, ClockIcon, MapPinIcon, CurrencyDollarIcon,
  PencilSquareIcon, PlusIcon, TrashIcon, SparklesIcon, CheckCircleIcon,
  ExclamationCircleIcon,
} from '@heroicons/vue/24/outline'

import LoadingSpinner from '@/components/common/LoadingSpinner.vue'
import EmptyState     from '@/components/common/EmptyState.vue'
import StatusBadge    from '@/components/course/StatusBadge.vue'
import ClassModal     from '@/components/class/ClassModal.vue'
import ScheduleModal  from '@/components/schedule/ScheduleModal.vue'

import classService         from '@/services/class.service'
import classScheduleService from '@/services/classSchedule.service'
import courseLevelService   from '@/services/courseLevel.service'
import courseService        from '@/services/course.service'
import teacherService       from '@/services/teacher.service'

const route  = useRoute()
const router = useRouter()

// ─── State ───────────────────────────────────────────────────────────────────
const loading          = ref(true)
const loadingSchedules = ref(false)
const cls              = ref(null)
const level            = ref(null)
const course           = ref(null)
const teacher          = ref(null)
const schedules        = ref([])

// Modals State
const showClassModal    = ref(false)
const showScheduleModal = ref(false)
const selectedSchedule  = ref(null)
const scheduleModalMode = ref('create')

// ─── Days of Week Constants ───────────────────────────────────────────────────
const DAYS_OF_WEEK = [
  { value: 1, label: 'Thứ 2', full: 'Thứ Hai', color: 'border-blue-200 bg-blue-50 text-blue-700' },
  { value: 2, label: 'Thứ 3', full: 'Thứ Ba', color: 'border-emerald-200 bg-emerald-50 text-emerald-700' },
  { value: 3, label: 'Thứ 4', full: 'Thứ Tư', color: 'border-amber-200 bg-amber-50 text-amber-700' },
  { value: 4, label: 'Thứ 5', full: 'Thứ Năm', color: 'border-purple-200 bg-purple-50 text-purple-700' },
  { value: 5, label: 'Thứ 6', full: 'Thứ Sáu', color: 'border-rose-200 bg-rose-50 text-rose-700' },
  { value: 6, label: 'Thứ 7', full: 'Thứ Bảy', color: 'border-cyan-200 bg-cyan-50 text-cyan-700' },
  { value: 0, label: 'Chủ nhật', full: 'Chủ Nhật', color: 'border-red-200 bg-red-50 text-red-700' },
]

// ─── Computed ─────────────────────────────────────────────────────────────────
const activeCount = computed(() => cls.value?.activeStudentsCount ?? cls.value?.currentStudents ?? 0)
const maxCapacity = computed(() => cls.value?.maxCapacity ?? cls.value?.maxStudents)
const maxCapacityDisplay = computed(() => maxCapacity.value ?? '∞')

const capacityFillPct = computed(() => {
  if (!maxCapacity.value) return 0
  return Math.min(100, Math.round((activeCount.value / maxCapacity.value) * 100))
})

const courseBenefits = computed(() => {
  if (!course.value?.benefits) return []
  if (Array.isArray(course.value.benefits)) return course.value.benefits
  try {
    return JSON.parse(course.value.benefits)
  } catch {
    return [course.value.benefits]
  }
})

const sortedSchedules = computed(() => {
  const list = [...schedules.value]
  return list.sort((a, b) => {
    // Thứ 2 (1) -> Thứ 7 (6) -> CN (0)
    const dayA = a.dayOfWeek === 0 ? 7 : a.dayOfWeek
    const dayB = b.dayOfWeek === 0 ? 7 : b.dayOfWeek
    if (dayA !== dayB) return dayA - dayB
    return String(a.startTime).localeCompare(String(b.startTime))
  })
})

const weeklyScheduleSummary = computed(() => {
  if (schedules.value.length === 0) return 'Chưa xếp lịch học'
  const dayLabels = [...new Set(sortedSchedules.value.map(s => getDayBadge(s.dayOfWeek).label))]
  return `Học vào: ${dayLabels.join(', ')}`
})

// ─── Day Helpers ──────────────────────────────────────────────────────────────
function getDayBadge(dow) {
  return DAYS_OF_WEEK.find(d => d.value === Number(dow)) ?? {
    label: `Thứ ${dow}`,
    full: `Thứ ${dow}`,
    color: 'border-gray-200 bg-gray-50 text-gray-700',
  }
}

function hasScheduleOnDay(dow) {
  return schedules.value.some(s => Number(s.dayOfWeek) === Number(dow))
}

function getSchedulesOnDay(dow) {
  return schedules.value.filter(s => Number(s.dayOfWeek) === Number(dow))
}

function formatFee(fee) {
  return Number(fee || 0).toLocaleString('vi-VN') + 'đ'
}

function formatDate(d) {
  if (!d) return '--'
  return new Date(d).toLocaleDateString('vi-VN')
}

function formatTime(t) {
  if (!t) return '--:--'
  const parts = String(t).split(':')
  if (parts.length >= 2) return `${parts[0].padStart(2, '0')}:${parts[1].padStart(2, '0')}`
  return t
}

function calcDuration(start, end) {
  if (!start || !end) return ''
  const [sH, sM] = String(start).split(':').map(Number)
  const [eH, eM] = String(end).split(':').map(Number)
  if (isNaN(sH) || isNaN(eH)) return ''
  const diff = (eH * 60 + (eM || 0)) - (sH * 60 + (sM || 0))
  if (diff <= 0) return ''
  return `${diff} phút`
}

// ─── Modal Actions ────────────────────────────────────────────────────────────
function openEditClassModal() {
  showClassModal.value = true
}

function handleClassDeleted() {
  router.push({ name: 'classes' })
}

function openAddScheduleModal() {
  selectedSchedule.value = null
  scheduleModalMode.value = 'create'
  showScheduleModal.value = true
}

function openEditScheduleModal(item) {
  selectedSchedule.value = item
  scheduleModalMode.value = 'edit'
  showScheduleModal.value = true
}

async function confirmDeleteSchedule(item) {
  const dInfo = getDayBadge(item.dayOfWeek)
  const msg = `Bạn có chắc chắn muốn xóa buổi học ${dInfo.label} (${formatTime(item.startTime)} - ${formatTime(item.endTime)}) của lớp không?`
  if (!window.confirm(msg)) return

  try {
    await classScheduleService.delete(item.id)
    await loadSchedules()
  } catch (err) {
    console.error('Lỗi khi xóa lịch học:', err)
    alert(err?.response?.data?.message || 'Không thể xóa lịch học. Vui lòng thử lại.')
  }
}

// ─── Data Loading ─────────────────────────────────────────────────────────────
async function loadSchedules() {
  const classId = route.params.id
  loadingSchedules.value = true
  try {
    const data = await classScheduleService.getByClass(classId)
    schedules.value = Array.isArray(data) ? data : (data.items ?? data.data ?? [])
  } catch (err) {
    console.error('Lỗi khi tải lịch học của lớp:', err)
    schedules.value = []
  } finally {
    loadingSchedules.value = false
  }
}

async function loadAllData() {
  const classId = route.params.id
  loading.value = true
  try {
    const classData = await classService.get(classId)
    cls.value = classData

    if (classData?.courseLevelId) {
      try {
        const levelData = await courseLevelService.get(classData.courseLevelId)
        level.value = levelData

        if (levelData?.courseId) {
          const courseData = await courseService.get(levelData.courseId)
          course.value = courseData
        }
      } catch (err) {
        console.error('Lỗi khi tải cấp độ / khóa học:', err)
      }
    }

    if (classData?.teacherId) {
      try {
        teacher.value = await teacherService.get(classData.teacherId)
      } catch (err) {
        console.error('Lỗi khi tải thông tin giáo viên:', err)
      }
    }

    await loadSchedules()
  } catch (err) {
    console.error('Không tìm thấy lớp học:', err)
    cls.value = null
  } finally {
    loading.value = false
  }
}

onMounted(loadAllData)
</script>
