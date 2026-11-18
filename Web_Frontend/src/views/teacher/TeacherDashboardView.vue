<template>
  <div class="space-y-6">
    <!-- ─── Welcome Banner ───────────────────────────────────────────────── -->
    <div class="rounded-lg bg-white border border-stone-300 border-l-4 border-l-orange-600 p-5 shadow-[0_1px_2px_rgba(0,0,0,0.04)]">
      <div class="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div class="flex items-center gap-4">
          <div class="w-12 h-12 rounded-md bg-orange-50 border border-orange-200 flex items-center justify-center text-orange-700 text-lg font-bold flex-shrink-0 overflow-hidden">
            <img v-if="teacherAvatar" :src="teacherAvatar" :alt="teacherName" class="w-full h-full object-cover" />
            <span v-else>{{ teacherInitials }}</span>
          </div>
          <div>
            <div class="flex flex-wrap items-center gap-2">
              <span class="rounded border border-orange-300 bg-orange-50 text-orange-800 px-2 py-0.5 text-[11px] font-semibold uppercase tracking-wide">
                Cổng Giáo Viên
              </span>
              <span class="text-xs font-medium text-stone-500 tabular-nums">
                {{ formattedToday }}
              </span>
            </div>
            <h1 class="text-lg md:text-xl font-bold text-stone-900 mt-1">
              Xin chào, {{ teacherName }}!
            </h1>
            <p class="text-xs md:text-sm text-stone-600 mt-0.5">
              Chúc thầy/cô một ngày giảng dạy tràn đầy năng lượng và hiệu quả.
            </p>
          </div>
        </div>

        <!-- Quick navigation shortcuts -->
        <div class="flex flex-wrap items-center gap-2">
          <button
            type="button"
            :disabled="isLoading"
            @click="loadDashboardData"
            class="inline-flex items-center gap-1.5 h-9 px-3 rounded-md bg-white hover:bg-stone-100 text-stone-700 border border-stone-300 hover:border-stone-400 text-xs font-semibold transition-colors cursor-pointer disabled:opacity-50"
            title="Tải lại dữ liệu tổng quan"
          >
            <ArrowPathIcon class="w-4 h-4 text-orange-600" :class="{ 'animate-spin': isLoading }" />
            <span>Làm mới</span>
          </button>
          <router-link
            to="/teacher/attendance"
            class="inline-flex items-center gap-1.5 h-9 px-3.5 rounded-md bg-orange-600 hover:bg-orange-700 active:bg-orange-800 text-white border border-orange-700 text-xs font-semibold shadow-xs transition-colors cursor-pointer"
          >
            <ClipboardDocumentCheckIcon class="w-4 h-4" />
            Điểm danh ngay
          </router-link>
          <router-link
            to="/teacher/assignments"
            class="inline-flex items-center gap-1.5 h-9 px-3.5 rounded-md bg-white hover:bg-stone-100 text-stone-800 border border-stone-300 hover:border-stone-400 text-xs font-semibold transition-colors cursor-pointer"
          >
            <PlusCircleIcon class="w-4 h-4 text-orange-600" />
            Giao bài tập
          </router-link>
        </div>
      </div>
    </div>

    <!-- ─── Error Alert ──────────────────────────────────────────────────── -->
    <div
      v-if="errorMessage"
      class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 rounded-lg border border-rose-200 bg-rose-50 px-4 py-3 text-xs text-rose-800"
    >
      <span>{{ errorMessage }}</span>
      <button
        type="button"
        @click="loadDashboardData"
        class="shrink-0 font-semibold text-rose-700 underline cursor-pointer"
      >
        Thử lại
      </button>
    </div>

    <!-- ─── Stat Cards ───────────────────────────────────────────────────── -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <div
        v-for="stat in quickStats"
        :key="stat.label"
        class="bg-white rounded-lg p-4 border border-stone-300 shadow-[0_1px_2px_rgba(0,0,0,0.04)] flex items-center gap-3.5"
      >
        <div :class="['w-11 h-11 rounded-md border flex items-center justify-center flex-shrink-0', stat.bg]">
          <component :is="stat.icon" class="w-5 h-5" />
        </div>
        <div class="min-w-0 flex-1">
          <p class="text-xs text-stone-500 font-semibold uppercase tracking-wide truncate">{{ stat.label }}</p>
          <p class="text-xl font-bold text-stone-900 tabular-nums mt-0.5">
            {{ isLoading ? 'Đang tải…' : stat.value }}
          </p>
          <p class="text-xs text-stone-600 font-medium mt-0.5 truncate">{{ stat.sub }}</p>
        </div>
      </div>
    </div>

    <!-- ─── Main Two Columns ─────────────────────────────────────────────── -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <!-- Left 2 Cols: Lịch dạy hôm nay / Tuần này -->
      <div class="lg:col-span-2 space-y-4">
        <div class="bg-white rounded-lg border border-stone-300 shadow-[0_1px_2px_rgba(0,0,0,0.04)] p-5">
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-4 border-b border-stone-200">
            <div>
              <h2 class="text-base font-bold text-stone-900 flex items-center gap-2">
                <CalendarDaysIcon class="w-5 h-5 text-orange-600" />
                Lịch dạy học của tôi
              </h2>
              <p class="text-xs text-stone-500 mt-0.5">Theo dõi các ca dạy trong ngày và kế hoạch cả tuần</p>
            </div>

            <!-- Tab Switcher -->
            <div class="inline-flex items-center bg-stone-100 border border-stone-300 p-1 rounded-md text-xs font-semibold tabular-nums">
              <button
                type="button"
                @click="scheduleTab = 'today'"
                :class="[
                  'h-7 px-3 rounded transition-colors cursor-pointer',
                  scheduleTab === 'today'
                    ? 'bg-orange-600 text-white shadow-xs'
                    : 'text-stone-700 hover:text-stone-900',
                ]"
              >
                Hôm nay ({{ todaySchedule.length }})
              </button>
              <button
                type="button"
                @click="scheduleTab = 'week'"
                :class="[
                  'h-7 px-3 rounded transition-colors cursor-pointer',
                  scheduleTab === 'week'
                    ? 'bg-orange-600 text-white shadow-xs'
                    : 'text-stone-700 hover:text-stone-900',
                ]"
              >
                Tuần này ({{ weekSchedule.length }})
              </button>
            </div>
          </div>

          <!-- Schedule list content -->
          <div class="pt-4">
            <div v-if="isLoading" class="py-10 text-center space-y-2">
              <ArrowPathIcon class="w-6 h-6 animate-spin text-orange-600 mx-auto" />
              <p class="text-xs text-stone-500">Đang tải lịch dạy từ hệ thống...</p>
            </div>

            <!-- Today's Schedule -->
            <div v-else-if="scheduleTab === 'today'" class="space-y-3">
              <div
                v-if="todaySchedule.length === 0"
                class="py-10 text-center flex flex-col items-center justify-center gap-2"
              >
                <div class="w-12 h-12 rounded-md bg-orange-50 border border-orange-200 flex items-center justify-center text-orange-600">
                  <CheckBadgeIcon class="w-6 h-6" />
                </div>
                <p class="text-sm font-semibold text-stone-800">Hôm nay không có ca dạy nào</p>
                <p class="text-xs text-stone-500 max-w-sm">
                  Thầy/cô có thể chuẩn bị bài giảng, chấm bài tập hoặc xem lịch dạy các ngày tiếp theo trong tuần.
                </p>
                <router-link
                  to="/teacher/schedule"
                  class="mt-2 text-xs font-semibold text-orange-600 hover:text-orange-700 underline"
                >
                  Xem thời khóa biểu đầy đủ
                </router-link>
              </div>

              <div
                v-for="item in todaySchedule"
                :key="item.id"
                class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 p-3.5 rounded-md border border-stone-300 bg-stone-50/50 hover:bg-orange-50/40 hover:border-stone-400 transition-colors"
              >
                <div class="flex items-start gap-3">
                  <div class="w-12 h-12 rounded-md bg-orange-50 border border-orange-200 flex flex-col items-center justify-center text-orange-800 flex-shrink-0 tabular-nums">
                    <span class="text-[10px] font-semibold uppercase">Ca</span>
                    <span class="text-sm font-bold leading-tight">{{ item.shift }}</span>
                  </div>
                  <div>
                    <div class="flex flex-wrap items-center gap-2">
                      <span class="text-xs font-bold text-stone-900">{{ item.className }}</span>
                      <span class="text-[11px] font-semibold px-2 py-0.5 rounded border border-stone-300 bg-white text-stone-700">
                        {{ item.lessonNumber }} · {{ item.room }}
                      </span>
                      <span
                        :class="[
                          'text-[11px] font-semibold px-2 py-0.5 rounded border',
                          item.status === 'upcoming' ? 'border-amber-300 bg-amber-50 text-amber-800' :
                          item.status === 'in_progress' ? 'border-emerald-300 bg-emerald-50 text-emerald-800' :
                          'border-stone-300 bg-stone-100 text-stone-700',
                        ]"
                      >
                        {{ item.statusLabel }}
                      </span>
                    </div>
                    <div class="flex items-center gap-3 text-xs text-stone-600 mt-1 tabular-nums">
                      <span class="flex items-center gap-1">
                        <ClockIcon class="w-3.5 h-3.5 text-stone-400" />
                        {{ item.time }}
                      </span>
                      <span class="text-stone-300">•</span>
                      <span>Sĩ số: {{ item.studentsCount }} học viên</span>
                    </div>
                  </div>
                </div>

                <div class="flex items-center gap-2 self-end sm:self-center">
                  <router-link
                    :to="{ path: '/teacher/attendance', query: { lessonId: item.id, classId: item.classId } }"
                    class="inline-flex items-center h-8 px-3 rounded-md bg-orange-600 hover:bg-orange-700 text-white border border-orange-700 text-xs font-semibold shadow-xs transition-colors"
                  >
                    Điểm danh
                  </router-link>
                  <router-link
                    :to="{ path: '/teacher/students', query: { classId: item.classId } }"
                    class="inline-flex items-center h-8 px-2.5 rounded-md bg-white border border-stone-300 text-stone-800 hover:bg-stone-100 hover:border-stone-400 text-xs font-semibold transition-colors"
                  >
                    Vào lớp
                  </router-link>
                </div>
              </div>
            </div>

            <!-- Week Schedule -->
            <div v-else class="space-y-2">
              <div
                v-if="weekSchedule.length === 0"
                class="py-10 text-center flex flex-col items-center justify-center gap-2"
              >
                <div class="w-12 h-12 rounded-md bg-stone-100 border border-stone-300 flex items-center justify-center text-stone-500">
                  <CalendarDaysIcon class="w-6 h-6" />
                </div>
                <p class="text-sm font-semibold text-stone-800">Tuần này chưa có ca dạy nào</p>
                <router-link
                  to="/teacher/schedule"
                  class="mt-1 text-xs font-semibold text-orange-600 hover:text-orange-700 underline"
                >
                  Xem thời khóa biểu đầy đủ
                </router-link>
              </div>

              <div
                v-for="item in weekSchedule"
                :key="item.id"
                class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 p-3 rounded-md border border-stone-300 hover:bg-stone-50 transition-colors"
              >
                <div class="flex items-center gap-3">
                  <div
                    :class="[
                      'w-10 h-10 rounded-md border text-xs font-bold flex items-center justify-center tabular-nums flex-shrink-0',
                      item.isToday
                        ? 'bg-orange-600 border-orange-700 text-white'
                        : 'bg-orange-50 border-orange-200 text-orange-800',
                    ]"
                  >
                    {{ item.dayLabel }}
                  </div>
                  <div>
                    <p class="text-xs font-bold text-stone-900">
                      {{ item.className }}
                      <span class="font-medium text-stone-500">({{ item.lessonNumber }} · {{ item.room }})</span>
                    </p>
                    <p class="text-xs text-stone-500 tabular-nums">{{ item.time }} · {{ item.date }}</p>
                  </div>
                </div>
                <div class="flex items-center gap-2 self-end sm:self-center">
                  <span class="text-xs font-semibold text-stone-700 bg-stone-100 border border-stone-300 px-2.5 py-0.5 rounded tabular-nums">
                    {{ item.studentsCount }} học sinh
                  </span>
                  <router-link
                    :to="{ path: '/teacher/attendance', query: { lessonId: item.id, classId: item.classId } }"
                    class="inline-flex items-center h-7 px-2.5 rounded border border-orange-300 bg-orange-50 hover:bg-orange-100 text-orange-800 text-[11px] font-semibold transition-colors"
                  >
                    Điểm danh
                  </router-link>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Quick Task Cards -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div class="bg-white rounded-lg border border-stone-300 p-4 shadow-[0_1px_2px_rgba(0,0,0,0.04)] hover:border-stone-400 transition-colors flex flex-col justify-between">
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-md bg-orange-50 border border-orange-200 text-orange-600 flex items-center justify-center flex-shrink-0">
                <DocumentCheckIcon class="w-5 h-5" />
              </div>
              <div class="min-w-0 flex-1">
                <h3 class="text-sm font-bold text-stone-900">Bài tập cần chấm</h3>
                <p class="text-xs text-stone-500 tabular-nums mt-0.5">
                  {{ isLoading ? 'Đang kiểm tra bài nộp...' : gradingSummaryText }}
                </p>
              </div>
            </div>
            <router-link
              to="/teacher/grading"
              class="mt-3 block text-right text-xs font-bold text-orange-600 hover:text-orange-700"
            >
              Chấm bài ngay →
            </router-link>
          </div>

          <div class="bg-white rounded-lg border border-stone-300 p-4 shadow-[0_1px_2px_rgba(0,0,0,0.04)] hover:border-stone-400 transition-colors flex flex-col justify-between">
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-md bg-amber-50 border border-amber-200 text-amber-700 flex items-center justify-center flex-shrink-0">
                <ChatBubbleBottomCenterTextIcon class="w-5 h-5" />
              </div>
              <div class="min-w-0 flex-1">
                <h3 class="text-sm font-bold text-stone-900">Phản hồi phụ huynh &amp; Đơn nghỉ</h3>
                <p class="text-xs text-stone-500 tabular-nums mt-0.5">
                  {{ isLoading ? 'Đang kiểm tra tin nhắn & đơn từ...' : parentCommSummaryText }}
                </p>
              </div>
            </div>
            <div class="mt-3 flex items-center justify-end gap-3">
              <router-link
                v-if="commSummary.pendingLeaveRequests > 0"
                to="/teacher/attendance"
                class="text-xs font-bold text-amber-700 hover:text-amber-800"
              >
                Duyệt đơn ({{ commSummary.pendingLeaveRequests }}) →
              </router-link>
              <router-link
                to="/teacher/communication/chat"
                class="text-xs font-bold text-orange-600 hover:text-orange-700"
              >
                Mở hộp thoại chat →
              </router-link>
            </div>
          </div>
        </div>
      </div>

      <!-- Right 1 Col: Thông báo từ trung tâm -->
      <div class="space-y-4">
        <div class="bg-white rounded-lg border border-stone-300 shadow-[0_1px_2px_rgba(0,0,0,0.04)] p-5 flex flex-col h-full">
          <div class="flex items-center justify-between pb-3 border-b border-stone-200">
            <h2 class="text-base font-bold text-stone-900 flex items-center gap-2">
              <BellAlertIcon class="w-5 h-5 text-orange-600" />
              Thông báo từ trung tâm
            </h2>
            <span
              :class="[
                'text-xs font-semibold px-2 py-0.5 rounded border tabular-nums',
                unreadCount > 0
                  ? 'text-orange-800 bg-orange-50 border-orange-300'
                  : 'text-stone-600 bg-stone-100 border-stone-300',
              ]"
            >
              {{ unreadCount > 0 ? `${unreadCount} chưa đọc` : 'Đã đọc hết' }}
            </span>
          </div>

          <!-- Loading / Empty -->
          <div v-if="notifLoading && centerNotices.length === 0" class="py-12 text-center space-y-2 flex-1">
            <ArrowPathIcon class="w-5 h-5 animate-spin text-orange-600 mx-auto" />
            <p class="text-xs text-stone-500">Đang tải thông báo...</p>
          </div>

          <div v-else-if="centerNotices.length === 0" class="py-12 text-center space-y-1.5 flex-1">
            <BellAlertIcon class="w-8 h-8 text-stone-300 mx-auto" />
            <p class="text-xs font-semibold text-stone-700">Chưa có thông báo nào</p>
            <p class="text-[11px] text-stone-400">Thông báo mới từ trung tâm sẽ xuất hiện tại đây.</p>
          </div>

          <!-- Notification items -->
          <div v-else class="divide-y divide-stone-200 flex-1 overflow-y-auto max-h-[460px]">
            <div
              v-for="notice in centerNotices"
              :key="notice.id"
              :class="[
                'py-3 hover:bg-orange-50/40 transition-colors rounded-md px-2 cursor-pointer',
                !notice.isRead ? 'bg-orange-50/35' : '',
              ]"
              @click="openNoticeDetail(notice)"
            >
              <div class="flex items-start gap-2.5">
                <span
                  :class="[
                    'w-2 h-2 rounded-full mt-1.5 flex-shrink-0',
                    !notice.isRead
                      ? (notice.type === 'LEAVE_REQUEST' ? 'bg-rose-600' : 'bg-orange-600')
                      : 'bg-stone-300',
                  ]"
                ></span>
                <div class="min-w-0 flex-1">
                  <div class="flex items-center justify-between gap-2">
                    <p :class="['text-xs leading-snug line-clamp-1', !notice.isRead ? 'font-bold text-stone-900' : 'font-semibold text-stone-800']">
                      {{ notice.title }}
                    </p>
                    <span
                      :class="[
                        'text-[10px] font-semibold px-1.5 py-0.5 rounded border shrink-0',
                        getNotificationTypeMeta(notice.type).badgeClass,
                      ]"
                    >
                      {{ getNotificationTypeMeta(notice.type).label }}
                    </span>
                  </div>
                  <p class="text-xs text-stone-600 line-clamp-2 mt-1 leading-relaxed">
                    {{ notice.body || 'Không có nội dung chi tiết.' }}
                  </p>
                  <div class="flex items-center justify-between text-[11px] text-stone-400 mt-2 tabular-nums">
                    <span>{{ formatRelativeNotificationTime(notice.createdAt) }}</span>
                    <button
                      type="button"
                      class="inline-flex items-center gap-1 font-semibold text-orange-600 hover:text-orange-700 hover:underline cursor-pointer"
                      @click.stop="openNoticeDetail(notice)"
                    >
                      <EyeIcon class="w-3.5 h-3.5" />
                      <span>Xem chi tiết</span>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="pt-3 border-t border-stone-200 text-center">
            <router-link
              to="/teacher/communication/notices?tab=inbox"
              class="text-xs font-semibold text-orange-600 hover:text-orange-700"
            >
              Xem tất cả thông báo →
            </router-link>
          </div>
        </div>
      </div>
    </div>

    <!-- Notice Quick-View Modal -->
    <NotificationDetailModal
      :open="showNoticeModal"
      :notification="selectedNotice"
      user-role="teacher"
      @close="showNoticeModal = false"
    />
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useUserState } from '@/services/user-state'
import { getProfileId } from '@/services/auth-storage'
import classService from '@/services/class.service'
import lessonService from '@/services/lesson.service'
import classScheduleService from '@/services/classSchedule.service'
import teacherService from '@/services/teacher.service'
import attendanceService from '@/services/attendance.service'
import assignmentService from '@/services/assignment.service'
import chatService from '@/services/chat.service'
import leaveRequestService from '@/services/leaveRequest.service'
import { useNotificationState } from '@/services/notification-state'
import {
  getNotificationTypeMeta,
  formatRelativeNotificationTime,
} from '@/utils/notification.helper'
import NotificationDetailModal from '@/components/notification/NotificationDetailModal.vue'
import {
  CalendarDaysIcon,
  ClipboardDocumentCheckIcon,
  PlusCircleIcon,
  ClockIcon,
  CheckBadgeIcon,
  DocumentCheckIcon,
  ChatBubbleBottomCenterTextIcon,
  BellAlertIcon,
  AcademicCapIcon,
  UserGroupIcon,
  BookOpenIcon,
  TrophyIcon,
  ArrowPathIcon,
  EyeIcon,
} from '@heroicons/vue/24/outline'

