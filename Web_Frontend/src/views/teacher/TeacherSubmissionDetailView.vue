<template>
  <div class="space-y-6 max-w-7xl mx-auto pb-12">
    <!-- ─── Header & Breadcrumb ─────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <nav class="flex items-center gap-2 text-xs font-medium text-orange-600 mb-1">
          <router-link to="/teacher/grading" class="hover:underline">Chấm điểm & Nhận xét</router-link>
          <span>›</span>
          <span class="text-orange-950 font-semibold">Chi tiết bài làm học sinh</span>
        </nav>
        <h1 class="text-2xl md:text-3xl font-bold text-gray-900 tracking-tight">
          Chi tiết bài làm & Đánh giá học sinh
        </h1>
        <p class="text-xs sm:text-sm text-gray-500 mt-0.5">
          Xem lại nội dung bài thi và viết nhận xét, đánh giá kết quả của học sinh
        </p>
      </div>

      <div class="flex items-center gap-2">
        <router-link
          to="/teacher/grading"
          class="inline-flex items-center gap-1.5 px-3.5 py-2.5 rounded-xl border border-gray-300 bg-white hover:bg-gray-50 text-gray-700 text-xs font-semibold shadow-xs transition-colors cursor-pointer"
        >
          <ArrowLeftIcon class="w-4 h-4 text-gray-500" />
          <span>Quay lại danh sách</span>
        </router-link>

        <button
          type="button"
          @click="submitGrading"
          :disabled="isSubmitting"
          class="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-orange-500 hover:bg-orange-600 disabled:opacity-50 text-white text-xs font-bold shadow-sm transition-all cursor-pointer"
        >
          <ArrowPathIcon v-if="isSubmitting" class="w-4 h-4 animate-spin" />
          <CheckCircleIcon v-else class="w-4 h-4" />
          <span>Lưu đánh giá</span>
        </button>
      </div>
    </div>

    <!-- ─── Notification Alerts ─────────────────────────────────────────── -->
    <div
      v-if="feedback.message"
      :class="[
        'px-4 py-3 rounded-2xl text-xs font-medium flex items-center justify-between transition-all duration-300',
        feedback.type === 'error'
          ? 'bg-rose-50 text-rose-800 border border-rose-200'
          : 'bg-emerald-50 text-emerald-800 border border-emerald-200'
      ]"
    >
      <div class="flex items-center gap-2">
        <span class="text-base">{{ feedback.type === 'error' ? '⚠️' : '✅' }}</span>
        <span>{{ feedback.message }}</span>
      </div>
      <button @click="feedback.message = ''" class="text-gray-400 hover:text-gray-600 text-sm font-bold cursor-pointer">✕</button>
    </div>

    <!-- ─── Loading State ───────────────────────────────────────────────── -->
    <div v-if="isLoading" class="bg-white rounded-3xl border border-orange-100 p-16 text-center shadow-sm">
      <ArrowPathIcon class="w-8 h-8 text-orange-500 animate-spin mx-auto mb-3" />
      <p class="text-sm font-semibold text-gray-700">Đang tải chi tiết bài làm của học sinh...</p>
    </div>

    <div v-else-if="!submission" class="bg-white rounded-3xl border border-orange-100 p-16 text-center shadow-sm">
      <p class="text-sm text-gray-500">Không tìm thấy thông tin bài làm này.</p>
      <router-link
        to="/teacher/grading"
        class="mt-3 inline-block px-4 py-2 rounded-xl bg-orange-500 text-white text-xs font-bold"
      >
        Về trang chấm điểm
      </router-link>
    </div>

    <template v-else>
      <!-- ─── Hero Overview Card: Student & Test Info ─────────────────────── -->
      <div class="bg-white rounded-3xl border border-orange-100 p-5 sm:p-6 shadow-sm flex flex-col md:flex-row items-start md:items-center justify-between gap-6">
        <!-- Student Info -->
        <div class="flex items-center gap-4">
          <div class="w-16 h-16 rounded-2xl bg-gradient-to-br from-orange-400 to-amber-500 text-white font-extrabold text-xl flex items-center justify-center shadow-md flex-shrink-0">
            {{ studentInitials }}
          </div>

          <div>
            <div class="flex flex-wrap items-center gap-2">
              <h2 class="text-lg font-bold text-gray-900">{{ submission.studentName }}</h2>
              <span
                :class="[
                  'text-[10px] font-bold px-2.5 py-0.5 rounded-full',
                  submission.status === 'GRADED' ? 'bg-emerald-100 text-emerald-800' :
                  submission.status === 'SUBMITTED' ? 'bg-blue-100 text-blue-800' :
                  'bg-amber-100 text-amber-800'
                ]"
              >
                {{
                  submission.status === 'GRADED' ? 'Đã chấm điểm' :
                  submission.status === 'SUBMITTED' ? 'Đã nộp bài' : 'Chưa nộp bài'
                }}
              </span>
            </div>

            <p class="text-xs text-gray-500 mt-1">
              Học sinh ID: <strong class="text-orange-950 font-mono">HS-{{ submission.studentId }}</strong>
              • Lớp: <strong class="text-orange-900">{{ submission.className || 'Toán Tư Duy' }}</strong>
            </p>

            <p class="text-xs text-gray-400 mt-1 flex items-center gap-2">
              <span>📅 Thời gian nộp: <strong>{{ formatDateTime(submission.completedAt) }}</strong></span>
            </p>
          </div>
        </div>

        <!-- Assignment Stats Summary -->
        <div class="flex flex-wrap items-center gap-3 self-stretch md:self-auto justify-between md:justify-end pt-3 md:pt-0 border-t md:border-t-0 border-orange-100">
          <div class="p-3 rounded-2xl bg-orange-50/70 border border-orange-200 text-center min-w-[100px]">
            <p class="text-[11px] text-gray-500 font-medium">Số câu trong đề</p>
            <p class="text-base font-extrabold text-orange-950 mt-0.5">
              {{ submission.questions?.length || 0 }} câu
            </p>
          </div>

          <div class="p-3 rounded-2xl bg-amber-50/70 border border-amber-200 text-center min-w-[100px]">
            <p class="text-[11px] text-gray-500 font-medium">Tổng điểm gốc</p>
            <p class="text-base font-extrabold text-amber-900 mt-0.5">
              {{ totalExamPoints }}đ
            </p>
          </div>

          <div class="p-3 rounded-2xl bg-emerald-50/70 border border-emerald-200 text-center min-w-[110px]">
            <p class="text-[11px] text-emerald-700 font-medium">Điểm số hiện tại</p>
            <p class="text-base font-extrabold text-emerald-800 mt-0.5">
              {{ gradeForm.score !== null && gradeForm.score !== undefined ? `${gradeForm.score}/100đ` : 'Chưa chấm' }}
            </p>
          </div>
        </div>
      </div>

      <!-- ─── Main Two Columns Grid ───────────────────────────────────────── -->
      <div class="grid grid-cols-1 lg:grid-cols-12 gap-6">
        <!-- ─── Left Column: Questions & Standard Answers (READ ONLY) (7 cols) ── -->
        <div class="lg:col-span-7 space-y-4">
          <!-- Policy notice badge -->
          <div class="p-3.5 rounded-2xl bg-amber-50 border border-amber-200 text-xs text-amber-900 flex items-start gap-2.5">
            <span class="text-base">🔒</span>
            <div class="leading-relaxed">
              <strong class="font-bold">Chế độ xem bài thi (Không được chỉnh sửa đáp án):</strong>
              Toàn bộ đề bài, phương án và đáp án chuẩn được hiển thị để giáo viên đối chiếu, chấm điểm công tâm và không được thay đổi cấu trúc đề.
            </div>
          </div>

          <div class="space-y-4">
            <div
              v-for="(q, idx) in submission.questions"
              :key="q.questionAssignmentId || q.questionId"
              class="bg-white rounded-2xl border border-orange-100 p-5 shadow-sm space-y-3 transition-all"
            >
              <!-- Question Header -->
              <div class="flex items-center justify-between pb-2 border-b border-orange-50">
                <div class="flex items-center gap-2">
                  <span class="text-xs font-bold text-orange-900 bg-orange-100 px-2.5 py-0.5 rounded-lg">
                    Câu {{ idx + 1 }}
                  </span>
                  <span
                    :class="[
                      'text-[10px] font-bold px-2 py-0.5 rounded-full',
                      q.difficulty === 'EASY' ? 'bg-emerald-100 text-emerald-800' :
                      q.difficulty === 'MEDIUM' ? 'bg-blue-100 text-blue-800' : 'bg-amber-100 text-amber-800'
                    ]"
                  >
                    {{ q.difficulty === 'EASY' ? 'Dễ (1.0đ)' : q.difficulty === 'MEDIUM' ? 'Trung bình (1.5đ)' : 'Khó (2.0đ)' }}
                  </span>
                  <span class="text-[10px] font-semibold text-gray-500">
                    {{ q.type === 'MULTIPLE_CHOICE' ? 'Trắc nghiệm' : q.type === 'FILL_IN' ? 'Điền khuyết' : 'Tự luận' }}
                  </span>
                </div>

                <!-- Point and Checkmark helper -->
                <div class="flex items-center gap-2">
                  <span class="text-xs font-bold text-orange-950">{{ q.pointValue }} điểm</span>
                  <label class="flex items-center gap-1.5 cursor-pointer bg-orange-50 hover:bg-orange-100 px-2 py-1 rounded-lg border border-orange-200 transition-colors">
                    <input
                      type="checkbox"
                      :checked="checkedQuestions[q.questionId]"
                      @change="toggleQuestionScore(q)"
                      class="rounded text-emerald-600 focus:ring-emerald-400 cursor-pointer"
                    />
                    <span class="text-[11px] font-bold text-orange-900">Đạt câu này</span>
                  </label>
                </div>
              </div>

              <!-- Question Content (READ ONLY) -->
              <div class="text-xs sm:text-sm text-gray-900 leading-relaxed font-medium whitespace-pre-line">
                {{ q.content }}
              </div>

              <!-- Question Image if available -->
              <div v-if="q.imageUrl" class="max-w-md rounded-xl overflow-hidden border border-orange-100">
                <img :src="q.imageUrl" alt="Ảnh câu hỏi" class="w-full object-contain max-h-48 bg-orange-50/30" />
              </div>

              <!-- Standard Answer (READ ONLY) -->
              <div class="mt-2.5 p-3 rounded-xl bg-emerald-50/80 border border-emerald-200 text-xs text-emerald-950 space-y-1">
                <p class="font-bold text-emerald-900 flex items-center gap-1.5">
                  <span>🎯</span>
                  <span>Đáp án chuẩn & Hướng dẫn chấm bài:</span>
                </p>
                <div class="p-2.5 rounded-lg bg-white/80 border border-emerald-100 text-gray-800 leading-relaxed whitespace-pre-line font-medium">
                  {{ q.answer || 'Đáp án mẫu đang được cập nhật...' }}
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- ─── Right Column: Grading & Teacher Feedback Panel (5 cols) ─────── -->
        <div class="lg:col-span-5 space-y-4">
          <div class="sticky top-6 bg-white rounded-3xl border border-orange-200 p-5 sm:p-6 shadow-sm space-y-5">
            <div class="border-b border-orange-100 pb-3">
              <h3 class="text-base font-bold text-orange-950 flex items-center gap-2">
                <span>📝</span>
                <span>Chấm điểm & Nhận xét đánh giá</span>
              </h3>
              <p class="text-xs text-gray-500 mt-0.5">
                Nhập điểm số và viết đánh giá sư phạm chi tiết cho học sinh
              </p>
            </div>

            <!-- Grade Score Inputs -->
            <div class="space-y-3">
              <div class="p-3 rounded-2xl bg-orange-50/50 border border-orange-200 text-xs space-y-2">
                <div class="flex items-center justify-between">
                  <span class="text-gray-600 font-medium">Tổng điểm câu đạt:</span>
                  <span class="font-bold text-orange-950 text-sm">
                    {{ earnedPointsTotal }} / {{ totalExamPoints }} điểm
                  </span>
                </div>
                <div class="flex items-center justify-between pt-1 border-t border-orange-100">
                  <span class="text-gray-600 font-medium">Công thức quy đổi:</span>
                  <span class="font-mono text-orange-800 font-bold">(Điểm đạt / Tổng đề) × 100</span>
                </div>
              </div>

              <div>
                <label class="block text-xs font-bold text-gray-700 mb-1">
                  Điểm bài kiểm tra (Thang 100đ) <span class="text-rose-500">*</span>
                </label>
                <div class="relative">
                  <input
                    type="number"
                    step="0.1"
                    min="0"
                    max="100"
                    v-model.number="gradeForm.score"
                    placeholder="VD: 85.5"
                    class="w-full text-base font-extrabold text-orange-950 bg-amber-50/50 border border-amber-300 rounded-xl px-4 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
                  />
                  <span class="absolute right-3.5 top-3 text-xs font-bold text-orange-700">/ 100 điểm</span>
                </div>
              </div>
            </div>

            <!-- Teacher Feedback Area (CORE REQUIREMENT) -->
            <div class="space-y-2">
              <label class="block text-xs font-bold text-gray-700">
                Nhận xét & Đánh giá năng lực của Giáo viên <span class="text-rose-500">*</span>
              </label>

              <!-- Quick Feedback Template Pills -->
              <div class="flex flex-wrap gap-1.5 pt-1">
                <button
                  type="button"
                  v-for="pill in quickFeedbackTemplates"
                  :key="pill"
                  @click="appendQuickFeedback(pill)"
                  class="text-[11px] font-medium px-2 py-1 rounded-lg bg-orange-50 hover:bg-orange-100 border border-orange-200 text-orange-800 transition-colors cursor-pointer"
                >
                  + {{ pill }}
                </button>
              </div>

              <textarea
                v-model="gradeForm.teacherFeedback"
                rows="5"
                placeholder="Nhập nhận xét chi tiết về bài làm: ưu điểm về tư duy logic, các câu còn nhầm lẫn, thái độ làm bài và lời khuyên dành cho phụ huynh..."
                class="w-full text-xs rounded-xl border border-orange-200 p-3 leading-relaxed focus:outline-none focus:ring-2 focus:ring-orange-300 placeholder:text-gray-400"
              ></textarea>
            </div>

            <!-- Submit Button -->
            <div class="pt-3 border-t border-orange-100 space-y-2">
              <button
                type="button"
                @click="submitGrading"
                :disabled="isSubmitting"
                class="w-full py-3 rounded-xl bg-orange-500 hover:bg-orange-600 disabled:opacity-50 text-white font-bold text-xs shadow-md transition-all cursor-pointer flex items-center justify-center gap-2"
              >
                <ArrowPathIcon v-if="isSubmitting" class="w-4 h-4 animate-spin" />
                <CheckCircleIcon v-else class="w-4 h-4" />
                <span>Hoàn tất chấm điểm & Lưu nhận xét</span>
              </button>

              <router-link
                to="/teacher/grading"
                class="w-full py-2.5 rounded-xl border border-gray-300 bg-white hover:bg-gray-50 text-gray-700 font-semibold text-xs transition-colors flex items-center justify-center"
              >
                Hủy & Quay lại
              </router-link>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  ArrowLeftIcon,
  CheckCircleIcon,
  ArrowPathIcon,
} from '@heroicons/vue/24/outline'
import assignmentService from '@/services/assignment.service'
import { calculateExamScore } from '@/utils/media.helper'

