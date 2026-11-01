<template>
  <div class="space-y-5">
    <!-- ─── Header ───────────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-orange-950">Chấm điểm & Nhập nhận xét</h1>
        <p class="text-xs text-gray-500 mt-0.5">
          Cập nhật điểm số, nhận xét chi tiết và tích điểm rèn luyện trực tiếp vào hồ sơ học viên
        </p>
      </div>
      <div class="flex items-center gap-2">
        <button
          type="button"
          class="inline-flex items-center gap-1.5 px-4 py-2.5 rounded-xl bg-orange-400 hover:bg-orange-500 text-white text-xs font-semibold shadow-xs transition-colors cursor-pointer"
          @click="saveGrades"
        >
          <CheckCircleIcon class="w-4 h-4" />
          Lưu điểm & Gửi phụ huynh
        </button>
      </div>
    </div>

    <!-- ─── Selectors Toolbar ────────────────────────────────────────────── -->
    <div class="bg-white rounded-2xl border border-orange-100 p-4 shadow-sm space-y-3">
      <div class="grid grid-cols-1 md:grid-cols-3 gap-3">
        <div>
          <label class="block text-xs font-semibold text-gray-600 mb-1">Chọn lớp học</label>
          <select
            v-model="selectedClassId"
            class="w-full text-xs font-semibold bg-orange-50/60 border border-orange-200 text-orange-950 rounded-xl px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
          >
            <option value="1">Toán Tư Duy - Khối 3A</option>
            <option value="2">Hình Học Trực Quan 4B</option>
          </select>
        </div>

        <div class="md:col-span-2">
          <label class="block text-xs font-semibold text-gray-600 mb-1">Chọn bài tập / bài kiểm tra cần chấm</label>
          <select
            v-model="selectedAssignmentId"
            class="w-full text-xs font-semibold bg-white border border-orange-200 text-gray-800 rounded-xl px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
          >
            <option value="1">Phiếu ôn tập tư duy số học tuần 44 (Đã nộp: 14/18)</option>
            <option value="2">Bài kiểm tra đánh giá năng lực định kỳ tháng 10</option>
          </select>
        </div>
      </div>
    </div>

    <!-- ─── Student Grading List ─────────────────────────────────────────── -->
    <div class="bg-white rounded-2xl border border-orange-100 shadow-sm overflow-hidden">
      <div class="p-4 border-b border-orange-100 flex items-center justify-between text-xs">
        <span class="font-bold text-orange-950">
          Danh sách bài nộp của học viên ({{ studentsList.length }})
        </span>
        <span class="text-gray-400">Thang điểm: 10đ</span>
      </div>

      <div class="divide-y divide-orange-50">
        <div
          v-for="st in studentsList"
          :key="st.id"
          class="p-4 hover:bg-orange-50/30 transition-colors flex flex-col md:flex-row md:items-center justify-between gap-4"
        >
          <!-- Student Info -->
          <div class="flex items-start gap-3 min-w-[200px]">
            <div class="w-10 h-10 rounded-full bg-gradient-to-br from-orange-400 to-amber-500 text-white font-bold flex items-center justify-center text-xs shadow-2xs flex-shrink-0">
              {{ st.name.split(' ').pop()[0] }}
            </div>
            <div>
              <p class="font-bold text-gray-900 text-xs">{{ st.name }}</p>
              <p class="text-[11px] font-mono text-orange-700">{{ st.code }}</p>
              <span
                :class="[
                  'inline-block mt-1 text-[10px] font-bold px-2 py-0.2 rounded-full',
                  st.submitted ? 'bg-emerald-100 text-emerald-800' : 'bg-red-100 text-red-800'
                ]"
              >
                {{ st.submitted ? 'Đã nộp bài' : 'Chưa nộp bài' }}
              </span>
            </div>
          </div>

          <!-- Grade and Comment Controls -->
          <div class="flex-1 grid grid-cols-1 sm:grid-cols-3 gap-3">
            <!-- Score input -->
            <div>
              <label class="block text-[11px] text-gray-500 mb-0.5">Điểm số (0 - 10)</label>
              <input
                type="number"
                min="0"
                max="10"
                step="0.5"
                v-model.number="st.score"
                :disabled="!st.submitted"
                placeholder="--/10"
                class="w-full text-xs font-bold text-orange-950 bg-orange-50/60 border border-orange-200 rounded-xl px-3 py-2 text-center focus:outline-none focus:ring-2 focus:ring-orange-300 disabled:bg-gray-100 disabled:text-gray-400"
              />
            </div>

            <!-- Badge award -->
            <div>
              <label class="block text-[11px] text-gray-500 mb-0.5">Huy hiệu khen thưởng</label>
              <select
                v-model="st.badge"
                :disabled="!st.submitted"
                class="w-full text-xs bg-white border border-orange-200 text-gray-800 rounded-xl px-2 py-2 focus:outline-none disabled:bg-gray-100 disabled:text-gray-400"
              >
                <option value="">Không có</option>
                <option value="star">⭐ Xuất sắc</option>
                <option value="creative">💡 Sáng tạo</option>
                <option value="progress">🚀 Tiến bộ</option>
                <option value="diligent">📚 Chăm chỉ</option>
              </select>
            </div>

            <!-- Comment input -->
            <div>
              <label class="block text-[11px] text-gray-500 mb-0.5">Nhận xét của giáo viên</label>
              <input
                type="text"
                v-model="st.comment"
                :disabled="!st.submitted"
                placeholder="Nhập lời khen / nhắc nhở..."
                class="w-full text-xs bg-white border border-orange-200 text-gray-800 rounded-xl px-3 py-2 focus:outline-none focus:ring-2 focus:ring-orange-300 disabled:bg-gray-100 disabled:text-gray-400"
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { CheckCircleIcon } from '@heroicons/vue/24/outline'

const selectedClassId = ref('1')
const selectedAssignmentId = ref('1')

const studentsList = ref([
  { id: 101, name: 'Nguyễn Minh Quân', code: 'HS-00101', submitted: true, score: 9.5, badge: 'star', comment: 'Tính toán nhanh và trình bày rất rõ ràng!' },
  { id: 102, name: 'Trần Bảo An', code: 'HS-00102', submitted: true, score: 9.0, badge: 'creative', comment: 'Cách giải câu hình học rất sáng tạo.' },
  { id: 103, name: 'Lê Hoàng Phúc', code: 'HS-00103', submitted: false, score: null, badge: '', comment: 'Chưa nộp bài tập.' },
  { id: 104, name: 'Phạm Ngọc Trâm', code: 'HS-00104', submitted: true, score: 10, badge: 'star', comment: 'Bài làm xuất sắc, không mắc lỗi nào.' },
  { id: 105, name: 'Vũ Đức Minh', code: 'HS-00105', submitted: true, score: 8.0, badge: 'progress', comment: 'Có nhiều tiến bộ so với tuần trước, cần chú ý đơn vị đo.' },
])

function saveGrades() {
  alert('Đã lưu kết quả đánh giá thành công! Báo cáo học tập đã được cập nhật vào hồ sơ từng học sinh và đồng bộ về ứng dụng phụ huynh.')
}
</script>
