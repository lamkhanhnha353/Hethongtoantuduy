<template>
  <div class="space-y-6">
    <!-- ─── Back + Loading ────────────────────────────────────────────────── -->
    <LoadingSpinner v-if="loadingCourse" label="Đang tải thông tin khóa học từ backend..." />

    <template v-else-if="course">
      <!-- ─── Breadcrumb ───────────────────────────────────────────────── -->
      <nav class="flex items-center gap-2 text-sm text-stone-500">
        <router-link to="/training/courses" class="hover:text-orange-600 transition-colors flex items-center gap-1 font-medium">
          <HomeIcon class="w-4 h-4" />
          Danh mục Khóa học
        </router-link>
        <ChevronRightIcon class="w-4 h-4 text-stone-400" />
        <span class="text-stone-900 font-semibold truncate">{{ course.name }}</span>
      </nav>

      <!-- ─── Course info card ─────────────────────────────────────────── -->
      <div class="bg-white rounded-lg border border-stone-300 shadow-[0_1px_2px_rgba(0,0,0,0.04)] overflow-hidden">
        <div class="h-1.5 w-full bg-orange-600" />
        <div class="p-6">
          <div class="flex flex-col lg:flex-row lg:items-start gap-6">
            <!-- Course Image / Avatar -->
            <div class="w-full sm:w-44 h-40 rounded-md overflow-hidden bg-stone-100 flex-shrink-0 border border-stone-300">
              <img
                v-if="course.imageUrl && !imageError"
                :src="course.imageUrl"
                :alt="course.name"
                class="w-full h-full object-cover"
                @error="imageError = true"
              />
              <div v-else class="w-full h-full flex flex-col items-center justify-center bg-orange-50 text-orange-700">
                <BookOpenIcon class="w-12 h-12 text-orange-500 mb-1" />
                <span class="font-bold text-xs uppercase">{{ course.code }}</span>
              </div>
            </div>

            <div class="flex-1 min-w-0">
              <div class="flex flex-wrap items-center gap-2 mb-2">
                <span class="text-xs font-mono font-bold text-orange-800 bg-orange-50 px-2 py-0.5 rounded border border-orange-300 tabular-nums">
                  {{ course.code ?? `#${course.id}` }}
                </span>
                <StatusBadge :status="course.status" />
                <span v-if="course.minAge || course.maxAge" class="text-xs text-stone-700 bg-stone-100 px-2 py-0.5 rounded border border-stone-300 font-medium tabular-nums">
                  Độ tuổi: {{ course.minAge ?? '?' }} - {{ course.maxAge ?? '?' }} tuổi
                </span>
              </div>
              <h1 class="text-2xl sm:text-3xl font-bold text-stone-900 mb-3 leading-tight">
                {{ course.name }}
              </h1>
              <p v-if="course.description" class="text-sm text-stone-600 mb-5 leading-relaxed max-w-3xl">
                {{ course.description }}
              </p>

              <!-- Stats row -->
              <div class="grid grid-cols-2 sm:grid-cols-4 gap-4 text-sm text-stone-600 border-t border-stone-200 pt-4">
                <div class="flex items-center gap-3">
                  <div class="w-10 h-10 rounded-md border border-orange-200 bg-orange-50 flex items-center justify-center flex-shrink-0">
                    <AcademicCapIcon class="w-5 h-5 text-orange-600" />
                  </div>
                  <div>
                    <p class="font-bold text-stone-900 leading-none text-base tabular-nums">{{ levels.length }}</p>
                    <p class="text-[11px] text-stone-500 mt-1">Cấp độ đào tạo</p>
                  </div>
                </div>
                <div class="flex items-center gap-3">
                  <div class="w-10 h-10 rounded-md border border-stone-200 bg-stone-100 flex items-center justify-center flex-shrink-0">
                    <UserGroupIcon class="w-5 h-5 text-stone-700" />
                  </div>
                  <div>
                    <p class="font-bold text-stone-900 leading-none text-base tabular-nums">{{ totalClassStudents }}</p>
                    <p class="text-[11px] text-stone-500 mt-1">Học viên đang học</p>
                  </div>
                </div>
                <div v-if="feeDisplay" class="flex items-center gap-3 sm:col-span-2">
                  <div class="w-10 h-10 rounded-md border border-emerald-200 bg-emerald-50 flex items-center justify-center flex-shrink-0">
                    <CurrencyDollarIcon class="w-5 h-5 text-emerald-700" />
                  </div>
                  <div>
                    <p class="font-bold text-emerald-700 leading-none text-base tabular-nums">{{ feeDisplay }}</p>
                    <p class="text-[11px] text-stone-500 mt-1">Học phí theo cấp độ</p>
                  </div>
                </div>
              </div>
            </div>

            <!-- Action buttons -->
            <div class="flex items-center gap-2 flex-shrink-0 self-start">
              <button
                @click="openEditCourseModal"
                class="inline-flex h-9 items-center gap-1.5 px-3.5 text-xs font-semibold text-stone-800 bg-white hover:bg-stone-100 border border-stone-300 hover:border-stone-400 rounded-md transition-colors cursor-pointer"
              >
                <PencilIcon class="w-3.5 h-3.5 text-stone-600" />
                Chỉnh sửa khóa học
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- ─── Course Levels (Cấp độ khóa học) ────────────────────────── -->
      <div v-if="levels.length > 0" class="bg-white rounded-lg border border-stone-300 shadow-[0_1px_2px_rgba(0,0,0,0.04)] p-6 space-y-4">
        <div class="flex items-center justify-between">
          <div>
            <h2 class="text-lg font-bold text-stone-900 flex items-center gap-2">
              <AcademicCapIcon class="w-5 h-5 text-orange-600" />
              Lộ trình Cấp độ (Course Levels)
            </h2>
            <p class="text-xs text-stone-500 mt-0.5 tabular-nums">Khóa học gồm {{ levels.length }} cấp độ giảng dạy liên tiếp</p>
          </div>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          <div
            v-for="(lv, idx) in levels"
            :key="lv.id"
            @click="selectLevelAndScroll(lv.id)"
            :class="[
              'p-4 rounded-md border transition-all cursor-pointer flex flex-col justify-between gap-3',
              selectedLevel === lv.id
                ? 'border-orange-600 bg-orange-50/50 ring-2 ring-orange-500/20'
                : 'border-stone-300 bg-white hover:border-stone-400'
            ]"
          >
            <div>
              <div class="flex items-center justify-between gap-2 mb-2">
                <span class="text-[11px] font-mono font-bold px-2 py-0.5 rounded border border-orange-300 bg-orange-50 text-orange-800">
                  {{ lv.code ?? `L${idx + 1}` }}
                </span>
                <StatusBadge :status="lv.status" />
              </div>
              <h3 class="font-bold text-stone-900 text-sm leading-snug line-clamp-2">
                {{ lv.name }}
              </h3>
              <p v-if="lv.description" class="text-xs text-stone-500 line-clamp-2 mt-1">
                {{ lv.description }}
              </p>
            </div>

            <div class="pt-3 border-t border-stone-200 flex items-center justify-between text-xs">
              <span class="font-bold text-orange-600 text-sm tabular-nums">
                {{ formatFee(lv.fee) }}
              </span>
              <span class="text-stone-600 font-medium tabular-nums">
                {{ lv.totalSessions ?? 0 }} buổi học
              </span>
            </div>
          </div>
        </div>
      </div>

      <!-- ─── Classes section ──────────────────────────────────────────── -->
      <div id="classes-section" class="bg-white rounded-lg border border-stone-300 shadow-[0_1px_2px_rgba(0,0,0,0.04)] overflow-hidden">
        <div class="px-6 py-4 border-b border-stone-200 bg-stone-50">
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
            <div>
              <h2 class="font-bold text-stone-900 text-base">Danh sách Lớp học trực thuộc</h2>
              <p class="text-xs text-stone-500 mt-0.5 tabular-nums">
                Tìm thấy {{ totalCount }} lớp
                <template v-if="selectedLevel"> · Đang lọc theo cấp độ: <span class="font-semibold text-orange-700">{{ selectedLevelName }}</span></template>
              </p>
            </div>
            <button
              @click="openCreateClassModal"
              class="inline-flex h-9 items-center gap-1.5 px-3.5 bg-orange-600 hover:bg-orange-700 border border-orange-700 text-white text-xs font-semibold rounded-md transition-colors flex-shrink-0 shadow-xs cursor-pointer"
            >
              <PlusIcon class="w-3.5 h-3.5" />
              Mở lớp mới
            </button>
          </div>
        </div>

        <div class="p-5 space-y-4">
          <!-- Level tabs -->
          <div v-if="levels.length > 0" class="flex flex-wrap gap-2 items-center">
            <span class="text-xs text-stone-500 font-semibold uppercase tracking-wide mr-1">Lọc cấp độ:</span>
            <button
              @click="selectedLevel = null"
              :class="[
                'h-8 px-3 rounded-md text-xs font-semibold border transition-all tabular-nums',
                selectedLevel === null
                  ? 'bg-orange-600 text-white border-orange-700 shadow-xs'
                  : 'bg-white text-stone-800 border-stone-300 hover:bg-stone-100 hover:border-stone-400',
              ]"
            >
              Tất cả{{ courseTotalClasses !== null ? ` (${courseTotalClasses})` : '' }}
            </button>
            <button
              v-for="lv in levels"
              :key="lv.id"
              @click="selectedLevel = lv.id"
              :class="[
                'h-8 px-3 rounded-md text-xs font-semibold border transition-all',
                selectedLevel === lv.id
                  ? 'bg-orange-600 text-white border-orange-700 shadow-xs'
                  : 'bg-white text-stone-800 border-stone-300 hover:bg-stone-100 hover:border-stone-400',
              ]"
            >
              {{ lv.name }}
            </button>
          </div>

          <!-- Toolbar for classes -->
          <div class="flex flex-col sm:flex-row gap-3">
            <SearchBar v-model="classSearch" placeholder="Tìm theo mã lớp, tên lớp, giáo viên..." class="flex-1" />
            <!-- Class filter -->
            <button
              @click="showClassFilter = !showClassFilter"
              :class="[
                'inline-flex h-9 items-center gap-2 px-3.5 rounded-md text-sm font-semibold border transition-all flex-shrink-0',
                showClassFilter || classFilters.status
                  ? 'bg-orange-600 text-white border-orange-700'
                  : 'bg-white text-stone-800 border-stone-300 hover:bg-stone-100 hover:border-stone-400',
              ]"
            >
              <FunnelIcon class="w-4 h-4" />
              Lọc lớp
            </button>
            <ViewToggle v-model="classViewMode" />
          </div>

          <!-- Class filter panel -->
          <transition name="slide-down">
            <div v-if="showClassFilter" class="grid grid-cols-1 sm:grid-cols-3 gap-3 p-3.5 bg-stone-50 rounded-md border border-stone-300">
              <div>
                <label class="block text-xs font-semibold uppercase tracking-wide text-stone-700 mb-1.5">Trạng thái lớp</label>
                <select v-model="classFilters.status" class="filter-select">
                  <option value="">Tất cả</option>
                  <option value="active">Đang mở (ACTIVE)</option>
                  <option value="full">Đã đầy (FULL)</option>
                  <option value="upcoming">Sắp khai giảng (UPCOMING)</option>
                  <option value="inactive">Đã kết thúc (INACTIVE)</option>
                </select>
              </div>
              <div>
                <label class="block text-xs font-semibold uppercase tracking-wide text-stone-700 mb-1.5">Sắp xếp</label>
                <select v-model="classFilters.sort" class="filter-select">
                  <option value="name_asc">Tên A → Z</option>
                  <option value="students_desc">Nhiều học viên nhất</option>
                  <option value="date_asc">Khai giảng sớm nhất</option>
                </select>
              </div>
              <div class="flex items-end">
                <button @click="resetClassFilters" class="h-9 text-xs font-semibold text-stone-600 hover:text-stone-900 px-3 rounded-md border border-transparent hover:bg-white hover:border-stone-300 transition-colors">
                  Đặt lại bộ lọc
                </button>
              </div>
            </div>
          </transition>

          <!-- Loading classes -->
          <LoadingSpinner v-if="loadingClasses" label="Đang tải danh sách lớp học từ backend..." />

          <!-- Empty classes -->
          <EmptyState
            v-else-if="!loadingClasses && classes.length === 0"
            :icon="RectangleGroupIcon"
            message="Không tìm thấy lớp học nào"
            sub="Hãy mở lớp mới cho cấp độ hoặc khóa học này"
          />

          <!-- Classes Grid -->
          <div
            v-else-if="classViewMode === 'grid'"
            class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4"
          >
            <ClassCard
              v-for="cls in classes"
              :key="cls.id"
              :cls="cls"
              @click="goToClass"
              @edit="openEditClassModal"
            />
          </div>

          <!-- Classes List -->
          <div v-else class="space-y-2">
            <ClassListItem
              v-for="cls in classes"
              :key="cls.id"
              :cls="cls"
              @click="goToClass"
              @edit="openEditClassModal"
            />
          </div>

          <!-- Classes Pagination -->
          <PaginationBar
            v-model:page="page"
            v-model:page-size="pageSize"
            :page-size-options="pageSizeOptions"
            :total-count="totalCount"
            :total-pages="totalPages"
            :disabled="loadingClasses"
            :bordered="false"
            item-label="lớp học"
            class="border-t border-stone-200 px-0 pt-4"
            @change="loadClasses"
          />
        </div>
      </div>

      <!-- ─── Add/Edit Course Modal ────────────────────────────────────── -->
      <CourseModal
        :show="showCourseModal"
        :course="course"
        @close="showCourseModal = false"
        @saved="handleCourseSaved"
        @deleted="handleCourseDeleted"
      />

      <!-- ─── Add/Edit Class Modal ─────────────────────────────────────── -->
      <ClassModal
        :show="showClassModal"
        :cls="selectedClass"
        :preselected-level-id="selectedLevel"
        @close="showClassModal = false"
        @saved="loadClasses"
        @deleted="loadClasses"
      />
    </template>

    <!-- ─── Error / not found ──────────────────────────────────────────── -->
    <div v-else class="flex flex-col items-center justify-center py-20 gap-3 bg-white rounded-lg border border-stone-300 shadow-[0_1px_2px_rgba(0,0,0,0.04)]">
      <ExclamationCircleIcon class="w-12 h-12 text-rose-500" />
      <p class="text-stone-900 font-bold text-lg">Không tìm thấy khóa học</p>
      <p class="text-stone-500 text-sm">Khóa học không tồn tại hoặc máy chủ backend không phản hồi.</p>
      <router-link to="/training/courses" class="mt-2 text-sm text-orange-600 hover:text-orange-700 underline font-semibold">
        ← Quay lại danh mục khóa học
      </router-link>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onBeforeUnmount, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  HomeIcon, ChevronRightIcon, BookOpenIcon, AcademicCapIcon, UserGroupIcon,
  CurrencyDollarIcon, PencilIcon, PlusIcon, FunnelIcon, RectangleGroupIcon,
  ExclamationCircleIcon,
} from '@heroicons/vue/24/outline'