const { profile, fetchProfile } = useUserState()
const {
  items: notificationItems,
  unreadCount,
  isLoading: notifLoading,
  fetchRecentNotifications,
  markNotificationRead,
} = useNotificationState()

const isLoading = ref(false)
const errorMessage = ref('')
const scheduleTab = ref('today')

const assignedClasses = ref([])
const todaySchedule = ref([])
const weekSchedule = ref([])

const attendanceSummary = ref({
  rateDisplay: '—',
  sub: 'Chưa có dữ liệu điểm danh',
})

const gradingSummary = ref({
  ungradedCount: 0,
  totalAssignments: 0,
  classNames: [],
})

const commSummary = ref({
  unreadMessages: 0,
  pendingLeaveRequests: 0,
})

const showNoticeModal = ref(false)
const selectedNotice = ref(null)

const teacherName = computed(() => profile.value?.fullName || 'Thầy/Cô')
const teacherAvatar = computed(() => profile.value?.avatar || '')
const teacherInitials = computed(() => {
  const name = teacherName.value.trim()
  const parts = name.split(/\s+/)
  return parts.length >= 2
    ? (parts[parts.length - 2][0] + parts[parts.length - 1][0]).toUpperCase()
    : name.slice(0, 2).toUpperCase()
})

const formattedToday = computed(() => {
  return new Date().toLocaleDateString('vi-VN', {
    weekday: 'long',
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  })
})

