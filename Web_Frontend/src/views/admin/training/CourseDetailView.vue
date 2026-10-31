<template>
  <div class="space-y-5">
    <!-- ─── Back + Loading ────────────────────────────────────────────────── -->
    <LoadingSpinner v-if="loadingCourse" label="Đang tải khóa học..." />

    <template v-else-if="course">
      <!-- ─── Breadcrumb ───────────────────────────────────────────────── -->
      <nav class="flex items-center gap-2 text-sm text-gray-400">
        <router-link to="/training/courses" class="hover:text-orange-500 transition-colors flex items-center gap-1">
          <HomeIcon class="w-4 h-4" />
          Danh mục Khóa học
        </router-link>
        <ChevronRightIcon class="w-4 h-4" />
        <span class="text-orange-700 font-medium">{{ course.name }}</span>
      </nav>

      <!-- ─── Course info card ─────────────────────────────────────────── -->
      <div class="bg-white rounded-2xl border border-orange-100 shadow-sm overflow-hidden">
        <div class="h-2 w-full bg-gradient-to-r from-orange-300 via-orange-400 to-orange-500" />
        <div class="p-6">
          <div class="flex flex-col lg:flex-row lg:items-start gap-4">
            <!-- Icon/Avatar -->
            <div class="w-16 h-16 rounded-2xl bg-orange-100 flex items-center justify-center flex-shrink-0">
              <BookOpenIcon class="w-8 h-8 text-orange-400" />
            </div>

            <div class="flex-1 min-w-0">
              <div class="flex flex-wrap items-center gap-2 mb-1">
                <span class="text-xs font-mono font-semibold text-orange-400 bg-orange-50 px-2 py-0.5 rounded-full border border-orange-200">
                  {{ course.code ?? `#${course.id}` }}
                </span>
                <StatusBadge :status="course.status" />
              </div>
              <h1 class="text-2xl font-bold text-orange-900 mb-2">{{ course.name }}</h1>
              <p v-if="course.description" class="text-sm text-gray-500 mb-4 max-w-2xl">{{ course.description }}</p>

              <!-- Stats row -->
              <div class="flex flex-wrap gap-6 text-sm text-gray-500">
                <div class="flex items-center gap-2">
                  <div class="w-8 h-8 rounded-xl bg-orange-100 flex items-center justify-center">
                    <AcademicCapIcon class="w-4 h-4 text-orange-400" />
                  </div>
                  <div>
                    <p class="font-bold text-gray-800 leading-none">{{ course.totalLevels ?? levels.length }}</p>
                    <p class="text-[11px]">Cấp độ</p>
                  </div>
                </div>
                <div class="flex items-center gap-2">
                  <div class="w-8 h-8 rounded-xl bg-blue-100 flex items-center justify-center">
                    <UserGroupIcon class="w-4 h-4 text-blue-400" />
                  </div>
                  <div>
                    <p class="font-bold text-gray-800 leading-none">{{ course.totalStudents ?? 0 }}</p>
                    <p class="text-[11px]">Học viên</p>
                  </div>
                </div>
                <div v-if="course.fee" class="flex items-center gap-2">
                  <div class="w-8 h-8 rounded-xl bg-green-100 flex items-center justify-center">
                    <CurrencyDollarIcon class="w-4 h-4 text-green-500" />
                  </div>
                  <div>
                    <p class="font-bold text-gray-800 leading-none">{{ formatFee(course.fee) }}</p>
                    <p class="text-[11px]">Học phí/tháng</p>
                  </div>
                </div>
              </div>
            </div>

            <!-- Action buttons -->
            <div class="flex items-center gap-2 flex-shrink-0">
              <button class="inline-flex items-center gap-1.5 px-3 py-2 text-xs font-semibold text-orange-600 bg-orange-100 hover:bg-orange-200 rounded-xl transition-colors">
                <PencilIcon class="w-3.5 h-3.5" />
                Chỉnh sửa
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- ─── Classes section ──────────────────────────────────────────── -->
      <div class="bg-white rounded-2xl border border-orange-100 shadow-sm overflow-hidden">
        <div class="px-6 py-4 border-b border-orange-100">
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
            <div>
              <h2 class="font-bold text-orange-900">Danh sách Lớp học</h2>
              <p class="text-xs text-gray-500 mt-0.5">
                {{ filteredClasses.length }} lớp
                <template v-if="selectedLevel"> · Cấp độ: <span class="font-medium text-orange-600">{{ selectedLevelName }}</span></template>
              </p>
            </div>
            <button class="inline-flex items-center gap-2 px-3 py-2 bg-orange-400 hover:bg-orange-500 text-white text-xs font-semibold rounded-xl transition-colors flex-shrink-0">
              <PlusIcon class="w-3.5 h-3.5" />
              Mở lớp mới
            </button>
          </div>
        </div>

        <div class="p-4 space-y-4">
          <!-- Level tabs -->
          <div v-if="levels.length > 0" class="flex flex-wrap gap-2">
            <button
              @click="selectedLevel = null"
              :class="[
                'px-3 py-1.5 rounded-xl text-xs font-semibold border transition-all',
                selectedLevel === null
                  ? 'bg-orange-400 text-white border-orange-400'
                  : 'bg-white text-orange-600 border-orange-200 hover:bg-orange-50',
              ]"
            >
              Tất cả
            </button>
            <button
              v-for="lv in levels"
              :key="lv.id"
              @click="selectedLevel = lv.id"
              :class="[
                'px-3 py-1.5 rounded-xl text-xs font-semibold border transition-all',
                selectedLevel === lv.id
                  ? 'bg-orange-400 text-white border-orange-400'
                  : 'bg-white text-orange-600 border-orange-200 hover:bg-orange-50',
              ]"
            >
              {{ lv.name }}
            </button>
          </div>

          <!-- Toolbar for classes -->
          <div class="flex flex-col sm:flex-row gap-3">
            <SearchBar v-model="classSearch" placeholder="Tìm theo mã, tên lớp học..." />
            <!-- Class filter -->
            <button
              @click="showClassFilter = !showClassFilter"
              :class="[
                'inline-flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium border transition-all flex-shrink-0',
                showClassFilter || classFilters.status
                  ? 'bg-orange-400 text-white border-orange-400'
                  : 'bg-white text-orange-600 border-orange-200 hover:bg-orange-50',
              ]"
            >
              <FunnelIcon class="w-4 h-4" />
              Lọc lớp
            </button>
            <ViewToggle v-model="classViewMode" />
          </div>

          <!-- Class filter panel -->
          <transition name="slide-down">
            <div v-if="showClassFilter" class="grid grid-cols-1 sm:grid-cols-3 gap-3 p-3 bg-orange-50 rounded-xl border border-orange-100">
              <div>
                <label class="block text-xs font-semibold text-gray-500 mb-1.5">Trạng thái lớp</label>
                <select v-model="classFilters.status" class="filter-select">
                  <option value="">Tất cả</option>
                  <option value="active">Đang mở</option>
                  <option value="full">Đã đầy</option>
                  <option value="upcoming">Sắp khai giảng</option>
                  <option value="inactive">Đã kết thúc</option>
                </select>
              </div>
              <div>
                <label class="block text-xs font-semibold text-gray-500 mb-1.5">Sắp xếp</label>
                <select v-model="classFilters.sort" class="filter-select">
                  <option value="name_asc">Tên A → Z</option>
                  <option value="students_desc">Nhiều học viên nhất</option>
                  <option value="date_asc">Khai giảng sớm nhất</option>
                </select>
              </div>
              <div class="flex items-end">
                <button @click="resetClassFilters" class="text-xs text-gray-500 hover:text-orange-500 px-3 py-2 rounded-lg hover:bg-white transition-colors border border-transparent hover:border-orange-200">
                  Đặt lại bộ lọc
                </button>
              </div>
            </div>
          </transition>

          <!-- Loading classes -->
          <LoadingSpinner v-if="loadingClasses" label="Đang tải danh sách lớp..." />

          <!-- Empty classes -->
          <EmptyState
            v-else-if="!loadingClasses && filteredClasses.length === 0"
            :icon="RectangleGroupIcon"
            message="Không có lớp học nào"
            sub="Hãy mở lớp mới cho khóa học này"
          />

          <!-- Classes Grid -->
          <div
            v-else-if="classViewMode === 'grid'"
            class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4"
          >
            <ClassCard
              v-for="cls in filteredClasses"
              :key="cls.id"
              :cls="cls"
              @click="goToClass"
            />
          </div>

          <!-- Classes List -->
          <div v-else class="space-y-2">
            <ClassListItem
              v-for="cls in filteredClasses"
              :key="cls.id"
              :cls="cls"
              @click="goToClass"
            />
          </div>
        </div>
      </div>
    </template>

    <!-- ─── Error / not found ──────────────────────────────────────────── -->
    <div v-else class="flex flex-col items-center justify-center py-20 gap-3">
      <ExclamationCircleIcon class="w-12 h-12 text-red-300" />
      <p class="text-gray-500 font-medium">Không tìm thấy khóa học</p>
      <router-link to="/training/courses" class="text-sm text-orange-500 hover:text-orange-700 underline">
        Quay lại danh sách
      </router-link>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  HomeIcon, ChevronRightIcon, BookOpenIcon, AcademicCapIcon, UserGroupIcon,
  CurrencyDollarIcon, PencilIcon, PlusIcon, FunnelIcon, RectangleGroupIcon,
  ExclamationCircleIcon,
} from '@heroicons/vue/24/outline'

