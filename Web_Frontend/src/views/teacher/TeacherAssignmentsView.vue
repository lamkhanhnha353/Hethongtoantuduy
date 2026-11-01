<template>
  <div class="space-y-5">
    <!-- ─── Header ───────────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-orange-950">Giao bài tập về nhà</h1>
        <p class="text-xs text-gray-500 mt-0.5">
          Tạo và phân công bài tập về nhà cho cả lớp hoặc từng học viên cụ thể
        </p>
      </div>
      <div class="flex items-center gap-2">
        <button
          type="button"
          class="inline-flex items-center gap-1.5 px-4 py-2.5 rounded-xl bg-orange-400 hover:bg-orange-500 text-white text-xs font-semibold shadow-xs transition-colors cursor-pointer"
          @click="showCreateModal = true"
        >
          <PlusIcon class="w-4 h-4" />
          Tạo bài tập mới
        </button>
      </div>
    </div>

    <!-- ─── Filter Toolbar ───────────────────────────────────────────────── -->
    <div class="bg-white rounded-2xl border border-orange-100 p-4 shadow-sm space-y-3">
      <div class="grid grid-cols-1 md:grid-cols-3 gap-3">
        <!-- Class filter -->
        <div>
          <label class="block text-xs font-semibold text-gray-600 mb-1">Lớp học</label>
          <select
            v-model="classFilter"
            class="w-full text-xs font-semibold bg-white border border-orange-200 text-gray-800 rounded-xl px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
          >
            <option value="all">Tất cả lớp học</option>
            <option value="1">Toán Tư Duy - Khối 3A</option>
            <option value="2">Hình Học Trực Quan 4B</option>
            <option value="3">Toán Nâng Cao 5A</option>
          </select>
        </div>

        <!-- Status filter -->
        <div>
          <label class="block text-xs font-semibold text-gray-600 mb-1">Trạng thái hạn nộp</label>
          <select
            v-model="statusFilter"
            class="w-full text-xs font-semibold bg-white border border-orange-200 text-gray-800 rounded-xl px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
          >
            <option value="all">Tất cả trạng thái</option>
            <option value="open">Đang mở (Còn hạn)</option>
            <option value="expired">Đã hết hạn</option>
            <option value="graded">Đã chấm xong</option>
          </select>
        </div>

        <!-- Search -->
        <div>
          <label class="block text-xs font-semibold text-gray-600 mb-1">Tìm kiếm bài tập</label>
          <SearchBar v-model="search" placeholder="Tìm theo tên bài tập..." />
        </div>
      </div>
    </div>

    <!-- ─── Assignment Cards ─────────────────────────────────────────────── -->
    <div v-if="filteredAssignments.length === 0" class="bg-white rounded-2xl border border-orange-100 p-12 text-center">
      <EmptyState
        :icon="ClipboardDocumentListIcon"
        message="Chưa có bài tập nào"
        sub="Nhấn 'Tạo bài tập mới' để giao bài tập rèn luyện cho học sinh."
      />
    </div>

    <div v-else class="space-y-3">
      <div
        v-for="hw in filteredAssignments"
        :key="hw.id"
        class="bg-white rounded-2xl border border-orange-100 p-5 shadow-sm hover:border-orange-300 transition-all flex flex-col md:flex-row md:items-center justify-between gap-4"
      >
        <div class="flex items-start gap-3.5">
          <div class="w-12 h-12 rounded-xl bg-orange-100 text-orange-900 font-bold text-xs flex flex-col items-center justify-center flex-shrink-0">
            <span>BÀI</span>
            <span class="text-sm font-extrabold">{{ hw.order }}</span>
          </div>

          <div>
            <div class="flex flex-wrap items-center gap-2">
              <span class="text-sm font-bold text-gray-900 hover:text-orange-600 cursor-pointer">
                {{ hw.title }}
              </span>
              <span class="text-[10px] font-bold px-2 py-0.5 rounded-full bg-orange-100 text-orange-800">
                {{ hw.className }}
              </span>
              <span
                :class="[
                  'text-[10px] font-bold px-2 py-0.5 rounded-full',
                  hw.status === 'open' ? 'bg-emerald-100 text-emerald-800' :
                  hw.status === 'expired' ? 'bg-amber-100 text-amber-800' :
                  'bg-blue-100 text-blue-800'
                ]"
              >
                {{ hw.statusLabel }}
              </span>
            </div>

            <p class="text-xs text-gray-500 mt-1 line-clamp-1">
              {{ hw.description }}
            </p>

            <div class="flex flex-wrap items-center gap-4 text-[11px] text-gray-400 mt-2">
              <span>📅 Hạn nộp: <strong class="text-gray-700">{{ hw.deadline }}</strong></span>
              <span>🎯 Mục tiêu: {{ hw.targetType === 'all' ? 'Toàn bộ lớp' : 'Học sinh cá nhân' }}</span>
              <span>⭐ Điểm tối đa: {{ hw.maxPoints }}đ</span>
            </div>
          </div>
        </div>

        <div class="flex flex-col sm:flex-row sm:items-center gap-4 self-end md:self-center border-t md:border-t-0 pt-3 md:pt-0 border-orange-50 w-full md:w-auto justify-between md:justify-end">
          <!-- Submission Rate -->
          <div class="text-left md:text-right min-w-[120px]">
            <div class="flex items-center justify-between md:justify-end gap-2 text-xs font-semibold text-gray-700">
              <span>Đã nộp:</span>
              <span class="text-orange-700 font-bold">{{ hw.submittedCount }} / {{ hw.totalCount }}</span>
            </div>
            <div class="w-full bg-orange-100 rounded-full h-1.5 mt-1 overflow-hidden">
              <div
                class="bg-orange-500 h-1.5 rounded-full"
                :style="{ width: `${(hw.submittedCount / hw.totalCount) * 100}%` }"
              ></div>
            </div>
          </div>

          <!-- Actions -->
          <div class="flex items-center gap-2">
            <button
              type="button"
              class="px-2.5 py-1.5 rounded-xl border border-orange-200 text-orange-800 hover:bg-orange-50 text-xs font-semibold transition-colors cursor-pointer"
              @click="remindStudents(hw)"
            >
              Nhắc nhở
            </button>
            <router-link
              to="/teacher/grading"
              class="px-3.5 py-1.5 rounded-xl bg-orange-400 hover:bg-orange-500 text-white text-xs font-semibold shadow-2xs transition-colors cursor-pointer"
            >
              Chấm bài
            </router-link>
          </div>
        </div>
      </div>
    </div>

    <!-- ─── Create Assignment Modal (Scaffold) ───────────────────────────── -->
    <div
      v-if="showCreateModal"
      class="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4 backdrop-blur-2xs"
    >
      <div class="bg-white rounded-3xl max-w-lg w-full p-6 shadow-xl border border-orange-100 space-y-4">
        <div class="flex items-center justify-between border-b border-orange-100 pb-3">
          <h3 class="text-base font-bold text-orange-950">Giao bài tập về nhà mới</h3>
          <button @click="showCreateModal = false" class="text-gray-400 hover:text-gray-600 text-lg">✕</button>
        </div>

        <div class="space-y-3 text-xs">
          <div>
            <label class="block font-semibold text-gray-700 mb-1">Chọn lớp học</label>
            <select v-model="newHw.classId" class="w-full rounded-xl border border-orange-200 p-2.5">
              <option value="1">Toán Tư Duy - Khối 3A</option>
              <option value="2">Hình Học Trực Quan 4B</option>
              <option value="3">Toán Nâng Cao 5A</option>
            </select>
          </div>

          <div>
            <label class="block font-semibold text-gray-700 mb-1">Tiêu đề bài tập</label>
            <input
              type="text"
              v-model="newHw.title"
              placeholder="VD: Phiếu bài tập tuần 44 - Tư duy số học"
              class="w-full rounded-xl border border-orange-200 p-2.5"
            />
          </div>

          <div>
            <label class="block font-semibold text-gray-700 mb-1">Mô tả / Hướng dẫn làm bài</label>
            <textarea
              v-model="newHw.description"
              rows="3"
              placeholder="Nhập ghi chú hoặc yêu cầu nộp bài cho học viên..."
              class="w-full rounded-xl border border-orange-200 p-2.5"
            ></textarea>
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block font-semibold text-gray-700 mb-1">Hạn chót nộp</label>
              <input type="datetime-local" v-model="newHw.deadline" class="w-full rounded-xl border border-orange-200 p-2" />
            </div>
            <div>
              <label class="block font-semibold text-gray-700 mb-1">Thang điểm</label>
              <input type="number" v-model="newHw.maxPoints" class="w-full rounded-xl border border-orange-200 p-2" />
            </div>
          </div>
        </div>

        <div class="pt-3 border-t border-orange-100 flex items-center justify-end gap-2">
          <button
            type="button"
            @click="showCreateModal = false"
            class="px-4 py-2 rounded-xl text-xs font-semibold text-gray-600 hover:bg-gray-100"
          >
            Hủy
          </button>
          <button
            type="button"
            @click="createAssignment"
            class="px-4 py-2 rounded-xl bg-orange-400 hover:bg-orange-500 text-white text-xs font-bold shadow-xs"
          >
            Phát hành bài tập
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import {
  PlusIcon,
  ClipboardDocumentListIcon,
} from '@heroicons/vue/24/outline'
import SearchBar from '@/components/common/SearchBar.vue'
import EmptyState from '@/components/common/EmptyState.vue'

