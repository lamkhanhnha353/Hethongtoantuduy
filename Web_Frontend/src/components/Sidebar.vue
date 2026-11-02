<template>
  <!-- ─── Sidebar shell ──────────────────────────────────────────────────── -->
  <aside
    :class="[
      'fixed lg:relative inset-y-0 left-0 z-30 flex flex-col',
      'bg-orange-100 border-r border-orange-200',
      'transition-all duration-300 ease-in-out',
      // Desktop: collapsed or expanded
      collapsed ? 'lg:w-20' : 'lg:w-64',
      // Mobile: slide in / out
      mobileOpen ? 'translate-x-0 w-72' : '-translate-x-full lg:translate-x-0',
    ]"
  >
    <!-- ─── Logo / Brand ─────────────────────────────────────────────────── -->
    <div
      class="flex items-center gap-3 px-4 py-4 border-b border-orange-200 min-h-[64px]"
    >
      <div
        class="w-10 h-10 rounded-xl bg-orange-400 flex items-center justify-center flex-shrink-0 shadow"
      >
        <AcademicCapIcon class="w-6 h-6 text-white" />
      </div>
      <transition name="label-fade">
        <div v-if="!collapsed || mobileOpen" class="flex flex-col whitespace-nowrap overflow-hidden">
          <span class="font-bold text-orange-950 text-base leading-tight">
            {{ isTeacherPortal ? 'EduTeacher' : 'EduAdmin' }}
          </span>
          <span class="text-[10px] text-orange-700 font-semibold tracking-wide">
            {{ isTeacherPortal ? 'Cổng Giáo Viên' : 'Quản Trị Hệ Thống' }}
          </span>
        </div>
      </transition>

      <!-- Mobile close button -->
      <button
        class="ml-auto lg:hidden text-orange-600 hover:text-orange-800 p-1 rounded-lg cursor-pointer"
        @click="$emit('closeMobile')"
      >
        <XMarkIcon class="w-5 h-5" />
      </button>
    </div>

    <!-- ─── Navigation ───────────────────────────────────────────────────── -->
    <nav class="flex-1 overflow-y-auto sidebar-scroll py-3 px-2 space-y-0.5">
      <template v-for="item in menuItems" :key="item.id">
        <!-- Single item -->
        <template v-if="!item.children">
          <router-link
            :to="item.to"
            custom
            v-slot="{ isActive, navigate }"
          >
            <button
              @click="navigate"
              :class="[
                'w-full flex items-center gap-3 px-3 py-2.5 rounded-xl',
                'text-sm font-medium transition-all duration-150 cursor-pointer',
                isActive
                  ? 'bg-orange-400 text-white shadow-sm'
                  : 'text-orange-800 hover:bg-orange-200/70 hover:text-orange-900',
                collapsed && !mobileOpen ? 'justify-center' : '',
              ]"
              :title="collapsed && !mobileOpen ? item.label : ''"
            >
              <component :is="item.icon" class="w-5 h-5 flex-shrink-0" />
              <transition name="label-fade">
                <span v-if="!collapsed || mobileOpen" class="truncate">{{ item.label }}</span>
              </transition>
            </button>
          </router-link>
        </template>

        <!-- Accordion item with children -->
        <template v-else>
          <div>
            <button
              @click="toggleAccordion(item.id)"
              :class="[
                'w-full flex items-center gap-3 px-3 py-2.5 rounded-xl',
                'text-sm font-medium transition-all duration-150 cursor-pointer',
                isAccordionOpen(item.id)
                  ? 'bg-orange-200 text-orange-900'
                  : 'text-orange-800 hover:bg-orange-200/70 hover:text-orange-900',
                collapsed && !mobileOpen ? 'justify-center' : '',
              ]"
              :title="collapsed && !mobileOpen ? item.label : ''"
            >
              <component :is="item.icon" class="w-5 h-5 flex-shrink-0" />
              <transition name="label-fade">
                <span v-if="!collapsed || mobileOpen" class="flex-1 text-left truncate">
                  {{ item.label }}
                </span>
              </transition>
              <transition name="label-fade">
                <ChevronDownIcon
                  v-if="!collapsed || mobileOpen"
                  :class="[
                    'w-4 h-4 flex-shrink-0 transition-transform duration-200',
                    isAccordionOpen(item.id) ? 'rotate-180' : '',
                  ]"
                />
              </transition>
            </button>

            <!-- Children -->
            <transition
              name="accordion"
              @before-enter="onBeforeEnter"
              @enter="onEnter"
              @leave="onLeave"
            >
              <div
                v-if="isAccordionOpen(item.id) && (!collapsed || mobileOpen)"
                class="overflow-hidden"
              >
                <div class="ml-4 mt-0.5 space-y-0.5 border-l-2 border-orange-300 pl-3">
                  <router-link
                    v-for="child in item.children"
                    :key="child.id"
                    :to="child.to"
                    custom
                    v-slot="{ isActive, navigate }"
                  >
                    <button
                      @click="navigate"
                      :class="[
                        'w-full flex items-center gap-2 px-2 py-2 rounded-lg',
                        'text-xs font-medium transition-all duration-150 cursor-pointer',
                        isActive
                          ? 'bg-orange-300/60 text-orange-900 font-semibold'
                          : 'text-orange-700 hover:bg-orange-200/60 hover:text-orange-900',
                      ]"
                    >
                      <component :is="child.icon" class="w-4 h-4 flex-shrink-0" />
                      <span class="text-left leading-tight">{{ child.label }}</span>
                    </button>
                  </router-link>
                </div>
              </div>
            </transition>
          </div>
        </template>
      </template>
    </nav>

    <!-- ─── Logout button ────────────────────────────────────────────────── -->
    <div class="px-2 pb-4 border-t border-orange-200 pt-3">
      <button
        :class="[
          'w-full flex items-center gap-3 px-3 py-2.5 rounded-xl',
          'text-sm font-medium text-orange-700 cursor-pointer',
          'hover:bg-red-100 hover:text-red-600 transition-all duration-150',
          collapsed && !mobileOpen ? 'justify-center' : '',
        ]"
        :title="collapsed && !mobileOpen ? 'Đăng xuất' : ''"
        @click="handleLogout"
      >
        <ArrowRightOnRectangleIcon class="w-5 h-5 flex-shrink-0" />
        <transition name="label-fade">
          <span v-if="!collapsed || mobileOpen">Đăng xuất</span>
        </transition>
      </button>
    </div>
  </aside>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import authService from '@/services/auth.service'