import PaginationBar  from '@/components/common/PaginationBar.vue'
import SearchBar      from '@/components/common/SearchBar.vue'
import ViewToggle     from '@/components/common/ViewToggle.vue'
import LoadingSpinner from '@/components/common/LoadingSpinner.vue'
import EmptyState     from '@/components/common/EmptyState.vue'
import StatusBadge    from '@/components/course/StatusBadge.vue'
import ClassCard      from '@/components/class/ClassCard.vue'
import ClassListItem  from '@/components/class/ClassListItem.vue'
import CourseModal    from '@/components/course/CourseModal.vue'
import ClassModal     from '@/components/class/ClassModal.vue'
import { usePagination } from '@/composables/usePagination'

import courseService      from '@/services/course.service'
import courseLevelService from '@/services/courseLevel.service'
import classService       from '@/services/class.service'

const route  = useRoute()
const router = useRouter()

const { page, pageSize, totalCount, totalPages, pageSizeOptions, applyPagedResult, resetPage } = usePagination({
  initialPageSize: 12,
  pageSizeOptions: [12, 24, 48, 96],
})

// ─── State ────────────────────────────────────────────────────────────────────
const loadingCourse      = ref(false)
const loadingClasses     = ref(false)
const imageError         = ref(false)
const course             = ref(null)
const levels             = ref([])
const classes            = ref([])
const courseTotalClasses = ref(null)

