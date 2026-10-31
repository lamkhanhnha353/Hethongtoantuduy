<template>
  <transition name="modal-fade">
    <div
      v-if="show"
      class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs overflow-y-auto"
      @click.self="handleClose"
    >
      <div
        class="bg-white rounded-2xl shadow-xl border border-orange-100 w-full max-w-xl overflow-hidden transform transition-all my-8"
        role="dialog"
      >
        <!-- ─── Header ──────────────────────────────────────────────────────── -->
        <div class="px-6 py-4 border-b border-orange-100 flex items-center justify-between bg-gradient-to-r from-orange-50 to-amber-50">
          <div class="flex items-center gap-2.5">
            <div class="w-9 h-9 rounded-xl bg-orange-500 text-white flex items-center justify-center shadow-xs">
              <UserGroupIcon class="w-5 h-5" />
            </div>
            <div>
              <h3 class="text-base font-bold text-orange-950">
                <template v-if="isEdit">Chỉnh sửa Lớp học: {{ form.name || `#${cls?.id}` }}</template>
                <template v-else>Mở Lớp học mới</template>
              </h3>
              <p class="text-xs text-gray-500">
                <template v-if="isEdit">Cập nhật thông tin giảng viên, sĩ số và thời gian học</template>
                <template v-else>Thiết lập lớp học mới thuộc cấp độ đào tạo</template>
              </p>
            </div>
          </div>
          <button
            @click="handleClose"
            class="text-gray-400 hover:text-gray-600 p-1.5 rounded-lg hover:bg-orange-100/60 transition-colors"
          >
            <XMarkIcon class="w-5 h-5" />
          </button>
        </div>

        <!-- ─── Alert Messages ──────────────────────────────────────────────── -->
        <div v-if="errorMessage" class="mx-6 mt-4 p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-xs flex items-start gap-2">
          <ExclamationCircleIcon class="w-4 h-4 text-red-500 flex-shrink-0 mt-0.5" />
          <span class="flex-1 leading-relaxed">{{ errorMessage }}</span>
        </div>

        <div v-if="successMessage" class="mx-6 mt-4 p-3 rounded-xl bg-green-50 border border-green-200 text-green-700 text-xs flex items-center gap-2">
          <CheckCircleIcon class="w-4 h-4 text-green-600 flex-shrink-0" />
          <span class="flex-1 font-medium">{{ successMessage }}</span>
        </div>

        <!-- ─── Form Body ───────────────────────────────────────────────────── -->
        <form @submit.prevent="handleSubmit" class="p-6 space-y-4 max-h-[75vh] overflow-y-auto">
          <!-- Cấp độ khóa học -->
          <div>
            <label class="block text-xs font-bold text-gray-700 mb-1.5">
              Cấp độ khóa học (Course Level) <span class="text-red-500">*</span>
            </label>
            <div v-if="loadingLevels" class="text-xs text-gray-400 py-2">
              Đang tải danh sách khóa học và cấp độ...
            </div>
            <select
              v-else
              v-model.number="form.courseLevelId"
              required
              class="w-full px-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-semibold text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden"
            >
              <option value="" disabled>-- Chọn cấp độ đào tạo --</option>
              <optgroup
                v-for="course in groupedCourses"
                :key="course.id"
                :label="`${course.name} (${course.code})`"
              >
                <option
                  v-for="level in course.courseLevels"
                  :key="level.id"
                  :value="level.id"
                >
                  {{ level.name }} — Học phí: {{ formatFee(level.fee) }} ({{ level.totalSessions || 0 }} buổi)
                </option>
              </optgroup>
            </select>
            <p v-if="groupedCourses.length === 0 && !loadingLevels" class="text-[11px] text-amber-600 mt-1">
              Chưa có cấp độ khóa học nào trong hệ thống. Vui lòng tạo khóa học và cấp độ trước.
            </p>
          </div>

          <!-- Tên lớp học -->
          <div>
            <label class="block text-xs font-bold text-gray-700 mb-1.5">
              Tên lớp học <span class="text-red-500">*</span>
            </label>
            <input
              v-model.trim="form.name"
              type="text"
              required
              placeholder="VD: Soroban Nhập môn Sáng T7 - CN (Phòng 201)"
              class="w-full px-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-medium text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden"
            />
          </div>

          <!-- Giáo viên phụ trách & Trạng thái -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-bold text-gray-700 mb-1.5">
                Giáo viên phụ trách
              </label>
              <select
                v-model="form.teacherId"
                class="w-full px-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-medium text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden"
              >
                <option :value="null">-- Chưa phân công --</option>
                <option
                  v-for="teacher in teachers"
                  :key="teacher.id"
                  :value="teacher.id"
                >
                  {{ teacher.fullName }} ({{ teacher.phone || teacher.email || `ID: ${teacher.id}` }})
                </option>
              </select>
            </div>

            <div>
              <label class="block text-xs font-bold text-gray-700 mb-1.5">
                Trạng thái lớp <span class="text-red-500">*</span>
              </label>
              <select
                v-model="form.status"
                required
                class="w-full px-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-semibold text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden"
              >
                <option value="ACTIVE">Đang mở (ACTIVE)</option>
                <option value="UPCOMING">Sắp khai giảng (UPCOMING)</option>
                <option value="FULL">Đã đầy sĩ số (FULL)</option>
                <option value="INACTIVE">Đã kết thúc / Đóng (INACTIVE)</option>
              </select>
            </div>
          </div>

          <!-- Sĩ số tối đa -->
          <div>
            <label class="block text-xs font-bold text-gray-700 mb-1.5">
              Sĩ số tối đa (MaxCapacity) <span class="text-red-500">*</span>
            </label>
            <div class="relative">
              <input
                v-model.number="form.maxCapacity"
                type="number"
                min="1"
                max="100"
                required
                placeholder="20"
                class="w-full px-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-medium text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden"
              />
              <span class="absolute right-3 top-2.5 text-xs text-gray-400">học viên</span>
            </div>
            <p v-if="isEdit && cls?.activeStudentsCount != null" class="text-[11px] text-gray-500 mt-1">
              Hiện tại lớp đang có: <strong class="text-orange-600">{{ cls.activeStudentsCount }}</strong> học viên đã đăng ký.
            </p>
          </div>

          <!-- Thời gian: Ngày khai giảng & Ngày bế giảng -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-bold text-gray-700 mb-1.5">
                Ngày khai giảng <span class="text-red-500">*</span>
              </label>
              <input
                v-model="form.startDate"
                type="date"
                required
                class="w-full px-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-medium text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden font-mono"
              />
            </div>

            <div>
              <label class="block text-xs font-bold text-gray-700 mb-1.5">
                Ngày bế giảng dự kiến
              </label>
              <input
                v-model="form.endDate"
                type="date"
                class="w-full px-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-medium text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden font-mono"
              />
              <p class="text-[10px] text-gray-400 mt-1">Có thể để trống để hệ thống tự tính toán theo lịch học</p>
            </div>
          </div>

          <!-- ─── Footer Buttons ────────────────────────────────────────────── -->
          <div class="flex items-center justify-between pt-4 border-t border-orange-100">
            <button
              v-if="isEdit"
              type="button"
              @click="confirmDelete"
              :disabled="saving || deleting"
              class="inline-flex items-center gap-1 px-3 py-2 text-xs font-semibold text-red-600 hover:text-red-700 hover:bg-red-50 rounded-xl transition-colors disabled:opacity-50"
            >
              <TrashIcon class="w-4 h-4" />
              <span>Xóa lớp học</span>
            </button>
            <div v-else></div>

            <div class="flex items-center gap-2">
              <button
                type="button"
                @click="handleClose"
                :disabled="saving || deleting"
                class="px-4 py-2 text-xs font-semibold text-gray-600 hover:bg-gray-100 rounded-xl transition-colors disabled:opacity-50"
              >
                Hủy
              </button>
              <button
                type="submit"
                :disabled="saving || deleting"
                class="inline-flex items-center gap-1.5 px-5 py-2 bg-orange-500 hover:bg-orange-600 text-white text-xs font-semibold rounded-xl shadow-sm transition-colors disabled:opacity-50"
              >
                <svg v-if="saving" class="animate-spin h-3.5 w-3.5 text-white" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" fill="none" />
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
                <span>{{ isEdit ? 'Lưu thay đổi' : 'Mở lớp mới' }}</span>
              </button>
            </div>
          </div>
        </form>
      </div>
    </div>
  </transition>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import {
  UserGroupIcon,
  XMarkIcon,
  TrashIcon,
  ExclamationCircleIcon,
  CheckCircleIcon,
} from '@heroicons/vue/24/outline'