import {
  AcademicCapIcon,
  XMarkIcon,
  ChevronDownIcon,
  ArrowRightOnRectangleIcon,
  // Common & Admin icons
  HomeIcon,
  UsersIcon,
  BookOpenIcon,
  ClipboardDocumentListIcon,
  BanknotesIcon,
  ChatBubbleLeftRightIcon,
  ChartBarIcon,
  UserCircleIcon,
  ShieldCheckIcon,
  CalendarDaysIcon,
  QuestionMarkCircleIcon,
  TrophyIcon,
  DocumentTextIcon,
  BellIcon,
  PresentationChartLineIcon,
  UserGroupIcon,
  AdjustmentsHorizontalIcon,
  CurrencyDollarIcon,
  ArrowsRightLeftIcon,
  AcademicCapIcon as GraduationIcon,
  // Teacher-specific icons
  ClipboardDocumentCheckIcon,
  PencilSquareIcon,
  StarIcon,
  BellAlertIcon,
  IdentificationIcon,
} from '@heroicons/vue/24/outline'

// ─── Props & emits ───────────────────────────────────────────────────────────
defineProps({
  collapsed: { type: Boolean, default: false },
  mobileOpen: { type: Boolean, default: false },
})
defineEmits(['closeMobile'])

const route = useRoute()
const router = useRouter()

