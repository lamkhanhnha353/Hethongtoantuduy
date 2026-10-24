<template>
  <header
    class="flex items-center gap-3 px-4 md:px-6 bg-white border-b border-orange-100 shadow-sm min-h-[64px] z-10"
  >
    <!-- ─── Hamburger ──────────────────────────────────────────────────── -->
    <!-- Desktop: collapse/expand sidebar -->
    <button
      class="hidden lg:flex items-center justify-center w-9 h-9 rounded-lg text-orange-600 hover:bg-orange-100 transition-colors"
      @click="$emit('toggleSidebar')"
      title="Thu gọn / Mở rộng sidebar"
    >
      <Bars3Icon class="w-5 h-5" />
    </button>

    <!-- Mobile: open sidebar overlay -->
    <button
      class="flex lg:hidden items-center justify-center w-9 h-9 rounded-lg text-orange-600 hover:bg-orange-100 transition-colors"
      @click="$emit('toggleMobileSidebar')"
      title="Mở menu"
    >
      <Bars3Icon class="w-5 h-5" />
    </button>

    <!-- ─── Page title ────────────────────────────────────────────────── -->
    <div class="flex-1 min-w-0">
      <h1 class="text-base md:text-lg font-semibold text-orange-900 truncate">
        {{ pageTitle }}
      </h1>
    </div>

    <!-- ─── Right section ────────────────────────────────────────────── -->
    <div class="flex items-center gap-2 md:gap-3">
      <!-- Clock -->
      <div class="hidden sm:flex items-center gap-1.5 bg-orange-50 border border-orange-200 rounded-full px-3 py-1.5">
        <ClockIcon class="w-4 h-4 text-orange-500" />
        <span class="text-xs font-mono text-orange-700 tabular-nums">{{ currentTime }}</span>
      </div>

      <!-- Notification bell -->
      <button
        class="relative flex items-center justify-center w-9 h-9 rounded-full hover:bg-orange-100 text-orange-600 transition-colors"
        @click="toggleNotifications"
      >
        <BellIcon class="w-5 h-5" />
        <span
          v-if="unreadCount > 0"
          class="absolute top-0.5 right-0.5 flex items-center justify-center w-4 h-4 rounded-full bg-red-500 text-white text-[10px] font-bold leading-none"
        >
          {{ unreadCount > 9 ? '9+' : unreadCount }}
        </span>
      </button>

      <!-- Notification dropdown -->
      <transition name="dropdown">
        <div
          v-if="showNotifications"
          ref="notifDropdown"
          class="absolute top-16 right-16 md:right-24 w-80 bg-white rounded-2xl shadow-xl border border-orange-100 z-50 overflow-hidden"
        >
          <div class="flex items-center justify-between px-4 py-3 border-b border-orange-100">
            <span class="font-semibold text-orange-900 text-sm">Thông báo</span>
            <button class="text-xs text-orange-500 hover:text-orange-700" @click="markAllRead">
              Đánh dấu đã đọc
            </button>
          </div>
          <div class="max-h-72 overflow-y-auto divide-y divide-orange-50">
            <div
              v-for="notif in notifications"
              :key="notif.id"
              :class="[
                'flex items-start gap-3 px-4 py-3 hover:bg-orange-50 cursor-pointer transition-colors',
                !notif.read ? 'bg-orange-50/50' : '',
              ]"
            >
              <div
                :class="[
                  'w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0 mt-0.5',
                  notif.color,
                ]"
              >
                <component :is="notif.icon" class="w-4 h-4 text-white" />
              </div>
              <div class="min-w-0">
                <p class="text-xs font-medium text-gray-800 leading-snug">{{ notif.message }}</p>
                <p class="text-[11px] text-gray-400 mt-0.5">{{ notif.time }}</p>
              </div>
              <div v-if="!notif.read" class="w-2 h-2 rounded-full bg-orange-400 flex-shrink-0 mt-1.5"></div>
            </div>
          </div>
          <div class="px-4 py-2 border-t border-orange-100 text-center">
            <button class="text-xs text-orange-500 hover:text-orange-700 font-medium">
              Xem tất cả thông báo
            </button>
          </div>
        </div>
      </transition>

      <!-- ─── User profile ────────────────────────────────────────────── -->
      <div class="relative">
        <button
          class="flex items-center gap-2 pl-2 pr-3 py-1.5 rounded-full hover:bg-orange-100 transition-colors"
          @click="toggleUserMenu"
        >
          <div
            class="w-8 h-8 rounded-full bg-gradient-to-br from-orange-400 to-orange-500 flex items-center justify-center text-white text-sm font-bold shadow-sm flex-shrink-0"
          >
            {{ userInitials }}
          </div>
          <div class="hidden md:block text-left min-w-0">
            <p class="text-sm font-semibold text-orange-900 leading-none truncate max-w-[120px]">{{ userName }}</p>
            <p class="text-xs text-orange-500 leading-none mt-0.5">Admin</p>
          </div>
          <ChevronDownIcon
            :class="['w-4 h-4 text-orange-400 transition-transform duration-200', showUserMenu ? 'rotate-180' : '']"
          />
        </button>

        <!-- User dropdown -->
        <transition name="dropdown">
          <div
            v-if="showUserMenu"
            ref="userDropdown"
            class="absolute right-0 top-12 w-52 bg-white rounded-2xl shadow-xl border border-orange-100 z-50 overflow-hidden py-1"
          >
            <div class="px-4 py-3 border-b border-orange-100">
              <p class="text-sm font-semibold text-gray-800">{{ userName }}</p>
              <p class="text-xs text-gray-400">admin@eduadmin.vn</p>
            </div>
            <router-link to="/account/profile" @click="showUserMenu = false">
              <div class="flex items-center gap-2 px-4 py-2.5 text-sm text-gray-700 hover:bg-orange-50 transition-colors">
                <UserCircleIcon class="w-4 h-4 text-orange-400" />
                Thông tin cá nhân
              </div>
            </router-link>
            <router-link to="/account/password" @click="showUserMenu = false">
              <div class="flex items-center gap-2 px-4 py-2.5 text-sm text-gray-700 hover:bg-orange-50 transition-colors">
                <ShieldCheckIcon class="w-4 h-4 text-orange-400" />
                Đổi mật khẩu
              </div>
            </router-link>
            <div class="border-t border-orange-100 mt-1">
              <button
                class="w-full flex items-center gap-2 px-4 py-2.5 text-sm text-red-500 hover:bg-red-50 transition-colors"
                @click="handleLogout"
              >
                <ArrowRightOnRectangleIcon class="w-4 h-4" />
                Đăng xuất
              </button>
            </div>
          </div>
        </transition>
      </div>
    </div>
  </header>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute } from 'vue-router'
