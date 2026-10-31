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
        <SearchBar v-model="search" placeholder="Tìm theo mã, tên lớp, giáo viên..." />
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
          <div class="flex items-end">
            <button @click="resetFilters" class="text-xs text-gray-500 hover:text-orange-500 px-3 py-2 rounded-lg hover:bg-orange-50 transition-colors">
              Đặt lại
            </button>
          </div>
        </div>
      </transition>
    </div>

    <!-- ─── Results summary ────────────────────────────────────────────────── -->
    <div class="flex items-center justify-between">
      <p class="text-sm text-gray-500">
        Hiển thị <span class="font-semibold text-orange-700">{{ filteredClasses.length }}</span>
        / {{ classes.length }} lớp học
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
      v-else-if="!loading && filteredClasses.length === 0"
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
        v-for="cls in filteredClasses"
        :key="cls.id"
        :cls="cls"
        @click="goToDetail"
        @edit="openEditModal"
      />
    </div>

    <!-- ─── List view ──────────────────────────────────────────────────────── -->
    <div v-else class="space-y-2">
      <ClassListItem
        v-for="cls in filteredClasses"
        :key="cls.id"
        :cls="cls"
        @click="goToDetail"
        @edit="openEditModal"
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
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { PlusIcon, FunnelIcon, XMarkIcon, RectangleGroupIcon } from '@heroicons/vue/24/outline'

import SearchBar      from '@/components/common/SearchBar.vue'
import ViewToggle     from '@/components/common/ViewToggle.vue'
import LoadingSpinner from '@/components/common/LoadingSpinner.vue'
import EmptyState     from '@/components/common/EmptyState.vue'
import ClassCard      from '@/components/class/ClassCard.vue'
import ClassListItem  from '@/components/class/ClassListItem.vue'
import ClassModal     from '@/components/class/ClassModal.vue'

import classService from '@/services/class.service'

const router = useRouter()

// ─── State ───────────────────────────────────────────────────────────────────
const loading       = ref(false)
const classes       = ref([])
const search        = ref('')
const viewMode      = ref('grid')
const showFilter    = ref(false)
const showModal     = ref(false)
const selectedClass = ref(null)

function openCreateModal() {
  selectedClass.value = null
  showModal.value = true
}

function openEditModal(cls) {
  selectedClass.value = cls
  showModal.value = true
}

const filters = ref({
  status: '',
  capacity: '',
  sort: 'name_asc',
})

// ─── Derived ──────────────────────────────────────────────────────────────────
const hasActiveFilters = computed(() =>
  filters.value.status || filters.value.capacity
)
const activeFilterCount = computed(() =>
  [filters.value.status, filters.value.capacity].filter(Boolean).length
)

const filteredClasses = computed(() => {
  let list = [...classes.value]

  const q = search.value.toLowerCase().trim()
  if (q) {
    list = list.filter(c =>
      c.name?.toLowerCase().includes(q) ||
      c.code?.toLowerCase().includes(q) ||
      c.teacherName?.toLowerCase().includes(q) ||
      c.room?.toLowerCase().includes(q) ||
      String(c.id).includes(q)
    )
  }

  if (filters.value.status) {
    list = list.filter(c => String(c.status || '').toLowerCase() === filters.value.status.toLowerCase())
  }

  if (filters.value.capacity === 'available') {
    list = list.filter(c => !c.maxStudents || c.currentStudents < c.maxStudents)
  } else if (filters.value.capacity === 'full') {
    list = list.filter(c => c.maxStudents && (c.currentStudents / c.maxStudents) >= 0.9)
  } else if (filters.value.capacity === 'low') {
    list = list.filter(c => !c.maxStudents || (c.currentStudents / c.maxStudents) < 0.5)
  }

  switch (filters.value.sort) {
    case 'name_desc':     list.sort((a, b) => b.name?.localeCompare(a.name, 'vi')); break
    case 'students_desc': list.sort((a, b) => (b.currentStudents ?? 0) - (a.currentStudents ?? 0)); break
    case 'date_asc':      list.sort((a, b) => new Date(a.startDate) - new Date(b.startDate)); break
    default:              list.sort((a, b) => a.name?.localeCompare(b.name, 'vi'))
  }
  return list
})

// ─── Methods ──────────────────────────────────────────────────────────────────
function resetFilters() {
  filters.value = { status: '', capacity: '', sort: 'name_asc' }
}
function clearAll() {
  search.value = ''
  resetFilters()
}
function goToDetail(cls) {
  router.push({ name: 'schedule', query: { classId: cls.id } })
}

// ─── Load data ────────────────────────────────────────────────────────────────
async function loadClasses() {
  loading.value = true
  try {
    const data = await classService.getAll()
    const raw = Array.isArray(data) ? data : (data.items ?? data.data ?? [])
    classes.value = raw.map(c => ({
      ...c,
      currentStudents: c.activeStudentsCount ?? c.currentStudents ?? 0,
      maxStudents: c.maxCapacity ?? c.maxStudents ?? 0,
    }))
  } catch (err) {
    console.error('Lỗi khi tải danh sách lớp học:', err)
    classes.value = []
  } finally {
    loading.value = false
  }
}

onMounted(loadClasses)

// ─── Mock data ────────────────────────────────────────────────────────────────
const mockClasses = [
  { id: 101, code: 'LH-101', name: 'Toán Cơ Bản - Sáng T2T4T6', status: 'active', teacherName: 'Nguyễn Văn A', room: 'P201', maxStudents: 25, currentStudents: 22, startDate: '2026-09-01' },
  { id: 102, code: 'LH-102', name: 'Toán Cơ Bản - Chiều T3T5', status: 'full', teacherName: 'Trần Thị B', room: 'P301', maxStudents: 20, currentStudents: 20, startDate: '2026-09-05' },
  { id: 103, code: 'LH-103', name: 'Vật Lý Trung Cấp - Tối T2T4', status: 'active', teacherName: 'Lê Văn C', room: 'P103', maxStudents: 20, currentStudents: 15, startDate: '2026-09-10' },
  { id: 104, code: 'LH-104', name: 'Tiếng Anh Nâng Cao - T7CN', status: 'upcoming', teacherName: 'Phạm Thị D', room: 'P401', maxStudents: 15, currentStudents: 8, startDate: '2026-11-01' },
  { id: 105, code: 'LH-105', name: 'Hóa Học Cơ Bản - Sáng T7', status: 'active', teacherName: 'Hoàng Văn E', room: 'P202', maxStudents: 18, currentStudents: 7, startDate: '2026-09-20' },
  { id: 106, code: 'LH-106', name: 'Lập Trình Python - Online', status: 'upcoming', teacherName: 'Vũ Thanh F', room: 'Online', maxStudents: 30, currentStudents: 12, startDate: '2026-11-15' },
  { id: 107, code: 'LH-107', name: 'Ngữ Văn THPT - Ôn Thi', status: 'inactive', teacherName: 'Ngô Thị G', room: 'P105', maxStudents: 22, currentStudents: 0, startDate: '2026-07-01' },
  { id: 108, code: 'LH-108', name: 'Sinh Học 12 - Nâng Cao', status: 'active', teacherName: 'Bùi Văn H', room: 'P304', maxStudents: 16, currentStudents: 14, startDate: '2026-09-15' },
]
</script>

<style scoped>
.slide-down-enter-active, .slide-down-leave-active { transition: all 0.2s ease; overflow: hidden; }
.slide-down-enter-from, .slide-down-leave-to { opacity: 0; max-height: 0; }
.slide-down-enter-to, .slide-down-leave-from { opacity: 1; max-height: 300px; }
</style>
