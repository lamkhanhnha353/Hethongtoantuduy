<template>
  <transition name="modal-fade">
    <div
      v-if="show"
      class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs overflow-y-auto"
      @click.self="handleClose"
    >
      <div
        class="bg-white rounded-2xl shadow-xl border border-orange-100 w-full max-w-lg overflow-hidden transform transition-all my-8"
        role="dialog"
      >
        <!-- ─── Header ──────────────────────────────────────────────────────── -->
        <div class="px-6 py-4 border-b border-orange-100 flex items-center justify-between bg-gradient-to-r from-orange-50 to-amber-50">
          <div class="flex items-center gap-2.5">
            <div class="w-9 h-9 rounded-xl bg-orange-500 text-white flex items-center justify-center shadow-xs">
              <CalendarDaysIcon class="w-5 h-5" />
            </div>
            <div>
              <h3 class="text-base font-bold text-orange-950">
                <template v-if="isEdit">Chỉnh sửa Buổi học #{{ lesson?.id }}</template>
                <template v-else>Thêm Buổi học mới</template>
              </h3>
              <p class="text-xs text-gray-500">
                <template v-if="isEdit">Cập nhật ngày, thời gian diễn ra hoặc trạng thái buổi học</template>
                <template v-else>Thiết lập thêm 1 buổi học đặc biệt cho lớp</template>
              </p>
            </div>
          </div>
          <button
            @click="handleClose"
            class="text-gray-400 hover:text-gray-600 p-1.5 rounded-lg hover:bg-orange-100/60 transition-colors cursor-pointer"
          >
            <XMarkIcon class="w-5 h-5" />
          </button>
        </div>

        <!-- ─── Alerts ──────────────────────────────────────────────────────── -->
        <div v-if="errorMessage" class="mx-6 mt-4 p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-xs flex items-start gap-2">
          <ExclamationCircleIcon class="w-4 h-4 text-red-500 flex-shrink-0 mt-0.5" />
          <span class="flex-1 leading-relaxed">{{ errorMessage }}</span>
        </div>

        <div v-if="successMessage" class="mx-6 mt-4 p-3 rounded-xl bg-green-50 border border-green-200 text-green-700 text-xs flex items-center gap-2">
          <CheckCircleIcon class="w-4 h-4 text-green-600 flex-shrink-0" />
          <span class="flex-1 font-medium">{{ successMessage }}</span>
        </div>

        <!-- ─── Form ────────────────────────────────────────────────────────── -->
        <form @submit.prevent="handleSubmit" class="p-6 space-y-4">
          <!-- Ngày diễn ra buổi học -->
          <div>
            <label class="block text-xs font-bold text-gray-700 mb-1.5">
              Ngày diễn ra buổi học <span class="text-red-500">*</span>
            </label>
            <input
              v-model="form.lessonDate"
              type="date"
              required
              :min="minLessonDate"
              :max="maxLessonDate"
              class="w-full px-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-medium text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden font-mono"
            />
          </div>

          <!-- Khung giờ học -->
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-bold text-gray-700 mb-1.5">
                Giờ bắt đầu <span class="text-red-500">*</span>
              </label>
              <input
                v-model="form.startTime"
                type="time"
                required
                class="w-full px-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-medium text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden font-mono"
              />
            </div>

            <div>
              <label class="block text-xs font-bold text-gray-700 mb-1.5">
                Giờ kết thúc <span class="text-red-500">*</span>
              </label>
              <input
                v-model="form.endTime"
                type="time"
                required
                class="w-full px-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-medium text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden font-mono"
              />
            </div>
          </div>

          <!-- Trạng thái buổi học -->
          <div>
            <label class="block text-xs font-bold text-gray-700 mb-1.5">
              Trạng thái buổi học <span class="text-red-500">*</span>
            </label>
            <select
              v-model="form.status"
              required
              class="w-full px-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-semibold text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden"
            >
              <option value="SCHEDULED">Lên lịch (SCHEDULED)</option>
              <option value="COMPLETED">Đã hoàn thành (COMPLETED)</option>
              <option value="CANCELLED">Đã hủy / Nghỉ học (CANCELLED)</option>
            </select>
          </div>

          <!-- Đánh dấu quan trọng -->
          <div class="pt-2">
            <label class="flex items-center gap-2.5 cursor-pointer">
              <input
                v-model="form.isImportan"
                type="checkbox"
                class="w-4 h-4 rounded text-orange-500 focus:ring-orange-400 border-gray-300"
              />
              <span class="text-xs font-semibold text-gray-700">
                Đánh dấu là buổi học quan trọng (Kiểm tra định kỳ / Thi sát hạch)
              </span>
            </label>
          </div>

          <!-- Footer Buttons -->
          <div class="flex items-center justify-between pt-4 border-t border-orange-100">
            <button
              v-if="isEdit"
              type="button"
              @click="confirmDelete"
              :disabled="saving || deleting"
              class="inline-flex items-center gap-1 px-3 py-2 text-xs font-semibold text-red-600 hover:text-red-700 hover:bg-red-50 rounded-xl transition-colors disabled:opacity-50 cursor-pointer"
            >
              <TrashIcon class="w-4 h-4" />
              <span>Xóa buổi</span>
            </button>
            <div v-else></div>

            <div class="flex items-center gap-2">
              <button
                type="button"
                @click="handleClose"
                :disabled="saving || deleting"
                class="px-4 py-2 text-xs font-semibold text-gray-600 hover:bg-gray-100 rounded-xl transition-colors disabled:opacity-50 cursor-pointer"
              >
                Hủy
              </button>
              <button
                type="submit"
                :disabled="saving || deleting"
                class="inline-flex items-center gap-1.5 px-5 py-2 bg-orange-500 hover:bg-orange-600 text-white text-xs font-semibold rounded-xl shadow-sm transition-colors disabled:opacity-50 cursor-pointer"
              >
                <svg v-if="saving" class="animate-spin h-3.5 w-3.5 text-white" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" fill="none" />
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
                <span>{{ isEdit ? 'Lưu thay đổi' : 'Thêm buổi học' }}</span>
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
  CalendarDaysIcon,
  XMarkIcon,
  TrashIcon,
  ExclamationCircleIcon,
  CheckCircleIcon,
} from '@heroicons/vue/24/outline'
import lessonService from '@/services/lesson.service'

