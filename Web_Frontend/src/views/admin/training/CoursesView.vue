<template>
  <div class="space-y-5">
    <!-- ─── Page header ───────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-orange-900">Danh mục Khóa học</h1>
        <p class="text-sm text-gray-500 mt-0.5">
          Quản lý tất cả các khóa học trong hệ thống
        </p>
      </div>
      <button
        @click="openCreateModal"
        class="inline-flex items-center gap-2 px-4 py-2.5 bg-orange-500 hover:bg-orange-600
               text-white text-sm font-semibold rounded-xl shadow-sm transition-colors flex-shrink-0 cursor-pointer"
      >
        <PlusIcon class="w-4 h-4" />
        Thêm khóa học
      </button>
    </div>

    <!-- ─── Toolbar: Search + Filter + View toggle ─────────────────────── -->
    <div class="bg-white rounded-2xl border border-orange-100 shadow-sm p-4 space-y-3">
      <div class="flex flex-col sm:flex-row gap-3">
        <!-- Search -->
        <SearchBar
          v-model="search"
          placeholder="Tìm theo mã, tên khóa học, mô tả..."
          class="flex-1"
        />
        <!-- Filter toggle button -->
        <button
          @click="showFilter = !showFilter"
          :class="[
            'inline-flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium border transition-all flex-shrink-0',
            showFilter || hasActiveFilters
              ? 'bg-orange-500 text-white border-orange-500'
              : 'bg-white text-orange-600 border-orange-200 hover:bg-orange-50',
          ]"
        >
          <FunnelIcon class="w-4 h-4" />
          Bộ lọc
          <span
            v-if="hasActiveFilters"
            class="bg-white text-orange-500 text-[10px] font-bold w-4 h-4 rounded-full flex items-center justify-center"
          >
            {{ activeFilterCount }}
          </span>
        </button>
        <!-- View toggle -->
        <ViewToggle v-model="viewMode" />
      </div>

      <!-- Filter panel (collapsible) -->
      <transition name="slide-down">
        <div v-if="showFilter" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 pt-1 border-t border-orange-100">
          <!-- Status -->
          <div>
            <label class="block text-xs font-semibold text-gray-500 mb-1.5">Trạng thái</label>
            <select v-model="filters.status" class="filter-select">
              <option value="">Tất cả</option>
              <option value="active">Đang mở (ACTIVE)</option>
              <option value="inactive">Tạm dừng (INACTIVE)</option>
              <option value="draft">Nháp (DRAFT)</option>
              <option value="upcoming">Sắp mở (UPCOMING)</option>
            </select>
          </div>
          <!-- Fee range -->
          <div>
            <label class="block text-xs font-semibold text-gray-500 mb-1.5">Học phí (từ)</label>
            <input v-model.number="filters.feeMin" type="number" min="0" placeholder="0"
              class="filter-select" />
          </div>
          <div>
            <label class="block text-xs font-semibold text-gray-500 mb-1.5">Học phí (đến)</label>
            <input v-model.number="filters.feeMax" type="number" min="0" placeholder="Không giới hạn"
              class="filter-select" />
          </div>
          <!-- Sort -->
          <div>
            <label class="block text-xs font-semibold text-gray-500 mb-1.5">Sắp xếp</label>
            <select v-model="filters.sort" class="filter-select">
              <option value="name_asc">Tên A → Z</option>
              <option value="name_desc">Tên Z → A</option>
              <option value="fee_asc">Học phí tăng dần</option>
              <option value="fee_desc">Học phí giảm dần</option>
              <option value="students_desc">Nhiều học viên nhất</option>
            </select>
          </div>
          <!-- Actions -->
          <div class="sm:col-span-2 lg:col-span-4 flex justify-end gap-2">
            <button @click="resetFilters" class="text-xs text-gray-500 hover:text-orange-500 px-3 py-1.5 rounded-lg hover:bg-orange-50 transition-colors">
              Đặt lại
            </button>
          </div>
        </div>
      </transition>
    </div>

    <!-- ─── Error state ────────────────────────────────────────────────── -->
    <div
      v-if="errorMessage"
      class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl flex items-center justify-between gap-3 text-sm"
    >
      <div class="flex items-center gap-2">
        <ExclamationCircleIcon class="w-5 h-5 flex-shrink-0 text-red-500" />
        <span>{{ errorMessage }}</span>
      </div>
      <button
        @click="loadCourses"
        class="px-3 py-1 bg-red-600 hover:bg-red-700 text-white rounded-lg text-xs font-medium transition-colors"
      >
        Thử lại
      </button>
    </div>

    <!-- ─── Results summary ────────────────────────────────────────────── -->
    <div v-if="!loading && !errorMessage" class="flex items-center justify-between">
      <p class="text-sm text-gray-500">
        Hiển thị <span class="font-semibold text-orange-700">{{ filteredCourses.length }}</span>
        / {{ courses.length }} khóa học
      </p>
      <button
        v-if="hasActiveFilters || search"
        @click="clearAll"
        class="text-xs text-orange-500 hover:text-orange-700 flex items-center gap-1 font-medium"
      >
        <XMarkIcon class="w-3.5 h-3.5" />
        Xóa bộ lọc
      </button>
    </div>

    <!-- ─── Loading ────────────────────────────────────────────────────── -->
    <LoadingSpinner v-if="loading" label="Đang tải danh sách khóa học từ backend..." />

    <!-- ─── Empty ──────────────────────────────────────────────────────── -->
    <EmptyState
      v-else-if="!loading && filteredCourses.length === 0"
      :icon="BookOpenIcon"
      message="Không tìm thấy khóa học nào"
      sub="Thử thay đổi từ khóa hoặc bộ lọc tìm kiếm"
    />

    <!-- ─── Grid view ──────────────────────────────────────────────────── -->
    <div
      v-else-if="viewMode === 'grid'"
      class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4"
    >
      <CourseCard
        v-for="(course, i) in filteredCourses"
        :key="course.id"
        :course="course"
        :index="i"
        @click="goToDetail"
        @edit="openEditModal"
      />
    </div>

    <!-- ─── List view ──────────────────────────────────────────────────── -->
    <div v-else class="space-y-2">
      <CourseListItem
        v-for="(course, i) in filteredCourses"
        :key="course.id"
        :course="course"
        :index="i"
        @click="goToDetail"
        @edit="openEditModal"
      />
    </div>

    <!-- ─── Add/Edit Course Modal ──────────────────────────────────────── -->
    <CourseModal
      :show="showModal"
      :course="selectedCourse"
      @close="showModal = false"
      @saved="loadCourses"
      @deleted="loadCourses"
    />
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import {
  PlusIcon,
  FunnelIcon,
  XMarkIcon,
  BookOpenIcon,
  ExclamationCircleIcon,
} from '@heroicons/vue/24/outline'

