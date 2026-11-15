<template>
  <div class="space-y-6">
    <!-- ─── Welcome Banner ───────────────────────────────────────────────── -->
    <div class="rounded-lg bg-white border border-stone-300 border-l-4 border-l-orange-600 p-5 shadow-[0_1px_2px_rgba(0,0,0,0.04)]">
      <div class="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div class="flex items-center gap-4">
          <div class="w-12 h-12 rounded-md bg-orange-50 border border-orange-200 flex items-center justify-center text-orange-700 text-lg font-bold flex-shrink-0">
            {{ teacherInitials }}
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
              Xin chào, {{ teacherName }}! 👋
            </h1>
            <p class="text-xs md:text-sm text-stone-600 mt-0.5">
              Chúc thầy/cô một ngày giảng dạy tràn đầy năng lượng và hiệu quả.
            </p>
          </div>
        </div>

        <!-- Quick navigation shortcuts -->
        <div class="flex flex-wrap items-center gap-2">
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
          <p class="text-xl font-bold text-stone-900 tabular-nums mt-0.5">{{ stat.value }}</p>
          <p class="text-xs text-stone-600 font-medium mt-0.5">{{ stat.sub }}</p>
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
            <!-- Today's Schedule -->
            <div v-if="scheduleTab === 'today'" class="space-y-3">
              <div
                v-if="todaySchedule.length === 0"
                class="py-10 text-center flex flex-col items-center justify-center gap-2"
              >
                <div class="w-12 h-12 rounded-md bg-orange-50 border border-orange-200 flex items-center justify-center text-orange-600">
                  <CheckBadgeIcon class="w-6 h-6" />
                </div>
                <p class="text-sm font-semibold text-stone-800">Hôm nay không có ca dạy nào</p>
                <p class="text-xs text-stone-500 max-w-sm">
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
                        {{ item.room }}
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
                    to="/teacher/attendance"
                    class="inline-flex items-center h-8 px-3 rounded-md bg-orange-600 hover:bg-orange-700 text-white border border-orange-700 text-xs font-semibold shadow-xs transition-colors"
                  >
                    Điểm danh
                  </router-link>
                  <router-link
                    to="/teacher/classes"
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
                v-for="item in weekSchedule"
                :key="item.id"
                class="flex items-center justify-between p-3 rounded-md border border-stone-300 hover:bg-stone-50 transition-colors"
              >
                <div class="flex items-center gap-3">
                  <div class="w-10 h-10 rounded-md bg-orange-50 border border-orange-200 text-orange-800 text-xs font-bold flex items-center justify-center tabular-nums">
                    {{ item.dayLabel }}
                  </div>
                  <div>
                    <p class="text-xs font-bold text-stone-900">{{ item.className }} ({{ item.room }})</p>
                    <p class="text-xs text-stone-500 tabular-nums">{{ item.time }} · {{ item.date }}</p>
                  </div>
                </div>
                <span class="text-xs font-semibold text-stone-700 bg-stone-100 border border-stone-300 px-2.5 py-0.5 rounded tabular-nums">
                  {{ item.studentsCount }} học sinh
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- Quick Task Cards -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div class="bg-white rounded-lg border border-stone-300 p-4 shadow-[0_1px_2px_rgba(0,0,0,0.04)] hover:border-stone-400 transition-colors">
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-md bg-orange-50 border border-orange-200 text-orange-600 flex items-center justify-center flex-shrink-0">
                <DocumentCheckIcon class="w-5 h-5" />
              </div>
              <div class="min-w-0 flex-1">
                <h3 class="text-sm font-bold text-stone-900">Bài tập cần chấm</h3>
                <p class="text-xs text-stone-500 tabular-nums">14 bài tập mới nộp từ lớp Toán Tư Duy 02</p>
              </div>
            </div>
            <router-link
              to="/teacher/grading"
              class="mt-3 block text-right text-xs font-bold text-orange-600 hover:text-orange-700"
            >
              Chấm bài ngay →
            </router-link>
          </div>

          <div class="bg-white rounded-lg border border-stone-300 p-4 shadow-[0_1px_2px_rgba(0,0,0,0.04)] hover:border-stone-400 transition-colors">
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-md bg-amber-50 border border-amber-200 text-amber-700 flex items-center justify-center flex-shrink-0">
                <ChatBubbleBottomCenterTextIcon class="w-5 h-5" />
              </div>
              <div class="min-w-0 flex-1">
                <h3 class="text-sm font-bold text-stone-900">Phản hồi phụ huynh</h3>
                <p class="text-xs text-stone-500 tabular-nums">2 tin nhắn nhắc nhở cần gửi trước 18:00</p>
              </div>
            </div>
            <router-link
              to="/teacher/communication/notices"
              class="mt-3 block text-right text-xs font-bold text-orange-600 hover:text-orange-700"
            >
              Gửi thông báo →
            </router-link>
          </div>
        </div>
      </div>

      <!-- Right 1 Col: Thông báo mới từ trung tâm -->
      <div class="space-y-4">
        <div class="bg-white rounded-lg border border-stone-300 shadow-[0_1px_2px_rgba(0,0,0,0.04)] p-5 flex flex-col h-full">
          <div class="flex items-center justify-between pb-3 border-b border-stone-200">
            <h2 class="text-base font-bold text-stone-900 flex items-center gap-2">
              <BellAlertIcon class="w-5 h-5 text-orange-600" />
              Thông báo từ trung tâm
            </h2>
            <span class="text-xs font-semibold text-orange-800 bg-orange-50 border border-orange-300 px-2 py-0.5 rounded tabular-nums">
              {{ centerNotices.length }} mới
            </span>
          </div>

          <!-- Notification items -->
          <div class="divide-y divide-stone-200 flex-1 overflow-y-auto max-h-[460px]">
            <div
              v-for="notice in centerNotices"
              :key="notice.id"
              class="py-3 hover:bg-orange-50/40 transition-colors rounded-md px-2 cursor-pointer"
            >
              <div class="flex items-start gap-2.5">
                <span
                  :class="[
                    'w-2 h-2 rounded-full mt-1.5 flex-shrink-0',
                    notice.urgent ? 'bg-rose-600' : 'bg-orange-500',
                  ]"
                ></span>
                <div class="min-w-0 flex-1">
                  <div class="flex items-center justify-between gap-2">
                    <p class="text-xs font-bold text-stone-900 leading-snug line-clamp-1">
                      {{ notice.title }}
                    </p>
                    <span v-if="notice.urgent" class="text-[10px] font-bold uppercase tracking-wider text-rose-800 bg-rose-50 border border-rose-300 px-1.5 py-0.5 rounded">
                      Khẩn
                    </span>
                  </div>
                  <p class="text-xs text-stone-600 line-clamp-2 mt-1 leading-relaxed">
                    {{ notice.content }}
                  </p>
                  <div class="flex items-center justify-between text-[11px] text-stone-400 mt-2 tabular-nums">
                    <span>{{ notice.sender }}</span>
                    <span>{{ notice.time }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="pt-3 border-t border-stone-200 text-center">
            <router-link
              to="/teacher/communication/notices"
              class="text-xs font-semibold text-orange-600 hover:text-orange-700"
            >
              Xem tất cả thông báo trung tâm →
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
} from '@heroicons/vue/24/outline'

