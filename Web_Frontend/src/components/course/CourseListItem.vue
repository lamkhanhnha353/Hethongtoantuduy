<template>
  <!-- Course row — List view -->
  <div
    @click="$emit('click', course)"
    class="group flex items-center gap-4 bg-white rounded-xl border border-orange-100 px-5 py-4
           hover:border-orange-300 hover:shadow-sm transition-all duration-150 cursor-pointer"
  >
    <!-- Color dot -->
    <div :class="['w-1 self-stretch rounded-full flex-shrink-0', colorBand]" />

    <!-- Code -->
    <span class="hidden sm:block text-xs font-mono font-semibold text-orange-400 w-20 flex-shrink-0">
      {{ course.code ?? `#${course.id}` }}
    </span>

    <!-- Name + desc -->
    <div class="flex-1 min-w-0">
      <p class="font-semibold text-gray-800 group-hover:text-orange-600 transition-colors truncate">
        {{ course.name }}
      </p>
      <p v-if="course.description" class="text-xs text-gray-400 truncate mt-0.5">
        {{ course.description }}
      </p>
    </div>

    <!-- Meta chips -->
    <div class="hidden md:flex items-center gap-4 text-xs text-gray-400 flex-shrink-0">
      <span class="flex items-center gap-1">
        <AcademicCapIcon class="w-3.5 h-3.5 text-orange-300" />
        {{ course.totalLevels ?? 0 }} cấp độ
      </span>
      <span class="flex items-center gap-1">
        <UserGroupIcon class="w-3.5 h-3.5 text-orange-300" />
        {{ course.totalStudents ?? 0 }} HV
      </span>
    </div>

    <!-- Status -->
    <StatusBadge :status="course.status" />

    <!-- Arrow -->
    <ArrowRightIcon class="w-4 h-4 text-orange-300 group-hover:text-orange-500 group-hover:translate-x-1 transition-all flex-shrink-0" />
  </div>
</template>

<script setup>
import { AcademicCapIcon, UserGroupIcon, ArrowRightIcon } from '@heroicons/vue/24/outline'
import StatusBadge from './StatusBadge.vue'

const props = defineProps({
  course: { type: Object, required: true },
  index: { type: Number, default: 0 },
})
defineEmits(['click'])

const bands = ['bg-orange-400','bg-blue-400','bg-green-400','bg-purple-400','bg-pink-400','bg-teal-400']
const colorBand = bands[props.index % bands.length]
</script>