import SearchBar      from '@/components/common/SearchBar.vue'
import ViewToggle     from '@/components/common/ViewToggle.vue'
import LoadingSpinner from '@/components/common/LoadingSpinner.vue'
import EmptyState     from '@/components/common/EmptyState.vue'
import CourseCard     from '@/components/course/CourseCard.vue'
import CourseListItem from '@/components/course/CourseListItem.vue'
import CourseModal    from '@/components/course/CourseModal.vue'

import courseService from '@/services/course.service'

const router = useRouter()

// ─── State ───────────────────────────────────────────────────────────────────
const loading        = ref(false)
const courses        = ref([])
const errorMessage   = ref('')
const search         = ref('')
const viewMode       = ref('grid')
const showFilter     = ref(false)
const showModal      = ref(false)
const selectedCourse = ref(null)

function openCreateModal() {
  selectedCourse.value = null
  showModal.value = true
}

function openEditModal(course) {
  selectedCourse.value = course
  showModal.value = true
}

const filters = ref({
  status: '',
  feeMin: null,
  feeMax: null,
  sort: 'name_asc',
})

// ─── Course normalizer ────────────────────────────────────────────────────────
function normalizeCourse(item) {
  if (!item) return null
  const levels = Array.isArray(item.courseLevels) ? item.courseLevels : []
  const fees = levels
    .map(l => Number(l.fee))
    .filter(f => !isNaN(f) && f > 0)
  
  const minFee = fees.length > 0 ? Math.min(...fees) : (Number(item.fee) || 0)
  const maxFee = fees.length > 0 ? Math.max(...fees) : minFee

  return {
    ...item,
    courseLevels: levels,
    totalLevels: item.totalLevels ?? levels.length,
    fee: minFee,
    minFee,
    maxFee,
    totalStudents: item.totalStudents ?? 0,
  }
}

