<template>
  <div class="space-y-6">
    <!-- ─── Welcome Banner ───────────────────────────────────────────────── -->
    <div class="relative overflow-hidden rounded-3xl bg-gradient-to-r from-orange-400 via-amber-400 to-orange-500 p-6 text-white shadow-md shadow-orange-200/50">
      <div class="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div class="flex items-center gap-4">
          <div class="w-14 h-14 rounded-2xl bg-white/20 backdrop-blur-md flex items-center justify-center text-white text-2xl font-bold shadow-inner">
            {{ teacherInitials }}
          </div>
          <div>
            <div class="flex items-center gap-2">
              <span class="rounded-full bg-white/25 px-2.5 py-0.5 text-xs font-semibold uppercase tracking-wider backdrop-blur-sm">
                Cổng Giáo Viên
              </span>
              <span class="text-xs text-orange-100">
                {{ formattedToday }}
              </span>
            </div>
            <h1 class="text-xl md:text-2xl font-bold mt-1">
              Xin chào, {{ teacherName }}! 👋
            </h1>
            <p class="text-xs md:text-sm text-orange-100 mt-0.5">
              Chúc thầy/cô một ngày giảng dạy tràn đầy năng lượng và hiệu quả.
            </p>
          </div>
        </div>

        <!-- Quick navigation shortcuts -->
        <div class="flex flex-wrap items-center gap-2">
          <span
            v-if="isLoading"
            class="inline-flex items-center gap-1.5 px-3 py-2 rounded-xl bg-white/20 text-white text-xs font-semibold backdrop-blur-md"
          >
            <ArrowPathIcon class="w-4 h-4 animate-spin" />
            Đang đồng bộ...
          </span>
          <router-link
            to="/teacher/attendance"
            class="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-white text-orange-800 text-xs font-semibold shadow-sm hover:bg-orange-50 transition-all cursor-pointer"
          >
            <ClipboardDocumentCheckIcon class="w-4 h-4 text-orange-500" />
            Điểm danh ngay
          </router-link>
          <router-link
            to="/teacher/assignments"
            class="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-orange-600/30 hover:bg-orange-600/40 text-white text-xs font-semibold backdrop-blur-md transition-all cursor-pointer"
          >
            <PlusCircleIcon class="w-4 h-4" />
            Giao bài tập
          </router-link>
        </div>
      </div>

      <!-- Decorative circles -->
      <div class="absolute -right-8 -bottom-8 w-40 h-40 rounded-full bg-white/10 blur-xl pointer-events-none"></div>
      <div class="absolute left-1/3 -top-10 w-32 h-32 rounded-full bg-white/10 blur-lg pointer-events-none"></div>
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

    <!-- ─── Stat Cards ───────────────────────────────────────────────────── -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <div
        v-for="stat in quickStats"
        :key="stat.label"
        class="bg-white rounded-2xl p-4 border border-orange-100 shadow-sm hover:shadow-md transition-shadow flex items-center gap-3.5"
      >
        <div :class="['w-11 h-11 rounded-xl flex items-center justify-center flex-shrink-0 text-white', stat.bg]">
          <component :is="stat.icon" class="w-5 h-5" />
        </div>
        <div class="min-w-0 flex-1">
          <p class="text-xs text-gray-500 font-medium truncate">{{ stat.label }}</p>
          <p class="text-xl font-bold text-gray-800 mt-0.5">{{ stat.value }}</p>
          <p class="text-[11px] text-orange-600 font-medium mt-0.5 truncate" :title="stat.sub">{{ stat.sub }}</p>
        </div>
      </div>
    </div>

    <!-- ─── Main Two Columns ─────────────────────────────────────────────── -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <!-- Left 2 Cols: Lịch dạy hôm nay / Tuần này -->
      <div class="lg:col-span-2 space-y-4">
        <div class="bg-white rounded-2xl border border-orange-100 shadow-sm p-5">
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-4 border-b border-orange-100">
            <div>
              <h2 class="text-base font-bold text-orange-950 flex items-center gap-2">
                <CalendarDaysIcon class="w-5 h-5 text-orange-500" />
                Lịch dạy học của tôi
              </h2>
              <p class="text-xs text-gray-500 mt-0.5">Theo dõi các ca dạy trong ngày và kế hoạch cả tuần</p>
            </div>

            <!-- Tab Switcher -->
            <div class="flex items-center bg-orange-100/70 p-1 rounded-xl text-xs font-semibold">
              <button
                @click="scheduleTab = 'today'"
                :class="[
                  'px-3 py-1.5 rounded-lg transition-all cursor-pointer',
                  scheduleTab === 'today'
                    ? 'bg-orange-400 text-white shadow-xs'
                    : 'text-orange-800 hover:text-orange-950',
                ]"
              >
                Hôm nay ({{ todaySchedule.length }})
              </button>
              <button
                @click="scheduleTab = 'week'"
                :class="[
                  'px-3 py-1.5 rounded-lg transition-all cursor-pointer',
                  scheduleTab === 'week'
                    ? 'bg-orange-400 text-white shadow-xs'
                    : 'text-orange-800 hover:text-orange-950',
                ]"
              >
                Tuần này ({{ weekSchedule.length }})
              </button>
            </div>
          </div>

          <!-- Schedule list content -->
          <div class="pt-4">
            <!-- Today's Schedule -->
            <div v-if="scheduleTab === 'today'" class="space-y-3">
              <div
                v-if="todaySchedule.length === 0"
                class="py-10 text-center flex flex-col items-center justify-center gap-2"
              >
                <div class="w-14 h-14 rounded-full bg-orange-50 flex items-center justify-center text-orange-400">
                  <CheckBadgeIcon class="w-7 h-7" />
                </div>
                <p class="text-sm font-semibold text-gray-700">Hôm nay không có ca dạy nào</p>
                <p class="text-xs text-gray-400 max-w-sm">
                  Thầy/cô có thể chuẩn bị bài giảng, chấm bài tập hoặc chuẩn bị ngân hàng câu hỏi.
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
                class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 p-3.5 rounded-xl border border-orange-100 bg-orange-50/40 hover:bg-orange-50 hover:border-orange-200 transition-all"
              >
                <div class="flex items-start gap-3">
                  <div class="w-12 h-12 rounded-xl bg-orange-100 border border-orange-200 flex flex-col items-center justify-center text-orange-900 flex-shrink-0">
                    <span class="text-[10px] font-semibold uppercase">Ca</span>
                    <span class="text-sm font-bold leading-tight">{{ item.shift }}</span>
                  </div>
                  <div>
                    <div class="flex items-center gap-2 flex-wrap">
                      <span class="text-xs font-bold text-gray-900">{{ item.className }}</span>
                      <span class="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-orange-200/80 text-orange-900">
                        {{ item.room }}
                      </span>
                      <span
                        :class="[
                          'text-[10px] font-semibold px-2 py-0.5 rounded-full',
                          item.status === 'upcoming' ? 'bg-amber-100 text-amber-700' :
                          item.status === 'in_progress' ? 'bg-emerald-100 text-emerald-700' :
                          'bg-gray-100 text-gray-600',
                        ]"
                      >
                        {{ item.statusLabel }}
                      </span>
                    </div>
                    <div class="flex items-center gap-3 text-xs text-gray-500 mt-1">
                      <span class="flex items-center gap-1">
                        <ClockIcon class="w-3.5 h-3.5 text-orange-400" />
                        {{ item.time }}
                      </span>
                      <span>•</span>
                      <span>Sĩ số: {{ item.studentsCount }} học viên</span>
                    </div>
                  </div>
                </div>

                <div class="flex items-center gap-2 self-end sm:self-center">
                  <router-link
                    :to="{ path: '/teacher/attendance', query: { lessonId: item.lessonId, classId: item.classId } }"
                    class="px-3 py-1.5 rounded-lg bg-orange-400 hover:bg-orange-500 text-white text-xs font-semibold shadow-xs transition-colors"
                  >
                    Điểm danh
                  </router-link>
                  <router-link
                    to="/teacher/classes"
                    class="px-2.5 py-1.5 rounded-lg border border-orange-200 text-orange-800 hover:bg-orange-100 text-xs font-semibold transition-colors"
                  >
                    Vào lớp
                  </router-link>
                </div>
              </div>
            </div>

            <!-- Week Schedule -->
            <div v-else>
              <div
                v-if="weekSchedule.length === 0"
                class="py-10 text-center flex flex-col items-center justify-center gap-2"
              >
                <div class="w-14 h-14 rounded-full bg-orange-50 flex items-center justify-center text-orange-400">
                  <CalendarDaysIcon class="w-7 h-7" />
                </div>
                <p class="text-sm font-semibold text-gray-700">Tuần này chưa có ca dạy nào</p>
                <p class="text-xs text-gray-400 max-w-sm">
                  Lịch dạy sẽ hiển thị tại đây ngay khi lớp học được xếp lịch và đồng bộ buổi học.
                </p>
              </div>

              <div v-else class="space-y-2">
                <div
                  v-for="item in weekSchedule"
                  :key="item.id"
                  class="flex items-center justify-between p-3 rounded-xl border border-orange-100 hover:bg-orange-50/50 transition-colors"
                >
                  <div class="flex items-center gap-3">
                    <div class="w-10 h-10 rounded-lg bg-orange-100 text-orange-900 text-xs font-bold flex items-center justify-center">
                      {{ item.dayLabel }}
                    </div>
                    <div>
                      <p class="text-xs font-bold text-gray-900">{{ item.className }} ({{ item.room }})</p>
                      <p class="text-[11px] text-gray-500">{{ item.time }} · {{ item.date }}</p>
                    </div>
                  </div>
                  <span class="text-xs font-medium text-orange-600 bg-orange-50 border border-orange-200 px-2.5 py-1 rounded-lg">
                    {{ item.studentsCount }} học sinh
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Quick Task Cards -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div class="bg-white rounded-2xl border border-orange-100 p-4 shadow-sm hover:border-orange-300 transition-colors">
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-xl bg-orange-100 text-orange-600 flex items-center justify-center">
                <DocumentCheckIcon class="w-5 h-5" />
              </div>
              <div class="min-w-0 flex-1">
                <h3 class="text-sm font-bold text-gray-900">Bài tập cần chấm</h3>
                <p class="text-xs text-gray-500 line-clamp-2">
                  {{ pendingGrading.total > 0
                    ? `${pendingGrading.total} bài nộp chờ chấm${pendingGrading.className ? ` · ${pendingGrading.className}` : ''}`
                    : 'Không có bài nộp nào đang chờ chấm' }}
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

          <div class="bg-white rounded-2xl border border-orange-100 p-4 shadow-sm hover:border-orange-300 transition-colors">
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-xl bg-amber-100 text-amber-600 flex items-center justify-center">
                <ClipboardDocumentCheckIcon class="w-5 h-5" />
              </div>
              <div class="min-w-0 flex-1">
                <h3 class="text-sm font-bold text-gray-900">Chuyên cần tuần này</h3>
                <p class="text-xs text-gray-500 line-clamp-2">
                  {{ attendanceSummary.marked > 0
                    ? `Tỷ lệ ${attendanceRateLabel} · ${attendanceSummary.absent} lượt vắng`
                    : 'Chưa có dữ liệu điểm danh trong tuần' }}
                </p>
              </div>
            </div>
            <router-link
              to="/teacher/reports/attendance"
              class="mt-3 block text-right text-xs font-bold text-orange-600 hover:text-orange-700"
            >
              Xem báo cáo →
            </router-link>
          </div>
        </div>
      </div>

      <!-- Right 1 Col: Thông báo & nhắc nhở -->
      <div class="space-y-4">
        <div class="bg-white rounded-2xl border border-orange-100 shadow-sm p-5 flex flex-col h-full">
          <div class="flex items-center justify-between pb-3 border-b border-orange-100">
            <h2 class="text-base font-bold text-orange-950 flex items-center gap-2">
              <BellAlertIcon class="w-5 h-5 text-orange-500" />
              Thông báo & Nhắc nhở
            </h2>
            <span class="text-[11px] font-semibold text-orange-600 bg-orange-100 px-2 py-0.5 rounded-full">
              {{ systemNotices.length }} mục
            </span>
          </div>

          <!-- Empty state -->
          <div
            v-if="systemNotices.length === 0"
            class="py-10 text-center flex flex-col items-center justify-center gap-2 flex-1"
          >
            <div class="w-14 h-14 rounded-full bg-orange-50 flex items-center justify-center text-orange-400">
              <BellAlertIcon class="w-7 h-7" />
            </div>
            <p class="text-sm font-semibold text-gray-700">Không có nhắc nhở mới</p>
            <p class="text-xs text-gray-400 max-w-sm">
              Mọi hoạt động giảng dạy của thầy/cô đang trong trạng thái ổn định.
            </p>
          </div>

          <!-- Notification items -->
          <div v-else class="divide-y divide-orange-50 flex-1 overflow-y-auto max-h-[460px]">
            <div
              v-for="notice in systemNotices"
              :key="notice.id"
              class="py-3 hover:bg-orange-50/40 transition-colors rounded-xl px-1.5 cursor-default"
            >
              <div class="flex items-start gap-2.5">
                <span
                  :class="[
                    'w-2 h-2 rounded-full mt-1.5 flex-shrink-0',
                    notice.urgent ? 'bg-red-500 ring-2 ring-red-200' : 'bg-orange-400',
                  ]"
                ></span>
                <div class="min-w-0 flex-1">
                  <div class="flex items-center justify-between gap-2">
                    <p class="text-xs font-bold text-gray-900 leading-snug line-clamp-1">
                      {{ notice.title }}
                    </p>
                    <span v-if="notice.urgent" class="text-[9px] font-bold uppercase tracking-wider text-red-600 bg-red-100 px-1.5 py-0.2 rounded">
                      Khẩn
                    </span>
                  </div>
                  <p class="text-xs text-gray-600 line-clamp-2 mt-1 leading-relaxed">
                    {{ notice.content }}
                  </p>
                  <div class="flex items-center justify-between text-[11px] text-gray-400 mt-2">
                    <span>{{ notice.sender }}</span>
                    <span>{{ notice.time }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="pt-3 border-t border-orange-100 text-center">
            <router-link
              to="/teacher/communication/notices"
              class="text-xs font-semibold text-orange-600 hover:text-orange-700"
            >
              Xem tất cả thông báo →
            </router-link>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useUserState } from '@/services/user-state'