const centerNotices = computed(() => (notificationItems.value || []).slice(0, 8))

const activeClassesCount = computed(() => {
  return assignedClasses.value.filter((c) => {
    const st = (c.status || '').toUpperCase()
    return !['COMPLETED', 'CLOSED', 'FINISHED', 'CANCELLED'].includes(st)
  }).length
})

const totalStudentsCount = computed(() => {
  return assignedClasses.value.reduce((sum, c) => sum + (Number(c.activeStudentsCount) || 0), 0)
})

const quickStats = computed(() => {
  const todaySub = todaySchedule.value.length > 0
    ? todaySchedule.value
        .slice(0, 2)
        .map((s) => s.startLabel)
        .filter(Boolean)
        .join(' · ') + (todaySchedule.value.length > 2 ? ` (+${todaySchedule.value.length - 2})` : '')
    : 'Hôm nay không có ca dạy'

  return [
    {
      label: 'Lớp học phụ trách',
      value: `${activeClassesCount.value} Lớp`,
      sub: assignedClasses.value.length > 0
        ? `Tổng ${assignedClasses.value.length} lớp được phân công`
        : 'Chưa có lớp được phân công',
      icon: AcademicCapIcon,
      bg: 'bg-orange-50 text-orange-600 border-orange-200',
    },
    {
      label: 'Tổng số học sinh',
      value: `${totalStudentsCount.value} HS`,
      sub: assignedClasses.value.length > 0
        ? `Trên ${assignedClasses.value.length} lớp phụ trách`
        : 'Chưa có học viên',
      icon: UserGroupIcon,
      bg: 'bg-stone-100 text-stone-700 border-stone-300',
    },
    {
      label: 'Tiết dạy hôm nay',
      value: `${todaySchedule.value.length} Ca`,
      sub: todaySub,
      icon: BookOpenIcon,
      bg: 'bg-emerald-50 text-emerald-700 border-emerald-200',
    },
    {
      label: 'Tỷ lệ chuyên cần',
      value: attendanceSummary.value.rateDisplay,
      sub: attendanceSummary.value.sub,
      icon: TrophyIcon,
      bg: 'bg-amber-50 text-amber-700 border-amber-200',
    },
  ]
})