const route = useRoute()
const router = useRouter()

const assignmentId = computed(() => Number(route.params.assignmentId))
const studentId = computed(() => Number(route.params.studentId))

const submission = ref(null)
const isLoading = ref(true)
const isSubmitting = ref(false)

const checkedQuestions = reactive({})

const gradeForm = reactive({
  score: null,
  teacherFeedback: '',
  status: 'GRADED',
})

const feedback = reactive({
  message: '',
  type: 'success',
})

const quickFeedbackTemplates = [
  'Tư duy logic rất tốt',
  'Tính toán nhanh và chuẩn xác',
  'Trình bày bài làm rõ ràng',
  'Cần chú ý đọc kỹ câu hỏi có lời văn',
  'Tiến bộ vượt bậc so với buổi trước',
  'Cần rèn luyện thêm bài toán hình học',
]

const studentInitials = computed(() => {
  const name = submission.value?.studentName || 'Học sinh'
  const words = name.trim().split(/\s+/)
  return words.slice(-2).map((w) => w[0]).join('').toUpperCase()
})

const totalExamPoints = computed(() => {
  if (!submission.value?.questions) return 10
  return submission.value.questions.reduce((sum, q) => sum + (Number(q.pointValue) || 1.0), 0)
})

const earnedPointsTotal = computed(() => {
  if (!submission.value?.questions) return 0
  return submission.value.questions.reduce((sum, q) => {
    return checkedQuestions[q.questionId] ? sum + (Number(q.pointValue) || 1.0) : sum
  }, 0)
})

