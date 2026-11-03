<template>
  <div class="space-y-5">
    <!-- ─── Header ───────────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-orange-950">Chấm điểm & Đánh giá bài kiểm tra</h1>
        <p class="text-xs text-gray-500 mt-0.5">
          Quy chuẩn chấm điểm theo mốc độ khó và tính điểm theo thang điểm 100 chuẩn mực
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

    <!-- ─── Formula & Rules Banner ───────────────────────────────────────── -->
    <div class="p-4 bg-gradient-to-r from-orange-50 via-amber-50 to-orange-50 rounded-2xl border border-orange-200 shadow-2xs space-y-2">
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
        <div class="flex items-center gap-2">
          <span class="p-1.5 rounded-xl bg-orange-200/80 text-orange-900 text-sm font-bold">📐</span>
          <div>
            <h3 class="text-xs font-bold text-orange-950">Công thức tính điểm bài kiểm tra</h3>
            <p class="text-[11px] text-gray-600">
              Điểm bài thi được quy đổi về thang điểm 100 theo tỷ lệ điểm đạt trên tổng điểm gốc của đề.
            </p>
          </div>
        </div>

        <div class="px-3.5 py-1.5 rounded-xl bg-white border border-orange-300 font-mono text-xs font-bold text-orange-900 shadow-2xs">
          Điểm kiểm tra = (Tổng điểm đạt / Tổng số điểm) × 100
        </div>
      </div>

      <div class="flex flex-wrap items-center gap-2 pt-1 border-t border-orange-200/60 text-[11px]">
        <span class="font-bold text-orange-900">Mốc điểm theo độ khó:</span>
        <span class="px-2 py-0.5 rounded-md bg-emerald-100 text-emerald-800 font-bold">
          Câu Dễ: 1.0 điểm
        </span>
        <span class="px-2 py-0.5 rounded-md bg-blue-100 text-blue-800 font-bold">
          Câu Trung bình: 1.5 điểm
        </span>
        <span class="px-2 py-0.5 rounded-md bg-amber-100 text-amber-900 font-bold">
          Câu Khó: 2.0 điểm
        </span>
      </div>
    </div>

    <!-- ─── Selectors Toolbar ────────────────────────────────────────────── -->
    <div class="bg-white rounded-2xl border border-orange-100 p-4 shadow-sm space-y-3">
      <div class="grid grid-cols-1 md:grid-cols-3 gap-3">
        <div>
          <label class="block text-xs font-semibold text-gray-600 mb-1">Chọn lớp học</label>
          <select
            v-model="selectedClassId"
            @change="onClassChange"
            class="w-full text-xs font-semibold bg-orange-50/60 border border-orange-200 text-orange-950 rounded-xl px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 cursor-pointer"
          >
            <option value="1">Toán Tư Duy - Khối 3A</option>
            <option value="2">Hình Học Trực Quan 4B</option>
          </select>
        </div>

        <div class="md:col-span-2">
          <label class="block text-xs font-semibold text-gray-600 mb-1">Chọn bài kiểm tra cần chấm</label>
          <select
            v-model="selectedAssignmentId"
            @change="onAssignmentChange"
            class="w-full text-xs font-semibold bg-white border border-orange-200 text-gray-800 rounded-xl px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 cursor-pointer"
          >
            <option value="1">Phiếu ôn tập tư duy số học tuần 44 (Đã nộp: 4/5)</option>
            <option value="2">Bài kiểm tra đánh giá năng lực định kỳ tháng 10</option>
          </select>
        </div>
      </div>

      <!-- Test Metrics Summary -->
      <div class="pt-2 border-t border-orange-50 flex flex-wrap items-center justify-between gap-3 text-xs">
        <div class="flex items-center gap-3 text-gray-600">
          <span>Số câu hỏi: <strong class="text-orange-950">{{ currentExamQuestions.length }} câu</strong></span>
          <span>•</span>
          <span>Tổng số điểm gốc của đề: <strong class="text-orange-600">{{ currentExamTotalPoints }} điểm</strong></span>
          <span>•</span>
          <span class="text-emerald-700 font-semibold">Thang điểm chuẩn: 100 điểm</span>
        </div>
      </div>
    </div>

    <!-- ─── Student Grading List ─────────────────────────────────────────── -->
    <div class="bg-white rounded-2xl border border-orange-100 shadow-sm overflow-hidden">
      <div class="p-4 border-b border-orange-100 flex items-center justify-between text-xs">
        <span class="font-bold text-orange-950">
          Danh sách bài nộp của học viên ({{ studentsList.length }})
        </span>
        <span class="text-orange-700 font-semibold bg-orange-50 px-2.5 py-1 rounded-lg border border-orange-200">
          Thang điểm: 100đ (Tự động tính từ Tổng điểm đạt / {{ currentExamTotalPoints }}đ)
        </span>
      </div>

      <div class="divide-y divide-orange-50">
        <div
          v-for="st in studentsList"
          :key="st.id"
          class="p-4 hover:bg-orange-50/30 transition-colors flex flex-col lg:flex-row lg:items-center justify-between gap-4"
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
          <div class="flex-1 grid grid-cols-1 sm:grid-cols-12 gap-3 items-center">
            <!-- 1. Điểm đạt gốc / Tổng điểm đề (3 cols) -->
            <div class="sm:col-span-3">
              <label class="block text-[11px] text-gray-500 mb-0.5">
                Điểm đạt gốc (/{{ currentExamTotalPoints }}đ)
              </label>
              <div class="flex items-center gap-1.5">
                <input
                  type="number"
                  min="0"
                  :max="currentExamTotalPoints"
                  step="0.5"
                  v-model.number="st.earnedPoints"
                  @input="recalculateStudentScore(st)"
                  :disabled="!st.submitted"
                  placeholder="0"
                  class="w-full text-xs font-bold text-orange-950 bg-orange-50/60 border border-orange-200 rounded-xl px-2.5 py-2 text-center focus:outline-none focus:ring-2 focus:ring-orange-300 disabled:bg-gray-100 disabled:text-gray-400"
                />
                <button
                  type="button"
                  @click="openQuestionChecklist(st)"
                  :disabled="!st.submitted"
                  title="Chấm điểm chi tiết từng câu"
                  class="px-2 py-2 rounded-xl bg-orange-100 hover:bg-orange-200 text-orange-900 text-[11px] font-bold cursor-pointer disabled:opacity-40"
                >
                  📝
                </button>
              </div>
            </div>

            <!-- 2. Điểm kiểm tra quy đổi thang 100 (3 cols) -->
            <div class="sm:col-span-3">
              <label class="block text-[11px] text-gray-500 mb-0.5 font-bold text-orange-900">
                Điểm bài thi (0 - 100đ)
              </label>
              <div class="relative">
                <input
                  type="number"
                  min="0"
                  max="100"
                  step="0.1"
                  v-model.number="st.score"
                  :disabled="!st.submitted"
                  placeholder="--/100"
                  class="w-full text-xs font-extrabold text-orange-950 bg-amber-50/70 border border-amber-300 rounded-xl px-3 py-2 text-center focus:outline-none focus:ring-2 focus:ring-orange-300 disabled:bg-gray-100 disabled:text-gray-400"
                />
                <span class="absolute right-2.5 top-2 text-[10px] font-bold text-orange-600">
                  / 100
                </span>
              </div>
            </div>

            <!-- 3. Huy hiệu khen thưởng (3 cols) -->
            <div class="sm:col-span-3">
              <label class="block text-[11px] text-gray-500 mb-0.5">Huy hiệu</label>
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

            <!-- 4. Nhận xét của giáo viên (3 cols) -->
            <div class="sm:col-span-3">
              <label class="block text-[11px] text-gray-500 mb-0.5">Nhận xét</label>
              <input
                type="text"
                v-model="st.comment"
                :disabled="!st.submitted"
                placeholder="Lời khen / nhắc nhở..."
                class="w-full text-xs bg-white border border-orange-200 text-gray-800 rounded-xl px-3 py-2 focus:outline-none focus:ring-2 focus:ring-orange-300 disabled:bg-gray-100 disabled:text-gray-400"
              />
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ─── Modal: Bảng chấm điểm chi tiết từng câu ───────────────────────── -->
    <div
      v-if="showChecklistModal"
      class="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4 backdrop-blur-2xs"
    >
      <div class="bg-white rounded-3xl max-w-lg w-full p-6 shadow-2xl border border-orange-100 space-y-4 max-h-[90vh] overflow-y-auto">
        <div class="flex items-center justify-between border-b border-orange-100 pb-3">
          <div>
            <h3 class="text-base font-bold text-orange-950">Chấm điểm chi tiết từng câu</h3>
            <p class="text-xs text-gray-500 mt-0.5">
              Học sinh: <strong class="text-orange-900">{{ activeStudent?.name }}</strong>
            </p>
          </div>
          <button @click="showChecklistModal = false" class="text-gray-400 hover:text-gray-600 text-lg cursor-pointer">✕</button>
        </div>

        <!-- Formula display inside modal -->
        <div class="p-3 rounded-xl bg-orange-50 border border-orange-200 text-xs flex items-center justify-between">
          <span>Công thức: <strong>(Tổng điểm đạt / {{ currentExamTotalPoints }}) × 100</strong></span>
          <span class="text-orange-900 font-bold bg-white px-2 py-0.5 rounded-lg border border-orange-200">
            Điểm tạm tính: {{ tempCalculatedScore }} / 100đ
          </span>
        </div>

        <!-- Questions Checklist -->
        <div class="space-y-2 text-xs">
          <div
            v-for="(q, idx) in currentExamQuestions"
            :key="q.id"
            :class="[
              'p-3 rounded-xl border flex items-center justify-between gap-3 transition-colors cursor-pointer',
              checklistAnswers[q.id] ? 'bg-emerald-50/70 border-emerald-300' : 'bg-white border-orange-100'
            ]"
            @click="toggleChecklistQuestion(q.id)"
          >
            <div class="flex items-center gap-2.5">
              <input
                type="checkbox"
                :checked="checklistAnswers[q.id]"
                @click.stop="toggleChecklistQuestion(q.id)"
                class="rounded text-emerald-600 focus:ring-emerald-400 cursor-pointer"
              />
              <div>
                <p class="font-bold text-gray-900">
                  Câu {{ idx + 1 }}: {{ q.title }}
                </p>
                <span
                  :class="[
                    'text-[10px] font-bold px-1.5 py-0.2 rounded',
                    q.difficulty === 'EASY' ? 'bg-emerald-100 text-emerald-800' :
                    q.difficulty === 'MEDIUM' ? 'bg-blue-100 text-blue-800' : 'bg-amber-100 text-amber-800'
                  ]"
                >
                  {{ q.difficulty === 'EASY' ? 'Dễ (1.0đ)' : q.difficulty === 'MEDIUM' ? 'Trung bình (1.5đ)' : 'Khó (2.0đ)' }}
                </span>
              </div>
            </div>

            <span class="font-bold text-orange-950">
              {{ checklistAnswers[q.id] ? `+${q.point}đ` : '0đ' }}
            </span>
          </div>
        </div>

        <div class="pt-3 border-t border-orange-100 flex items-center justify-between text-xs">
          <div>
            <span class="text-gray-500">Tổng điểm đạt:</span>
            <strong class="text-orange-900 ml-1 text-sm">{{ tempEarnedPoints }} / {{ currentExamTotalPoints }}đ</strong>
          </div>

          <div class="flex items-center gap-2">
            <button
              type="button"
              @click="showChecklistModal = false"
              class="px-3 py-2 rounded-xl text-xs font-semibold text-gray-600 hover:bg-gray-100 cursor-pointer"
            >
              Hủy
            </button>
            <button
              type="button"
              @click="applyChecklistScore"
              class="px-4 py-2 rounded-xl bg-orange-400 hover:bg-orange-500 text-white text-xs font-bold shadow-xs cursor-pointer"
            >
              Áp dụng điểm ({{ tempCalculatedScore }}đ)
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed } from 'vue'
import { CheckCircleIcon } from '@heroicons/vue/24/outline'
import { calculateExamScore } from '@/utils/media.helper'