import { getProfileId } from '@/services/auth-storage'
import classService from '@/services/class.service'
import classScheduleService from '@/services/classSchedule.service'
import lessonService from '@/services/lesson.service'
import assignmentService from '@/services/assignment.service'
import attendanceService from '@/services/attendance.service'
import teacherService from '@/services/teacher.service'
import {
  CalendarDaysIcon,
  ClipboardDocumentCheckIcon,
  PlusCircleIcon,
  ClockIcon,
  CheckBadgeIcon,
  DocumentCheckIcon,
  BellAlertIcon,
  AcademicCapIcon,
  UserGroupIcon,
  BookOpenIcon,
  TrophyIcon,
  ArrowPathIcon,
} from '@heroicons/vue/24/outline'

const { profile, fetchProfile } = useUserState()

onMounted(async () => {
  await loadDashboard()
})

const teacherName = computed(() => profile.value?.fullName || 'Thầy/Cô')
const teacherInitials = computed(() => {
  const name = teacherName.value.trim()
  const parts = name.split(' ')
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

const scheduleTab = ref('today')

// ─── Raw data ────────────────────────────────────────────────────────────────
const isLoading = ref(false)
const loadError = ref('')
const myClasses = ref([])
const allLessons = ref([])
const pendingGrading = ref({ total: 0, className: '', assignmentTitle: '' })
const attendanceSummary = ref({ marked: 0, present: 0, late: 0, absent: 0, excused: 0, lessons: 0 })

// ─── Computed summaries ──────────────────────────────────────────────────────
const attendanceRate = computed(() => {
  const s = attendanceSummary.value
  if (!s.marked) return null
  return Math.round(((s.present + s.late) / s.marked) * 1000) / 10
})

const attendanceRateLabel = computed(() => {
  return attendanceRate.value === null ? '—' : `${attendanceRate.value.toFixed(1)}%`
})

const quickStats = computed(() => {
  const classCount = myClasses.value.length
  const studentCount = myClasses.value.reduce((sum, c) => sum + (c.activeStudentsCount || 0), 0)
  const todayCount = todaySchedule.value.length
  const nextSession = todaySchedule.value.find((s) => s.status !== 'completed') || todaySchedule.value[0]

  return [
    {
      label: 'Lớp học phụ trách',
      value: `${classCount} Lớp`,
      sub: classCount > 0 ? 'Đang được phân công' : 'Chưa có lớp được phân công',
      icon: AcademicCapIcon,
      bg: 'bg-gradient-to-br from-orange-400 to-amber-500',
    },
    {
      label: 'Tổng số học sinh',
      value: `${studentCount} HS`,
      sub: 'Quản lý trực tiếp',
      icon: UserGroupIcon,
      bg: 'bg-gradient-to-br from-blue-400 to-indigo-500',
    },
    {
      label: 'Tiết dạy hôm nay',
      value: `${todayCount} Ca`,
      sub: nextSession ? `Ca gần nhất: ${nextSession.time}` : 'Hôm nay không có ca dạy',
      icon: BookOpenIcon,
      bg: 'bg-gradient-to-br from-emerald-400 to-teal-500',
    },
    {
      label: 'Tỷ lệ chuyên cần',
      value: attendanceRateLabel.value,
      sub: attendanceSummary.value.marked > 0
        ? `Tuần này · ${attendanceSummary.value.lessons} buổi đã điểm danh`
        : 'Chưa có dữ liệu điểm danh',
      icon: TrophyIcon,
      bg: 'bg-gradient-to-br from-purple-400 to-pink-500',
    },
  ]
})

const todaySchedule = computed(() => {
  const todayStr = formatYMD(new Date())
  const list = allLessons.value
    .filter((l) => String(l.lessonDate || '').slice(0, 10) === todayStr)
    .sort((a, b) => lessonTimeValue(a) - lessonTimeValue(b))

  return list.map((l, idx) => {
    const timing = resolveLessonTiming(l)
    return {
      id: l.id,
      lessonId: l.id,
      classId: l.classId,
      shift: String(idx + 1).padStart(2, '0'),
      className: l.className,
      room: l.room || 'Phòng học chính',
      time: formatLessonTime(l.startTime, l.endTime) || 'Theo lịch lớp',
      studentsCount: l.studentsCount || 0,
      status: timing.status,
      statusLabel: timing.label,
    }
  })
})

const weekSchedule = computed(() => {
  const { start, end } = currentWeekRangeDates()
  const dayLabels = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN']
  const todayStr = formatYMD(new Date())

  return allLessons.value
    .filter((l) => {
      const d = toDateSafe(l.lessonDate)
      return d && d >= start && d < end
    })
    .sort((a, b) => lessonTimeValue(a) - lessonTimeValue(b))
    .map((l) => {
      const d = toDateSafe(l.lessonDate)
      const dayIndex = d ? (d.getDay() + 6) % 7 : 0
      const dateStr = d ? formatYMD(d) : ''
      return {
        id: l.id,
        dayLabel: dayLabels[dayIndex],
        className: l.className,
        room: l.room || 'Phòng học chính',
        time: formatLessonTime(l.startTime, l.endTime) || 'Theo lịch lớp',
        date: dateStr === todayStr ? 'Hôm nay' : (d ? formatDateDM(d) : ''),
        studentsCount: l.studentsCount || 0,
      }
    })
})

const systemNotices = computed(() => {
  const notices = []

  if (pendingGrading.value.total > 0) {
    notices.push({
      id: 'grading',
      title: `${pendingGrading.value.total} bài nộp chưa chấm điểm`,
      content: pendingGrading.value.assignmentTitle
        ? `Bài "${pendingGrading.value.assignmentTitle}"${pendingGrading.value.className ? ` của lớp ${pendingGrading.value.className}` : ''} đang chờ thầy/cô chấm điểm và nhận xét.`
        : 'Có bài nộp mới cần chấm điểm và nhận xét.',
      sender: 'Hệ thống chấm điểm',
      time: 'Mới cập nhật',
      urgent: true,
    })
  }

  const currentSession = todaySchedule.value.find((s) => s.status === 'in_progress')
  const nextSession = todaySchedule.value.find((s) => s.status === 'upcoming')
  if (currentSession || nextSession) {
    const session = currentSession || nextSession
    notices.push({
      id: `session-${session.id}`,
      title: currentSession ? `Ca dạy đang diễn ra: ${session.className}` : `Ca dạy sắp tới: ${session.className}`,
      content: `${session.time} tại ${session.room}, sĩ số ${session.studentsCount} học viên.`,
      sender: 'Thời khóa biểu',
      time: currentSession ? 'Bây giờ' : 'Hôm nay',
      urgent: Boolean(currentSession),
    })
  }

  if (attendanceSummary.value.marked > 0) {
    notices.push({
      id: 'attendance',
      title: `Chuyên cần tuần này đạt ${attendanceRateLabel.value}`,
      content: `Đã điểm danh ${attendanceSummary.value.lessons} buổi, ghi nhận ${attendanceSummary.value.absent} lượt vắng và ${attendanceSummary.value.excused} lượt có phép.`,
      sender: 'Báo cáo điểm danh',
      time: 'Tuần này',
      urgent: attendanceSummary.value.absent > 0,
    })
  }

  if (myClasses.value.length === 0) {
    notices.push({
      id: 'no-class',
      title: 'Chưa có lớp học được phân công',
      content: 'Vui lòng liên hệ ban đào tạo để được phân công lớp học và lịch giảng dạy.',
      sender: 'Hệ thống',
      time: 'Hôm nay',
      urgent: false,
    })
  }

  return notices
})

// ─── Data loading ────────────────────────────────────────────────────────────
async function loadDashboard() {
  isLoading.value = true
  loadError.value = ''

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
        const matched = rawTeachers.find((t) =>
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
    myClasses.value = Array.isArray(assigned) ? assigned : []

    const perClass = await Promise.all(myClasses.value.map(loadClassLessons))
    allLessons.value = perClass.flat().sort((a, b) => lessonTimeValue(a) - lessonTimeValue(b))

    await Promise.all([loadAttendanceSummary(), loadPendingGrading()])
  } catch (err) {
    console.error('Lỗi khi tải dữ liệu tổng quan giáo viên:', err)
    loadError.value = 'Không thể tải dữ liệu tổng quan. Vui lòng kiểm tra kết nối và thử lại.'
  } finally {
    isLoading.value = false
  }
}

async function loadClassLessons(cls) {
  let room = cls.room || ''

  try {
    const schedulesData = await classScheduleService.getByClass(cls.id)
    const schedules = Array.isArray(schedulesData) ? schedulesData : (schedulesData?.items || [])
    const withRoom = schedules.find((s) => s.room)
    if (withRoom?.room) room = withRoom.room
  } catch {
    // Không có lịch tuần vẫn có thể hiển thị buổi học
  }

  try {
    const lessonsData = await lessonService.getByClass(cls.id)
    const lessons = Array.isArray(lessonsData) ? lessonsData : (lessonsData?.items || [])
    return lessons.map((l) => ({
      ...l,
      className: cls.name,
      classId: cls.id,
      room: room || 'Phòng học chính',
      studentsCount: cls.activeStudentsCount || 0,
    }))
  } catch (err) {
    console.warn(`Không thể tải buổi học của lớp ${cls.name}:`, err)
    return []
  }
}

async function loadAttendanceSummary() {
  const summary = { marked: 0, present: 0, late: 0, absent: 0, excused: 0, lessons: 0 }
  const { start, end } = currentWeekRangeDates()

  const weekLessons = allLessons.value.filter((l) => {
    const d = toDateSafe(l.lessonDate)
    return d && d >= start && d < end && (l.attendancesCount || 0) > 0
  })

  const details = await Promise.all(
    weekLessons.map(async (l) => {
      try {
        return await attendanceService.getByLesson(l.id)
      } catch {
        return null
      }
    })
  )

  details.filter(Boolean).forEach((detail) => {
    const marked =
      (detail.presentCount || 0) +
      (detail.absentCount || 0) +
      (detail.lateCount || 0) +
      (detail.excusedCount || 0)

    if (marked === 0) return

    summary.lessons += 1
    summary.present += detail.presentCount || 0
    summary.late += detail.lateCount || 0
    summary.absent += detail.absentCount || 0
    summary.excused += detail.excusedCount || 0
    summary.marked += marked
  })

  attendanceSummary.value = summary
}

async function loadPendingGrading() {
  const result = { total: 0, className: '', assignmentTitle: '' }
  const now = Date.now()
  const lookbackStart = now - 14 * 24 * 60 * 60 * 1000
  const tomorrowEnd = now + 24 * 60 * 60 * 1000

  const recentLessons = allLessons.value
    .filter((l) => {
      const d = toDateSafe(l.startTime) || toDateSafe(l.lessonDate)
      return d && d.getTime() >= lookbackStart && d.getTime() <= tomorrowEnd
    })
    .sort((a, b) => lessonTimeValue(b) - lessonTimeValue(a))

  const assignmentGroups = await Promise.all(
    recentLessons.map(async (lesson) => {
      try {
        const assignmentsData = await assignmentService.getByLesson(lesson.id)
        return { lesson, assignments: Array.isArray(assignmentsData) ? assignmentsData : (assignmentsData?.items || []) }
      } catch {
        return { lesson, assignments: [] }
      }
    })
  )

  const candidates = []
  const seen = new Set()

  for (const { lesson, assignments } of assignmentGroups) {
    for (const a of assignments) {
      if (!a?.id || seen.has(a.id)) continue
      if ((a.completedStudentsCount || 0) <= 0) continue

      const status = (a.status || '').toUpperCase()
      if (status === 'DRAFT' || status === 'CLOSED') continue

      seen.add(a.id)
      candidates.push({ ...a, className: lesson.className, lessonDate: lesson.lessonDate })
    }
  }

  // Giới hạn số lượt gọi chi tiết để dashboard tải nhanh
  const limited = candidates
    .sort((a, b) => new Date(b.deadline || 0) - new Date(a.deadline || 0))
    .slice(0, 20)

  const details = await Promise.all(
    limited.map(async (a) => {
      try {
        return { assignment: a, detail: await assignmentService.getById(a.id) }
      } catch {
        return null
      }
    })
  )

  for (const item of details) {
    if (!item) continue
    const students = item.detail?.assignedStudents || []
    const ungraded = students.filter((s) => (s.status || '').toUpperCase() === 'SUBMITTED').length
    if (ungraded <= 0) continue

    result.total += ungraded
    if (!result.assignmentTitle) {
      result.assignmentTitle = item.assignment.title || item.detail?.title || ''
      result.className = item.assignment.className || item.detail?.className || ''
    }
  }

  pendingGrading.value = result
}

// ─── Helpers ─────────────────────────────────────────────────────────────────
function toDateSafe(value) {
  if (!value) return null
  const d = new Date(value)
  return isNaN(d.getTime()) ? null : d
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

function startOfWeek(date) {
  const d = new Date(date)
  d.setHours(0, 0, 0, 0)
  const day = d.getDay()
  const diff = d.getDate() - day + (day === 0 ? -6 : 1)
  return new Date(d.setDate(diff))
}

function currentWeekRangeDates() {
  const start = startOfWeek(new Date())
  const end = new Date(start)
  end.setDate(end.getDate() + 7)
  return { start, end }
}

function lessonTimeValue(lesson) {
  const d = toDateSafe(lesson.startTime) || toDateSafe(lesson.lessonDate)
  return d ? d.getTime() : 0
}

function resolveLessonTiming(lesson) {
  const status = (lesson.status || '').toUpperCase()
  if (status === 'COMPLETED') return { status: 'completed', label: 'Đã dạy' }
  if (status === 'CANCELED') return { status: 'completed', label: 'Đã hủy' }
  if (status === 'HOLIDAY') return { status: 'completed', label: 'Nghỉ lễ' }

  const now = Date.now()
  const start = toDateSafe(lesson.startTime)
  const end = toDateSafe(lesson.endTime)

  if (start && end && now >= start.getTime() && now <= end.getTime()) {
    return { status: 'in_progress', label: 'Đang diễn ra' }
  }
  if (end && now > end.getTime()) {
    return { status: 'completed', label: 'Đã kết thúc' }
  }
  return { status: 'upcoming', label: 'Sắp diễn ra' }
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
