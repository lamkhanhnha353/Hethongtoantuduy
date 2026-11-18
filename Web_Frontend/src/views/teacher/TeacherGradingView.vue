<template>
  <div class="space-y-6 max-w-7xl mx-auto pb-12">
    <!-- ─── Header ───────────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <nav class="flex items-center gap-2 text-xs font-medium text-stone-500 mb-1">
          <span class="text-orange-600 font-semibold">Giáo viên</span>
          <span>›</span>
          <span class="text-stone-900 font-semibold">Chấm điểm & Nhận xét</span>
        </nav>
        <h1 class="text-2xl md:text-3xl font-bold text-stone-900 tracking-tight">Chấm điểm & Đánh giá bài kiểm tra</h1>
        <p class="text-xs md:text-sm text-stone-500 mt-0.5">
          Xem bài làm chi tiết, chấm điểm theo barem chuẩn và viết nhận xét đánh giá năng lực học sinh
        </p>
      </div>

      <div class="flex items-center gap-2 flex-wrap">
        <button
          type="button"
          @click="loadAssignmentDetail"
          :disabled="isLoadingDetail || !selectedAssignmentId"
          class="inline-flex items-center gap-1.5 h-9 px-3.5 rounded-md border border-stone-300 bg-white hover:bg-stone-100 hover:border-stone-400 text-stone-800 text-xs font-semibold transition-colors cursor-pointer disabled:opacity-50"
          title="Tải lại dữ liệu bài kiểm tra"
        >
          <ArrowPathIcon class="w-4 h-4 text-orange-600" :class="{ 'animate-spin': isLoadingDetail }" />
          <span>Làm mới</span>
        </button>

        <button
          type="button"
          @click="saveAllGrades"
          :disabled="isSavingBatch || studentsList.length === 0 || !selectedAssignmentId"
          class="inline-flex items-center gap-1.5 h-9 px-4 rounded-md bg-orange-600 hover:bg-orange-700 active:bg-orange-800 disabled:opacity-50 text-white border border-orange-700 text-xs font-semibold shadow-xs transition-colors cursor-pointer"
        >
          <ArrowPathIcon v-if="isSavingBatch" class="w-4 h-4 animate-spin" />
          <CheckCircleIcon v-else class="w-4 h-4" />
          <span>Lưu tất cả kết quả</span>
        </button>
      </div>
    </div>

    <!-- ─── Notification Alerts ─────────────────────────────────────────── -->
    <div
      v-if="feedback.message"
      :class="[
        'px-4 py-3 rounded-lg text-xs font-medium flex items-center justify-between transition-colors',
        feedback.type === 'error'
          ? 'bg-rose-50 text-rose-800 border border-rose-300'
          : 'bg-emerald-50 text-emerald-800 border border-emerald-300'
      ]"
    >
      <div class="flex items-center gap-2">
        <span class="text-base">{{ feedback.type === 'error' ? '⚠️' : '✅' }}</span>
        <span>{{ feedback.message }}</span>
      </div>
      <button @click="feedback.message = ''" class="text-stone-400 hover:text-stone-700 text-sm font-bold cursor-pointer">✕</button>
    </div>

    <!-- ─── Formula & Rules Banner ───────────────────────────────────────── -->
    <div class="p-4 bg-white rounded-lg border border-stone-300 border-l-4 border-l-orange-600 shadow-[0_1px_2px_rgba(0,0,0,0.04)] space-y-2">
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div class="flex items-center gap-2.5">
          <span class="w-9 h-9 rounded-md bg-orange-50 border border-orange-200 text-orange-700 text-base font-bold flex items-center justify-center flex-shrink-0">📐</span>
          <div>
            <h3 class="text-xs sm:text-sm font-bold text-stone-900">Quy chuẩn chấm điểm & Đánh giá sư phạm</h3>
            <p class="text-xs text-stone-600">
              Chấm điểm theo barem gốc của đề, tự động quy đổi về thang điểm 100 chuẩn mực và gửi nhận xét đến phụ huynh.
            </p>
          </div>
        </div>

        <div class="px-3.5 py-1.5 rounded-md bg-stone-50 border border-stone-300 font-mono text-xs font-bold text-stone-800 self-start sm:self-auto tabular-nums">
          Điểm kiểm tra = (Tổng điểm đạt / Tổng số điểm) × 100
        </div>
      </div>
    </div>

    <!-- ─── Selectors Toolbar ────────────────────────────────────────────── -->
    <div class="bg-white rounded-lg border border-stone-300 p-4 sm:p-5 shadow-[0_1px_2px_rgba(0,0,0,0.04)] space-y-4">
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <!-- Class Selector -->
        <div>
          <label class="block text-xs font-semibold uppercase tracking-wide text-stone-700 mb-1.5 flex items-center gap-1.5">
            <AcademicCapIcon class="w-4 h-4 text-orange-600" />
            Lớp học phụ trách
          </label>
          <select
            v-model="selectedClassId"
            @change="handleClassChange"
            :disabled="isLoadingClasses"
            class="w-full h-10 text-xs font-semibold bg-stone-50/70 border border-stone-300 text-stone-800 rounded-md px-3 focus:bg-white focus:border-orange-600 focus:ring-2 focus:ring-orange-500/20 outline-none cursor-pointer"
          >
            <option :value="null" disabled>-- Chọn lớp học --</option>
            <option v-for="c in classes" :key="c.id" :value="c.id">
              {{ c.name }} ({{ c.code || 'Mã lớp' }})
            </option>
          </select>
        </div>

        <!-- Assignment Selector -->
        <div class="md:col-span-2">
          <label class="block text-xs font-semibold uppercase tracking-wide text-stone-700 mb-1.5 flex items-center gap-1.5">
            <ClipboardDocumentListIcon class="w-4 h-4 text-orange-600" />
            Bài kiểm tra cần chấm
          </label>
          <select
            v-model="selectedAssignmentId"
            @change="loadAssignmentDetail"
            :disabled="isLoadingAssignments || classAssignments.length === 0"
            class="w-full h-10 text-xs font-semibold bg-stone-50/70 border border-stone-300 text-stone-800 rounded-md px-3 focus:bg-white focus:border-orange-600 focus:ring-2 focus:ring-orange-500/20 outline-none cursor-pointer tabular-nums"
          >
            <option :value="null" disabled>
              {{ classAssignments.length === 0 ? '-- Lớp này chưa có bài kiểm tra nào --' : '-- Chọn bài kiểm tra cần chấm --' }}
            </option>
            <option v-for="a in classAssignments" :key="a.id" :value="a.id">
              {{ a.title }} ({{ a.lessonTitle || 'Buổi học' }}) — Đã nộp: {{ a.completedStudentsCount || 0 }}/{{ a.assignedStudentsCount || 0 }} học sinh
            </option>
          </select>
        </div>
      </div>

      <!-- Test Metrics Summary Ribbon -->
      <div
        v-if="activeAssignment"
        class="pt-3 border-t border-stone-200 flex flex-wrap items-center justify-between gap-3 text-xs tabular-nums"
      >
        <div class="flex flex-wrap items-center gap-3 text-stone-600">
          <span>Số câu hỏi: <strong class="text-stone-900">{{ totalQuestions }} câu</strong></span>
          <span class="text-stone-300">•</span>
          <span>Tổng số điểm gốc: <strong class="text-orange-700">{{ totalExamPoints }} điểm</strong></span>
          <span class="text-stone-300">•</span>
          <span>Thời gian làm: <strong class="text-stone-900">{{ activeAssignment.timeLimitMinutes }} phút</strong></span>
          <span class="text-stone-300">•</span>
          <span class="text-emerald-700 font-semibold">Thang điểm chuẩn: 100 điểm</span>
        </div>

        <div class="flex items-center gap-2.5">
          <span class="bg-orange-50 border border-orange-300 text-orange-800 px-2.5 py-0.5 rounded font-semibold text-xs">
            Tiến độ chấm: {{ gradedCount }} / {{ studentsList.length }} học sinh
          </span>
          <span v-if="classAverageScore !== null" class="bg-emerald-50 border border-emerald-300 text-emerald-800 px-2.5 py-0.5 rounded font-semibold text-xs">
            Điểm TB: {{ classAverageScore }}đ
          </span>
        </div>
      </div>
    </div>

    <!-- ─── Student Grading List ─────────────────────────────────────────── -->
    <div class="bg-white rounded-lg border border-stone-300 shadow-[0_1px_2px_rgba(0,0,0,0.04)] overflow-hidden">
      <!-- Toolbar & Status Filter -->
      <div class="p-4 border-b border-stone-200 bg-stone-50 flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-xs">
        <div class="flex items-center gap-2">
          <span class="font-bold text-stone-900 tabular-nums">
            Danh sách học sinh được giao bài ({{ filteredStudents.length }})
          </span>
        </div>

        <!-- Filter tabs: All, Submitted, Graded, Unsubmitted -->
        <div class="inline-flex items-center gap-1 bg-stone-100 border border-stone-300 p-1 rounded-md flex-wrap">
          <button
            type="button"
            v-for="tab in filterTabs"
            :key="tab.value"
            @click="currentFilterTab = tab.value"
            :class="[
              'h-7 px-2.5 rounded text-xs font-semibold transition-colors cursor-pointer',
              currentFilterTab === tab.value
                ? 'bg-orange-600 text-white shadow-xs'
                : 'text-stone-700 hover:text-stone-900'
            ]"
          >
            {{ tab.label }}
          </button>
        </div>
      </div>

      <!-- Loading State -->
      <div v-if="isLoadingDetail" class="p-16 text-center">
        <ArrowPathIcon class="w-7 h-7 text-orange-600 animate-spin mx-auto mb-2" />
        <p class="text-xs text-stone-600">Đang tải danh sách bài làm của học sinh...</p>
      </div>

      <div v-else-if="!selectedAssignmentId" class="p-16 text-center">
        <ClipboardDocumentListIcon class="w-8 h-8 text-stone-300 mx-auto mb-2" />
        <p class="text-xs text-stone-500">Vui lòng chọn một bài kiểm tra để bắt đầu chấm điểm.</p>
      </div>

      <div v-else-if="filteredStudents.length === 0" class="p-16 text-center">
        <p class="text-xs text-stone-500">Không tìm thấy học sinh nào phù hợp với bộ lọc hiện tại.</p>
      </div>

      <div v-else class="divide-y divide-stone-200">
        <div
          v-for="st in filteredStudents"
          :key="st.studentId"
          class="p-4 sm:p-5 hover:bg-orange-50/40 transition-colors flex flex-col xl:flex-row xl:items-center justify-between gap-4"
        >
          <!-- Student Info -->
          <div class="flex items-start gap-3.5 min-w-[220px]">
            <div class="w-10 h-10 rounded-full bg-orange-600 text-white font-bold flex items-center justify-center text-xs flex-shrink-0">
              {{ getInitials(st.studentName) }}
            </div>

            <div>
              <p class="font-bold text-stone-900 text-xs sm:text-sm">{{ st.studentName }}</p>
              <p class="text-[11px] font-mono text-orange-700 tabular-nums">HS-{{ st.studentId }}</p>

              <div class="flex items-center gap-1.5 mt-1">
                <span
                  :class="[
                    'text-[11px] font-semibold px-2 py-0.5 rounded border inline-block',
                    st.status === 'GRADED' ? 'border-emerald-300 bg-emerald-50 text-emerald-800' :
                    st.status === 'SUBMITTED' ? 'border-orange-300 bg-orange-50 text-orange-800' :
                    'border-amber-300 bg-amber-50 text-amber-800'
                  ]"
                >
                  {{
                    st.status === 'GRADED' ? 'Đã chấm điểm' :
                    st.status === 'SUBMITTED' ? 'Đã nộp bài' : 'Chưa nộp bài'
                  }}
                </span>
                <span v-if="st.completedAt" class="text-[11px] text-stone-400 tabular-nums">
                  {{ formatDate(st.completedAt) }}
                </span>
              </div>
            </div>
          </div>

          <!-- Grade, Score & Comment Inputs -->
          <div class="flex-1 grid grid-cols-1 sm:grid-cols-12 gap-3 items-center">
            <!-- 1. Điểm bài thi (0 - 100đ) (4 cols) -->
            <div class="sm:col-span-4">
              <label class="block text-[11px] font-semibold uppercase tracking-wide text-stone-700 mb-1">
                Điểm bài thi (0 - 100đ)
              </label>
              <div class="relative">
                <input
                  type="number"
                  min="0"
                  max="100"
                  step="0.1"
                  :value="checkValue(st.score)"
                  @input="st.score = normalizeScore($event.target.value)"
                  placeholder="--/100"
                  class="w-full h-9 text-xs font-bold text-stone-900 bg-stone-50/70 border border-stone-300 rounded-md pl-3 pr-12 focus:bg-white focus:border-orange-600 focus:ring-2 focus:ring-orange-500/20 outline-none tabular-nums"
                />
                <span class="absolute right-2.5 top-2.5 text-[11px] font-semibold text-stone-500 tabular-nums">
                  / 100đ
                </span>
              </div>
            </div>

            <!-- 2. Nhận xét của giáo viên (8 cols) -->
            <div class="sm:col-span-8">
              <label class="block text-[11px] font-semibold uppercase tracking-wide text-stone-700 mb-1">
                Lời nhận xét / Đánh giá
              </label>
              <input
                type="text"
                v-model="st.teacherFeedback"
                maxlength="1000"
                placeholder="Nhập nhận xét về tư duy logic, sự tiến bộ..."
                class="w-full h-9 text-xs bg-stone-50/70 border border-stone-300 text-stone-800 rounded-md px-3 focus:bg-white focus:border-orange-600 focus:ring-2 focus:ring-orange-500/20 outline-none"
              />
            </div>
          </div>

          <!-- Actions Column (Navigation to Detail & Save Row) -->
          <div class="flex items-center gap-2 self-end xl:self-center flex-shrink-0">
            <!-- Xem chi tiết bài làm (REQUIREMENT) -->
            <button
              type="button"
              @click="goToSubmissionDetail(st.studentId)"
              class="inline-flex items-center gap-1.5 h-8 px-3 rounded-md border border-stone-300 bg-white hover:bg-stone-100 hover:border-stone-400 text-stone-800 text-xs font-semibold transition-colors cursor-pointer"
              title="Xem bài làm chi tiết từng câu và đáp án chuẩn để viết nhận xét"
            >
              <EyeIcon class="w-3.5 h-3.5 text-orange-600" />
              <span>Xem chi tiết bài làm</span>
            </button>

            <!-- Lưu điểm hàng này -->
            <button
              type="button"
              @click="saveSingleStudentGrade(st)"
              :disabled="st.isSaving"
              class="h-8 w-8 inline-flex items-center justify-center rounded-md bg-orange-600 hover:bg-orange-700 disabled:opacity-50 text-white border border-orange-700 text-xs font-semibold transition-colors cursor-pointer shadow-xs"
              title="Lưu điểm & nhận xét cho học sinh này"
            >
              <ArrowPathIcon v-if="st.isSaving" class="w-4 h-4 animate-spin" />
              <CheckIcon v-else class="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import {
  CheckCircleIcon,
  AcademicCapIcon,
  ClipboardDocumentListIcon,
  ArrowPathIcon,
  EyeIcon,
  CheckIcon,
} from '@heroicons/vue/24/outline'