const gradingSummaryText = computed(() => {
  if (gradingSummary.value.ungradedCount > 0) {
    const clsNames = gradingSummary.value.classNames.slice(0, 2).join(', ')
    const suffix = gradingSummary.value.classNames.length > 2
      ? ` và ${gradingSummary.value.classNames.length - 2} lớp khác`
      : ''
    return `${gradingSummary.value.ungradedCount} bài nộp chờ chấm${clsNames ? ` (${clsNames}${suffix})` : ''}`
  }
  if (gradingSummary.value.totalAssignments > 0) {
    return `${gradingSummary.value.totalAssignments} bài tập gần đây đã được chấm xong`
  }
  return 'Chưa có bài tập nào cần chấm'
})

const parentCommSummaryText = computed(() => {
  const msgs = commSummary.value.unreadMessages
  const leaves = commSummary.value.pendingLeaveRequests
  if (msgs === 0 && leaves === 0) {
    return 'Không có tin nhắn chưa đọc hay đơn nghỉ chờ duyệt'
  }
  const parts = []
  if (msgs > 0) parts.push(`${msgs} tin nhắn chưa đọc`)
  if (leaves > 0) parts.push(`${leaves} đơn xin nghỉ chờ duyệt`)
  return parts.join(' · ')
})

async function openNoticeDetail(notice) {
  if (!notice) return
  selectedNotice.value = { ...notice, isRead: true }
  showNoticeModal.value = true
  if (!notice.isRead) {
    await markNotificationRead(notice.id)
  }
}

