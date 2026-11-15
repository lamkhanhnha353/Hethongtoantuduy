<template>
  <div class="space-y-5">
    <!-- ─── Page header ───────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-orange-900">Danh sách Lớp học</h1>
        <p class="text-sm text-gray-500 mt-0.5">Tất cả lớp học đang hoạt động trong hệ thống</p>
      </div>
      <button
        @click="openCreateModal"
        class="inline-flex items-center gap-2 px-4 py-2.5 bg-orange-500 hover:bg-orange-600
               text-white text-sm font-semibold rounded-xl shadow-sm transition-colors flex-shrink-0 cursor-pointer"
      >
        <PlusIcon class="w-4 h-4" />
        Mở lớp mới
      </button>
    </div>

    <!-- ─── Toolbar ────────────────────────────────────────────────────────── -->
    <div class="bg-white rounded-2xl border border-orange-100 shadow-sm p-4 space-y-3">
      <div class="flex flex-col sm:flex-row gap-3">
        <SearchBar v-model="search" placeholder="Tìm theo mã, tên lớp, giáo viên, khóa học..." />
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
          <span v-if="hasActiveFilters"
            class="bg-white text-orange-500 text-[10px] font-bold w-4 h-4 rounded-full flex items-center justify-center">
            {{ activeFilterCount }}
          </span>
        </button>
        <ViewToggle v-model="viewMode" />
      </div>

      <!-- Filter panel -->
      <transition name="slide-down">
        <div v-if="showFilter" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 pt-1 border-t border-orange-100">
          <!-- Filter theo Khóa học (Đồng bộ Backend) -->
          <div>
            <label class="block text-xs font-semibold text-gray-500 mb-1.5">Khóa học</label>
            <select v-model="filters.courseId" class="filter-select">
              <option value="">Tất cả khóa học</option>
              <option v-for="c in courses" :key="c.id" :value="c.id">
                {{ c.name }} ({{ c.code }})
              </option>
            </select>
          </div>
          <div>
            <label class="block text-xs font-semibold text-gray-500 mb-1.5">Trạng thái</label>
            <select v-model="filters.status" class="filter-select">
              <option value="">Tất cả</option>
              <option value="active">Đang mở</option>
              <option value="full">Đã đầy</option>
              <option value="upcoming">Sắp khai giảng</option>
              <option value="inactive">Đã kết thúc</option>
            </select>
          </div>
          <div>
            <label class="block text-xs font-semibold text-gray-500 mb-1.5">Sĩ số</label>
            <select v-model="filters.capacity" class="filter-select">
              <option value="">Tất cả</option>
              <option value="available">Còn chỗ trống</option>
              <option value="full">Đầy (&ge; 90%)</option>
              <option value="low">Ít học viên (&lt; 50%)</option>
            </select>
          </div>
          <div>
            <label class="block text-xs font-semibold text-gray-500 mb-1.5">Sắp xếp</label>
            <select v-model="filters.sort" class="filter-select">
              <option value="name_asc">Tên A → Z</option>
              <option value="name_desc">Tên Z → A</option>
              <option value="students_desc">Nhiều học viên nhất</option>
              <option value="date_asc">Khai giảng sớm nhất</option>
            </select>
          </div>
          <div class="sm:col-span-2 lg:col-span-4 flex justify-end gap-2">
            <button @click="resetFilters" class="text-xs text-gray-500 hover:text-orange-500 px-3 py-1.5 rounded-lg hover:bg-orange-50 transition-colors">
              Đặt lại
            </button>
          </div>
        </div>
      </transition>
    </div>

    <!-- ─── Results summary ────────────────────────────────────────────────── -->
    <div class="flex items-center justify-between">
      <p class="text-sm text-gray-500">
        Tìm thấy <span class="font-semibold text-orange-700">{{ totalCount }}</span> lớp học
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

    <!-- ─── Loading ────────────────────────────────────────────────────────── -->
    <LoadingSpinner v-if="loading" label="Đang tải danh sách lớp học..." />

    <!-- ─── Empty ──────────────────────────────────────────────────────────── -->
    <EmptyState
      v-else-if="!loading && classes.length === 0"
      :icon="RectangleGroupIcon"
      message="Không tìm thấy lớp học nào"
      sub="Thử thay đổi từ khóa hoặc bộ lọc"
    />

    <!-- ─── Grid view ──────────────────────────────────────────────────────── -->
    <div
      v-else-if="viewMode === 'grid'"
      class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4"
    >
      <ClassCard
        v-for="cls in classes"
        :key="cls.id"
        :cls="cls"
        @click="goToDetail"
        @edit="openEditModal"
      />
    </div>

    <!-- ─── List view ──────────────────────────────────────────────────────── -->
    <div v-else class="space-y-2">
      <ClassListItem
        v-for="cls in classes"
        :key="cls.id"
        :cls="cls"
        @click="goToDetail"
        @edit="openEditModal"
      />
    </div>

    <!-- ─── Pagination ─────────────────────────────────────────────────────── -->
    <div v-if="totalCount > 0" class="overflow-hidden rounded-2xl border border-orange-100 bg-white shadow-sm">
      <PaginationBar
        v-model:page="page"
        v-model:page-size="pageSize"
        :page-size-options="pageSizeOptions"
        :total-count="totalCount"
        :total-pages="totalPages"
        :disabled="loading"
        :bordered="false"
        item-label="lớp học"
        @change="loadClasses"
      />
    </div>

    <!-- ─── Add/Edit Class Modal ───────────────────────────────────────────── -->
    <ClassModal
      :show="showModal"
      :cls="selectedClass"
      @close="showModal = false"
      @saved="loadClasses"
      @deleted="loadClasses"
    />
  </div>