import {
  Bars3Icon,
  ClockIcon,
  BellIcon,
  ChevronDownIcon,
  UserCircleIcon,
  ShieldCheckIcon,
  ArrowRightOnRectangleIcon,
  UserPlusIcon,
  CurrencyDollarIcon,
  ExclamationTriangleIcon,
} from '@heroicons/vue/24/outline'

// ─── Props & Emits ───────────────────────────────────────────────────────────
defineProps({
  sidebarCollapsed: { type: Boolean, default: false },
})
defineEmits(['toggleSidebar', 'toggleMobileSidebar'])

// ─── Clock ───────────────────────────────────────────────────────────────────
const currentTime = ref('')

function updateTime() {
  const now = new Date()
  currentTime.value = now.toLocaleTimeString('vi-VN', {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
  })
}

let timer = null
onMounted(() => {
  updateTime()
  timer = setInterval(updateTime, 1000)
  document.addEventListener('click', handleOutsideClick)
})
onUnmounted(() => {
  clearInterval(timer)
  document.removeEventListener('click', handleOutsideClick)
})

// ─── Page title from route ───────────────────────────────────────────────────
const route = useRoute()
const routeTitleMap = {
  '/': 'Tổng quan',
  '/users/students': 'Phụ huynh & Học sinh',
  '/users/teachers': 'Giáo viên',
  '/users/roles': 'Phân quyền & Bảo mật',
  '/training/courses': 'Danh mục Khóa học',
  '/training/classes': 'Danh sách Lớp học',
  '/training/schedule': 'Lịch học & Thời khóa biểu',
  '/learning/questions': 'Ngân hàng câu hỏi',
  '/learning/gamification': 'Gamification',
  '/finance/invoices': 'Hóa đơn & Biên lai',
  '/finance/transactions': 'Lịch sử giao dịch',
  '/communication/notifications': 'Quản lý Thông báo',
  '/reports/revenue': 'Báo cáo Doanh thu',
  '/reports/growth': 'Tăng trưởng học viên',
  '/reports/attendance': 'Thống kê chuyên cần',
  '/account/profile': 'Thông tin cá nhân',
  '/account/password': 'Đổi mật khẩu / PIN',
}
const pageTitle = computed(() =>
  route.meta?.title ?? routeTitleMap[route.path] ?? 'EduAdmin'
)