const props = defineProps({
  show: { type: Boolean, default: false },
  lesson: { type: Object, default: null },
  classId: { type: [Number, String], required: true },
})

const emit = defineEmits(['close', 'saved', 'deleted'])

const isEdit = computed(() => Boolean(props.lesson && props.lesson.id))

// Giới hạn ngày diễn ra buổi học: từ 1 năm trước đến 2 năm sau
function toDateInputValue(date) {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

const minLessonDate = computed(() => {
  const d = new Date()
  d.setFullYear(d.getFullYear() - 1)
  return toDateInputValue(d)
})

const maxLessonDate = computed(() => {
  const d = new Date()
  d.setFullYear(d.getFullYear() + 2)
  return toDateInputValue(d)
})

const saving = ref(false)
const deleting = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const form = ref({
  lessonDate: '',
  startTime: '17:30',
  endTime: '19:00',
  status: 'SCHEDULED',
  isImportan: false,
})

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

function formatTimeForInput(timeStr) {
  if (!timeStr) return '17:30'
  try {
    // If it's full ISO string
    if (String(timeStr).includes('T')) {
      const d = new Date(timeStr)
      if (!isNaN(d.getTime())) {
        const hh = String(d.getHours()).padStart(2, '0')
        const mm = String(d.getMinutes()).padStart(2, '0')
        return `${hh}:${mm}`
      }
    }
    const parts = String(timeStr).split(':')
    return `${parts[0].padStart(2, '0')}:${(parts[1] || '00').padStart(2, '0')}`
  } catch {
    return '17:30'
  }
}

function initForm() {
  errorMessage.value = ''
  successMessage.value = ''

  if (props.lesson) {
    form.value = {
      lessonDate: formatDateForInput(props.lesson.lessonDate),
      startTime: formatTimeForInput(props.lesson.startTime),
      endTime: formatTimeForInput(props.lesson.endTime),
      status: props.lesson.status || 'SCHEDULED',
      isImportan: Boolean(props.lesson.isImportan),
    }
  } else {
    const today = new Date().toISOString().split('T')[0]
    form.value = {
      lessonDate: today,
      startTime: '17:30',
      endTime: '19:00',
      status: 'SCHEDULED',
      isImportan: false,
    }
  }
}

watch(
  () => [props.show, props.lesson],
  () => {
    if (props.show) {
      initForm()
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

  if (!form.value.lessonDate) {
    errorMessage.value = 'Ngày diễn ra buổi học không được để trống.'
    return
  }
  if (form.value.lessonDate < minLessonDate.value || form.value.lessonDate > maxLessonDate.value) {
    errorMessage.value = `Ngày diễn ra buổi học phải nằm trong khoảng ${minLessonDate.value} đến ${maxLessonDate.value}.`
    return
  }
  if (!form.value.startTime || !form.value.endTime) {
    errorMessage.value = 'Giờ bắt đầu và kết thúc không được để trống.'
    return
  }
  if (form.value.endTime <= form.value.startTime) {
    errorMessage.value = 'Giờ kết thúc phải lớn hơn giờ bắt đầu.'
    return
  }

  const startDateTime = `${form.value.lessonDate}T${form.value.startTime}:00`
  const endDateTime = `${form.value.lessonDate}T${form.value.endTime}:00`

  const payload = {
    classId: Number(props.classId),
    lessonDate: `${form.value.lessonDate}T00:00:00`,
    startTime: new Date(startDateTime).toISOString(),
    endTime: new Date(endDateTime).toISOString(),
    status: form.value.status,
    isImportan: form.value.isImportan,
  }

  saving.value = true
  try {
    let result
    if (isEdit.value) {
      await lessonService.update(props.lesson.id, payload)
      successMessage.value = 'Cập nhật buổi học thành công!'
      result = { ...props.lesson, ...payload }
    } else {
      result = await lessonService.create(payload)
      successMessage.value = 'Thêm buổi học mới thành công!'
    }

    setTimeout(() => {
      emit('saved', result)
      emit('close')
    }, 600)
  } catch (err) {
    console.error('Lỗi khi lưu buổi học:', err)
    errorMessage.value =
      err?.response?.data?.message ||
      err?.message ||
      'Không thể lưu buổi học. Vui lòng kiểm tra lại.'
  } finally {
    saving.value = false
  }
}

async function confirmDelete() {
  if (!props.lesson?.id) return
  if (props.lesson.attendancesCount > 0) {
    alert('Không thể xóa buổi học đã có dữ liệu điểm danh học viên.')
    return
  }
  if (!window.confirm(`Bạn có chắc chắn muốn xóa buổi học ngày ${form.value.lessonDate} không?`)) {
    return
  }

  deleting.value = true
  errorMessage.value = ''
  try {
    await lessonService.delete(props.lesson.id)
    successMessage.value = 'Xóa buổi học thành công!'
    setTimeout(() => {
      emit('deleted', props.lesson.id)
      emit('close')
    }, 600)
  } catch (err) {
    console.error('Lỗi khi xóa buổi học:', err)
    errorMessage.value =
      err?.response?.data?.message ||
      err?.message ||
      'Không thể xóa buổi học.'
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
