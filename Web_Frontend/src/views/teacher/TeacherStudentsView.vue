<template>
  <div class="space-y-6 max-w-7xl mx-auto pb-12">
    <!-- ─── Header ───────────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div>
        <nav class="flex items-center gap-2 text-xs font-medium text-stone-500 mb-1">
          <span class="text-orange-600 font-semibold">Giáo viên</span>
          <ChevronRightIcon class="w-3.5 h-3.5 text-stone-400" />
          <span>Danh sách học sinh theo lớp</span>
        </nav>
        <h1 class="text-2xl md:text-3xl font-bold text-stone-900 tracking-tight">Danh sách học sinh</h1>
        <p class="text-xs md:text-sm text-stone-500 mt-0.5">
          Xem hồ sơ học viên, điểm rèn luyện, lịch sử chuyên cần và thông tin liên hệ phụ huynh
        </p>
      </div>

      <div class="flex items-center gap-2 flex-wrap">
        <button
          type="button"
          @click="loadStudentsByClass"
          :disabled="isLoadingStudents || !selectedClassId"
          class="inline-flex items-center gap-1.5 h-9 px-3.5 rounded-md bg-white hover:bg-stone-100 text-stone-800 border border-stone-300 hover:border-stone-400 text-xs font-semibold transition-colors cursor-pointer disabled:opacity-50"
        >
          <ArrowPathIcon class="w-4 h-4 text-orange-600" :class="{ 'animate-spin': isLoadingStudents }" />
          <span>Làm mới</span>
        </button>

        <button
          type="button"
          class="inline-flex items-center gap-1.5 h-9 px-3.5 rounded-md bg-orange-600 hover:bg-orange-700 active:bg-orange-800 text-white border border-orange-700 text-xs font-semibold shadow-xs transition-colors cursor-pointer disabled:opacity-50"
          @click="exportCsv"
          :disabled="!selectedClassId || totalCount === 0"
        >
          <ArrowDownTrayIcon class="w-4 h-4" />
          <span>Xuất danh sách (CSV)</span>
        </button>
      </div>
    </div>

    <!-- ─── Filter & Class Selector Card ─────────────────────────────────── -->
    <div class="bg-white rounded-lg border border-stone-300 p-4 shadow-[0_1px_2px_rgba(0,0,0,0.04)] space-y-3">
      <div class="grid grid-cols-1 md:grid-cols-3 gap-3">
        <!-- Class Selector (Assigned classes only) -->
        <div>
          <label class="block text-xs font-semibold uppercase tracking-wide text-stone-700 mb-1.5">
            Chọn lớp học phụ trách <span class="text-rose-600">*</span>
          </label>
          <select
            v-model="selectedClassId"
            @change="onClassChange"
            class="w-full h-10 text-xs font-semibold bg-stone-50/70 border border-stone-300 text-stone-900 rounded-md px-3.5 focus:bg-white focus:border-orange-600 focus:ring-2 focus:ring-orange-500/20 outline-none transition-colors"
          >
            <option :value="null" disabled>-- Chọn lớp học --</option>
            <option v-for="cls in myClasses" :key="cls.id" :value="cls.id">
              {{ cls.name }} {{ cls.room ? `(${cls.room})` : '' }}
            </option>
          </select>
        </div>

        <!-- Search Bar -->
        <div class="md:col-span-2">
          <label class="block text-xs font-semibold uppercase tracking-wide text-stone-700 mb-1.5">
            Tìm kiếm học sinh hoặc phụ huynh
          </label>
          <SearchBar
            v-model="search"
            placeholder="Tìm theo tên học sinh, mã HS, họ tên hoặc SĐT phụ huynh..."
          />
        </div>
      </div>
    </div>

    <!-- ─── Current Class Summary Ribbon ─────────────────────────────────── -->
    <div
      v-if="currentClass"
      class="bg-white rounded-lg border border-stone-300 border-l-4 border-l-orange-600 p-4 sm:p-5 flex flex-wrap items-center justify-between gap-4 text-xs shadow-[0_1px_2px_rgba(0,0,0,0.04)]"
    >
      <div class="flex items-center gap-3.5">
        <div class="w-11 h-11 rounded-md bg-orange-600 border border-orange-700 text-white flex items-center justify-center font-bold text-sm flex-shrink-0 tabular-nums">
          #{{ currentClass.id }}
        </div>
        <div>
          <div class="flex items-center gap-2 flex-wrap">
            <h2 class="font-bold text-stone-900 text-base">{{ currentClass.name }}</h2>
            <span class="px-2 py-0.5 rounded border border-emerald-300 bg-emerald-50 text-emerald-800 font-semibold text-[11px]">
              {{ currentClass.status === 'ACTIVE' || currentClass.status === 'OPEN' ? 'Đang hoạt động' : currentClass.status }}
            </span>
          </div>
          <p class="text-stone-600 mt-1 font-medium">
            {{ currentClass.courseLevelName || currentClass.courseName || 'Khóa học' }} &bull; Phòng: <span class="font-bold text-stone-900">{{ currentClass.room || 'Phòng chính' }}</span>
          </p>
        </div>
      </div>

      <div class="flex items-center gap-2.5 flex-wrap">
        <div class="bg-stone-50 px-3 h-9 inline-flex items-center rounded-md border border-stone-300 text-stone-800 font-semibold text-xs tabular-nums">
          Sĩ số: <span class="text-emerald-700 font-bold mx-1">{{ currentClass.activeStudentsCount ?? totalCount }}</span> / {{ currentClass.maxCapacity || 20 }} học sinh
        </div>

        <router-link
          :to="{ path: '/teacher/attendance', query: { classId: currentClass.id } }"
          class="inline-flex items-center h-9 px-3.5 rounded-md bg-orange-600 hover:bg-orange-700 text-white border border-orange-700 font-semibold transition-colors shadow-xs"
        >
          Điểm danh lớp này
        </router-link>

        <router-link
          :to="{ path: '/teacher/schedule', query: { classId: currentClass.id } }"
          class="inline-flex items-center h-9 px-3 rounded-md bg-white hover:bg-stone-100 text-stone-800 border border-stone-300 hover:border-stone-400 font-semibold transition-colors"
        >
          Lịch học
        </router-link>
      </div>
    </div>

    <!-- ─── Students Table ───────────────────────────────────────────────── -->
    <div class="bg-white rounded-lg border border-stone-300 shadow-[0_1px_2px_rgba(0,0,0,0.04)] overflow-hidden">
      <!-- Loading State -->
      <div v-if="isLoadingStudents" class="p-12 text-center text-stone-600 space-y-3">
        <ArrowPathIcon class="w-8 h-8 animate-spin mx-auto text-orange-600" />
        <p class="text-sm font-semibold">Đang tải danh sách học sinh từ máy chủ...</p>
      </div>

      <!-- No Class Selected -->
      <div v-else-if="!selectedClassId" class="p-12 text-center text-stone-500 space-y-2">
        <UserGroupIcon class="w-10 h-10 text-stone-400 mx-auto" />
        <h3 class="text-base font-bold text-stone-800">Chưa chọn lớp học</h3>
        <p class="text-xs text-stone-500">Vui lòng chọn lớp học ở danh sách bên trên để hiển thị học sinh.</p>
      </div>

      <!-- Empty Students -->
      <div v-else-if="students.length === 0" class="p-12 text-center">
        <EmptyState
          :icon="UserGroupIcon"
          message="Không tìm thấy học sinh nào"
          sub="Lớp học này hiện chưa có học sinh ghi danh, hoặc không có học sinh khớp với từ khóa tìm kiếm."
        />
      </div>

      <!-- Table View -->
      <div v-else class="overflow-x-auto">
        <table class="w-full text-left border-collapse text-xs">
          <thead>
            <tr class="bg-stone-100 border-b-2 border-stone-300 text-stone-700 font-bold uppercase tracking-wider text-xs">
              <th class="py-3.5 px-4 w-12 text-center">STT</th>
              <th class="py-3.5 px-4 min-w-[200px]">Học viên</th>
              <th class="py-3.5 px-4 min-w-[100px]">Mã HS</th>
              <th class="py-3.5 px-4 min-w-[180px]">Phụ huynh liên hệ</th>
              <th class="py-3.5 px-4 min-w-[120px]">Trạng thái</th>
              <th class="py-3.5 px-4 text-center min-w-[110px]">Tích lũy & Sao</th>
              <th class="py-3.5 px-4 text-center min-w-[110px]">Ngày tham gia</th>
              <th class="py-3.5 px-4 text-right min-w-[130px]">Thao tác</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-stone-200">
            <tr
              v-for="(st, index) in students"
              :key="st.studentId || st.id"
              class="hover:bg-orange-50/50 transition-colors"
            >
              <!-- Index -->
              <td class="py-3.5 px-4 text-center text-stone-500 font-medium tabular-nums">
                {{ (page - 1) * pageSize + index + 1 }}
              </td>

              <!-- Student Name & Avatar -->
              <td class="py-3.5 px-4">
                <div class="flex items-center gap-3">
                  <div class="w-9 h-9 rounded-full bg-orange-50 border border-orange-200 text-orange-800 font-bold flex items-center justify-center text-xs flex-shrink-0">
                    <img
                      v-if="st.avatar"
                      :src="st.avatar"
                      :alt="st.studentName"
                      class="w-full h-full rounded-full object-cover"
                    />
                    <span v-else>{{ getInitials(st.studentName) }}</span>
                  </div>
                  <div>
                    <p class="font-bold text-sm text-stone-900">{{ st.studentName || 'Học sinh' }}</p>
                    <p class="text-[11px] text-stone-500 tabular-nums">ID tài khoản: #{{ st.studentId }}</p>
                  </div>
                </div>
              </td>

              <!-- Student Code -->
              <td class="py-3.5 px-4 font-mono font-bold text-orange-700 tabular-nums">
                HS-{{ String(st.studentId).padStart(4, '0') }}
              </td>

              <!-- Parent Info -->
              <td class="py-3.5 px-4">
                <p class="font-semibold text-stone-800">{{ st.parentName || 'Chưa cập nhật PH' }}</p>
                <p v-if="st.parentPhone" class="text-[11px] font-mono text-stone-600 mt-0.5 tabular-nums">
                  📞 {{ st.parentPhone }}
                </p>
                <p v-if="st.parentEmail" class="text-[11px] text-stone-500 truncate max-w-[180px]">
                  ✉️ {{ st.parentEmail }}
                </p>
              </td>

              <!-- Status -->
              <td class="py-3.5 px-4">
                <span
                  :class="[
                    'px-2 py-0.5 rounded border text-[11px] font-semibold inline-block',
                    st.status === 'ACTIVE' || !st.status ? 'border-emerald-300 bg-emerald-50 text-emerald-800' :
                    st.status === 'PENDING' ? 'border-amber-300 bg-amber-50 text-amber-800' :
                    'border-stone-300 bg-stone-100 text-stone-700'
                  ]"
                >
                  {{ st.status === 'ACTIVE' || !st.status ? 'Đang học' : (st.status === 'PENDING' ? 'Chờ duyệt' : st.status) }}
                </span>
              </td>

              <!-- Points & Stars -->
              <td class="py-3.5 px-4 text-center tabular-nums">
                <div class="flex items-center justify-center gap-2">
                  <span
                    :class="[
                      'inline-flex items-center gap-1 px-2 py-0.5 rounded border text-[11px]',
                      (st.stars || 0) === 0
                        ? 'text-stone-400 font-normal bg-stone-50 border-stone-200'
                        : 'font-bold text-orange-700 bg-orange-50 border-orange-200'
                    ]"
                  >
                    ⭐ {{ st.stars || 0 }}
                  </span>
                  <span
                    :class="[
                      'text-[11px]',
                      (st.totalPoints || 0) === 0 ? 'text-stone-400 font-normal' : 'text-stone-700 font-semibold'
                    ]"
                  >
                    🏆 {{ st.totalPoints || 0 }}đ
                  </span>
                </div>
              </td>

              <!-- Joined Date -->
              <td class="py-3.5 px-4 text-center text-stone-600 font-mono text-[11px] tabular-nums">
                <span :class="!st.joinedAt ? 'text-stone-400' : ''">{{ formatDate(st.joinedAt) || '—' }}</span>
              </td>

              <!-- Actions -->
              <td class="py-3.5 px-4 text-right">
                <div class="flex items-center justify-end gap-1.5">
                  <!-- History Attendance -->
                  <button
                    type="button"
                    @click="openStudentHistory(st)"
                    class="h-8 w-8 inline-flex items-center justify-center rounded-md border border-stone-300 bg-white text-stone-700 hover:bg-stone-100 hover:border-stone-400 transition-colors cursor-pointer"
                    title="Xem lịch sử chuyên cần của học sinh này"
                  >
                    <ClockIcon class="w-4 h-4 text-orange-600" />
                  </button>

                  <!-- Grading -->
                  <router-link
                    to="/teacher/grading"
                    class="h-8 w-8 inline-flex items-center justify-center rounded-md border border-stone-300 bg-white text-stone-700 hover:bg-stone-100 hover:border-stone-400 transition-colors"
                    title="Chấm điểm & Nhận xét"
                  >
                    <PencilSquareIcon class="w-4 h-4 text-orange-600" />
                  </router-link>

                  <!-- Chat with parent -->
                  <router-link
                    v-if="st.parentId"
                    :to="{ path: '/teacher/communication/chat', query: { parentId: st.parentId } }"
                    class="h-8 w-8 inline-flex items-center justify-center rounded-md border border-stone-300 bg-white text-stone-700 hover:bg-stone-100 hover:border-stone-400 transition-colors"
                    title="Chat với phụ huynh"
                  >
                    <ChatBubbleLeftRightIcon class="w-4 h-4" />
                  </router-link>

                  <!-- Phone / Contact -->
                  <a
                    v-if="st.parentPhone"
                    :href="`tel:${st.parentPhone}`"
                    class="h-8 w-8 inline-flex items-center justify-center rounded-md border border-emerald-300 bg-emerald-50 text-emerald-700 hover:bg-emerald-100 transition-colors"
                    title="Gọi cho phụ huynh"
                  >
                    <PhoneIcon class="w-4 h-4" />
                  </a>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Table Pagination Footer -->
      <PaginationBar
        v-model:page="page"
        v-model:page-size="pageSize"
        :total-count="totalCount"
        :total-pages="totalPages"
        :disabled="isLoadingStudents"
        item-label="học sinh"
        @change="loadStudentsByClass"
      />
    </div>

    <!-- ─── Modal: Student Attendance History ──────────────────────────── -->
    <div
      v-if="showHistoryModal"
      class="fixed inset-0 bg-black/50 backdrop-blur-xs z-50 flex items-center justify-center p-4"
    >
      <div class="bg-white rounded-lg w-full max-w-2xl overflow-hidden shadow-xl border border-stone-300 flex flex-col max-h-[90vh]">
        <!-- Modal Header -->
        <div class="px-6 py-4 bg-stone-50 border-b border-stone-200 flex items-center justify-between">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-full bg-orange-600 text-white flex items-center justify-center font-bold text-sm">
              {{ getInitials(selectedStudent?.studentName || 'HS') }}
            </div>
            <div>
              <h3 class="font-bold text-stone-900 text-base">Lịch sử chuyên cần học sinh</h3>
              <p class="text-xs text-stone-600 tabular-nums">
                {{ selectedStudent?.studentName }} &bull; Mã HS: HS-{{ String(selectedStudent?.studentId).padStart(4, '0') }}
              </p>
            </div>
          </div>
          <button
            type="button"
            @click="showHistoryModal = false"
            class="text-stone-400 hover:text-stone-700 p-1 rounded-md hover:bg-stone-200/60 transition-colors cursor-pointer"
          >
            <XMarkIcon class="w-5 h-5" />
          </button>
        </div>

        <!-- Modal Body Content -->
        <div class="p-6 overflow-y-auto space-y-4 flex-1">
          <!-- Stats Ribbon -->
          <div class="grid grid-cols-4 gap-2 text-center text-xs tabular-nums">
            <div class="p-2.5 rounded-md bg-stone-50 border border-stone-300">
              <p class="text-stone-500 font-medium">Tổng buổi</p>
              <p class="text-base font-bold text-stone-900 mt-0.5">{{ studentHistoryList.length }}</p>
            </div>
            <div class="p-2.5 rounded-md bg-emerald-50 border border-emerald-300">
              <p class="text-emerald-800 font-medium">Có mặt</p>
              <p class="text-base font-bold text-emerald-800 mt-0.5">
                {{ studentHistoryList.filter(h => h.status === 'PRESENT').length }}
              </p>
            </div>
            <div class="p-2.5 rounded-md bg-amber-50 border border-amber-300">
              <p class="text-amber-800 font-medium">Đi muộn</p>
              <p class="text-base font-bold text-amber-800 mt-0.5">
                {{ studentHistoryList.filter(h => h.status === 'LATE').length }}
              </p>
            </div>
            <div class="p-2.5 rounded-md bg-rose-50 border border-rose-300">
              <p class="text-rose-800 font-medium">Vắng</p>
              <p class="text-base font-bold text-rose-800 mt-0.5">
                {{ studentHistoryList.filter(h => h.status === 'ABSENT' || h.status === 'EXCUSED').length }}
              </p>
            </div>
          </div>

          <!-- History Table -->
          <div v-if="isLoadingHistory" class="py-8 text-center text-stone-600 space-y-2">
            <ArrowPathIcon class="w-6 h-6 animate-spin mx-auto text-orange-600" />
            <p class="text-xs font-semibold">Đang tải lịch sử điểm danh...</p>
          </div>

          <div v-else-if="studentHistoryList.length === 0" class="py-8 text-center text-stone-500 space-y-1">
            <CalendarDaysIcon class="w-8 h-8 mx-auto text-stone-300" />
            <p class="text-xs font-medium">Chưa có bản ghi điểm danh nào trước đây của học sinh này.</p>
          </div>

          <div v-else class="border border-stone-300 rounded-md overflow-hidden">
            <table class="w-full text-left text-xs">
              <thead class="bg-stone-100 border-b-2 border-stone-300 text-stone-700 font-bold uppercase text-[11px]">
                <tr>
                  <th class="py-2.5 px-3">Ngày học</th>
                  <th class="py-2.5 px-3">Lớp học</th>
                  <th class="py-2.5 px-3 text-center">Trạng thái</th>
                  <th class="py-2.5 px-3">Ghi chú</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-stone-200">
                <tr v-for="h in studentHistoryList" :key="h.attendanceId || h.lessonId" class="hover:bg-orange-50/40">
                  <td class="py-2.5 px-3 font-medium text-stone-900 tabular-nums">
                    {{ formatDate(h.lessonDate) }}
                  </td>
                  <td class="py-2.5 px-3 text-stone-700">
                    {{ h.className || currentClass?.name || 'Toán tư duy' }}
                  </td>
                  <td class="py-2.5 px-3 text-center">
                    <span
                      :class="[
                        'px-2 py-0.5 rounded border font-semibold text-[11px] inline-block',
                        h.status === 'PRESENT' ? 'border-emerald-300 bg-emerald-50 text-emerald-800' :
                        h.status === 'LATE' ? 'border-amber-300 bg-amber-50 text-amber-800' :
                        h.status === 'EXCUSED' ? 'border-stone-300 bg-stone-100 text-stone-700' :
                        'border-rose-300 bg-rose-50 text-rose-800'
                      ]"
                    >
                      {{ formatStatusLabel(h.status) }}
                    </span>
                  </td>
                  <td class="py-2.5 px-3 text-stone-500 italic">
                    {{ h.teacherNotes || '—' }}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Modal Footer -->
        <div class="px-6 py-3.5 bg-stone-50 border-t border-stone-200 flex justify-end">
          <button
            type="button"
            @click="showHistoryModal = false"
            class="h-9 px-4 rounded-md text-xs font-semibold bg-white border border-stone-300 text-stone-800 hover:bg-stone-100 hover:border-stone-400 transition-colors cursor-pointer"
          >
            Đóng
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onBeforeUnmount, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import classService from '@/services/class.service'
import teacherService from '@/services/teacher.service'
import attendanceService from '@/services/attendance.service'
import { useUserState } from '@/services/user-state'
import { getProfileId } from '@/services/auth-storage'
import {
  UserGroupIcon,
  ArrowDownTrayIcon,
  PencilSquareIcon,
  ArrowPathIcon,
  ChevronRightIcon,
  ClockIcon,
  PhoneIcon,
  XMarkIcon,
  CalendarDaysIcon,
  ChatBubbleLeftRightIcon,
} from '@heroicons/vue/24/outline'
import PaginationBar from '@/components/common/PaginationBar.vue'
import SearchBar from '@/components/common/SearchBar.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import { usePagination } from '@/composables/usePagination'

