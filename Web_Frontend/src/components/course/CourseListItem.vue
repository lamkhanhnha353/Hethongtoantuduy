<template>
  <!-- Course row — List view -->
  <div
    @click="$emit('click', course)"
    class="group flex items-center gap-4 bg-white rounded-xl border border-orange-100 px-5 py-4
           hover:border-orange-300 hover:shadow-sm transition-all duration-150 cursor-pointer"
  >
    <!-- Thumbnail or color bar -->
    <div v-if="course.imageUrl" class="w-14 h-14 rounded-xl overflow-hidden bg-orange-100 flex-shrink-0 border border-orange-100">
      <img
        :src="course.imageUrl"
        :alt="course.name"
        class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-200"
        loading="lazy"
        @error="hasImageError = true"
        v-show="!hasImageError"
      />
      <div v-if="hasImageError" :class="['w-full h-full flex items-center justify-center text-white text-xs font-bold', colorBand]">
        {{ course.code ?? 'KH' }}
      </div>
    </div>
    <div v-else :class="['w-1 self-stretch rounded-full flex-shrink-0', colorBand]" />

    <!-- Code -->
    <span class="hidden sm:inline-block text-xs font-mono font-semibold text-orange-600 bg-orange-50 px-2.5 py-1 rounded-lg border border-orange-200 w-24 text-center flex-shrink-0">
      {{ course.code ?? `#${course.id}` }}
    </span>

    <!-- Name + desc -->
    <div class="flex-1 min-w-0">
      <div class="flex items-center gap-2">
        <p class="font-semibold text-gray-800 group-hover:text-orange-600 transition-colors truncate">
          {{ course.name }}
        </p>
        <span v-if="course.minAge || course.maxAge" class="hidden md:inline-block text-[10px] font-medium text-orange-600 bg-orange-100 px-1.5 py-0.5 rounded-full flex-shrink-0">
          {{ course.minAge }}-{{ course.maxAge }} tuổi
        </span>
      </div>
      <p v-if="course.description" class="text-xs text-gray-400 truncate mt-0.5">
        {{ course.description }}
      </p>
    </div>

    <!-- Meta chips -->
    <div class="hidden md:flex items-center gap-4 text-xs text-gray-400 flex-shrink-0">
      <span class="flex items-center gap-1">
        <AcademicCapIcon class="w-3.5 h-3.5 text-orange-400" />
        {{ totalLevelsCount }} cấp độ
      </span>
      <span v-if="feeDisplay" class="flex items-center gap-1 font-semibold text-orange-600">
        <CurrencyDollarIcon class="w-3.5 h-3.5" />
        {{ feeDisplay }}
      </span>
    </div>

    <!-- Status -->
    <StatusBadge :status="course.status" />

    <!-- Edit action button -->
    <button
      type="button"
      @click.stop="$emit('edit', course)"
      class="text-gray-400 hover:text-orange-600 p-1.5 rounded-lg hover:bg-orange-50 transition-colors flex-shrink-0 cursor-pointer"
      title="Chỉnh sửa khóa học"
    >
      <PencilSquareIcon class="w-4 h-4 text-orange-500" />
    </button>

    <!-- Arrow -->
    <ArrowRightIcon class="w-4 h-4 text-orange-300 group-hover:text-orange-500 group-hover:translate-x-1 transition-all flex-shrink-0" />
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import {
  AcademicCapIcon,
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

const bands = ['bg-orange-400','bg-blue-400','bg-green-400','bg-purple-400','bg-pink-400','bg-teal-400']
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