const search = ref('')
const classFilter = ref('all')
const statusFilter = ref('all')
const showCreateModal = ref(false)

const newHw = ref({
  classId: '1',
  title: '',
  description: '',
  deadline: '',
  maxPoints: 10,
})

const assignmentsList = ref([
  {
    id: 1,
    order: '01',
    title: 'Phiếu ôn tập tư duy số học & dãy số quy luật tuần 44',
    description: 'Học sinh hoàn thành 10 bài tập trong phiếu và chụp ảnh bài làm nộp lên hệ thống.',
    className: 'Toán Tư Duy 3A',
    classId: '1',
    deadline: '20:00 - 02/11/2026',
    targetType: 'all',
    maxPoints: 10,
    submittedCount: 14,
    totalCount: 18,
    status: 'open',
    statusLabel: 'Còn hạn',
  },
  {
    id: 2,
    order: '02',
    title: 'Thực hành dựng hình học không gian bằng mô hình que tính',
    description: 'Tự tạo hình chóp tam giác và đo đạc góc cạnh thực tế.',
    className: 'Hình Học Trực Quan 4B',
    classId: '2',
    deadline: '18:00 - 30/10/2026',
    targetType: 'all',
    maxPoints: 10,
    submittedCount: 16,
    totalCount: 16,
    status: 'graded',
    statusLabel: 'Đã chấm xong',
  },
  {
    id: 3,
    order: '03',
    title: 'Chuyên đề 5 bài toán chuyển động đều nâng cao',
    description: 'Bài tập bồi dưỡng dành riêng cho nhóm học sinh đội tuyển.',
    className: 'Toán Nâng Cao 5A',
    classId: '3',
    deadline: '22:00 - 28/10/2026',
    targetType: 'all',
    maxPoints: 10,
    submittedCount: 11,
    totalCount: 15,
    status: 'expired',
    statusLabel: 'Đã hết hạn',
  },
])

