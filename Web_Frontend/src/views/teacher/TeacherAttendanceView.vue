<template>
  <div class="space-y-5">
    <!-- ─── Header ───────────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-orange-950">Điểm danh học viên & Báo vắng mặt</h1>
        <p class="text-xs text-gray-500 mt-0.5">
          Ghi nhận chuyên cần từng buổi học, tự động gửi cảnh báo vắng học đến phụ huynh
        </p>
      </div>
      <div class="flex items-center gap-2">
        <button
          type="button"
          class="inline-flex items-center gap-1.5 px-4 py-2.5 rounded-xl bg-orange-400 hover:bg-orange-500 text-white text-xs font-semibold shadow-xs transition-colors cursor-pointer"
          @click="saveAttendance"
        >
          <CheckCircleIcon class="w-4 h-4" />
          Lưu điểm danh & Báo phụ huynh
        </button>
      </div>
    </div>

    <!-- ─── Notification Integration Banner ──────────────────────────────── -->
    <div class="rounded-2xl border border-amber-200 bg-amber-50/80 p-4 flex items-start gap-3 text-xs text-amber-900">
      <BellAlertIcon class="w-5 h-5 text-amber-600 flex-shrink-0 mt-0.5" />
      <div class="flex-1">
        <p class="font-bold">Tính năng thông báo tự động cho phụ huynh đang BẬT</p>
        <p class="text-amber-800 mt-0.5">
          Khi thầy/cô tích chọn trạng thái <strong>"Vắng không phép"</strong> hoặc <strong>"Vắng có phép"</strong>, hệ thống sẽ tự động kích hoạt gửi tin nhắn SMS và In-App Notification đến tài khoản phụ huynh tương ứng.
        </p>
      </div>
      <label class="flex items-center gap-2 font-semibold text-xs cursor-pointer select-none">
        <input type="checkbox" v-model="autoNotifyParent" class="rounded text-orange-500 focus:ring-orange-400" />
        Bật gửi tự động
      </label>
    </div>

    <!-- ─── Controls Toolbar ─────────────────────────────────────────────── -->
    <div class="bg-white rounded-2xl border border-orange-100 p-4 shadow-sm">
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
        <div>
          <label class="block text-xs font-semibold text-gray-600 mb-1">Chọn lớp học</label>
          <select
            v-model="selectedClassId"
            class="w-full text-xs font-semibold bg-orange-50/60 border border-orange-200 text-orange-950 rounded-xl px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
          >
            <option v-for="cls in myClasses" :key="cls.id" :value="cls.id">
              {{ cls.name }} ({{ cls.room }})
            </option>
          </select>
        </div>

        <div>
          <label class="block text-xs font-semibold text-gray-600 mb-1">Ngày điểm danh</label>
          <input
            type="date"
            v-model="attendanceDate"
            class="w-full text-xs font-semibold bg-white border border-orange-200 text-gray-800 rounded-xl px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
          />
        </div>

        <div>
          <label class="block text-xs font-semibold text-gray-600 mb-1">Thao tác nhanh</label>
          <button
            type="button"
            class="w-full py-2.5 px-3 rounded-xl border border-emerald-300 bg-emerald-50 text-emerald-800 text-xs font-bold hover:bg-emerald-100 transition-colors cursor-pointer"
            @click="markAllPresent"
          >
            ✓ Đánh dấu tất cả CÓ MẶT
          </button>
        </div>
      </div>
    </div>

    <!-- ─── Realtime Attendance Stats ────────────────────────────────────── -->
    <div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
      <div class="bg-white rounded-xl border border-orange-100 p-3 text-center shadow-2xs">
        <p class="text-[11px] text-gray-400 font-semibold uppercase">Tổng sĩ số</p>
        <p class="text-xl font-bold text-gray-900 mt-0.5">{{ attendanceList.length }}</p>
      </div>
      <div class="bg-emerald-50 rounded-xl border border-emerald-200 p-3 text-center shadow-2xs">
        <p class="text-[11px] text-emerald-700 font-semibold uppercase">Có mặt</p>
        <p class="text-xl font-bold text-emerald-800 mt-0.5">{{ countPresent }}</p>
      </div>
      <div class="bg-red-50 rounded-xl border border-red-200 p-3 text-center shadow-2xs">
        <p class="text-[11px] text-red-700 font-semibold uppercase">Vắng mặt</p>
        <p class="text-xl font-bold text-red-800 mt-0.5">{{ countAbsent }}</p>
      </div>
      <div class="bg-amber-50 rounded-xl border border-amber-200 p-3 text-center shadow-2xs">
        <p class="text-[11px] text-amber-700 font-semibold uppercase">Đi muộn</p>
        <p class="text-xl font-bold text-amber-800 mt-0.5">{{ countLate }}</p>
      </div>
    </div>

    <!-- ─── Attendance List ──────────────────────────────────────────────── -->
    <div class="bg-white rounded-2xl border border-orange-100 shadow-sm overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse text-xs">
          <thead>
            <tr class="bg-orange-50/70 border-b border-orange-100 text-orange-900 font-bold uppercase tracking-wider text-[11px]">
              <th class="py-3.5 px-4 w-12 text-center">STT</th>
              <th class="py-3.5 px-4">Học viên</th>
              <th class="py-3.5 px-4">Phụ huynh</th>
              <th class="py-3.5 px-4 text-center">Trạng thái điểm danh</th>
              <th class="py-3.5 px-4">Ghi chú / Lý do vắng</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-orange-50">
            <tr
              v-for="(st, index) in attendanceList"
              :key="st.id"
              :class="[
                'transition-colors',
                st.status === 'absent' ? 'bg-red-50/40' :
                st.status === 'late' ? 'bg-amber-50/40' :
                'hover:bg-orange-50/30'
              ]"
            >
              <td class="py-3 px-4 text-center text-gray-400 font-medium">{{ index + 1 }}</td>
              <td class="py-3 px-4">
                <p class="font-bold text-gray-900">{{ st.name }}</p>
                <p class="text-[11px] font-mono text-orange-700">{{ st.code }}</p>
              </td>
              <td class="py-3 px-4">
                <p class="text-gray-800 font-medium">{{ st.parentName }}</p>
                <p class="text-[11px] text-gray-400">{{ st.phone }}</p>
              </td>
              <td class="py-3 px-4">
                <div class="flex items-center justify-center gap-1.5">
                  <button
                    type="button"
                    @click="st.status = 'present'"
                    :class="[
                      'px-2.5 py-1.5 rounded-lg font-bold text-xs transition-all cursor-pointer',
                      st.status === 'present'
                        ? 'bg-emerald-500 text-white shadow-xs'
                        : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                    ]"
                  >
                    Có mặt
                  </button>
                  <button
                    type="button"
                    @click="st.status = 'late'"
                    :class="[
                      'px-2.5 py-1.5 rounded-lg font-bold text-xs transition-all cursor-pointer',
                      st.status === 'late'
                        ? 'bg-amber-500 text-white shadow-xs'
                        : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                    ]"
                  >
                    Đi muộn
                  </button>
                  <button
                    type="button"
                    @click="st.status = 'excused'"
                    :class="[
                      'px-2.5 py-1.5 rounded-lg font-bold text-xs transition-all cursor-pointer',
                      st.status === 'excused'
                        ? 'bg-blue-500 text-white shadow-xs'
                        : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                    ]"
                  >
                    Có phép
                  </button>
                  <button
                    type="button"
                    @click="st.status = 'absent'"
                    :class="[
                      'px-2.5 py-1.5 rounded-lg font-bold text-xs transition-all cursor-pointer',
                      st.status === 'absent'
                        ? 'bg-red-500 text-white shadow-xs ring-2 ring-red-300'
                        : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                    ]"
                  >
                    Vắng
                  </button>
                </div>
              </td>
              <td class="py-3 px-4">
                <input
                  type="text"
                  v-model="st.note"
                  :placeholder="st.status === 'absent' ? 'Nhập lý do vắng để gửi phụ huynh...' : 'Ghi chú thêm...'"
                  :class="[
                    'w-full text-xs rounded-lg px-2.5 py-1.5 border transition-all focus:outline-none focus:ring-1 focus:ring-orange-400',
                    st.status === 'absent' ? 'border-red-300 bg-white placeholder-red-400 text-red-900' : 'border-gray-200 bg-white/70'
                  ]"
                />
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
  CheckCircleIcon,
  BellAlertIcon,
} from '@heroicons/vue/24/outline'

