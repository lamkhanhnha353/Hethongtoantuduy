<template>
  <transition name="modal-fade">
    <div
      v-if="show"
      class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm overflow-y-auto"
      @click.self="handleClose"
    >
      <div
        class="bg-white rounded-2xl shadow-xl border border-orange-100 w-full max-w-lg overflow-hidden transform transition-all my-8"
        role="dialog"
      >
        <!-- ─── Modal Header ─────────────────────────────────────────────── -->
        <div class="px-6 py-4 border-b border-orange-100 flex items-center justify-between bg-gradient-to-r from-orange-50 to-amber-50">
          <div class="flex items-center gap-2.5">
            <div class="w-9 h-9 rounded-xl bg-orange-500 text-white flex items-center justify-center shadow-sm">
              <CalendarDaysIcon class="w-5 h-5" />
            </div>
            <div>
              <h3 class="text-base font-bold text-orange-950">
                <template v-if="mode === 'create'">Thêm Lịch học mới</template>
                <template v-else-if="mode === 'edit'">Chỉnh sửa Lịch học #{{ form.id }}</template>
                <template v-else>Chi tiết Lịch học #{{ schedule?.id }}</template>
              </h3>
              <p class="text-xs text-gray-500">
                <template v-if="mode === 'create'">Thiết lập thời gian học trong tuần cho lớp</template>
                <template v-else-if="mode === 'edit'">Cập nhật thông tin thứ, khung giờ hoặc phòng học</template>
                <template v-else>{{ schedule?.className ?? 'Thông tin buổi học' }}</template>
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

        <!-- ─── Mode switcher tab when viewing an existing item ───────────── -->
        <div v-if="mode !== 'create'" class="flex border-b border-orange-100 bg-orange-50/40 px-6 pt-2">
          <button
            @click="mode = 'view'"
            :class="[
              'pb-2 px-3 text-xs font-semibold border-b-2 transition-all flex items-center gap-1.5',
              mode === 'view'
                ? 'border-orange-500 text-orange-600'
                : 'border-transparent text-gray-500 hover:text-orange-500'
            ]"
          >
            <EyeIcon class="w-4 h-4" />
            Xem chi tiết
          </button>
          <button
            @click="mode = 'edit'"
            :class="[
              'pb-2 px-3 text-xs font-semibold border-b-2 transition-all flex items-center gap-1.5',
              mode === 'edit'
                ? 'border-orange-500 text-orange-600'
                : 'border-transparent text-gray-500 hover:text-orange-500'
            ]"
          >
            <PencilSquareIcon class="w-4 h-4" />
            Chỉnh sửa
          </button>
        </div>

        <!-- ─── Alert error message ──────────────────────────────────────── -->
        <div v-if="errorMessage" class="mx-6 mt-4 p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-xs flex items-start gap-2">
          <ExclamationCircleIcon class="w-4 h-4 text-red-500 flex-shrink-0 mt-0.5" />
          <span class="flex-1 leading-relaxed">{{ errorMessage }}</span>
        </div>

        <!-- ─── Alert success message ────────────────────────────────────── -->
        <div v-if="successMessage" class="mx-6 mt-4 p-3 rounded-xl bg-green-50 border border-green-200 text-green-700 text-xs flex items-center gap-2">
          <CheckCircleIcon class="w-4 h-4 text-green-600 flex-shrink-0" />
          <span class="flex-1 font-medium">{{ successMessage }}</span>
        </div>

        <!-- ═══════════════════════════════════════════════════════════════════ -->
        <!-- ─── 1. VIEW MODE ──────────────────────────────────────────────── -->
        <!-- ═══════════════════════════════════════════════════════════════════ -->
        <div v-if="mode === 'view'" class="p-6 space-y-4">
          <!-- Class info -->
          <div class="p-4 bg-orange-50/60 rounded-xl border border-orange-100 flex items-center justify-between">
            <div>
              <p class="text-xs text-gray-500 font-medium">Lớp học</p>
              <h4 class="font-bold text-gray-900 text-sm mt-0.5">
                {{ schedule?.className ?? `Lớp học #${schedule?.classId}` }}
              </h4>
              <p class="text-[11px] text-orange-600 font-mono mt-0.5">
                Mã lớp: ID {{ schedule?.classId }}
              </p>
            </div>
            <span class="text-xs font-mono font-semibold px-2.5 py-1 rounded-lg bg-white border border-orange-200 text-orange-700 shadow-2xs">
              Mã lịch: #{{ schedule?.id }}
            </span>
          </div>

          <!-- Grid details -->
          <div class="grid grid-cols-2 gap-3">
            <!-- Day of week -->
            <div class="p-3.5 bg-gray-50 rounded-xl border border-gray-100">
              <span class="text-[11px] font-medium text-gray-500 flex items-center gap-1">
                <CalendarIcon class="w-3.5 h-3.5 text-orange-400" />
                Ngày trong tuần
              </span>
              <div class="mt-1.5 flex items-center gap-1.5">
                <span :class="['px-2.5 py-1 text-xs font-bold rounded-lg border', dayInfo.color]">
                  {{ dayInfo.label }}
                </span>
                <span class="text-xs text-gray-400">({{ dayInfo.full }})</span>
              </div>
            </div>

            <!-- Room -->
            <div class="p-3.5 bg-gray-50 rounded-xl border border-gray-100">
              <span class="text-[11px] font-medium text-gray-500 flex items-center gap-1">
                <MapPinIcon class="w-3.5 h-3.5 text-orange-400" />
                Phòng học
              </span>
              <p class="mt-1.5 text-xs font-bold text-gray-800">
                {{ schedule?.room ? schedule.room : 'Chưa xếp phòng' }}
              </p>
            </div>

            <!-- Time range -->
            <div class="p-3.5 bg-gray-50 rounded-xl border border-gray-100 col-span-2">
              <div class="flex items-center justify-between">
                <span class="text-[11px] font-medium text-gray-500 flex items-center gap-1">
                  <ClockIcon class="w-3.5 h-3.5 text-orange-400" />
                  Khung giờ học
                </span>
                <span v-if="durationText" class="text-[11px] text-orange-600 font-semibold bg-orange-100/70 px-2 py-0.5 rounded-md">
                  {{ durationText }}
                </span>
              </div>
              <p class="mt-1.5 text-base font-extrabold text-orange-950 font-mono">
                {{ formatTimeDisplay(schedule?.startTime) }} → {{ formatTimeDisplay(schedule?.endTime) }}
              </p>
            </div>
          </div>

          <!-- Timestamps -->
          <div class="text-[11px] text-gray-400 space-y-1 pt-2 border-t border-gray-100">
            <div class="flex justify-between">
              <span>Ngày tạo lịch:</span>
              <span class="text-gray-600 font-medium">{{ formatDateTime(schedule?.createdAt) }}</span>
            </div>
            <div v-if="schedule?.updatedAt" class="flex justify-between">
              <span>Cập nhật lần cuối:</span>
              <span class="text-gray-600 font-medium">{{ formatDateTime(schedule?.updatedAt) }}</span>
            </div>
          </div>

          <!-- Footer Actions for View mode -->
          <div class="flex items-center justify-between pt-4 border-t border-orange-100">
            <button
              @click="confirmDelete"
              :disabled="deleting"
              class="inline-flex items-center gap-1.5 px-3 py-2 text-xs font-semibold text-red-600 hover:text-red-700 hover:bg-red-50 rounded-xl transition-colors disabled:opacity-50"
            >
              <TrashIcon class="w-4 h-4" />
              <span>Xóa lịch học</span>
            </button>

            <div class="flex items-center gap-2">
              <button
                @click="handleClose"
                class="px-4 py-2 text-xs font-semibold text-gray-600 hover:bg-gray-100 rounded-xl transition-colors"
              >
                Đóng
              </button>
              <button
                @click="mode = 'edit'"
                class="inline-flex items-center gap-1.5 px-4 py-2 bg-orange-500 hover:bg-orange-600 text-white text-xs font-semibold rounded-xl shadow-sm transition-colors"
              >
                <PencilSquareIcon class="w-4 h-4" />
                Chỉnh sửa lịch
              </button>
            </div>
          </div>
        </div>

        <!-- ═══════════════════════════════════════════════════════════════════ -->
        <!-- ─── 2. EDIT / CREATE MODE ─────────────────────────────────────── -->
        <!-- ═══════════════════════════════════════════════════════════════════ -->
        <form v-else @submit.prevent="handleSubmit" class="p-6 space-y-4">
          <!-- Class Select -->
          <div>
            <label class="block text-xs font-bold text-gray-700 mb-1.5">
              Lớp học <span class="text-red-500">*</span>
            </label>
            <select
              v-model.number="form.classId"
              required
              class="w-full px-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-medium text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden"
            >
              <option value="" disabled>-- Chọn lớp học --</option>
              <option
                v-for="c in classes"
                :key="c.id"
                :value="c.id"
              >
                {{ c.name }} (ID: {{ c.id }})
              </option>
            </select>
          </div>

          <!-- Day of week Select -->
          <div>
            <label class="block text-xs font-bold text-gray-700 mb-1.5">
              Thứ trong tuần (Quy ước 1 = Thứ 2, 2 = Thứ 3, ..., 0 = CN) <span class="text-red-500">*</span>
            </label>
            <select
              v-model.number="form.dayOfWeek"
              required
              class="w-full px-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-medium text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden"
            >
              <option :value="1">1 = Thứ 2 (Thứ Hai)</option>
              <option :value="2">2 = Thứ 3 (Thứ Ba)</option>
              <option :value="3">3 = Thứ 4 (Thứ Tư)</option>
              <option :value="4">4 = Thứ 5 (Thứ Năm)</option>
              <option :value="5">5 = Thứ 6 (Thứ Sáu)</option>
              <option :value="6">6 = Thứ 7 (Thứ Bảy)</option>
              <option :value="0">0 = Chủ nhật (CN)</option>
            </select>
          </div>

          <!-- Time range -->
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-bold text-gray-700 mb-1.5">
                Giờ bắt đầu <span class="text-red-500">*</span>
              </label>
              <div class="relative">
                <input
                  v-model="form.startTime"
                  type="time"
                  required
                  class="w-full px-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-medium text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden font-mono"
                />
              </div>
            </div>
            <div>
              <label class="block text-xs font-bold text-gray-700 mb-1.5">
                Giờ kết thúc <span class="text-red-500">*</span>
              </label>
              <div class="relative">
                <input
                  v-model="form.endTime"
                  type="time"
                  required
                  class="w-full px-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-medium text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden font-mono"
                />
              </div>
            </div>
          </div>

          <!-- Room Input -->
          <div>
            <label class="block text-xs font-bold text-gray-700 mb-1.5">
              Phòng học (Không bắt buộc)
            </label>
            <div class="relative">
              <input
                v-model="form.room"
                type="text"
                placeholder="Ví dụ: P201, Phòng Thực hành, Online qua Zoom..."
                maxlength="50"
                class="w-full px-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-medium text-gray-800 placeholder-gray-400 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden"
              />
            </div>
          </div>

          <!-- Helper tip -->
          <div class="p-3 bg-amber-50 rounded-xl border border-amber-100 text-[11px] text-amber-800 flex items-start gap-2">
            <InformationCircleIcon class="w-4 h-4 text-amber-500 flex-shrink-0 mt-0.5" />
            <span>
              Hệ thống sẽ tự động tính toán lại <strong>Ngày bế giảng (EndDate)</strong> của lớp học dựa trên số buổi và các ngày học trong tuần.
            </span>
          </div>

          <!-- Footer Actions for Edit/Create mode -->
          <div class="flex items-center justify-end gap-2 pt-4 border-t border-orange-100">
            <button
              type="button"
              @click="cancelEdit"
              :disabled="saving"
              class="px-4 py-2 text-xs font-semibold text-gray-600 hover:bg-gray-100 rounded-xl transition-colors disabled:opacity-50"
            >
              Hủy
            </button>
            <button
              type="submit"
              :disabled="saving"
              class="inline-flex items-center gap-1.5 px-4 py-2 bg-orange-500 hover:bg-orange-600 text-white text-xs font-semibold rounded-xl shadow-sm transition-colors disabled:opacity-50 cursor-pointer"
            >
              <svg v-if="saving" class="animate-spin h-3.5 w-3.5 text-white" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" fill="none" />
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
              </svg>
              <span>{{ saving ? 'Đang lưu...' : (mode === 'create' ? 'Tạo lịch học' : 'Lưu thay đổi') }}</span>
            </button>
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
  EyeIcon,
  PencilSquareIcon,
  CalendarIcon,
  MapPinIcon,
  ClockIcon,
  TrashIcon,
  ExclamationCircleIcon,
  CheckCircleIcon,
  InformationCircleIcon,
} from '@heroicons/vue/24/outline'