const route = useRoute()
const { fetchProfile } = useUserState()
const { page, pageSize, totalCount, totalPages, applyPagedResult, resetPage } = usePagination({
  initialPageSize: 20,
})

const search = ref('')
const selectedClassId = ref(null)
const myClasses = ref([])
const students = ref([])
const isLoadingStudents = ref(false)

let searchTimer = null
let requestToken = 0

watch(search, () => {
  if (searchTimer) clearTimeout(searchTimer)
  searchTimer = setTimeout(() => {
    if (!selectedClassId.value) return
    resetPage()
    loadStudentsByClass()
  }, 300)
})

onBeforeUnmount(() => {
  if (searchTimer) clearTimeout(searchTimer)
})

// Modal history states
const showHistoryModal = ref(false)
const selectedStudent = ref(null)
const studentHistoryList = ref([])
const isLoadingHistory = ref(false)

onMounted(async () => {
  await loadClasses()
  if (route.query.classId) {
    const cId = parseInt(route.query.classId, 10)
    if (!isNaN(cId)) {
      selectedClassId.value = cId
      await loadStudentsByClass()
    }
  }
})

async function loadClasses() {
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
        const matched = rawTeachers.find(t =>
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

    if (myClasses.value.length > 0 && !selectedClassId.value) {
      selectedClassId.value = myClasses.value[0].id
      await loadStudentsByClass()
    }
  } catch (err) {
    console.error('Lỗi khi tải danh sách lớp học:', err)
  }
}