import SearchBar      from '@/components/common/SearchBar.vue'
import ViewToggle     from '@/components/common/ViewToggle.vue'
import LoadingSpinner from '@/components/common/LoadingSpinner.vue'
import EmptyState     from '@/components/common/EmptyState.vue'
import StatusBadge    from '@/components/course/StatusBadge.vue'
import ClassCard      from '@/components/class/ClassCard.vue'
import ClassListItem  from '@/components/class/ClassListItem.vue'

import courseService      from '@/services/course.service'
import courseLevelService from '@/services/courseLevel.service'
import classService       from '@/services/class.service'

const route  = useRoute()
const router = useRouter()

// ─── State ────────────────────────────────────────────────────────────────────
const loadingCourse  = ref(false)
const loadingClasses = ref(false)
const course   = ref(null)
const levels   = ref([])
const classes  = ref([])

const selectedLevel   = ref(null)
const classSearch     = ref('')
const classViewMode   = ref('grid')
const showClassFilter = ref(false)
const classFilters    = ref({ status: '', sort: 'name_asc' })

// ─── Computed ─────────────────────────────────────────────────────────────────
const selectedLevelName = computed(() =>
  levels.value.find(l => l.id === selectedLevel.value)?.name ?? ''
)

const filteredClasses = computed(() => {
  let list = [...classes.value]

  // Level tab filter
  if (selectedLevel.value !== null) {
    list = list.filter(c => c.levelId === selectedLevel.value || c.courseLevelId === selectedLevel.value)
  }

  // Text search
  const q = classSearch.value.toLowerCase().trim()
  if (q) {
    list = list.filter(c =>
      c.name?.toLowerCase().includes(q) ||
      c.code?.toLowerCase().includes(q) ||
      c.teacherName?.toLowerCase().includes(q) ||
      String(c.id).includes(q)
    )
  }

  // Status filter
  if (classFilters.value.status) {
    list = list.filter(c => String(c.status).toLowerCase() === classFilters.value.status)
  }

  // Sort
  switch (classFilters.value.sort) {
    case 'students_desc': list.sort((a, b) => (b.currentStudents ?? 0) - (a.currentStudents ?? 0)); break
    case 'date_asc':      list.sort((a, b) => new Date(a.startDate) - new Date(b.startDate)); break
    default:              list.sort((a, b) => a.name?.localeCompare(b.name, 'vi'))
  }

  return list
})