const selectedClassId = ref('1')
const selectedAssignmentId = ref('1')

// Current Exam structure with standard question milestones (1.0, 1.5, 2.0)
const currentExamQuestions = ref([
  { id: 1, title: 'Tính nhẩm Soroban 4 chữ số', difficulty: 'EASY', point: 1.0 },
  { id: 2, title: 'Công thức Anh bạn nhỏ +4', difficulty: 'EASY', point: 1.0 },
  { id: 3, title: 'Ảo tính Anzan cấp tốc', difficulty: 'EASY', point: 1.0 },
  { id: 4, title: 'Nhận biết quy tắc Finger Math', difficulty: 'EASY', point: 1.0 },
  { id: 5, title: 'Dãy số cách đều logic', difficulty: 'MEDIUM', point: 1.5 },
  { id: 6, title: 'Mô hình đoạn thẳng Bar Model', difficulty: 'MEDIUM', point: 1.5 },
  { id: 7, title: 'Tìm hai số khi biết tổng và tỉ số', difficulty: 'MEDIUM', point: 1.5 },
  { id: 8, title: 'Bài toán chia kẹo suy luận logic', difficulty: 'HARD', point: 2.0 },
  { id: 9, title: 'Bài toán số lượng cái bắt tay', difficulty: 'HARD', point: 2.0 },
  { id: 10, title: 'Quy nạp biểu thức đại số', difficulty: 'HARD', point: 2.0 },
])