const selectedLevel   = ref(null)
const classSearch     = ref('')
const classViewMode   = ref('grid')
const showClassFilter = ref(false)
const classFilters    = ref({ status: '', sort: 'name_asc' })

let classSearchTimer = null
let classesRequestToken = 0

// Modals
const showCourseModal = ref(false)
const showClassModal  = ref(false)
const selectedClass   = ref(null)

function openEditCourseModal() {
  showCourseModal.value = true
}

function handleCourseSaved() {
  loadCourse()
}

function handleCourseDeleted() {
  router.push({ name: 'courses' })
}

function openCreateClassModal() {
  selectedClass.value = null
  showClassModal.value = true
}

function openEditClassModal(cls) {
  selectedClass.value = cls
  showClassModal.value = true
}

// ─── Computed ─────────────────────────────────────────────────────────────────
const selectedLevelName = computed(() =>
  levels.value.find(l => l.id === selectedLevel.value)?.name ?? ''
)

const totalClassStudents = computed(() =>
  classes.value.reduce((acc, c) => acc + (c.currentStudents ?? 0), 0)
)

const feeDisplay = computed(() => {
  if (levels.value.length === 0) {
    return course.value?.fee ? formatFee(course.value.fee) : null
  }
  const fees = levels.value.map(l => Number(l.fee)).filter(f => !isNaN(f) && f > 0)
  if (fees.length === 0) return null
  const min = Math.min(...fees)
  const max = Math.max(...fees)
  if (min === max) return formatFee(min)
  return `${formatFee(min)} - ${formatFee(max)}`
})