const filteredAssignments = computed(() => {
  return assignmentsList.value.filter(hw => {
    const matchSearch = !search.value.trim() || hw.title.toLowerCase().includes(search.value.toLowerCase())
    const matchClass = classFilter.value === 'all' || hw.classId === classFilter.value
    const matchStatus = statusFilter.value === 'all' || hw.status === statusFilter.value
    return matchSearch && matchClass && matchStatus
  })
})

function remindStudents(hw) {
  alert(`Đã gửi thông báo nhắc nhở nộp bài "${hw.title}" đến các học sinh chưa hoàn thành!`)
}

function createAssignment() {
  if (!newHw.value.title) {
    alert('Vui lòng nhập tiêu đề bài tập!')
    return
  }
  assignmentsList.value.unshift({
    id: Date.now(),
    order: '0' + (assignmentsList.value.length + 1),
    title: newHw.value.title,
    description: newHw.value.description,
    className: 'Toán Tư Duy 3A',
    classId: newHw.value.classId,
    deadline: '20:00 - 05/11/2026',
    targetType: 'all',
    maxPoints: newHw.value.maxPoints,
    submittedCount: 0,
    totalCount: 18,
    status: 'open',
    statusLabel: 'Còn hạn',
  })
  showCreateModal.value = false
  newHw.value.title = ''
  newHw.value.description = ''
  alert('Đã phát hành bài tập về nhà thành công!')
}
</script>
