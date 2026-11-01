<template>
  <div class="space-y-5">
    <!-- ─── Header ───────────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-orange-950">Danh sách lớp học đang phụ trách</h1>
        <p class="text-xs text-gray-500 mt-0.5">
          Quản lý tiến độ giảng dạy, sĩ số và các hoạt động của từng lớp được phân công
        </p>
      </div>
      <div class="flex items-center gap-2">
        <router-link
          to="/teacher/students"
          class="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-orange-100 hover:bg-orange-200 text-orange-900 text-xs font-semibold transition-colors cursor-pointer"
        >
          <UserGroupIcon class="w-4 h-4 text-orange-600" />
          Xem tất cả học sinh
        </router-link>
      </div>
    </div>

    <!-- ─── Filter & Search Bar ─────────────────────────────────────────── -->
    <div class="bg-white rounded-2xl border border-orange-100 p-4 shadow-sm space-y-3">
      <div class="flex flex-col sm:flex-row gap-3">
        <SearchBar
          v-model="search"
          placeholder="Tìm theo tên lớp, mã lớp hoặc phòng học..."
          class="flex-1"
        />

        <div class="flex items-center gap-2">
          <!-- Status filter -->
          <select
            v-model="statusFilter"
            class="text-xs font-semibold bg-white border border-orange-200 text-gray-700 rounded-xl px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
          >
            <option value="all">Tất cả trạng thái</option>
            <option value="active">Đang giảng dạy</option>
            <option value="upcoming">Sắp mở</option>
            <option value="completed">Đã kết thúc</option>
          </select>

          <!-- View toggle -->
          <ViewToggle v-model="viewMode" />
        </div>
      </div>
    </div>

    <!-- ─── Summary ──────────────────────────────────────────────────────── -->
    <div class="flex items-center justify-between text-xs text-gray-500">
      <span>
        Hiển thị <strong class="text-orange-900">{{ filteredClasses.length }}</strong> lớp học
      </span>
      <span v-if="search || statusFilter !== 'all'">
        Đang áp dụng bộ lọc tìm kiếm
      </span>
    </div>

    <!-- ─── Empty State ──────────────────────────────────────────────────── -->
    <div
      v-if="filteredClasses.length === 0"
      class="bg-white rounded-2xl border border-orange-100 p-12 text-center"
    >
      <EmptyState
        :icon="BookOpenIcon"
        message="Không tìm thấy lớp học nào"
        sub="Thử thay đổi từ khóa tìm kiếm hoặc trạng thái lọc."
      />
    </div>

    <!-- ─── Grid View ────────────────────────────────────────────────────── -->
    <div
      v-else-if="viewMode === 'grid'"
      class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4"
    >
      <div
        v-for="cls in filteredClasses"
        :key="cls.id"
        class="bg-white rounded-2xl border border-orange-100 p-5 shadow-sm hover:shadow-md hover:border-orange-300 transition-all flex flex-col justify-between"
      >
        <div>
          <div class="flex items-start justify-between gap-2 mb-2">
            <span class="text-[11px] font-mono font-bold text-orange-600 bg-orange-50 border border-orange-200 px-2 py-0.5 rounded-md">
              {{ cls.code }}
            </span>
            <span
              :class="[
                'text-[10px] font-bold px-2 py-0.5 rounded-full',
                cls.status === 'active' ? 'bg-emerald-100 text-emerald-800' :
                cls.status === 'upcoming' ? 'bg-blue-100 text-blue-800' :
                'bg-gray-100 text-gray-600',
              ]"
            >
              {{ cls.statusLabel }}
            </span>
          </div>

          <h3 class="text-base font-bold text-gray-900 line-clamp-1 hover:text-orange-600 transition-colors">
            {{ cls.name }}
          </h3>
          <p class="text-xs text-gray-500 mt-0.5">{{ cls.courseName }}</p>

          <div class="mt-4 space-y-2 text-xs text-gray-600 border-t border-orange-50 pt-3">
            <div class="flex items-center justify-between">
              <span class="text-gray-400">Lịch học:</span>
              <span class="font-medium text-gray-800">{{ cls.schedule }}</span>
            </div>
            <div class="flex items-center justify-between">
              <span class="text-gray-400">Phòng học:</span>
              <span class="font-medium text-orange-700 bg-orange-50 px-2 py-0.5 rounded">{{ cls.room }}</span>
            </div>
            <div class="flex items-center justify-between">
              <span class="text-gray-400">Sĩ số:</span>
              <span class="font-bold text-gray-900">{{ cls.currentStudents }} / {{ cls.maxStudents }} học sinh</span>
            </div>
          </div>

          <!-- Progress Bar -->
          <div class="mt-3">
            <div class="flex items-center justify-between text-[11px] text-gray-500 mb-1">
              <span>Tiến độ chương trình</span>
              <span class="font-bold text-orange-700">{{ cls.completedLessons }} / {{ cls.totalLessons }} buổi</span>
            </div>
            <div class="w-full bg-orange-100 rounded-full h-1.5 overflow-hidden">
              <div
                class="bg-gradient-to-r from-orange-400 to-amber-500 h-1.5 rounded-full transition-all duration-300"
                :style="{ width: `${(cls.completedLessons / cls.totalLessons) * 100}%` }"
              ></div>
            </div>
          </div>
        </div>

        <div class="mt-5 pt-3 border-t border-orange-100 flex items-center justify-between gap-2">
          <router-link
            to="/teacher/students"
            class="text-xs font-semibold text-orange-700 hover:text-orange-900 px-2.5 py-1.5 rounded-lg hover:bg-orange-50 transition-colors"
          >
            Học sinh ({{ cls.currentStudents }})
          </router-link>
          <div class="flex items-center gap-1.5">
            <router-link
              to="/teacher/attendance"
              class="px-3 py-1.5 rounded-xl bg-orange-400 hover:bg-orange-500 text-white text-xs font-semibold shadow-2xs transition-colors"
            >
              Điểm danh
            </router-link>
          </div>
        </div>
      </div>
    </div>

    <!-- ─── List View ────────────────────────────────────────────────────── -->
    <div v-else class="space-y-2">
      <div
        v-for="cls in filteredClasses"
        :key="cls.id"
        class="bg-white rounded-2xl border border-orange-100 p-4 shadow-sm hover:border-orange-300 transition-colors flex flex-col md:flex-row md:items-center justify-between gap-4"
      >
        <div class="flex items-start gap-3">
          <div class="w-11 h-11 rounded-xl bg-orange-100 text-orange-900 flex items-center justify-center font-mono font-bold text-xs flex-shrink-0">
            {{ cls.code }}
          </div>
          <div>
            <div class="flex items-center gap-2">
              <h3 class="text-sm font-bold text-gray-900">{{ cls.name }}</h3>
              <span
                :class="[
                  'text-[10px] font-bold px-2 py-0.5 rounded-full',
                  cls.status === 'active' ? 'bg-emerald-100 text-emerald-800' : 'bg-gray-100 text-gray-600',
                ]"
              >
                {{ cls.statusLabel }}
              </span>
            </div>
            <p class="text-xs text-gray-500 mt-0.5">
              {{ cls.courseName }} · Phòng: <span class="text-orange-800 font-semibold">{{ cls.room }}</span> · Lịch: {{ cls.schedule }}
            </p>
          </div>
        </div>

        <div class="flex items-center justify-between md:justify-end gap-4 border-t md:border-t-0 pt-2 md:pt-0 border-orange-50">
          <div class="text-right text-xs">
            <p class="font-bold text-gray-900">{{ cls.currentStudents }} / {{ cls.maxStudents }} học sinh</p>
            <p class="text-[11px] text-gray-400">{{ cls.completedLessons }}/{{ cls.totalLessons }} buổi học</p>
          </div>

          <div class="flex items-center gap-2">
            <router-link
              to="/teacher/students"
              class="px-3 py-1.5 rounded-xl border border-orange-200 text-orange-800 hover:bg-orange-50 text-xs font-semibold"
            >
              Học sinh
            </router-link>
            <router-link
              to="/teacher/attendance"
              class="px-3.5 py-1.5 rounded-xl bg-orange-400 hover:bg-orange-500 text-white text-xs font-semibold shadow-xs"
            >
              Điểm danh
            </router-link>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import {
  BookOpenIcon,
  UserGroupIcon,
} from '@heroicons/vue/24/outline'
import SearchBar from '@/components/common/SearchBar.vue'
import ViewToggle from '@/components/common/ViewToggle.vue'
import EmptyState from '@/components/common/EmptyState.vue'