async function loadStudentsByClass() {
  if (!selectedClassId.value) return
  const token = ++requestToken
  isLoadingStudents.value = true
  try {
    const data = await classService.getStudentsPaged(selectedClassId.value, {
      page: page.value,
      pageSize: pageSize.value,
      keyword: search.value,
    })
    if (token !== requestToken) return

    const items = applyPagedResult(data)
    if (items.length === 0 && page.value > 1 && totalPages.value > 0 && page.value > totalPages.value) {
      page.value = totalPages.value
      await loadStudentsByClass()
      return
    }
    students.value = items
  } catch (err) {
    if (token !== requestToken) return
    console.error('Lỗi khi tải học sinh của lớp:', err)
    students.value = []
    totalCount.value = 0
  } finally {
    if (token === requestToken) {
      isLoadingStudents.value = false
    }
  }
}

async function onClassChange() {
  resetPage()
  await loadStudentsByClass()
}

const currentClass = computed(() => {
  return myClasses.value.find(c => c.id === selectedClassId.value) || null
})

// ─── Modal History Action ────────────────────────────────────────────────────
async function openStudentHistory(student) {
  selectedStudent.value = student
  studentHistoryList.value = []
  showHistoryModal.value = true
  isLoadingHistory.value = true
  try {
    const history = await attendanceService.getStudentHistory(
      student.studentId,
      selectedClassId.value || null
    )
    studentHistoryList.value = Array.isArray(history) ? history : []
  } catch (err) {
    console.warn('Lỗi khi tải lịch sử điểm danh:', err)
  } finally {
    isLoadingHistory.value = false
  }
}