import classService from '@/services/class.service'
import lessonService from '@/services/lesson.service'
import assignmentService from '@/services/assignment.service'
import { useUserState } from '@/services/user-state'
import { getProfileId } from '@/services/auth-storage'
import { normalizeScore } from '@/utils/media.helper'
import { validateTextLength } from '@/utils/validation.helper'

const router = useRouter()
const { profile, fetchProfile } = useUserState()

// ─── State ─────────────────────────────────────────────────────────────────
const classes = ref([])
const classAssignments = ref([])
const selectedClassId = ref(null)
const selectedAssignmentId = ref(null)

const activeAssignment = ref(null)
const studentsList = ref([])

const isLoadingClasses = ref(false)
const isLoadingAssignments = ref(false)
const isLoadingDetail = ref(false)
const isSavingBatch = ref(false)

const currentFilterTab = ref('ALL') // 'ALL', 'SUBMITTED', 'GRADED', 'ASSIGNED'

const filterTabs = [
  { label: 'Tất cả học sinh', value: 'ALL' },
  { label: 'Đã nộp bài', value: 'SUBMITTED' },
  { label: 'Đã chấm điểm', value: 'GRADED' },
  { label: 'Chưa nộp bài', value: 'ASSIGNED' },
]

const feedback = reactive({
  message: '',
  type: 'success',
})