const search = ref('')
const statusFilter = ref('all')
const viewMode = ref('grid')

const classesList = ref([
  {
    id: 1,
    code: 'LH-TD03A',
    name: 'Toán Tư Duy Tiểu Học - Khối 3A',
    courseName: 'Khóa Học Toán Tư Duy Khối Tiểu Học',
    room: 'Phòng P201',
    schedule: 'Thứ 2 & Thứ 5 (08:00 - 09:30)',
    currentStudents: 18,
    maxStudents: 20,
    completedLessons: 12,
    totalLessons: 24,
    status: 'active',
    statusLabel: 'Đang giảng dạy',
  },
  {
    id: 2,
    code: 'LH-HH04B',
    name: 'Hình Học Trực Quan & Sáng Tạo 4B',
    courseName: 'Hình Học Không Gian Sơ Cấp',
    room: 'Phòng P305',
    schedule: 'Thứ 2 & Thứ 6 (14:00 - 15:30)',
    currentStudents: 16,
    maxStudents: 18,
    completedLessons: 8,
    totalLessons: 20,
    status: 'active',
    statusLabel: 'Đang giảng dạy',
  },
  {
    id: 3,
    code: 'LH-NC05A',
    name: 'Toán Nâng Cao & Tư Duy Logic 5A',
    courseName: 'Toán Nâng Cao & Luyện Thi',
    room: 'Phòng P102',
    schedule: 'Thứ 4 (09:45 - 11:15)',
    currentStudents: 15,
    maxStudents: 15,
    completedLessons: 16,
    totalLessons: 24,
    status: 'active',
    statusLabel: 'Đang giảng dạy',
  },
  {
    id: 4,
    code: 'LH-HSG01',
    name: 'Bồi Dưỡng Học Sinh Giỏi Toán',
    courseName: 'Chuyên Đề Học Sinh Giỏi',
    room: 'Phòng P401',
    schedule: 'Thứ 7 (08:30 - 11:00)',
    currentStudents: 12,
    maxStudents: 15,
    completedLessons: 4,
    totalLessons: 16,
    status: 'active',
    statusLabel: 'Đang giảng dạy',
  },
])

const filteredClasses = computed(() => {
  return classesList.value.filter((cls) => {
    const matchSearch =
      !search.value.trim() ||
      cls.name.toLowerCase().includes(search.value.toLowerCase()) ||
      cls.code.toLowerCase().includes(search.value.toLowerCase()) ||
      cls.room.toLowerCase().includes(search.value.toLowerCase())
    const matchStatus = statusFilter.value === 'all' || cls.status === statusFilter.value
    return matchSearch && matchStatus
  })
})
</script>