const selectedClassId = ref(1)
const attendanceDate = ref(new Date().toISOString().slice(0, 10))
const autoNotifyParent = ref(true)

const myClasses = ref([
  { id: 1, name: 'Toán Tư Duy Tiểu Học - Khối 3A', room: 'P201' },
  { id: 2, name: 'Hình Học Trực Quan & Sáng Tạo 4B', room: 'P305' },
  { id: 3, name: 'Toán Nâng Cao & Tư Duy Logic 5A', room: 'P102' },
])

const attendanceList = ref([
  { id: 101, name: 'Nguyễn Minh Quân', code: 'HS-00101', parentName: 'Nguyễn Văn Tuấn', phone: '0912345678', status: 'present', note: '' },
  { id: 102, name: 'Trần Bảo An', code: 'HS-00102', parentName: 'Lê Thu Hương', phone: '0987654321', status: 'present', note: '' },
  { id: 103, name: 'Lê Hoàng Phúc', code: 'HS-00103', parentName: 'Lê Hoàng Nam', phone: '0934567890', status: 'absent', note: 'Phụ huynh gọi điện xin phép' },
  { id: 104, name: 'Phạm Ngọc Trâm', code: 'HS-00104', parentName: 'Phạm Văn Hùng', phone: '0965432109', status: 'present', note: '' },
  { id: 105, name: 'Vũ Đức Minh', code: 'HS-00105', parentName: 'Vũ Quốc Thái', phone: '0978901234', status: 'late', note: 'Đi muộn 15 phút do kẹt xe' },
])

const countPresent = computed(() => attendanceList.value.filter(s => s.status === 'present').length)
const countAbsent = computed(() => attendanceList.value.filter(s => s.status === 'absent' || s.status === 'excused').length)
const countLate = computed(() => attendanceList.value.filter(s => s.status === 'late').length)

function markAllPresent() {
  attendanceList.value.forEach(s => {
    s.status = 'present'
    s.note = ''
  })
}

function saveAttendance() {
  const absents = attendanceList.value.filter(s => s.status === 'absent' || s.status === 'excused')
  if (autoNotifyParent.value && absents.length > 0) {
    alert(`Đã lưu điểm danh thành công! Đã gửi thông báo vắng mặt đến ${absents.length} phụ huynh qua SMS & App.`)
  } else {
    alert('Đã lưu điểm danh thành công!')
  }
}
</script>