// ─── Portal Switcher (Admin vs Teacher) ───────────────────────────────────────
const isTeacherPortal = computed(() => {
  return (
    route.path.startsWith('/teacher') ||
    (!route.path.startsWith('/training') &&
      !route.path.startsWith('/users') &&
      !route.path.startsWith('/reports') &&
      !route.path.startsWith('/finance') &&
      authService.getRole()?.toLowerCase() === 'teacher')
  )
})

// ─── Accordion state ─────────────────────────────────────────────────────────
const openAccordions = ref(new Set(['class-schedule', 'training', 'users']))

function toggleAccordion(id) {
  if (openAccordions.value.has(id)) {
    openAccordions.value.delete(id)
  } else {
    openAccordions.value.add(id)
  }
}

function isAccordionOpen(id) {
  return openAccordions.value.has(id)
}

// ─── Accordion transition helpers ────────────────────────────────────────────
function onBeforeEnter(el) { el.style.height = '0' }
function onEnter(el) {
  el.style.height = el.scrollHeight + 'px'
  el.addEventListener('transitionend', () => { el.style.height = '' }, { once: true })
}
function onLeave(el) {
  el.style.height = el.scrollHeight + 'px'
  void el.offsetHeight
  el.style.height = '0'
}

// ─── Logout ──────────────────────────────────────────────────────────────────
function handleLogout() {
  authService.logout()
  router.replace({ name: 'login' })
}

// ─── Teacher Sitemap ─────────────────────────────────────────────────────────
const teacherMenuItems = [
  {
    id: 'teacher-dashboard',
    label: 'Tổng quan',
    icon: HomeIcon,
    to: '/teacher',
  },
  {
    id: 'class-schedule',
    label: 'Quản lý Lớp & Lịch dạy',
    icon: AcademicCapIcon,
    children: [
      { id: 't-schedule', label: 'Thời khóa biểu cá nhân', icon: CalendarDaysIcon, to: '/teacher/schedule' },
      { id: 't-classes', label: 'Lớp học đang phụ trách', icon: BookOpenIcon, to: '/teacher/classes' },
      { id: 't-students', label: 'Danh sách học sinh', icon: UserGroupIcon, to: '/teacher/students' },
      { id: 't-attendance', label: 'Điểm danh học viên', icon: ClipboardDocumentCheckIcon, to: '/teacher/attendance' },
    ],
  },
  {
    id: 'learning-space',
    label: 'Học tập & Đánh giá',
    icon: PencilSquareIcon,
    children: [
      { id: 't-questions', label: 'Ngân hàng câu hỏi', icon: DocumentTextIcon, to: '/teacher/questions' },
      { id: 't-assignments', label: 'Giao bài tập về nhà', icon: ClipboardDocumentListIcon, to: '/teacher/assignments' },
      { id: 't-grading', label: 'Chấm điểm & Nhận xét', icon: StarIcon, to: '/teacher/grading' },
    ],
  },
  {
    id: 'comm-stats',
    label: 'Giao tiếp & Thống kê',
    icon: ChatBubbleLeftRightIcon,
    children: [
      { id: 't-attendance-rep', label: 'Thống kê chuyên cần', icon: ChartBarIcon, to: '/teacher/reports/attendance' },
      { id: 't-notices', label: 'Gửi thông báo, nhắc nhở', icon: BellAlertIcon, to: '/teacher/communication/notices' },
    ],
  },
  {
    id: 'my-account',
    label: 'Tài khoản của tôi',
    icon: UserCircleIcon,
    children: [
      { id: 't-profile', label: 'Hồ sơ & Chuyên môn', icon: IdentificationIcon, to: '/teacher/account/profile' },
      { id: 't-password', label: 'Đổi mật khẩu / PIN', icon: ShieldCheckIcon, to: '/teacher/account/password' },
    ],
  },
]