async function resolveTeacherClasses(userProfile) {
  const rawClasses = await classService.getAll()
  const list = Array.isArray(rawClasses) ? rawClasses : (rawClasses?.items || [])
  const role = (userProfile?.role || '').toLowerCase()
  if (role === 'admin') return list

  let teacherProfileId = getProfileId()
  const teacherFullName = (userProfile?.fullName || '').trim().toLowerCase()
  const teacherPhone = (userProfile?.phone || '').trim()

  if (!teacherProfileId && role === 'teacher') {
    try {
      const teachersData = await teacherService.getAll()
      const rawTeachers = Array.isArray(teachersData) ? teachersData : (teachersData?.items || [])
      const matched = rawTeachers.find(
        (t) =>
          (userProfile?.accountId && t.accountId === userProfile.accountId) ||
          (teacherPhone && t.phone === teacherPhone) ||
          (teacherFullName && t.fullName && t.fullName.trim().toLowerCase() === teacherFullName)
      )
      if (matched) teacherProfileId = matched.id
    } catch {
      // Bỏ qua nếu không tra cứu được danh sách giáo viên
    }
  }

  return list.filter((c) => {
    if (teacherProfileId && c.teacherId === teacherProfileId) return true
    if (teacherFullName && c.teacherName && c.teacherName.trim().toLowerCase() === teacherFullName) return true
    return false
  })
}

