<template>
  <header class="flex items-center gap-3 px-4 md:px-6 bg-white border-b border-orange-100 shadow-sm min-h-[64px] z-10">
    <!-- ─── Hamburger ──────────────────────────────────────────────────── -->
    <!-- Desktop: collapse/expand sidebar -->
    <button
      class="hidden lg:flex items-center justify-center w-9 h-9 rounded-lg text-orange-600 hover:bg-orange-100 transition-colors cursor-pointer"
      @click="$emit('toggleSidebar')" title="Thu gọn / Mở rộng sidebar">
      <Bars3Icon class="w-5 h-5" />
    </button>

    <!-- Mobile: open sidebar overlay -->
    <button
      class="flex lg:hidden items-center justify-center w-9 h-9 rounded-lg text-orange-600 hover:bg-orange-100 transition-colors cursor-pointer"
      @click="$emit('toggleMobileSidebar')" title="Mở menu">
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
        class="relative flex items-center justify-center w-9 h-9 rounded-full hover:bg-orange-100 text-orange-600 transition-colors cursor-pointer"
        @click="toggleNotifications" title="Thông báo">
        <BellIcon class="w-5 h-5" />
        <span v-if="unreadCount > 0"
          class="absolute top-0.5 right-0.5 flex items-center justify-center w-4 h-4 rounded-full bg-red-500 text-white text-[10px] font-bold leading-none">
          {{ unreadCount > 9 ? '9+' : unreadCount }}
        </span>
      </button>

      <!-- Notification dropdown -->
      <transition name="dropdown">
        <div v-if="showNotifications" ref="notifDropdown"
          class="absolute top-16 right-16 md:right-24 w-80 bg-white rounded-2xl shadow-xl border border-orange-100 z-50 overflow-hidden">
          <div class="flex items-center justify-between px-4 py-3 border-b border-orange-100">
            <span class="font-semibold text-orange-900 text-sm">Thông báo</span>
            <button class="text-xs text-orange-500 hover:text-orange-700 cursor-pointer" @click="markAllRead">
              Đánh dấu đã đọc
            </button>
          </div>
          <div class="max-h-72 overflow-y-auto divide-y divide-orange-50">
            <div v-for="notif in notifications" :key="notif.id" :class="[
              'flex items-start gap-3 px-4 py-3 hover:bg-orange-50 cursor-pointer transition-colors',
              !notif.read ? 'bg-orange-50/50' : '',
            ]">
              <div :class="[
                'w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0 mt-0.5',
                notif.color,
              ]">
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
            <button class="text-xs text-orange-500 hover:text-orange-700 font-medium cursor-pointer">
              Xem tất cả thông báo
            </button>
          </div>
        </div>
      </transition>

      <!-- ─── User profile container ───────────────────────────────────── -->
      <div class="relative" ref="userContainerRef">
        <div
          class="flex items-center bg-orange-50/60 hover:bg-orange-100/70 border border-orange-200/70 rounded-full pl-1.5 pr-1.5 py-1 transition-all shadow-xs">
          <!-- Clickable Avatar & Name: Navigates to Personal Profile -->
          <button type="button"
            class="flex items-center gap-2 pr-1.5 rounded-full hover:opacity-90 transition-opacity cursor-pointer text-left focus:outline-none"
            @click="goToProfile" title="Nhấn để xem và chỉnh sửa thông tin cá nhân">
            <!-- Avatar image or gradient initials -->
            <div class="relative flex-shrink-0">
              <img v-if="userAvatar" :src="userAvatar" :alt="userName"
                class="w-8 h-8 rounded-full object-cover ring-2 ring-orange-300 shadow-xs" />
              <div v-else
                class="w-8 h-8 rounded-full bg-gradient-to-br from-orange-400 to-amber-500 flex items-center justify-center text-white text-xs font-bold shadow-xs ring-2 ring-orange-300/80">
                {{ userInitials }}
              </div>
              <!-- Online status dot -->
              <span class="absolute bottom-0 right-0 w-2.5 h-2.5 rounded-full bg-emerald-500 ring-2 ring-white"></span>
            </div>

            <!-- Name and Role -->
            <div class="hidden md:block text-left min-w-0 pr-1">
              <p
                class="text-sm font-semibold text-orange-950 leading-tight truncate max-w-[130px] hover:text-orange-600 transition-colors">
                {{ userName }}
              </p>
              <div class="flex items-center gap-1 mt-0.5">
                <span
                  class="text-[10px] font-semibold uppercase tracking-wider text-orange-700 bg-orange-100/90 px-1.5 py-0.2 rounded">
                  Admin
                </span>
              </div>
            </div>
          </button>

          <!-- Toggle dropdown menu button -->
          <button type="button"
            class="w-7 h-7 rounded-full flex items-center justify-center text-orange-500 hover:text-orange-800 hover:bg-orange-200/60 transition-colors cursor-pointer focus:outline-none"
            @click.stop="toggleUserMenu" title="Mở menu tài khoản" aria-label="Mở menu tài khoản">
            <ChevronDownIcon :class="['w-4 h-4 transition-transform duration-200', showUserMenu ? 'rotate-180' : '']" />
          </button>
        </div>

        <!-- User dropdown -->
        <transition name="dropdown">
          <div v-if="showUserMenu" ref="userDropdown"
            class="absolute right-0 top-12 w-64 bg-white rounded-2xl shadow-xl border border-orange-100 z-50 overflow-hidden py-1 divide-y divide-orange-50">
            <!-- Dropdown Header card -->
            <div
              class="px-4 py-3 bg-gradient-to-r from-orange-50/80 to-amber-50/40 cursor-pointer hover:bg-orange-50 transition-colors"
              @click="goToProfile">
              <div class="flex items-center gap-3">
                <div
                  class="w-10 h-10 rounded-full bg-orange-400 text-white flex items-center justify-center font-bold text-sm shadow-xs flex-shrink-0 overflow-hidden">
                  <img v-if="userAvatar" :src="userAvatar" :alt="userName" class="w-full h-full object-cover" />
                  <span v-else>{{ userInitials }}</span>
                </div>
                <div class="min-w-0 flex-1">
                  <p class="text-sm font-bold text-orange-950 truncate">{{ userName }}</p>
                  <p class="text-xs text-gray-500 truncate">{{ userEmail || 'Chưa cập nhật email' }}</p>
                  <div class="mt-1 flex items-center gap-1.5">
                    <span
                      class="inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-medium bg-emerald-100 text-emerald-800">
                      ● Đang hoạt động
                    </span>
                  </div>
                </div>
              </div>
            </div>

            <!-- Menu links -->
            <div class="py-1">
              <router-link to="/account/profile"
                class="flex items-center gap-3 px-4 py-2.5 text-sm text-gray-700 hover:bg-orange-50 hover:text-orange-900 transition-colors group"
                @click="showUserMenu = false">
                <div
                  class="w-7 h-7 rounded-lg bg-orange-100/60 group-hover:bg-orange-200/80 text-orange-600 flex items-center justify-center transition-colors">
                  <UserCircleIcon class="w-4 h-4" />
                </div>
                <div>
                  <p class="font-medium text-sm">Thông tin cá nhân</p>
                  <p class="text-[11px] text-gray-400">Xem và cập nhật hồ sơ cá nhân</p>
                </div>
              </router-link>
            </div>

            <!-- Logout -->
            <div class="py-1 bg-gray-50/50">
              <button
                class="w-full flex items-center gap-3 px-4 py-2.5 text-sm text-red-600 hover:bg-red-50 transition-colors cursor-pointer group"
                @click="handleLogout">
                <div
                  class="w-7 h-7 rounded-lg bg-red-100/70 group-hover:bg-red-200/80 text-red-600 flex items-center justify-center transition-colors">
                  <ArrowRightOnRectangleIcon class="w-4 h-4" />
                </div>
                <div class="text-left">
                  <p class="font-medium text-sm">Đăng xuất</p>
                  <p class="text-[11px] text-red-400">Thoát khỏi phiên làm việc</p>
                </div>
              </button>
            </div>
          </div>
        </transition>
      </div>
    </div>
  </header>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import authService from '@/services/auth.service'