// ─── Derived ──────────────────────────────────────────────────────────────────
const hasActiveFilters = computed(() =>
  Boolean(filters.value.status || filters.value.feeMin != null || filters.value.feeMax != null)
)
const activeFilterCount = computed(() =>
  [filters.value.status, filters.value.feeMin, filters.value.feeMax].filter(v => v !== '' && v != null).length
)

const filteredCourses = computed(() => {
  let list = [...courses.value]

  // Text search — mã, tên khóa học, mô tả
  const q = search.value.toLowerCase().trim()
  if (q) {
    list = list.filter(c =>
      c.name?.toLowerCase().includes(q) ||
      c.code?.toLowerCase().includes(q) ||
      c.description?.toLowerCase().includes(q) ||
      String(c.id).includes(q)
    )
  }

  // Status filter (case-insensitive)
  if (filters.value.status) {
    const targetStatus = filters.value.status.toLowerCase()
    list = list.filter(c => String(c.status || '').toLowerCase() === targetStatus)
  }

  // Fee range (so sánh theo fee/minFee)
  if (filters.value.feeMin != null && filters.value.feeMin !== '') {
    list = list.filter(c => (c.fee ?? 0) >= filters.value.feeMin)
  }
  if (filters.value.feeMax != null && filters.value.feeMax !== '') {
    list = list.filter(c => (c.fee ?? 0) <= filters.value.feeMax)
  }

  // Sort
  switch (filters.value.sort) {
    case 'name_desc':     list.sort((a, b) => b.name?.localeCompare(a.name, 'vi')); break
    case 'fee_asc':       list.sort((a, b) => (a.fee ?? 0) - (b.fee ?? 0)); break
    case 'fee_desc':      list.sort((a, b) => (b.fee ?? 0) - (a.fee ?? 0)); break
    case 'students_desc': list.sort((a, b) => (b.totalStudents ?? 0) - (a.totalStudents ?? 0)); break
    default:              list.sort((a, b) => a.name?.localeCompare(b.name, 'vi'))
  }

  return list
})

// ─── Methods ──────────────────────────────────────────────────────────────────
function resetFilters() {
  filters.value = { status: '', feeMin: null, feeMax: null, sort: 'name_asc' }
}
function clearAll() {
  search.value = ''
  resetFilters()
}

function goToDetail(course) {
  router.push({ name: 'course-detail', params: { id: course.id } })
}

// ─── Load real data from backend ─────────────────────────────────────────────
async function loadCourses() {
  loading.value = true
  errorMessage.value = ''
  try {
    const data = await courseService.getAll()
    const rawList = Array.isArray(data) ? data : (data.items ?? data.data ?? [])
    courses.value = rawList.map(normalizeCourse)
  } catch (err) {
    console.error('Lỗi khi tải danh sách khóa học từ backend:', err)
    errorMessage.value = 'Không thể kết nối đến máy chủ backend (cổng localhost 5114). Vui lòng kiểm tra dịch vụ backend.'
    courses.value = []
  } finally {
    loading.value = false
  }
}

onMounted(loadCourses)
</script>

<style scoped>
.slide-down-enter-active,
.slide-down-leave-active {
  transition: all 0.2s ease;
  overflow: hidden;
}
.slide-down-enter-from,
.slide-down-leave-to {
  opacity: 0;
  max-height: 0;
  padding-top: 0;
}
.slide-down-enter-to,
.slide-down-leave-from {
  opacity: 1;
  max-height: 400px;
}
</style>