async function loadDashboardData() {
  isLoading.value = true
  errorMessage.value = ''

  try {
    const userProfile = await fetchProfile()
    const role = (userProfile?.role || '').toLowerCase()

    const [classesResult, , conversationsResult, leaveRequestsResult] = await Promise.all([
      resolveTeacherClasses(userProfile),
      fetchRecentNotifications({ pageSize: 8, force: true }),
      chatService.getConversations().catch(() => []),
      role === 'teacher' ? leaveRequestService.getPending().catch(() => []) : Promise.resolve([]),
    ])

    assignedClasses.value = classesResult

    // Unread parent messages & pending leave requests
    const conversations = Array.isArray(conversationsResult) ? conversationsResult : []
    const unreadMessages = conversations.reduce((sum, conv) => sum + (Number(conv.unreadCount) || 0), 0)
    const pendingLeaves = Array.isArray(leaveRequestsResult)
      ? leaveRequestsResult
      : (leaveRequestsResult?.items || [])
    commSummary.value = {
      unreadMessages,
      pendingLeaveRequests: pendingLeaves.length,
    }

    // Fetch lessons and schedules per assigned class
    const perClassData = await Promise.all(
      assignedClasses.value.map(async (cls) => {
        const [lessonsRes, schedulesRes] = await Promise.all([
          lessonService.getByClass(cls.id).catch(() => []),
          classScheduleService.getByClass(cls.id).catch(() => []),
        ])
        const lessons = Array.isArray(lessonsRes) ? lessonsRes : (lessonsRes?.items || [])
        const schedules = Array.isArray(schedulesRes) ? schedulesRes : []
        const room = schedules.find((s) => s.room)?.room || cls.room || 'Phòng chính'

        const sortedLessons = [...lessons].sort((a, b) => {
          const diff = new Date(a.lessonDate).getTime() - new Date(b.lessonDate).getTime()
          if (diff !== 0) return diff
          return String(a.startTime || '').localeCompare(String(b.startTime || ''))
        })

        return sortedLessons.map((les, idx) => ({
          ...les,
          lessonNumber: `Buổi ${idx + 1}`,
          className: les.className || cls.name,
          room,
          studentsCount: cls.activeStudentsCount || 0,
        }))
      })
    )

    const allLessons = perClassData.flat()
    buildSchedules(allLessons)

    // Load real attendance stats & pending grading submissions in parallel
    await Promise.all([
      loadAttendanceStats(allLessons),
      loadGradingStats(allLessons),
    ])
  } catch (err) {
    console.error('Lỗi khi tải dữ liệu tổng quan giáo viên:', err)
    errorMessage.value = err.response?.data?.message || 'Không thể tải đầy đủ dữ liệu tổng quan. Vui lòng thử lại.'
  } finally {
    isLoading.value = false
  }
}