function formatDateTime(dateStr) {
  if (!dateStr) return 'Chưa ghi nhận'
  try {
    return new Date(dateStr).toLocaleString('vi-VN', {
      hour: '2-digit',
      minute: '2-digit',
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    })
  } catch {
    return dateStr
  }
}

function notify(message, type = 'success') {
  feedback.message = message
  feedback.type = type
  setTimeout(() => {
    if (feedback.message === message) feedback.message = ''
  }, 4500)
}

function appendQuickFeedback(text) {
  if (!gradeForm.teacherFeedback.trim()) {
    gradeForm.teacherFeedback = text + '. '
  } else {
    gradeForm.teacherFeedback = gradeForm.teacherFeedback.trim() + '; ' + text + '. '
  }
}

function toggleQuestionScore(q) {
  checkedQuestions[q.questionId] = !checkedQuestions[q.questionId]
  // Tự động cập nhật điểm theo tỷ lệ câu đạt trên thang 100
  gradeForm.score = calculateExamScore(earnedPointsTotal.value, totalExamPoints.value)
}

async function loadSubmission() {
  if (!assignmentId.value || !studentId.value) return
  isLoading.value = true
  try {
    const res = await assignmentService.getStudentSubmission(assignmentId.value, studentId.value)
    submission.value = res
    gradeForm.score = res.score ?? null
    gradeForm.teacherFeedback = res.teacherFeedback || ''
    gradeForm.status = res.status || 'GRADED'

    // Khởi tạo trạng thái checkbox theo điểm nếu đã chấm trước đó
    if (res.score && res.questions) {
      const approximateEarned = (res.score / 100) * totalExamPoints.value
      let acc = 0
      res.questions.forEach((q) => {
        const pt = Number(q.pointValue) || 1.0
        if (acc + pt <= approximateEarned + 0.1) {
          checkedQuestions[q.questionId] = true
          acc += pt
        } else {
          checkedQuestions[q.questionId] = false
        }
      })
    }
  } catch (err) {
    console.error('Lỗi tải chi tiết bài làm:', err)
    notify('Không thể tải chi tiết bài làm của học sinh.', 'error')
  } finally {
    isLoading.value = false
  }
}

async function submitGrading() {
  if (gradeForm.score === null || gradeForm.score === undefined || gradeForm.score < 0) {
    notify('Vui lòng nhập điểm bài thi hợp lệ (từ 0 đến 100 điểm)!', 'error')
    return
  }
  if (!gradeForm.teacherFeedback.trim()) {
    notify('Vui lòng nhập lời nhận xét / đánh giá cho học sinh!', 'error')
    return
  }

  isSubmitting.value = true
  try {
    await assignmentService.gradeStudent(assignmentId.value, studentId.value, {
      score: gradeForm.score,
      teacherFeedback: gradeForm.teacherFeedback.trim(),
      status: 'GRADED',
    })

    notify('Đã lưu kết quả chấm điểm và đánh giá học sinh thành công!')
    if (submission.value) {
      submission.value.status = 'GRADED'
      submission.value.score = gradeForm.score
      submission.value.teacherFeedback = gradeForm.teacherFeedback.trim()
    }
  } catch (err) {
    console.error('Lỗi lưu điểm:', err)
    notify('Không thể lưu kết quả chấm điểm.', 'error')
  } finally {
    isSubmitting.value = false
  }
}

onMounted(() => {
  loadSubmission()
})
</script>
