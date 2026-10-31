<template>
  <!-- Course card — Grid view -->
  <div
    @click="$emit('click', course)"
    class="group bg-white rounded-2xl border border-orange-100 shadow-sm hover:shadow-md hover:border-orange-300
           transition-all duration-200 cursor-pointer overflow-hidden flex flex-col"
  >
    <!-- Cover image or color band -->
    <div v-if="course.imageUrl" class="relative h-44 w-full overflow-hidden bg-orange-100 flex-shrink-0">
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
      <div class="absolute inset-0 bg-gradient-to-t from-black/60 via-black/10 to-transparent pointer-events-none" />
      <span class="absolute top-3 left-3 text-[11px] font-bold text-orange-700 bg-white/95 backdrop-blur-sm px-2.5 py-0.5 rounded-full shadow-sm">
        {{ course.code ?? `#${course.id}` }}
      </span>
      <div class="absolute top-3 right-3">
        <StatusBadge :status="course.status" />
      </div>
      <div v-if="course.minAge || course.maxAge" class="absolute bottom-2.5 left-3 text-xs text-white font-medium drop-shadow-sm flex items-center gap-1">
        <span>Độ tuổi: {{ course.minAge ?? '?' }} - {{ course.maxAge ?? '?' }} tuổi</span>
      </div>
    </div>
    <div
      v-else
      :class="['h-2 w-full', colorBand]"
    />

    <div class="p-5 flex flex-col flex-1">
      <!-- Header row when no cover image -->
      <div v-if="!course.imageUrl" class="flex items-start justify-between gap-2 mb-3">
        <div class="flex-1 min-w-0">
          <span class="inline-block text-[11px] font-semibold text-orange-500 bg-orange-100 px-2 py-0.5 rounded-full mb-1">
            {{ course.code ?? `#${course.id}` }}
          </span>
          <h3 class="font-bold text-gray-800 leading-snug group-hover:text-orange-600 transition-colors line-clamp-2">
            {{ course.name }}
          </h3>
        </div>
        <StatusBadge :status="course.status" />
      </div>
      <h3 v-else class="font-bold text-gray-800 leading-snug group-hover:text-orange-600 transition-colors line-clamp-2 mb-2">
        {{ course.name }}
      </h3>

      <!-- Description -->
      <p v-if="course.description" class="text-xs text-gray-500 line-clamp-2 mb-4 flex-1">
        {{ course.description }}
      </p>
      <div v-else class="flex-1" />

      <!-- Meta row -->
      <div class="flex items-center justify-between text-xs text-gray-400 border-t border-orange-50 pt-3 mt-auto">
        <div class="flex items-center gap-1">
          <AcademicCapIcon class="w-3.5 h-3.5 text-orange-400" />
          <span>{{ totalLevelsCount }} cấp độ</span>
        </div>
        <div v-if="course.totalStudents" class="flex items-center gap-1">
          <UserGroupIcon class="w-3.5 h-3.5 text-orange-400" />
          <span>{{ course.totalStudents }} HV</span>
        </div>
        <div v-else-if="!course.imageUrl && (course.minAge || course.maxAge)" class="flex items-center gap-1">
          <span>{{ course.minAge }}-{{ course.maxAge }} tuổi</span>
        </div>
        <div v-if="feeDisplay" class="flex items-center gap-1 font-semibold text-orange-600">
          <CurrencyDollarIcon class="w-3.5 h-3.5" />
          <span>{{ feeDisplay }}</span>
        </div>
      </div>
    </div>

    <!-- Actions row -->
    <div class="px-5 pb-4 flex items-center justify-between border-t border-orange-50 pt-3">
      <button
        type="button"
        @click.stop="$emit('edit', course)"
        class="inline-flex items-center gap-1 text-xs text-gray-500 hover:text-orange-600 font-medium py-1 px-2 rounded-lg hover:bg-orange-50 transition-colors cursor-pointer"
      >
        <PencilSquareIcon class="w-3.5 h-3.5 text-orange-500" />
        <span>Sửa</span>
      </button>

      <div class="flex items-center gap-1 text-xs text-orange-500 group-hover:text-orange-600 transition-colors font-medium">
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