watch(classSearch, () => {
  if (classSearchTimer) clearTimeout(classSearchTimer)
  classSearchTimer = setTimeout(() => {
    resetPage()
    loadClasses()
  }, 300)
})

watch(
  [selectedLevel, () => classFilters.value.status, () => classFilters.value.sort],
  () => {
    resetPage()
    loadClasses()
  }
)

onBeforeUnmount(() => {
  if (classSearchTimer) clearTimeout(classSearchTimer)
})

// ─── Normalizer ───────────────────────────────────────────────────────────────
function normalizeCourse(raw) {
  if (!raw) return null
  const rawLevels = Array.isArray(raw.courseLevels) ? raw.courseLevels : []
  const fees = rawLevels.map(l => Number(l.fee)).filter(f => !isNaN(f) && f > 0)
  const minFee = fees.length > 0 ? Math.min(...fees) : (Number(raw.fee) || 0)
  const maxFee = fees.length > 0 ? Math.max(...fees) : minFee

  return {
    ...raw,
    courseLevels: rawLevels,
    totalLevels: raw.totalLevels ?? rawLevels.length,
    fee: minFee,
    minFee,
    maxFee,
  }
}

// ─── Methods ──────────────────────────────────────────────────────────────────
function resetClassFilters() {
  classFilters.value = { status: '', sort: 'name_asc' }
  classSearch.value = ''
}