const { profile, fetchProfile } = useUserState()

onMounted(async () => {
  await fetchProfile()
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

// ─── Quick Stats ─────────────────────────────────────────────────────────────
const quickStats = [
  {
    label: 'Lớp học phụ trách',
    value: '4 Lớp',
    sub: 'Đang hoạt động',
    icon: AcademicCapIcon,
    bg: 'bg-orange-50 text-orange-600 border-orange-200',
  },
  {
    label: 'Tổng số học sinh',
    value: '68 HS',
    sub: 'Quản lý trực tiếp',
    icon: UserGroupIcon,
    bg: 'bg-stone-100 text-stone-700 border-stone-300',
  },
  {
    label: 'Tiết dạy hôm nay',
    value: '2 Ca',
    sub: 'Sáng 08:00 & Chiều 14:00',
    icon: BookOpenIcon,
    bg: 'bg-emerald-50 text-emerald-700 border-emerald-200',
  },
  {
    label: 'Tỷ lệ chuyên cần',
    value: '96.2%',
    sub: 'Tuần này đạt chuẩn',
    icon: TrophyIcon,
    bg: 'bg-amber-50 text-amber-700 border-amber-200',
  },
]

// ─── Today's Schedule Data ───────────────────────────────────────────────────
const todaySchedule = ref([
  {
    id: 1,
    shift: '01',
    className: 'Toán Tư Duy - Lớp 3A',
    room: 'Phòng P201',
    time: '08:00 - 09:30',
    studentsCount: 18,
    status: 'upcoming',
    statusLabel: 'Sắp diễn ra',
  },
  {
    id: 2,
    shift: '02',
    className: 'Hình Học Trực Quan - Lớp 4B',
    room: 'Phòng P305',
    time: '14:00 - 15:30',
    studentsCount: 16,
    status: 'upcoming',
    statusLabel: 'Buổi chiều',
  },
])

// ─── Week Schedule Data ──────────────────────────────────────────────────────
const weekSchedule = ref([
  { id: 1, dayLabel: 'T2', className: 'Toán Tư Duy - Lớp 3A', room: 'P201', time: '08:00 - 09:30', date: 'Hôm nay', studentsCount: 18 },
  { id: 2, dayLabel: 'T2', className: 'Hình Học Trực Quan - Lớp 4B', room: 'P305', time: '14:00 - 15:30', date: 'Hôm nay', studentsCount: 16 },
  { id: 3, dayLabel: 'T4', className: 'Toán Nâng Cao - Lớp 5A', room: 'P102', time: '09:45 - 11:15', date: 'Thứ Tư', studentsCount: 15 },
  { id: 4, dayLabel: 'T5', className: 'Toán Tư Duy - Lớp 3A', room: 'P201', time: '08:00 - 09:30', date: 'Thứ Năm', studentsCount: 18 },
  { id: 5, dayLabel: 'T6', className: 'Hình Học Trực Quan - Lớp 4B', room: 'P305', time: '14:00 - 15:30', date: 'Thứ Sáu', studentsCount: 16 },
  { id: 6, dayLabel: 'T7', className: 'Ôn Luyện Cuối Tuần - Đội Tuyển', room: 'P401', time: '08:30 - 11:00', date: 'Thứ Bảy', studentsCount: 12 },
])

// ─── Center Notices ──────────────────────────────────────────────────────────
const centerNotices = ref([
  {
    id: 1,
    title: 'Họp chuyên môn khối Toán Tư Duy Tiểu học tuần 44',
    content: 'Thống nhất cấu trúc bài kiểm tra đánh giá năng lực giữa kỳ và triển khai ngân hàng đề ôn tập số 3.',
    sender: 'Ban Đào Tạo',
    time: '35 phút trước',
    urgent: true,
  },
  {
    id: 2,
    title: 'Cập nhật tính năng báo vắng học sinh tự động',
    content: 'Giáo viên sau khi tích điểm danh "Vắng", hệ thống sẽ tự động gửi SMS & Thông báo qua ứng dụng Phụ huynh.',
    sender: 'Phòng IT & Vận Hành',
    time: '2 giờ trước',
    urgent: false,
  },
  {
    id: 3,
    title: 'Kế hoạch nghỉ lễ và dạy bù tháng 11',
    content: 'Kính gửi quý thầy cô lịch hoạt động nhân ngày Nhà giáo Việt Nam 20/11 và lịch dạy bù chi tiết.',
    sender: 'Văn Phòng Trung Tâm',
    time: '1 ngày trước',
    urgent: false,
  },
])
</script>