function notify(message, type = 'success') {
  feedback.message = message
  feedback.type = type
  setTimeout(() => {
    if (feedback.message === message) feedback.message = ''
  }, 4500)
}

function getInitials(fullName) {
  if (!fullName) return 'HS'
  const words = fullName.trim().split(/\s+/)
  return words.slice(-2).map((w) => w[0]).join('').toUpperCase()
}

function formatDate(dateStr) {
  if (!dateStr) return ''
  try {
    return new Date(dateStr).toLocaleDateString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
    })
  } catch {
    return dateStr
  }
}

// ─── Computed Metrics ──────────────────────────────────────────────────────
const totalQuestions = computed(() => {
  return activeAssignment.value?.questions?.length || 0
})

const totalExamPoints = computed(() => {
  if (!activeAssignment.value?.questions) return 10
  return activeAssignment.value.questions.reduce(
    (sum, q) => sum + (Number(q.pointValue) || 1.0),
    0
  )
})

const gradedCount = computed(() => {
  return studentsList.value.filter((s) => s.status === 'GRADED' || (s.score !== null && s.score !== undefined)).length
})

const classAverageScore = computed(() => {
  const gradedStudents = studentsList.value.filter(
    (s) => s.score !== null && s.score !== undefined
  )
  if (gradedStudents.length === 0) return null
  const total = gradedStudents.reduce((sum, s) => sum + Number(s.score), 0)
  return Math.round((total / gradedStudents.length) * 10) / 10
})