function selectLevelAndScroll(levelId) {
  selectedLevel.value = selectedLevel.value === levelId ? null : levelId
  const el = document.getElementById('classes-section')
  if (el) {
    el.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }
}

function goToClass(cls) {
  router.push({ name: 'class-detail', params: { id: cls.id } })
}

function formatFee(fee) {
  return Number(fee || 0).toLocaleString('vi-VN') + 'đ'
}

// ─── Data Loading ─────────────────────────────────────────────────────────────
async function loadCourse() {
  loadingCourse.value = true
  const id = route.params.id
  try {
    const data = await courseService.get(id)
    course.value = normalizeCourse(data)
    if (data?.courseLevels && Array.isArray(data.courseLevels)) {
      levels.value = data.courseLevels
    }
  } catch (err) {
    console.error('Không tải được thông tin khóa học:', err)
    course.value = null
  } finally {
    loadingCourse.value = false
  }
}

async function loadLevels() {
  if (levels.value.length > 0) return
  const id = route.params.id
  try {
    const data = await courseLevelService.getByCourse(id)
    levels.value = Array.isArray(data) ? data : (data.items ?? data.data ?? [])
  } catch (err) {
    console.error('Không tải được cấp độ khóa học:', err)
    levels.value = []
  }
}

async function loadClasses() {
  const token = ++classesRequestToken
  loadingClasses.value = true
  try {
    const paged = await classService.getPaged({
      page: page.value,
      pageSize: pageSize.value,
      keyword: classSearch.value,
      courseId: route.params.id,
      courseLevelId: selectedLevel.value || undefined,
      status: classFilters.value.status || undefined,
      sort: classFilters.value.sort || 'name_asc',
    })
    if (token !== classesRequestToken) return

    const items = applyPagedResult(paged)
    if (items.length === 0 && page.value > 1 && totalPages.value > 0 && page.value > totalPages.value) {
      page.value = totalPages.value
      await loadClasses()
      return
    }

    if (selectedLevel.value === null && !classSearch.value.trim() && !classFilters.value.status) {
      courseTotalClasses.value = totalCount.value
    }

    classes.value = items.map(cls => ({
      ...cls,
      currentStudents: cls.activeStudentsCount ?? cls.currentStudents ?? 0,
      maxStudents: cls.maxCapacity ?? cls.maxStudents ?? 0,
      levelId: cls.courseLevelId ?? cls.levelId,
    }))
  } catch (err) {
    if (token !== classesRequestToken) return
    console.error('Không tải được danh sách lớp học:', err)
    classes.value = []
    totalCount.value = 0
  } finally {
    if (token === classesRequestToken) {
      loadingClasses.value = false
    }
  }
}

onMounted(async () => {
  await loadCourse()
  await loadLevels()
  await loadClasses()
})
</script>

<style scoped>
.slide-down-enter-active, .slide-down-leave-active {
  transition: all 0.2s ease;
  overflow: hidden;
}
.slide-down-enter-from, .slide-down-leave-to { opacity: 0; max-height: 0; }
.slide-down-enter-to, .slide-down-leave-from { opacity: 1; max-height: 300px; }
</style>