function buildSchedules(allLessons) {
  const now = new Date()
  const todayYmd = formatYMD(now)
  const weekStart = getStartOfWeek(now)
  const weekEnd = new Date(weekStart)
  weekEnd.setDate(weekEnd.getDate() + 6)
  weekEnd.setHours(23, 59, 59, 999)

  // Today's lessons
  const todayLessons = allLessons
    .filter((l) => getLessonYmd(l.lessonDate) === todayYmd)
    .sort((a, b) => getTimeSortKey(a.startTime) - getTimeSortKey(b.startTime))

  todaySchedule.value = todayLessons.map((les, idx) => {
    const startLabel = formatTimeOnly(les.startTime) || '--:--'
    const endLabel = formatTimeOnly(les.endTime) || '--:--'
    const timing = computeLessonStatus(les, now)
    return {
      id: les.id,
      classId: les.classId,
      shift: String(idx + 1).padStart(2, '0'),
      className: les.className,
      lessonNumber: les.lessonNumber,
      room: les.room,
      startLabel,
      time: `${startLabel} - ${endLabel}`,
      studentsCount: les.studentsCount,
      status: timing.status,
      statusLabel: timing.label,
    }
  })

  // Week's lessons (Mon -> Sun)
  const weekLessons = allLessons
    .filter((l) => {
      const ymd = getLessonYmd(l.lessonDate)
      if (!ymd) return false
      const d = parseYmdLocal(ymd)
      return d >= weekStart && d <= weekEnd
    })
    .sort((a, b) => {
      const dDiff = getLessonYmd(a.lessonDate).localeCompare(getLessonYmd(b.lessonDate))
      if (dDiff !== 0) return dDiff
      return getTimeSortKey(a.startTime) - getTimeSortKey(b.startTime)
    })

  weekSchedule.value = weekLessons.map((les) => {
    const ymd = getLessonYmd(les.lessonDate)
    const d = parseYmdLocal(ymd)
    const isToday = ymd === todayYmd
    const startLabel = formatTimeOnly(les.startTime) || '--:--'
    const endLabel = formatTimeOnly(les.endTime) || '--:--'
    return {
      id: les.id,
      classId: les.classId,
      dayLabel: formatShortDayOfWeek(d.getDay()),
      className: les.className,
      lessonNumber: les.lessonNumber,
      room: les.room,
      time: `${startLabel} - ${endLabel}`,
      date: isToday ? 'Hôm nay' : `${formatFullDayOfWeek(d.getDay())} (${formatDateDM(d)})`,
      isToday,
      studentsCount: les.studentsCount,
    }
  })
}

async function loadAttendanceStats(allLessons) {
  const attendedLessons = allLessons
    .filter((l) => (Number(l.attendancesCount) || 0) > 0)
    .sort((a, b) => new Date(b.lessonDate).getTime() - new Date(a.lessonDate).getTime())

  if (attendedLessons.length === 0) {
    attendanceSummary.value = {
      rateDisplay: '—',
      sub: 'Chưa có buổi học được điểm danh',
    }
    return
  }

  const now = new Date()
  const weekStart = getStartOfWeek(now)
  const thisWeekAttended = attendedLessons.filter((l) => {
    const ymd = getLessonYmd(l.lessonDate)
    return ymd && parseYmdLocal(ymd) >= weekStart
  })

  const targetLessons = (thisWeekAttended.length > 0 ? thisWeekAttended : attendedLessons).slice(0, 12)
  const details = await Promise.all(
    targetLessons.map((l) => attendanceService.getByLesson(l.id).catch(() => null))
  )

  let totalStudents = 0
  let totalPresent = 0
  let validLessons = 0

  for (const detail of details) {
    if (!detail) continue
    const lessonTotal = Number(detail.totalStudents) || 0
    if (lessonTotal > 0) {
      totalStudents += lessonTotal
      totalPresent += Number(detail.presentCount) || 0
      validLessons += 1
    }
  }

  if (totalStudents === 0) {
    attendanceSummary.value = {
      rateDisplay: '—',
      sub: 'Chưa có dữ liệu điểm danh',
    }
    return
  }

  const pct = ((totalPresent / totalStudents) * 100).toFixed(1)
  attendanceSummary.value = {
    rateDisplay: `${pct}%`,
    sub: thisWeekAttended.length > 0
      ? `Tuần này (${validLessons} buổi)`
      : `Trên ${validLessons} buổi gần nhất`,
  }
}