const filteredStudents = computed(() => {
  if (currentFilterTab.value === 'ALL') return studentsList.value
  return studentsList.value.filter((s) => s.status === currentFilterTab.value)
})

// ─── Load Data ─────────────────────────────────────────────────────────────
async function loadClasses() {
  isLoadingClasses.value = true
  try {
    if (!profile.value) {
      await fetchProfile()
    }
    const teacherProfileId = getProfileId('TEACHER')
    const list = await classService.getAssignedClasses(profile.value, teacherProfileId)
    classes.value = Array.isArray(list) ? list : []

    if (classes.value.length > 0) {
      selectedClassId.value = classes.value[0].id
      await handleClassChange()
    }
  } catch (err) {
    console.error('Lỗi tải danh sách lớp học:', err)
    notify('Không thể tải danh sách lớp học của giáo viên.', 'error')
  } finally {
    isLoadingClasses.value = false
  }
}

async function handleClassChange() {
  if (!selectedClassId.value) return
  isLoadingAssignments.value = true
  try {
    // 1. Lấy tất cả các buổi học của lớp
    const lessons = await lessonService.getByClass(selectedClassId.value)
    const lessonList = Array.isArray(lessons) ? lessons : []

    // 2. Lấy danh sách bài kiểm tra đính kèm từng buổi
    const allAssignments = []
    for (const l of lessonList) {
      try {
        const assList = await assignmentService.getByLesson(l.id)
        if (Array.isArray(assList)) {
          assList.forEach((a) => {
            allAssignments.push({
              ...a,
              lessonTitle: l.title || `Buổi học ngày ${formatDate(l.date)}`,
            })
          })
        }
      } catch (e) {
        console.warn(`Lỗi lấy bài kiểm tra cho lesson ${l.id}:`, e)
      }
    }

    classAssignments.value = allAssignments

    // 3. Tự động chọn bài kiểm tra đầu tiên
    if (classAssignments.value.length > 0) {
      selectedAssignmentId.value = classAssignments.value[0].id
      await loadAssignmentDetail()
    } else {
      selectedAssignmentId.value = null
      activeAssignment.value = null
      studentsList.value = []
    }
  } catch (err) {
    console.error('Lỗi tải bài kiểm tra của lớp:', err)
    notify('Không thể tải danh sách bài kiểm tra của lớp.', 'error')
  } finally {
    isLoadingAssignments.value = false
  }
}