// ─── User info ───────────────────────────────────────────────────────────────
const userName = ref('Nguyễn Admin')
const userInitials = computed(() => {
  return userName.value
    .split(' ')
    .slice(-2)
    .map((w) => w[0])
    .join('')
    .toUpperCase()
})

// ─── Notifications ───────────────────────────────────────────────────────────
const showNotifications = ref(false)
const notifDropdown = ref(null)

const notifications = ref([
  {
    id: 1,
    message: 'Học viên mới Trần Minh Khoa vừa đăng ký khóa học Toán 10.',
    time: '2 phút trước',
    icon: UserPlusIcon,
    color: 'bg-green-400',
    read: false,
  },
  {
    id: 2,
    message: 'Thanh toán học phí tháng 10 của lớp Toán 8A đã hoàn tất.',
    time: '15 phút trước',
    icon: CurrencyDollarIcon,
    color: 'bg-blue-400',
    read: false,
  },
  {
    id: 3,
    message: 'Giáo viên Lê Thị Hoa xin nghỉ phép ngày 25/10.',
    time: '1 giờ trước',
    icon: ExclamationTriangleIcon,
    color: 'bg-orange-400',
    read: true,
  },
])

const unreadCount = computed(() => notifications.value.filter((n) => !n.read).length)

function toggleNotifications() {
  showNotifications.value = !showNotifications.value
  showUserMenu.value = false
}

function markAllRead() {
  notifications.value.forEach((n) => (n.read = true))
}

// ─── User menu ───────────────────────────────────────────────────────────────
const showUserMenu = ref(false)
const userDropdown = ref(null)

function toggleUserMenu() {
  showUserMenu.value = !showUserMenu.value
  showNotifications.value = false
}

// ─── Outside click handler ───────────────────────────────────────────────────
function handleOutsideClick(event) {
  if (showNotifications.value && notifDropdown.value && !notifDropdown.value.contains(event.target)) {
    // Check if the click was on the notification button itself
    if (!event.target.closest('[title="Thông báo"]')) {
      showNotifications.value = false
    }
  }
  if (showUserMenu.value && userDropdown.value && !userDropdown.value.contains(event.target)) {
    showUserMenu.value = false
  }
}

// ─── Logout ──────────────────────────────────────────────────────────────────
function handleLogout() {
  showUserMenu.value = false
  console.log('Logging out...')
}
</script>

<style scoped>
.dropdown-enter-active,
.dropdown-leave-active {
  transition: opacity 0.15s ease, transform 0.15s ease;
}
.dropdown-enter-from,
.dropdown-leave-to {
  opacity: 0;
  transform: translateY(-6px) scale(0.97);
}
</style>