const currentExamTotalPoints = computed(() => {
  return currentExamQuestions.value.reduce((acc, q) => acc + q.point, 0) // e.g. 15.5
})

// Students list with initial earned points and scores computed via (earned / total) * 100
const studentsList = ref([
  { id: 101, name: 'Nguyễn Minh Quân', code: 'HS-00101', submitted: true, earnedPoints: 14.5, score: 93.5, badge: 'star', comment: 'Tính toán nhanh và trình bày rất rõ ràng!' },
  { id: 102, name: 'Trần Bảo An', code: 'HS-00102', submitted: true, earnedPoints: 13.5, score: 87.1, badge: 'creative', comment: 'Cách giải câu hình học rất sáng tạo.' },
  { id: 103, name: 'Lê Hoàng Phúc', code: 'HS-00103', submitted: false, earnedPoints: null, score: null, badge: '', comment: 'Chưa nộp bài tập.' },
  { id: 104, name: 'Phạm Ngọc Trâm', code: 'HS-00104', submitted: true, earnedPoints: 15.5, score: 100, badge: 'star', comment: 'Bài làm xuất sắc, đạt điểm tuyệt đối!' },
  { id: 105, name: 'Vũ Đức Minh', code: 'HS-00105', submitted: true, earnedPoints: 12.0, score: 77.4, badge: 'progress', comment: 'Có nhiều tiến bộ so với tuần trước, cần chú ý đơn vị đo.' },
])