// ─── Export CSV ──────────────────────────────────────────────────────────────
async function exportCsv() {
  if (!selectedClassId.value) return
  let allStudents = students.value
  if (totalCount.value > students.value.length || search.value.trim()) {
    try {
      const fullData = await classService.getStudents(selectedClassId.value)
      allStudents = Array.isArray(fullData) ? fullData : (fullData?.items || students.value)
    } catch (err) {
      console.warn('Không thể tải đầy đủ danh sách để xuất CSV, xuất theo trang hiện tại:', err)
    }
  }
  if (allStudents.length === 0) return

  const className = currentClass.value?.name || 'LopHoc'
  const headers = ['STT', 'Mã HS', 'Họ và tên', 'Họ tên Phụ huynh', 'SĐT Phụ huynh', 'Email Phụ huynh', 'Trạng thái', 'Điểm rèn luyện', 'Số sao']

  const rows = allStudents.map((s, idx) => [
    idx + 1,
    `HS-${String(s.studentId).padStart(4, '0')}`,
    `"${(s.studentName || '').replace(/"/g, '""')}"`,
    `"${(s.parentName || '').replace(/"/g, '""')}"`,
    `"${s.parentPhone || ''}"`,
    `"${s.parentEmail || ''}"`,
    `"${s.status || 'ACTIVE'}"`,
    s.totalPoints || 0,
    s.stars || 0,
  ])

  const csvContent = '\uFEFF' + [headers.join(','), ...rows.map(r => r.join(','))].join('\r\n')
  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.setAttribute('href', url)
  link.setAttribute('download', `DanhSachHocSinh_${className.replace(/\s+/g, '_')}.csv`)
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
}

function formatDate(dateStr) {
  if (!dateStr) return ''
  const d = new Date(dateStr)
  return d.toLocaleDateString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  })
}

function formatStatusLabel(st) {
  switch (st) {
    case 'PRESENT': return 'Có mặt'
    case 'LATE': return 'Đi muộn'
    case 'EXCUSED': return 'Có phép'
    case 'ABSENT': return 'Vắng mặt'
    default: return st || '—'
  }
}

function getInitials(name) {
  if (!name) return 'HS'
  const parts = name.trim().split(/\s+/)
  if (parts.length >= 2) {
    return (parts[parts.length - 2][0] + parts[parts.length - 1][0]).toUpperCase()
  }
  return name.slice(0, 2).toUpperCase()
}
</script>