import classScheduleService from '@/services/classSchedule.service'

const props = defineProps({
  show: { type: Boolean, default: false },
  schedule: { type: Object, default: null },
  classes: { type: Array, default: () => [] },
  initialMode: { type: String, default: 'view' },
  defaultClassId: { type: [Number, String], default: '' },
})

const emit = defineEmits(['close', 'saved', 'deleted'])

const mode = ref(props.initialMode)
const saving = ref(false)
const deleting = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const form = ref({
  id: null,
  classId: '',
  dayOfWeek: 1,
  startTime: '17:30',
  endTime: '19:00',
  room: '',
})

// ─── Day of week maps ────────────────────────────────────────────────────────
const DAYS_OF_WEEK = [
  { value: 1, label: 'Thứ 2', full: 'Thứ Hai', color: 'border-blue-200 bg-blue-50 text-blue-700' },
  { value: 2, label: 'Thứ 3', full: 'Thứ Ba', color: 'border-emerald-200 bg-emerald-50 text-emerald-700' },
  { value: 3, label: 'Thứ 4', full: 'Thứ Tư', color: 'border-amber-200 bg-amber-50 text-amber-700' },
  { value: 4, label: 'Thứ 5', full: 'Thứ Năm', color: 'border-purple-200 bg-purple-50 text-purple-700' },
  { value: 5, label: 'Thứ 6', full: 'Thứ Sáu', color: 'border-rose-200 bg-rose-50 text-rose-700' },
  { value: 6, label: 'Thứ 7', full: 'Thứ Bảy', color: 'border-cyan-200 bg-cyan-50 text-cyan-700' },
  { value: 0, label: 'Chủ nhật', full: 'Chủ Nhật', color: 'border-red-200 bg-red-50 text-red-700' },
]

