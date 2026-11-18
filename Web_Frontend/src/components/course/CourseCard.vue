<template>
  <!-- Course card — Grid view -->
  <div
    @click="$emit('click', course)"
    class="group bg-white rounded-lg border border-stone-300 shadow-[0_1px_2px_rgba(0,0,0,0.04)] hover:border-orange-500
           transition-colors duration-150 cursor-pointer overflow-hidden flex flex-col"
  >
    <!-- Cover image or color band -->
    <div v-if="course.imageUrl" class="relative h-44 w-full overflow-hidden bg-stone-100 border-b border-stone-200 flex-shrink-0">
      <img
        :src="course.imageUrl"
        :alt="course.name"
        class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
        loading="lazy"
        @error="hasImageError = true"
        v-show="!hasImageError"
      />
      <div v-if="hasImageError" :class="['w-full h-full flex items-center justify-center text-white font-bold', colorBand]">
        {{ course.code ?? course.name }}
      </div>
      <div class="absolute inset-0 bg-black/25 pointer-events-none" />
      <span class="absolute top-3 left-3 text-[11px] font-mono font-bold text-stone-800 bg-white/95 border border-stone-300 px-2 py-0.5 rounded shadow-xs tabular-nums">
        {{ course.code ?? `#${course.id}` }}
      </span>
      <div class="absolute top-3 right-3">
        <StatusBadge :status="course.status" />
      </div>
      <div v-if="course.minAge || course.maxAge" class="absolute bottom-2.5 left-3 text-xs text-white font-semibold drop-shadow-sm flex items-center gap-1 tabular-nums">
        <span>Độ tuổi: {{ course.minAge ?? '?' }} - {{ course.maxAge ?? '?' }} tuổi</span>
      </div>
    </div>
    <div
      v-else
      :class="['h-1.5 w-full', colorBand]"
    />

    <div class="p-5 flex flex-col flex-1">
      <!-- Header row when no cover image -->
      <div v-if="!course.imageUrl" class="flex items-start justify-between gap-2 mb-3">
        <div class="flex-1 min-w-0">
          <span class="inline-block text-[11px] font-mono font-semibold text-orange-800 bg-orange-50 border border-orange-200 px-2 py-0.5 rounded mb-1.5 tabular-nums">
            {{ course.code ?? `#${course.id}` }}
          </span>
          <h3 class="font-bold text-stone-900 leading-snug group-hover:text-orange-600 transition-colors line-clamp-2">
            {{ course.name }}
          </h3>
        </div>
        <StatusBadge :status="course.status" />
      </div>
      <h3 v-else class="font-bold text-stone-900 leading-snug group-hover:text-orange-600 transition-colors line-clamp-2 mb-2">
        {{ course.name }}
      </h3>

      <!-- Description -->
      <p v-if="course.description" class="text-xs text-stone-500 line-clamp-2 mb-4 flex-1">
        {{ course.description }}
      </p>
      <div v-else class="flex-1" />

      <!-- Meta row -->
      <div class="flex items-center justify-between text-xs text-stone-500 border-t border-stone-200 pt-3 mt-auto tabular-nums">
        <div class="flex items-center gap-1">
          <AcademicCapIcon class="w-3.5 h-3.5 text-orange-600" />
          <span>{{ totalLevelsCount }} cấp độ</span>
        </div>
        <div v-if="course.totalStudents" class="flex items-center gap-1">
          <UserGroupIcon class="w-3.5 h-3.5 text-orange-600" />
          <span>{{ course.totalStudents }} HV</span>
        </div>
        <div v-else-if="!course.imageUrl && (course.minAge || course.maxAge)" class="flex items-center gap-1">
          <span>{{ course.minAge }}-{{ course.maxAge }} tuổi</span>
        </div>
        <div v-if="feeDisplay" class="flex items-center gap-1 font-bold text-orange-700">
          <CurrencyDollarIcon class="w-3.5 h-3.5" />
          <span>{{ feeDisplay }}</span>
        </div>
      </div>
    </div>

    <!-- Actions row -->
    <div class="px-5 pb-4 flex items-center justify-between border-t border-stone-200 pt-3 bg-stone-50/50">
      <button
        type="button"
        @click.stop="$emit('edit', course)"
        class="inline-flex items-center gap-1 text-xs font-semibold text-stone-800 bg-white border border-stone-300 hover:bg-stone-100 hover:border-stone-400 py-1 px-2.5 rounded-md transition-colors cursor-pointer"
      >
        <PencilSquareIcon class="w-3.5 h-3.5 text-orange-600" />
        <span>Sửa</span>
      </button>

      <div class="flex items-center gap-1 text-xs text-orange-600 group-hover:text-orange-700 transition-colors font-semibold">
        <span>Xem chi tiết</span>
        <ArrowRightIcon class="w-3.5 h-3.5 group-hover:translate-x-1 transition-transform" />
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import {
  AcademicCapIcon,
  UserGroupIcon,
  CurrencyDollarIcon,
  ArrowRightIcon,
  PencilSquareIcon,
} from '@heroicons/vue/24/outline'
import StatusBadge from './StatusBadge.vue'

const props = defineProps({
  course: { type: Object, required: true },
  index: { type: Number, default: 0 },
})
defineEmits(['click', 'edit'])

const hasImageError = ref(false)

const bands = [
  'bg-orange-400', 'bg-blue-400', 'bg-green-400',
  'bg-purple-400', 'bg-pink-400', 'bg-teal-400',
]
const colorBand = bands[props.index % bands.length]

const totalLevelsCount = computed(() => {
  if (props.course.totalLevels != null) return props.course.totalLevels
  if (Array.isArray(props.course.courseLevels)) return props.course.courseLevels.length
  return 0
})

const feeDisplay = computed(() => {
  if (props.course.fee != null && props.course.fee > 0) {
    if (props.course.minFee && props.course.maxFee && props.course.minFee !== props.course.maxFee) {
      return `Từ ${formatFee(props.course.minFee)}`
    }
    return formatFee(props.course.fee)
  }
  const levels = props.course.courseLevels
  if (levels && levels.length > 0) {
    const fees = levels.map(l => Number(l.fee)).filter(f => !isNaN(f) && f > 0)
    if (fees.length > 0) {
      const min = Math.min(...fees)
      const max = Math.max(...fees)
      return min === max ? formatFee(min) : `Từ ${formatFee(min)}`
    }
  }
  return null
})

function formatFee(fee) {
  return Number(fee).toLocaleString('vi-VN') + 'đ'
}
</script>