async function loadAssignmentDetail() {
  if (!selectedAssignmentId.value) return
  isLoadingDetail.value = true
  try {
    const res = await assignmentService.getById(selectedAssignmentId.value)
    activeAssignment.value = res

    // Map assigned students with initial scores & feedback
    studentsList.value = (res.assignedStudents || []).map((st) => ({
      ...st,
      score: st.score ?? null,
      teacherFeedback: st.teacherFeedback || '',
      isSaving: false,
    }))
  } catch (err) {
    console.error('Lỗi tải chi tiết bài kiểm tra:', err)
    notify('Không thể tải chi tiết bài kiểm tra.', 'error')
  } finally {
    isLoadingDetail.value = false
  }
}

// ─── Actions: Navigate to Submission Detail & Grade ────────────────────────
function goToSubmissionDetail(studentId) {
  if (!selectedAssignmentId.value || !studentId) return
  router.push(`/teacher/grading/submission/${selectedAssignmentId.value}/${studentId}`)
}

async function saveSingleStudentGrade(st) {
  const score = normalizeScore(st.score)
  if (score === null) {
    notify(`Vui lòng nhập điểm số hợp lệ cho học sinh ${st.studentName}!`, 'error')
    return
  }
  st.score = score

  const feedbackCheck = validateTextLength(st.teacherFeedback, {
    label: 'Lời nhận xét',
    max: 1000,
  })
  if (!feedbackCheck.valid) {
    notify(`${feedbackCheck.message} (${st.studentName})`, 'error')
    return
  }

  st.isSaving = true
  try {
    await assignmentService.gradeStudent(selectedAssignmentId.value, st.studentId, {
      score,
      teacherFeedback: st.teacherFeedback ? st.teacherFeedback.trim() : null,
      status: 'GRADED',
    })

    st.status = 'GRADED'
    notify(`Đã lưu kết quả chấm điểm cho ${st.studentName}!`)
  } catch (err) {
    console.error('Lỗi lưu điểm cho học sinh:', err)
    notify(`Không thể lưu điểm cho ${st.studentName}.`, 'error')
  } finally {
    st.isSaving = false
  }
}