const dayInfo = computed(() => {
  const dow = props.schedule?.dayOfWeek ?? form.value.dayOfWeek
  return DAYS_OF_WEEK.find(d => d.value === Number(dow)) ?? {
    label: `Thứ ${dow}`,
    full: `Thứ ${dow}`,
    color: 'border-gray-200 bg-gray-50 text-gray-700',
  }
})

const durationText = computed(() => {
  const s = props.schedule?.startTime ?? form.value.startTime
  const e = props.schedule?.endTime ?? form.value.endTime
  if (!s || !e) return null
  const [sH, sM] = s.split(':').map(Number)
  const [eH, eM] = e.split(':').map(Number)
  if (isNaN(sH) || isNaN(eH)) return null
  const total = (eH * 60 + (eM || 0)) - (sH * 60 + (sM || 0))
  if (total <= 0) return null
  const h = Math.floor(total / 60)
  const m = total % 60
  if (h > 0 && m > 0) return `${h}h ${m}m (${total} phút)`
  if (h > 0) return `${h} giờ (${total} phút)`
  return `${total} phút`
})

// ─── Sync when schedule or props change ───────────────────────────────────────
watch(
  () => [props.schedule, props.initialMode, props.show],
  () => {
    errorMessage.value = ''
    successMessage.value = ''
    mode.value = props.initialMode

    if (props.schedule) {
      form.value = {
        id: props.schedule.id,
        classId: props.schedule.classId,
        dayOfWeek: props.schedule.dayOfWeek,
        startTime: formatTimeInput(props.schedule.startTime),
        endTime: formatTimeInput(props.schedule.endTime),
        room: props.schedule.room || '',
      }
    } else {
      form.value = {
        id: null,
        classId: props.defaultClassId && props.defaultClassId !== 'all' ? Number(props.defaultClassId) : (props.classes[0]?.id || ''),
        dayOfWeek: 1,
        startTime: '17:30',
        endTime: '19:00',
        room: '',
      }
    }
  },
  { immediate: true }
)

