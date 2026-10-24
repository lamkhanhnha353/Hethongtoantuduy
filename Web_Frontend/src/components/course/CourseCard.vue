<template>
  <!-- Course card — Grid view -->
  <div
    @click="$emit('click', course)"
    class="group bg-white rounded-2xl border border-orange-100 shadow-sm hover:shadow-md hover:border-orange-300
           transition-all duration-200 cursor-pointer overflow-hidden flex flex-col"
  >
    <!-- Cover / color band -->
    <div
      :class="['h-2 w-full', colorBand]"
    />

    <div class="p-5 flex flex-col flex-1">
      <!-- Header row -->
      <div class="flex items-start justify-between gap-2 mb-3">
        <div class="flex-1 min-w-0">
          <!-- Code -->
          <span class="inline-block text-[11px] font-semibold text-orange-500 bg-orange-100 px-2 py-0.5 rounded-full mb-1">
            {{ course.code ?? `#${course.id}` }}
          </span>
          <!-- Name -->
          <h3 class="font-bold text-gray-800 leading-snug group-hover:text-orange-600 transition-colors line-clamp-2">
            {{ course.name }}
          </h3>
        </div>
        <!-- Status badge -->
        <StatusBadge :status="course.status" />
      </div>

      <!-- Description -->
      <p v-if="course.description" class="text-xs text-gray-500 line-clamp-2 mb-4 flex-1">
        {{ course.description }}
      </p>
      <div v-else class="flex-1" />

      <!-- Meta row -->
      <div class="flex items-center justify-between text-xs text-gray-400 border-t border-orange-50 pt-3 mt-auto">
        <div class="flex items-center gap-1">
          <AcademicCapIcon class="w-3.5 h-3.5 text-orange-300" />
          <span>{{ course.totalLevels ?? 0 }} cấp độ</span>
        </div>
        <div class="flex items-center gap-1">
          <UserGroupIcon class="w-3.5 h-3.5 text-orange-300" />
          <span>{{ course.totalStudents ?? 0 }} học viên</span>
        </div>
        <div v-if="course.fee" class="flex items-center gap-1 font-semibold text-orange-500">
          <CurrencyDollarIcon class="w-3.5 h-3.5" />
          <span>{{ formatFee(course.fee) }}</span>
        </div>
      </div>
    </div>

    <!-- Arrow hint -->
    <div class="px-5 pb-4">
      <div class="flex items-center gap-1 text-xs text-orange-400 group-hover:text-orange-600 transition-colors font-medium">
        <span>Xem chi tiết</span>
        <ArrowRightIcon class="w-3.5 h-3.5 group-hover:translate-x-1 transition-transform" />
      </div>
    </div>
  </div>
</template>

<script setup>
import {
  AcademicCapIcon,
  UserGroupIcon,
  CurrencyDollarIcon,
  ArrowRightIcon,
} from '@heroicons/vue/24/outline'
import StatusBadge from './StatusBadge.vue'

const props = defineProps({
  course: { type: Object, required: true },
  index: { type: Number, default: 0 },
})
defineEmits(['click'])

const bands = [
  'bg-orange-400', 'bg-blue-400', 'bg-green-400',
  'bg-purple-400', 'bg-pink-400', 'bg-teal-400',
]
const colorBand = bands[props.index % bands.length]

function formatFee(fee) {
  return Number(fee).toLocaleString('vi-VN') + 'đ'
}
</script>
