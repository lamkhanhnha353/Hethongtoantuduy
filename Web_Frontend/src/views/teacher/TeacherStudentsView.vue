<template>
  <div class="space-y-5">
    <!-- ─── Header ───────────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-orange-950">Danh sách học sinh theo lớp</h1>
        <p class="text-xs text-gray-500 mt-0.5">
          Xem thông tin học viên, tiến độ học tập và thông tin liên hệ phụ huynh
        </p>
      </div>
      <div class="flex items-center gap-2">
        <button
          type="button"
          class="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-orange-100 hover:bg-orange-200 text-orange-900 text-xs font-semibold transition-colors cursor-pointer"
          @click="exportExcel"
        >
          <ArrowDownTrayIcon class="w-4 h-4 text-orange-600" />
          Xuất danh sách
        </button>
      </div>
    </div>

    <!-- ─── Filter & Class Selector ─────────────────────────────────────── -->
    <div class="bg-white rounded-2xl border border-orange-100 p-4 shadow-sm space-y-3">
      <div class="grid grid-cols-1 md:grid-cols-3 gap-3">
        <!-- Class Selector -->
        <div>
          <label class="block text-xs font-semibold text-gray-600 mb-1">Chọn lớp học phụ trách</label>
          <select
            v-model="selectedClassId"
            class="w-full text-xs font-semibold bg-orange-50/60 border border-orange-200 text-orange-950 rounded-xl px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
          >
            <option v-for="cls in myClasses" :key="cls.id" :value="cls.id">
              {{ cls.name }} ({{ cls.studentsCount }} học sinh)
            </option>
          </select>
        </div>

        <!-- Search Bar -->
        <div class="md:col-span-2">
          <label class="block text-xs font-semibold text-gray-600 mb-1">Tìm kiếm học viên</label>
          <SearchBar
            v-model="search"
            placeholder="Tìm theo tên học sinh, mã học sinh hoặc SĐT phụ huynh..."
          />
        </div>
      </div>
    </div>

    <!-- ─── Class Info Summary Bar ──────────────────────────────────────── -->
    <div class="bg-gradient-to-r from-orange-100/70 to-amber-100/60 rounded-2xl border border-orange-200/80 p-4 flex flex-wrap items-center justify-between gap-3 text-xs">
      <div class="flex items-center gap-3">
        <div class="w-10 h-10 rounded-xl bg-orange-400 text-white flex items-center justify-center font-bold text-sm shadow-xs">
          {{ currentClass?.code.slice(-3) }}
        </div>
        <div>
          <h2 class="font-bold text-orange-950 text-sm">{{ currentClass?.name }}</h2>
          <p class="text-orange-800">{{ currentClass?.schedule }} · Phòng: {{ currentClass?.room }}</p>
        </div>
      </div>

      <div class="flex items-center gap-4 text-xs font-medium text-orange-900">
        <div>
          Sĩ số: <strong class="text-sm font-bold text-orange-950">{{ filteredStudents.length }}</strong> học sinh
        </div>
        <router-link
          to="/teacher/attendance"
          class="px-3 py-1.5 rounded-xl bg-orange-400 hover:bg-orange-500 text-white font-semibold transition-colors shadow-2xs"
        >
          Điểm danh lớp này
        </router-link>
      </div>
    </div>

    <!-- ─── Student Table ────────────────────────────────────────────────── -->
    <div class="bg-white rounded-2xl border border-orange-100 shadow-sm overflow-hidden">
      <div v-if="filteredStudents.length === 0" class="p-12 text-center">
        <EmptyState
          :icon="UserGroupIcon"
          message="Không tìm thấy học sinh nào"
          sub="Kiểm tra lại từ khóa tìm kiếm hoặc chọn lớp học khác."
        />
      </div>

      <div v-else class="overflow-x-auto">
        <table class="w-full text-left border-collapse text-xs">
          <thead>
            <tr class="bg-orange-50/70 border-b border-orange-100 text-orange-900 font-bold uppercase tracking-wider text-[11px]">
              <th class="py-3.5 px-4 w-12 text-center">STT</th>
              <th class="py-3.5 px-4">Học viên</th>
              <th class="py-3.5 px-4">Mã HS</th>
              <th class="py-3.5 px-4">Phụ huynh</th>
              <th class="py-3.5 px-4">Số điện thoại</th>
              <th class="py-3.5 px-4 text-center">Chuyên cần</th>
              <th class="py-3.5 px-4 text-center">Điểm rèn luyện</th>
              <th class="py-3.5 px-4 text-right">Thao tác</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-orange-50">
            <tr
              v-for="(st, index) in filteredStudents"
              :key="st.id"
              class="hover:bg-orange-50/40 transition-colors"
            >
              <td class="py-3 px-4 text-center text-gray-400 font-medium">{{ index + 1 }}</td>
              <td class="py-3 px-4">
                <div class="flex items-center gap-2.5">
                  <div class="w-8 h-8 rounded-full bg-gradient-to-br from-orange-400 to-amber-500 text-white font-bold flex items-center justify-center text-xs shadow-2xs flex-shrink-0">
                    {{ st.name.split(' ').pop()[0] }}
                  </div>
                  <div>
                    <p class="font-bold text-gray-900">{{ st.name }}</p>
                    <p class="text-[11px] text-gray-400">{{ st.gender }} · Sinh năm {{ st.birthYear }}</p>
                  </div>
                </div>
              </td>
              <td class="py-3 px-4 font-mono font-semibold text-orange-700">{{ st.code }}</td>
              <td class="py-3 px-4 font-medium text-gray-700">{{ st.parentName }}</td>
              <td class="py-3 px-4 font-mono text-gray-600">{{ st.phone }}</td>
              <td class="py-3 px-4 text-center">
                <span class="inline-block px-2 py-0.5 rounded-full text-[11px] font-bold bg-emerald-100 text-emerald-800">
                  {{ st.attendanceRate }}%
                </span>
              </td>
              <td class="py-3 px-4 text-center">
                <span class="font-bold text-orange-600">⭐ {{ st.points }}</span>
              </td>
              <td class="py-3 px-4 text-right">
                <div class="flex items-center justify-end gap-1.5">
                  <router-link
                    to="/teacher/grading"
                    class="p-1.5 rounded-lg text-orange-600 hover:bg-orange-100 transition-colors"
                    title="Chấm điểm & Nhận xét"
                  >
                    <PencilSquareIcon class="w-4 h-4" />
                  </router-link>
                  <router-link
                    to="/teacher/communication/notices"
                    class="p-1.5 rounded-lg text-orange-600 hover:bg-orange-100 transition-colors"
                    title="Nhắn tin phụ huynh"
                  >
                    <ChatBubbleLeftEllipsisIcon class="w-4 h-4" />
                  </router-link>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import {
  UserGroupIcon,
  ArrowDownTrayIcon,
  PencilSquareIcon,
  ChatBubbleLeftEllipsisIcon,
} from '@heroicons/vue/24/outline'
import SearchBar from '@/components/common/SearchBar.vue'
import EmptyState from '@/components/common/EmptyState.vue'