// ─── Format Helpers ───────────────────────────────────────────────────────────
function formatTimeDisplay(t) {
  if (!t) return '--:--'
  const parts = String(t).split(':')
  if (parts.length >= 2) return `${parts[0].padStart(2, '0')}:${parts[1].padStart(2, '0')}`
  return t
}

function formatTimeInput(t) {
  if (!t) return '17:30'
  const parts = String(t).split(':')
  if (parts.length >= 2) return `${parts[0].padStart(2, '0')}:${parts[1].padStart(2, '0')}`
  return t
}

function formatDateTime(d) {
  if (!d) return '--'
  return new Date(d).toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

// ─── Actions ──────────────────────────────────────────────────────────────────
function cancelEdit() {
  if (props.schedule) {
    mode.value = 'view'
  } else {
    handleClose()
  }
}

function handleClose() {
  errorMessage.value = ''
  successMessage.value = ''
  emit('close')
}

async function handleSubmit() {
  errorMessage.value = ''
  successMessage.value = ''

  // Validate StartTime < EndTime
  if (form.value.startTime >= form.value.endTime) {
    errorMessage.value = 'Giờ kết thúc phải muộn hơn giờ bắt đầu!'
    return
  }

  saving.value = true

  const payload = {
    classId: Number(form.value.classId),
    dayOfWeek: Number(form.value.dayOfWeek),
    startTime: form.value.startTime.length === 5 ? `${form.value.startTime}:00` : form.value.startTime,
    endTime: form.value.endTime.length === 5 ? `${form.value.endTime}:00` : form.value.endTime,
    room: form.value.room ? form.value.room.trim() : null,
  }

  try {
    if (mode.value === 'create') {
      const result = await classScheduleService.create(payload)
      successMessage.value = 'Thêm lịch học mới thành công!'
      setTimeout(() => {
        emit('saved', result)
        handleClose()
      }, 700)
    } else {
      await classScheduleService.update(form.value.id, payload)
      successMessage.value = 'Cập nhật lịch học thành công!'
      setTimeout(() => {
        emit('saved', { ...props.schedule, ...payload, id: form.value.id })
        mode.value = 'view'
      }, 700)
    }
  } catch (err) {
    console.error('Lỗi khi lưu lịch học:', err)
    errorMessage.value = err.response?.data?.message || err.message || 'Có lỗi xảy ra khi lưu lịch học. Vui lòng thử lại!'
  } finally {
    saving.value = false
  }
}

async function confirmDelete() {
  if (!props.schedule?.id) return
  if (!window.confirm(`Bạn có chắc chắn muốn xóa lịch học (${dayInfo.value.label} ${formatTimeDisplay(props.schedule.startTime)}) này không?`)) {
    return
  }

  deleting.value = true
  errorMessage.value = ''
  try {
    await classScheduleService.delete(props.schedule.id)
    emit('deleted', props.schedule.id)
    handleClose()
  } catch (err) {
    console.error('Lỗi khi xóa lịch học:', err)
    errorMessage.value = err.response?.data?.message || 'Không thể xóa lịch học này.'
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