// ─── Admin Sitemap ───────────────────────────────────────────────────────────
const adminMenuItems = [
  {
    id: 'dashboard',
    label: 'Tổng quan',
    icon: HomeIcon,
    to: '/',
  },
  {
    id: 'users',
    label: 'Quản lý Người dùng',
    icon: UsersIcon,
    children: [
      { id: 'parents-students', label: 'Phụ huynh & Học sinh', icon: UserGroupIcon, to: '/users/students' },
      { id: 'teachers', label: 'Giáo viên', icon: AcademicCapIcon, to: '/users/teachers' },
      { id: 'roles', label: 'Tài khoản & Bảo mật', icon: ShieldCheckIcon, to: '/users/roles' },
    ],
  },
  {
    id: 'training',
    label: 'Đào tạo & Vận hành',
    icon: BookOpenIcon,
    children: [
      { id: 'courses', label: 'Danh mục Khóa học', icon: ClipboardDocumentListIcon, to: '/training/courses' },
      { id: 'classes', label: 'Danh sách Lớp học', icon: GraduationIcon, to: '/training/classes' },
      { id: 'schedule', label: 'Lịch học & Thời khóa biểu', icon: CalendarDaysIcon, to: '/training/schedule' },
    ],
  },
  {
    id: 'learning',
    label: 'Học tập & Đánh giá',
    icon: QuestionMarkCircleIcon,
    children: [
      { id: 'question-bank', label: 'Ngân hàng câu hỏi', icon: DocumentTextIcon, to: '/learning/questions' },
      { id: 'gamification', label: 'Gamification', icon: TrophyIcon, to: '/learning/gamification' },
    ],
  },
  {
    id: 'finance',
    label: 'Quản lý Tài chính',
    icon: BanknotesIcon,
    children: [
      { id: 'invoices', label: 'Hóa đơn & Biên lai', icon: CurrencyDollarIcon, to: '/finance/invoices' },
      { id: 'transactions', label: 'Lịch sử giao dịch', icon: ArrowsRightLeftIcon, to: '/finance/transactions' },
    ],
  },
  {
    id: 'communication',
    label: 'Giao tiếp & CSKH',
    icon: ChatBubbleLeftRightIcon,
    children: [
      { id: 'notifications', label: 'Quản lý Thông báo', icon: BellIcon, to: '/communication/notifications' },
    ],
  },
  {
    id: 'reports',
    label: 'Báo cáo & Thống kê',
    icon: ChartBarIcon,
    children: [
      { id: 'revenue-report', label: 'Báo cáo Doanh thu', icon: PresentationChartLineIcon, to: '/reports/revenue' },
      { id: 'growth-report', label: 'Tăng trưởng học viên', icon: ChartBarIcon, to: '/reports/growth' },
      { id: 'attendance-report', label: 'Thống kê chuyên cần', icon: AdjustmentsHorizontalIcon, to: '/reports/attendance' },
    ],
  },
  {
    id: 'account',
    label: 'Tài khoản của tôi',
    icon: UserCircleIcon,
    children: [
      { id: 'profile', label: 'Thông tin cá nhân', icon: UserCircleIcon, to: '/account/profile' },
    ],
  },
]

const menuItems = computed(() => (isTeacherPortal.value ? teacherMenuItems : adminMenuItems))
</script>

<style scoped>
/* Label fade transition */
.label-fade-enter-active,
.label-fade-leave-active {
  transition: opacity 0.15s ease, width 0.2s ease;
}
.label-fade-enter-from,
.label-fade-leave-to {
  opacity: 0;
}

/* Accordion slide transition */
.accordion-enter-active,
.accordion-leave-active {
  transition: height 0.25s ease;
  overflow: hidden;
}
.accordion-enter-from,
.accordion-leave-to {
  height: 0 !important;
}
</style>
