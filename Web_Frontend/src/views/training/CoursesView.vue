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
        class="inline-flex items-center gap-2 px-4 py-2.5 bg-orange-400 hover:bg-orange-500
               text-white text-sm font-semibold rounded-xl shadow-sm transition-colors flex-shrink-0"
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
          placeholder="Tìm theo mã, tên khóa học..."
          class="flex-1"
        />
        <!-- Filter toggle button -->
        <button
          @click="showFilter = !showFilter"
          :class="[
            'inline-flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium border transition-all flex-shrink-0',
            showFilter || hasActiveFilters
              ? 'bg-orange-400 text-white border-orange-400'
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
              <option value="active">Đang mở</option>
              <option value="inactive">Tạm dừng</option>
              <option value="draft">Nháp</option>
              <option value="upcoming">Sắp mở</option>
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

    <!-- ─── Results summary ────────────────────────────────────────────── -->
    <div class="flex items-center justify-between">
      <p class="text-sm text-gray-500">
        Hiển thị <span class="font-semibold text-orange-700">{{ filteredCourses.length }}</span>
        / {{ courses.length }} khóa học
      </p>
      <button
        v-if="hasActiveFilters || search"
        @click="clearAll"
        class="text-xs text-orange-500 hover:text-orange-700 flex items-center gap-1"
      >
        <XMarkIcon class="w-3.5 h-3.5" />
        Xóa bộ lọc
      </button>
    </div>

    <!-- ─── Loading ────────────────────────────────────────────────────── -->
    <LoadingSpinner v-if="loading" label="Đang tải danh sách khóa học..." />

    <!-- ─── Empty ──────────────────────────────────────────────────────── -->
    <EmptyState
      v-else-if="!loading && filteredCourses.length === 0"
      :icon="BookOpenIcon"
      message="Không tìm thấy khóa học nào"
      sub="Thử thay đổi từ khóa hoặc bộ lọc"
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
      />
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { PlusIcon, FunnelIcon, XMarkIcon, BookOpenIcon } from '@heroicons/vue/24/outline'

import SearchBar    from '@/components/common/SearchBar.vue'
import ViewToggle   from '@/components/common/ViewToggle.vue'
import LoadingSpinner from '@/components/common/LoadingSpinner.vue'
import EmptyState   from '@/components/common/EmptyState.vue'
import CourseCard   from '@/components/course/CourseCard.vue'
import CourseListItem from '@/components/course/CourseListItem.vue'

import courseService from '@/services/course.service'

const router = useRouter()

// ─── State ───────────────────────────────────────────────────────────────────
const loading   = ref(false)
const courses   = ref([])
const search    = ref('')
const viewMode  = ref('grid')
const showFilter = ref(false)

const filters = ref({
  status: '',
  feeMin: null,
  feeMax: null,
  sort: 'name_asc',
})

// ─── Derived ──────────────────────────────────────────────────────────────────
const hasActiveFilters = computed(() =>
  filters.value.status || filters.value.feeMin || filters.value.feeMax
)
const activeFilterCount = computed(() =>
  [filters.value.status, filters.value.feeMin, filters.value.feeMax].filter(Boolean).length
)

const filteredCourses = computed(() => {
  let list = [...courses.value]

  // Text search — mã hoặc tên
  const q = search.value.toLowerCase().trim()
  if (q) {
    list = list.filter(c =>
      c.name?.toLowerCase().includes(q) ||
      c.code?.toLowerCase().includes(q) ||
      String(c.id).includes(q)
    )
  }

  // Status filter
  if (filters.value.status) {
    list = list.filter(c => String(c.status).toLowerCase() === filters.value.status)
  }

  // Fee range
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

// ─── Load data ────────────────────────────────────────────────────────────────
async function loadCourses() {
  loading.value = true
  try {
    const data = await courseService.getAll()
    courses.value = Array.isArray(data) ? data : (data.items ?? data.data ?? [])
  } catch (err) {
    console.error('Không tải được danh sách khóa học:', err)
    // Fallback mock data for development
    courses.value = mockCourses
  } finally {
    loading.value = false
  }
}

onMounted(loadCourses)

// ─── Mock data (used when API is unavailable) ─────────────────────────────────
const mockCourses = [
  { id: 1, code: 'KH-001', name: 'Toán Nâng Cao', description: 'Khóa học toán nâng cao cho học sinh THPT', status: 'active', fee: 1500000, totalLevels: 3, totalStudents: 124 },
  { id: 2, code: 'KH-002', name: 'Vật Lý Cơ Bản', description: 'Lý thuyết và bài tập vật lý phổ thông', status: 'active', fee: 1200000, totalLevels: 2, totalStudents: 89 },
  { id: 3, code: 'KH-003', name: 'Hóa Học Ứng Dụng', description: 'Hóa học thực nghiệm và ứng dụng thực tế', status: 'inactive', fee: 1350000, totalLevels: 2, totalStudents: 56 },
  { id: 4, code: 'KH-004', name: 'Tiếng Anh Giao Tiếp', description: 'Phát triển kỹ năng nghe nói tiếng Anh', status: 'active', fee: 1800000, totalLevels: 4, totalStudents: 210 },
  { id: 5, code: 'KH-005', name: 'Lập Trình Python', description: 'Nhập môn lập trình Python từ cơ bản đến nâng cao', status: 'upcoming', fee: 2000000, totalLevels: 3, totalStudents: 0 },
  { id: 6, code: 'KH-006', name: 'Ngữ Văn THPT', description: 'Ôn luyện văn học và kỹ năng làm văn thi THPT QG', status: 'active', fee: 1100000, totalLevels: 2, totalStudents: 78 },
  { id: 7, code: 'KH-007', name: 'Sinh Học Phân Tử', description: 'Chuyên sâu sinh học cấp độ phân tử và tế bào', status: 'draft', fee: 1600000, totalLevels: 1, totalStudents: 0 },
  { id: 8, code: 'KH-008', name: 'Lịch Sử Việt Nam', description: 'Lịch sử dân tộc từ thời tiền sử đến hiện đại', status: 'active', fee: 950000, totalLevels: 2, totalStudents: 45 },
]
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