function recalculateStudentScore(st) {
  if (st.earnedPoints !== null && st.earnedPoints !== undefined) {
    st.score = calculateExamScore(st.earnedPoints, currentExamTotalPoints.value)
  }
}

// ─── Question Checklist Modal ──────────────────────────────────────────────
const showChecklistModal = ref(false)
const activeStudent = ref(null)
const checklistAnswers = reactive({})

function openQuestionChecklist(st) {
  activeStudent.value = st
  // Reset checklist: mark questions as true up to the earned points approximately
  let remaining = st.earnedPoints || 0
  currentExamQuestions.value.forEach(q => {
    if (remaining >= q.point) {
      checklistAnswers[q.id] = true
      remaining -= q.point
    } else {
      checklistAnswers[q.id] = false
    }
  })
  showChecklistModal.value = true
}

function toggleChecklistQuestion(qId) {
  checklistAnswers[qId] = !checklistAnswers[qId]
}

const tempEarnedPoints = computed(() => {
  return currentExamQuestions.value.reduce((acc, q) => {
    return checklistAnswers[q.id] ? acc + q.point : acc
  }, 0)
})

const tempCalculatedScore = computed(() => {
  return calculateExamScore(tempEarnedPoints.value, currentExamTotalPoints.value)
})

function applyChecklistScore() {
  if (activeStudent.value) {
    activeStudent.value.earnedPoints = tempEarnedPoints.value
    activeStudent.value.score = tempCalculatedScore.value
  }
  showChecklistModal.value = false
}

function onClassChange() {
  // Demo refresh logic
}

function onAssignmentChange() {
  // Demo refresh logic
}

function saveGrades() {
  alert(
    `Đã lưu kết quả đánh giá theo công thức (Tổng điểm đạt / Tổng số điểm) × 100 thành công!\nĐiểm số chuẩn thang 100 đã được cập nhật vào hồ sơ học viên.`
  )
}
</script>