// ─── Methods ──────────────────────────────────────────────────────────────────
function resetClassFilters() {
  classFilters.value = { status: '', sort: 'name_asc' }
  classSearch.value = ''
}

function goToClass(cls) {
  // Navigate to class detail (can be added later)
  console.log('Class clicked:', cls)
}

function formatFee(fee) {
  return Number(fee).toLocaleString('vi-VN') + 'đ'
}

// ─── Load data ─────────────────────────────────────────────────────────────────
async function loadCourse() {
  loadingCourse.value = true
  const id = route.params.id
  try {
    const data = await courseService.get(id)
    course.value = data
  } catch {
    // Fallback mock
    course.value = mockCourses.find(c => String(c.id) === String(id)) ?? null
  } finally {
    loadingCourse.value = false
  }
}

async function loadLevels() {
  const id = route.params.id
  try {
    const data = await courseLevelService.getByCourse(id)
    levels.value = Array.isArray(data) ? data : (data.items ?? data.data ?? [])
  } catch {
    levels.value = mockLevels
  }
}

async function loadClasses() {
  loadingClasses.value = true
  const id = route.params.id

  try {
    // Try to load classes grouped by levels
    if (levels.value.length > 0) {
      const allClasses = await Promise.all(
        levels.value.map(lv => classService.getByCourseLevel(lv.id))
      )
      classes.value = allClasses
        .flat()
        .map((cls, i) => ({ ...cls, levelId: levels.value[Math.floor(i / 10)]?.id }))
    } else {
      const data = await classService.getAll()
      classes.value = Array.isArray(data) ? data : (data.items ?? data.data ?? [])
    }
  } catch {
    classes.value = mockClasses
  } finally {
    loadingClasses.value = false
  }
}