import classService from '@/services/class.service'
import courseService from '@/services/course.service'
import teacherService from '@/services/teacher.service'

const props = defineProps({
  show: { type: Boolean, default: false },
  cls: { type: Object, default: null }, // If null -> create mode, else edit mode
  preselectedLevelId: { type: [Number, String], default: null },
  preselectedLevels: { type: Array, default: () => [] },
})

const emit = defineEmits(['close', 'saved', 'deleted'])

const isEdit = computed(() => Boolean(props.cls && props.cls.id))

const saving = ref(false)
const deleting = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const loadingLevels = ref(false)

const groupedCourses = ref([])
const teachers = ref([])

const form = ref({
  courseLevelId: '',
  teacherId: null,
  name: '',
  maxCapacity: 20,
  startDate: '',
  endDate: '',
  status: 'ACTIVE',
})

function formatFee(fee) {
  return Number(fee || 0).toLocaleString('vi-VN') + 'đ'
}

function formatDateForInput(dateStr) {
  if (!dateStr) return ''
  try {
    const d = new Date(dateStr)
    if (isNaN(d.getTime())) return ''
    return d.toISOString().split('T')[0]
  } catch {
    return ''
  }
}

async function loadDependencies() {
  loadingLevels.value = true
  try {
    const [coursesRes, teachersRes] = await Promise.allSettled([
      courseService.getAll(),
      teacherService.getAll(),
    ])

    if (coursesRes.status === 'fulfilled') {
      const data = coursesRes.value
      const rawCourses = Array.isArray(data) ? data : (data.items ?? data.data ?? [])
      groupedCourses.value = rawCourses.filter(c => Array.isArray(c.courseLevels) && c.courseLevels.length > 0)
    }

    if (teachersRes.status === 'fulfilled') {
      const data = teachersRes.value
      teachers.value = Array.isArray(data) ? data : (data.items ?? data.data ?? [])
    }
  } catch (err) {
    console.error('Lỗi khi tải danh sách cấp độ hoặc giáo viên:', err)
  } finally {
    loadingLevels.value = false
  }
}