async function saveAllGrades() {
  if (!selectedAssignmentId.value) return
  const invalidGrade = studentsList.value.find((s) => {
    if (s.score === null || s.score === undefined || s.score === '') return false
    return normalizeScore(s.score) === null
  })
  if (invalidGrade) {
    notify(`Vui lòng nhập điểm số hợp lệ cho học sinh ${invalidGrade.studentName}!`, 'error')
    return
  }

  const invalidFeedback = studentsList.value.find(
    (s) => !validateTextLength(s.teacherFeedback, { label: 'Lời nhận xét', max: 1000 }).valid
  )
  if (invalidFeedback) {
    notify(`Lời nhận xét của học sinh ${invalidFeedback.studentName} không được vượt quá 1000 ký tự!`, 'error')
    return
  }

  const validGrades = studentsList.value
    .map((s) => ({ student: s, score: normalizeScore(s.score) }))
    .filter(({ score }) => score !== null)
    .map(({ student, score }) => ({
      studentId: student.studentId,
      score,
      teacherFeedback: student.teacherFeedback ? student.teacherFeedback.trim() : null,
      status: 'GRADED',
    }))

  if (validGrades.length === 0) {
    notify('Chưa có học sinh nào được nhập điểm để lưu!', 'error')
    return
  }

  isSavingBatch.value = true
  try {
    await assignmentService.batchGrade(selectedAssignmentId.value, validGrades)
    notify(`Đã lưu điểm và nhận xét cho ${validGrades.length} học sinh thành công!`)
    await loadAssignmentDetail()
  } catch (err) {
    console.error('Lỗi lưu điểm hàng loạt:', err)
    notify('Không thể lưu kết quả chấm điểm.', 'error')
  } finally {
    isSavingBatch.value = false
  }
}
function checkValue(value) {
  return normalizeScore(value)
}
onMounted(async () => {
  await loadClasses()
})
</script>