onMounted(async () => {
  await loadCourse()
  await loadLevels()
  await loadClasses()
})

// ─── Mock data ────────────────────────────────────────────────────────────────
const mockCourses = [
  { id: 1, code: 'KH-001', name: 'Toán Nâng Cao', description: 'Khóa học toán nâng cao cho học sinh THPT', status: 'active', fee: 1500000, totalLevels: 3, totalStudents: 124 },
  { id: 2, code: 'KH-002', name: 'Vật Lý Cơ Bản', description: 'Lý thuyết và bài tập vật lý phổ thông', status: 'active', fee: 1200000, totalLevels: 2, totalStudents: 89 },
  { id: 3, code: 'KH-003', name: 'Hóa Học Ứng Dụng', description: 'Hóa học thực nghiệm và ứng dụng thực tế', status: 'inactive', fee: 1350000, totalLevels: 2, totalStudents: 56 },
  { id: 4, code: 'KH-004', name: 'Tiếng Anh Giao Tiếp', description: 'Phát triển kỹ năng nghe nói tiếng Anh', status: 'active', fee: 1800000, totalLevels: 4, totalStudents: 210 },
  { id: 5, code: 'KH-005', name: 'Lập Trình Python', description: 'Nhập môn lập trình Python từ cơ bản đến nâng cao', status: 'upcoming', fee: 2000000, totalLevels: 3, totalStudents: 0 },
]

const mockLevels = [
  { id: 1, name: 'Cơ bản', order: 1 },
  { id: 2, name: 'Trung cấp', order: 2 },
  { id: 3, name: 'Nâng cao', order: 3 },
]

const mockClasses = [
  { id: 101, code: 'LH-101', name: 'Toán Cơ Bản - Sáng T2T4T6', status: 'active', teacherName: 'Nguyễn Văn A', room: 'P201', maxStudents: 25, currentStudents: 22, startDate: '2026-09-01', levelId: 1, courseLevelId: 1 },
  { id: 102, code: 'LH-102', name: 'Toán Cơ Bản - Chiều T3T5', status: 'full', teacherName: 'Trần Thị B', room: 'P301', maxStudents: 20, currentStudents: 20, startDate: '2026-09-05', levelId: 1, courseLevelId: 1 },
  { id: 103, code: 'LH-103', name: 'Toán Trung Cấp - Tối T2T4', status: 'active', teacherName: 'Lê Văn C', room: 'P103', maxStudents: 20, currentStudents: 15, startDate: '2026-09-10', levelId: 2, courseLevelId: 2 },
  { id: 104, code: 'LH-104', name: 'Toán Nâng Cao - T7CN', status: 'upcoming', teacherName: 'Phạm Thị D', room: 'P401', maxStudents: 15, currentStudents: 8, startDate: '2026-11-01', levelId: 3, courseLevelId: 3 },
  { id: 105, code: 'LH-105', name: 'Toán Trung Cấp - Sáng T7', status: 'active', teacherName: 'Hoàng Văn E', room: 'P202', maxStudents: 18, currentStudents: 12, startDate: '2026-09-20', levelId: 2, courseLevelId: 2 },
]
</script>

<style scoped>
.slide-down-enter-active, .slide-down-leave-active {
  transition: all 0.2s ease;
  overflow: hidden;
}
.slide-down-enter-from, .slide-down-leave-to { opacity: 0; max-height: 0; }
.slide-down-enter-to, .slide-down-leave-from { opacity: 1; max-height: 300px; }
</style>