import profileService from '@/services/profile.service'
import {
  Bars3Icon,
  ClockIcon,
  BellIcon,
  ChevronDownIcon,
  UserCircleIcon,
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

const route = useRoute()
const router = useRouter()

// ─── User Profile State (Logic từ nhánh feat/admin-profile) ──────────────────
const userName = ref('Admin')
const userEmail = ref('')
const userAvatar = ref('')

const userInitials = computed(() => {
  return userName.value
    .split(' ')
    .slice(-2)
    .map((w) => w[0])
    .join('')
    .toUpperCase()
})

async function loadUserProfile() {
  try {
    const profile = await profileService.get()
    userName.value = profile.fullName || 'Admin'
    userEmail.value = profile.email || ''
    userAvatar.value = profile.avatar || ''
  } catch (error) {
    console.error('Không thể tải thông tin Admin trên thanh tiêu đề.', error)
  }
}

watch(() => route.fullPath, loadUserProfile)

// ─── Navigation to Profile (Logic từ nhánh main) ─────────────────────────────
function goToProfile() {
  showUserMenu.value = false
  router.push('/account/profile')
}

// ─── Clock ───────────────────────────────────────────────────────────────────
const currentTime = ref('')
let timer = null

function updateTime() {
  const now = new Date()
  currentTime.value = now.toLocaleTimeString('vi-VN', {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
  })
}

onMounted(async () => {
  updateTime()
  loadUserProfile()
  timer = setInterval(updateTime, 1000)
  document.addEventListener('click', handleOutsideClick)
  // Lắng nghe event thay đổi avatar từ profile.service hoặc component con
  window.addEventListener('admin-profile-updated', loadUserProfile)
})

onUnmounted(() => {
  clearInterval(timer)
  document.removeEventListener('click', handleOutsideClick)
  window.removeEventListener('admin-profile-updated', loadUserProfile)
})

// ─── Page title from route ───────────────────────────────────────────────────
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
}
const pageTitle = computed(() =>
  route.meta?.title ?? routeTitleMap[route.path] ?? 'EduAdmin'
)

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
const userContainerRef = ref(null)

function toggleUserMenu() {
  showUserMenu.value = !showUserMenu.value
  showNotifications.value = false
}

// ─── Outside click handler ───────────────────────────────────────────────────
function handleOutsideClick(event) {
  if (showNotifications.value && notifDropdown.value && !notifDropdown.value.contains(event.target)) {
    if (!event.target.closest('[title="Thông báo"]')) {
      showNotifications.value = false
    }
  }
  if (showUserMenu.value && userContainerRef.value && !userContainerRef.value.contains(event.target)) {
    showUserMenu.value = false
  }
}

// ─── Logout ──────────────────────────────────────────────────────────────────
function handleLogout() {
  showUserMenu.value = false
  authService.logout()
  router.replace({ name: 'login' })
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