const search = ref('')
const selectedClassId = ref(1)

const myClasses = ref([
  { id: 1, code: 'LH-TD03A', name: 'Toán Tư Duy Tiểu Học - Khối 3A', room: 'P201', schedule: 'T2 & T5 (08:00 - 09:30)', studentsCount: 5 },
  { id: 2, code: 'LH-HH04B', name: 'Hình Học Trực Quan & Sáng Tạo 4B', room: 'P305', schedule: 'T2 & T6 (14:00 - 15:30)', studentsCount: 4 },
  { id: 3, code: 'LH-NC05A', name: 'Toán Nâng Cao & Tư Duy Logic 5A', room: 'P102', schedule: 'T4 (09:45 - 11:15)', studentsCount: 3 },
])

const currentClass = computed(() => myClasses.value.find(c => c.id === selectedClassId.value))

const studentsData = ref({
  1: [
    { id: 101, name: 'Nguyễn Minh Quân', code: 'HS-00101', gender: 'Nam', birthYear: 2017, parentName: 'Nguyễn Văn Tuấn', phone: '0912345678', attendanceRate: 100, points: 85 },
    { id: 102, name: 'Trần Bảo An', code: 'HS-00102', gender: 'Nữ', birthYear: 2017, parentName: 'Lê Thu Hương', phone: '0987654321', attendanceRate: 95, points: 92 },
    { id: 103, name: 'Lê Hoàng Phúc', code: 'HS-00103', gender: 'Nam', birthYear: 2017, parentName: 'Lê Hoàng Nam', phone: '0934567890', attendanceRate: 90, points: 78 },
    { id: 104, name: 'Phạm Ngọc Trâm', code: 'HS-00104', gender: 'Nữ', birthYear: 2017, parentName: 'Phạm Văn Hùng', phone: '0965432109', attendanceRate: 100, points: 96 },
    { id: 105, name: 'Vũ Đức Minh', code: 'HS-00105', gender: 'Nam', birthYear: 2017, parentName: 'Vũ Quốc Thái', phone: '0978901234', attendanceRate: 85, points: 80 },
  ],
  2: [
    { id: 201, name: 'Đặng Thảo Vy', code: 'HS-00201', gender: 'Nữ', birthYear: 2016, parentName: 'Đặng Văn Long', phone: '0901122334', attendanceRate: 98, points: 90 },
    { id: 202, name: 'Bùi Gia Khang', code: 'HS-00202', gender: 'Nam', birthYear: 2016, parentName: 'Bùi Minh Trí', phone: '0933445566', attendanceRate: 92, points: 84 },
    { id: 203, name: 'Hoàng Hải Đăng', code: 'HS-00203', gender: 'Nam', birthYear: 2016, parentName: 'Hoàng Văn Sơn', phone: '0977889900', attendanceRate: 100, points: 95 },
    { id: 204, name: 'Đỗ Quỳnh Chi', code: 'HS-00204', gender: 'Nữ', birthYear: 2016, parentName: 'Đỗ Tiến Dũng', phone: '0944556677', attendanceRate: 94, points: 88 },
  ],
  3: [
    { id: 301, name: 'Phan Anh Dũng', code: 'HS-00301', gender: 'Nam', birthYear: 2015, parentName: 'Phan Thanh Hải', phone: '0988112233', attendanceRate: 100, points: 98 },
    { id: 302, name: 'Ngô Khánh Linh', code: 'HS-00302', gender: 'Nữ', birthYear: 2015, parentName: 'Ngô Quang Khải', phone: '0911223344', attendanceRate: 92, points: 86 },
    { id: 303, name: 'Lý Quốc Huy', code: 'HS-00303', gender: 'Nam', birthYear: 2015, parentName: 'Lý Trọng Đức', phone: '0922334455', attendanceRate: 88, points: 82 },
  ],
})

const filteredStudents = computed(() => {
  const list = studentsData.value[selectedClassId.value] || []
  if (!search.value.trim()) return list
  const q = search.value.toLowerCase().trim()
  return list.filter(
    s =>
      s.name.toLowerCase().includes(q) ||
      s.code.toLowerCase().includes(q) ||
      s.phone.includes(q) ||
      s.parentName.toLowerCase().includes(q)
  )
})

function exportExcel() {
  alert('Đang xuất danh sách học viên lớp ' + (currentClass.value?.name || '') + ' sang Excel...')
}
</script>