</template>

<script setup>
import { ref, computed, onBeforeUnmount, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { PlusIcon, FunnelIcon, XMarkIcon, RectangleGroupIcon } from '@heroicons/vue/24/outline'

import PaginationBar  from '@/components/common/PaginationBar.vue'
import SearchBar      from '@/components/common/SearchBar.vue'
import ViewToggle     from '@/components/common/ViewToggle.vue'
import LoadingSpinner from '@/components/common/LoadingSpinner.vue'
import EmptyState     from '@/components/common/EmptyState.vue'
import ClassCard      from '@/components/class/ClassCard.vue'
import ClassListItem  from '@/components/class/ClassListItem.vue'
import ClassModal     from '@/components/class/ClassModal.vue'
import { usePagination } from '@/composables/usePagination'

import classService  from '@/services/class.service'
import courseService from '@/services/course.service'

const router = useRouter()

const { page, pageSize, totalCount, totalPages, pageSizeOptions, applyPagedResult, resetPage } = usePagination({
  initialPageSize: 12,
  pageSizeOptions: [12, 24, 48, 96],
})

// ─── State ───────────────────────────────────────────────────────────────────
const loading       = ref(false)
const classes       = ref([])
const courses       = ref([])
const search        = ref('')
const viewMode      = ref('grid')
const showFilter    = ref(false)
const showModal     = ref(false)
const selectedClass = ref(null)

let searchTimer = null
let requestToken = 0

function openCreateModal() {
  selectedClass.value = null
  showModal.value = true
}

function openEditModal(cls) {
  selectedClass.value = cls
  showModal.value = true
}

const filters = ref({
  courseId: '',
  status: '',
  capacity: '',
  sort: 'name_asc',
})

// ─── Derived ──────────────────────────────────────────────────────────────────
const hasActiveFilters = computed(() =>
  Boolean(filters.value.courseId || filters.value.status || filters.value.capacity)
)
const activeFilterCount = computed(() =>
  [filters.value.courseId, filters.value.status, filters.value.capacity].filter(Boolean).length
)

watch(search, () => {
  if (searchTimer) clearTimeout(searchTimer)
  searchTimer = setTimeout(() => {
    resetPage()
    loadClasses()
  }, 300)
})

watch(
  filters,
  () => {
    resetPage()
    loadClasses()
  },
  { deep: true }
)

onBeforeUnmount(() => {
  if (searchTimer) clearTimeout(searchTimer)
})

// ─── Methods ──────────────────────────────────────────────────────────────────
function resetFilters() {
  filters.value = { courseId: '', status: '', capacity: '', sort: 'name_asc' }
}
function clearAll() {
  search.value = ''
  resetFilters()
}
function goToDetail(cls) {
  router.push({ name: 'class-detail', params: { id: cls.id } })
}

// ─── Load data ────────────────────────────────────────────────────────────────
async function loadClasses() {
  const token = ++requestToken
  loading.value = true
  try {
    const paged = await classService.getPaged({
      page: page.value,
      pageSize: pageSize.value,
      keyword: search.value,
      courseId: filters.value.courseId || undefined,
      status: filters.value.status || undefined,
      capacity: filters.value.capacity || undefined,
      sort: filters.value.sort || 'name_asc',
    })
    if (token !== requestToken) return

    const items = applyPagedResult(paged)
    if (items.length === 0 && page.value > 1 && totalPages.value > 0 && page.value > totalPages.value) {
      page.value = totalPages.value
      await loadClasses()
      return
    }

    classes.value = items.map(c => ({
      ...c,
      currentStudents: c.activeStudentsCount ?? c.currentStudents ?? 0,
      maxStudents: c.maxCapacity ?? c.maxStudents ?? 0,
    }))
  } catch (err) {
    if (token !== requestToken) return
    console.error('Lỗi khi tải danh sách lớp học:', err)
    classes.value = []
    totalCount.value = 0
  } finally {
    if (token === requestToken) {
      loading.value = false
    }
  }
}

async function loadCourses() {
  try {
    const data = await courseService.getAll()
    courses.value = Array.isArray(data) ? data : (data.items ?? data.data ?? [])
  } catch (err) {
    console.error('Lỗi khi tải danh sách khóa học cho bộ lọc:', err)
    courses.value = []
  }
}

onMounted(() => {
  loadClasses()
  loadCourses()
})
</script>

<style scoped>
.slide-down-enter-active, .slide-down-leave-active { transition: all 0.2s ease; overflow: hidden; }
.slide-down-enter-from, .slide-down-leave-to { opacity: 0; max-height: 0; }
.slide-down-enter-to, .slide-down-leave-from { opacity: 1; max-height: 300px; }
</style>