async function loadGradingStats(allLessons) {
  if (allLessons.length === 0) {
    gradingSummary.value = { ungradedCount: 0, totalAssignments: 0, classNames: [] }
    return
  }

  // Prioritize lessons closest to today (up to 24 lessons) to avoid excessive API calls
  const todayMs = Date.now()
  const candidateLessons = [...allLessons]
    .sort((a, b) => Math.abs(new Date(a.lessonDate).getTime() - todayMs) - Math.abs(new Date(b.lessonDate).getTime() - todayMs))
    .slice(0, 24)

  const assignmentLists = await Promise.all(
    candidateLessons.map((l) => assignmentService.getByLesson(l.id).catch(() => []))
  )

  const allAssignments = assignmentLists.flat().filter(Boolean)
  if (allAssignments.length === 0) {
    gradingSummary.value = { ungradedCount: 0, totalAssignments: 0, classNames: [] }
    return
  }

  // Inspect details of assignments that have completed/submitted students to count ungraded SUBMITTED submissions
  const withSubmissions = allAssignments
    .filter((a) => (Number(a.completedStudentsCount) || 0) > 0)
    .slice(0, 12)

  const details = await Promise.all(
    withSubmissions.map((a) => assignmentService.getById(a.id).catch(() => null))
  )

  let ungradedCount = 0
  const classNameSet = new Set()

  for (const detail of details) {
    if (!detail || !Array.isArray(detail.assignedStudents)) continue
    const submittedCount = detail.assignedStudents.filter((st) => st.status === 'SUBMITTED').length
    if (submittedCount > 0) {
      ungradedCount += submittedCount
      if (detail.className) classNameSet.add(detail.className)
    }
  }

  gradingSummary.value = {
    ungradedCount,
    totalAssignments: allAssignments.length,
    classNames: Array.from(classNameSet),
  }
}

// ─── Date & Time Helpers ─────────────────────────────────────────────────────
function getLessonYmd(value) {
  if (!value) return ''
  const str = String(value)
  if (/^\d{4}-\d{2}-\d{2}/.test(str)) return str.slice(0, 10)
  const d = new Date(value)
  return Number.isNaN(d.getTime()) ? '' : formatYMD(d)
}

function parseYmdLocal(ymd) {
  const [y, m, d] = ymd.split('-').map(Number)
  return new Date(y, m - 1, d, 0, 0, 0, 0)
}

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

function getStartOfWeek(date) {
  const d = new Date(date)
  const day = d.getDay()
  const diff = d.getDate() - day + (day === 0 ? -6 : 1)
  const monday = new Date(d.setDate(diff))
  monday.setHours(0, 0, 0, 0)
  return monday
}

function formatShortDayOfWeek(dayIdx) {
  return ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'][dayIdx] || ''
}

function formatFullDayOfWeek(dayIdx) {
  return ['Chủ Nhật', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy'][dayIdx] || ''
}

function formatTimeOnly(value) {
  if (!value) return ''
  const d = new Date(value)
  if (!Number.isNaN(d.getTime())) {
    return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', hour12: false })
  }
  const str = String(value)
  return str.length >= 5 ? str.slice(0, 5) : str
}

function getTimeSortKey(value) {
  if (!value) return 0
  const d = new Date(value)
  if (!Number.isNaN(d.getTime())) {
    return d.getHours() * 60 + d.getMinutes()
  }
  return 0
}

function computeLessonStatus(lesson, now) {
  const start = lesson.startTime ? new Date(lesson.startTime) : null
  const end = lesson.endTime ? new Date(lesson.endTime) : null

  if (start && !Number.isNaN(start.getTime()) && end && !Number.isNaN(end.getTime())) {
    const startMins = start.getHours() * 60 + start.getMinutes()
    const endMins = end.getHours() * 60 + end.getMinutes()
    const nowMins = now.getHours() * 60 + now.getMinutes()
    if (nowMins >= startMins && nowMins <= endMins) {
      return { status: 'in_progress', label: 'Đang diễn ra' }
    }
    if (nowMins > endMins || lesson.status === 'COMPLETED') {
      return { status: 'ended', label: 'Đã kết thúc' }
    }
    return { status: 'upcoming', label: 'Sắp diễn ra' }
  }

  if (lesson.status === 'COMPLETED') {
    return { status: 'ended', label: 'Đã kết thúc' }
  }
  return { status: 'upcoming', label: 'Sắp diễn ra' }
}

onMounted(loadDashboardData)
</script>