function initForm() {
  errorMessage.value = ''
  successMessage.value = ''

  if (props.cls) {
    form.value = {
      courseLevelId: props.cls.courseLevelId ?? props.cls.levelId ?? '',
      teacherId: props.cls.teacherId ?? null,
      name: props.cls.name || '',
      maxCapacity: props.cls.maxCapacity ?? props.cls.maxStudents ?? 20,
      startDate: formatDateForInput(props.cls.startDate),
      endDate: formatDateForInput(props.cls.endDate),
      status: (props.cls.status || 'ACTIVE').toUpperCase(),
    }
  } else {
    // Default today for start date
    const today = new Date().toISOString().split('T')[0]
    form.value = {
      courseLevelId: props.preselectedLevelId ? Number(props.preselectedLevelId) : '',
      teacherId: null,
      name: '',
      maxCapacity: 20,
      startDate: today,
      endDate: '',
      status: 'ACTIVE',
    }
  }
}

watch(
  () => [props.show, props.cls],
  async () => {
    if (props.show) {
      initForm()
      if (groupedCourses.value.length === 0 || teachers.value.length === 0) {
        await loadDependencies()
      }
      // Re-apply preselectedLevelId if form was empty
      if (!form.value.courseLevelId && props.preselectedLevelId) {
        form.value.courseLevelId = Number(props.preselectedLevelId)
      }
    }
  },
  { immediate: true }
)

function handleClose() {
  if (saving.value || deleting.value) return
  emit('close')
}

async function handleSubmit() {
  errorMessage.value = ''
  successMessage.value = ''

  if (!form.value.courseLevelId) {
    errorMessage.value = 'Vui lòng chọn cấp độ khóa học cho lớp này.'
    return
  }
  if (!form.value.name?.trim()) {
    errorMessage.value = 'Tên lớp học không được để trống.'
    return
  }
  if (!form.value.startDate) {
    errorMessage.value = 'Ngày khai giảng không được để trống.'
    return
  }

  const payload = {
    courseLevelId: Number(form.value.courseLevelId),
    teacherId: form.value.teacherId ? Number(form.value.teacherId) : null,
    name: form.value.name.trim(),
    maxCapacity: Number(form.value.maxCapacity) || 20,
    startDate: new Date(form.value.startDate).toISOString(),
    endDate: form.value.endDate ? new Date(form.value.endDate).toISOString() : null,
    status: form.value.status,
  }

  saving.value = true
  try {
    let result
    if (isEdit.value) {
      await classService.update(props.cls.id, payload)
      successMessage.value = 'Cập nhật lớp học thành công!'
      result = { ...props.cls, ...payload }
    } else {
      result = await classService.create(payload)
      successMessage.value = 'Mở lớp học mới thành công!'
    }

    setTimeout(() => {
      emit('saved', result)
      emit('close')
    }, 700)
  } catch (err) {
    console.error('Lỗi khi lưu lớp học:', err)
    errorMessage.value =
      err?.response?.data?.message ||
      err?.message ||
      'Đã xảy ra lỗi khi lưu thông tin lớp học. Vui lòng kiểm tra lại.'
  } finally {
    saving.value = false
  }
}

async function confirmDelete() {
  if (!props.cls?.id) return
  const confirmMsg = `Bạn có chắc chắn muốn xóa lớp học "${props.cls.name}" không?\nLưu ý: Chỉ xóa được lớp học chưa có học sinh đăng ký.`
  if (!window.confirm(confirmMsg)) return

  deleting.value = true
  errorMessage.value = ''
  try {
    await classService.delete(props.cls.id)
    successMessage.value = 'Xóa lớp học thành công!'
    setTimeout(() => {
      emit('deleted', props.cls.id)
      emit('close')
    }, 700)
  } catch (err) {
    console.error('Lỗi khi xóa lớp học:', err)
    errorMessage.value =
      err?.response?.data?.message ||
      err?.message ||
      'Không thể xóa lớp học (Có thể lớp đang có học viên tham gia hoặc lịch học liên kết).'
  } finally {
    deleting.value = false
  }
}
</script>

<style scoped>
.modal-fade-enter-active,
.modal-fade-leave-active {
  transition: opacity 0.2s ease;
}
.modal-fade-enter-from,
.modal-fade-leave-to {
  opacity: 0;
}
</style>
