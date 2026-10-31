<template>
  <!-- Class card — Grid view -->
  <div
    @click="$emit('click', cls)"
    class="group bg-white rounded-2xl border border-orange-100 shadow-sm hover:shadow-md hover:border-orange-300
           transition-all duration-200 cursor-pointer overflow-hidden"
  >
    <!-- Top accent -->
    <div class="h-1.5 w-full bg-gradient-to-r from-orange-300 to-orange-400" />

    <div class="p-5">
      <!-- Code + Status -->
      <div class="flex items-center justify-between mb-2">
        <span class="text-[11px] font-mono font-semibold text-orange-500 bg-orange-50 px-2 py-0.5 rounded-full border border-orange-200">
          {{ cls.code ?? `LH-${cls.id}` }}
        </span>
        <StatusBadge :status="cls.status" />
      </div>

      <!-- Name -->
      <h3 class="font-bold text-gray-800 group-hover:text-orange-600 transition-colors line-clamp-2 mb-3">
        {{ cls.name }}
      </h3>

      <!-- Info grid -->
      <div class="grid grid-cols-2 gap-2 text-xs text-gray-500 mb-4">
        <div class="flex items-center gap-1.5">
          <UserGroupIcon class="w-3.5 h-3.5 text-orange-400 flex-shrink-0" />
          <span>{{ currentStudentsCount }}/{{ maxCapacityDisplay }} HV</span>
        </div>
        <div class="flex items-center gap-1.5">
          <AcademicCapIcon class="w-3.5 h-3.5 text-orange-400 flex-shrink-0" />
          <span class="truncate">{{ cls.teacherName ?? 'Chưa có GV' }}</span>
        </div>
        <div v-if="cls.startDate" class="flex items-center gap-1.5">
          <CalendarIcon class="w-3.5 h-3.5 text-orange-400 flex-shrink-0" />
          <span>{{ formatDate(cls.startDate) }}</span>
        </div>
        <div v-if="cls.room || cls.courseLevelName" class="flex items-center gap-1.5">
          <MapPinIcon class="w-3.5 h-3.5 text-orange-400 flex-shrink-0" />
          <span class="truncate">{{ cls.room ?? cls.courseLevelName }}</span>
        </div>
      </div>

      <!-- Capacity bar -->
      <div v-if="hasCapacity">
        <div class="flex justify-between text-[11px] text-gray-400 mb-1">
          <span>Sĩ số</span>
          <span>{{ Math.round(fillPct) }}%</span>
        </div>
        <div class="h-1.5 bg-orange-100 rounded-full overflow-hidden">
          <div
            :class="['h-full rounded-full transition-all duration-500', fillPct >= 90 ? 'bg-red-400' : 'bg-orange-500']"
            :style="{ width: fillPct + '%' }"
          />
        </div>
      </div>
    </div>

    <!-- Actions row -->
    <div class="px-5 pb-3 pt-2 border-t border-orange-50 flex items-center justify-between text-xs mt-auto">
      <router-link
        :to="{ name: 'schedule', query: { classId: cls.id } }"
        @click.stop
        class="inline-flex items-center gap-1 text-[11px] font-semibold text-orange-600 hover:text-orange-700 hover:underline"
      >
        <CalendarDaysIcon class="w-3.5 h-3.5 text-orange-500" />
        <span>Thời khóa biểu</span>
      </router-link>

      <button
        type="button"
        @click.stop="$emit('edit', cls)"
        class="inline-flex items-center gap-1 text-[11px] font-semibold text-gray-500 hover:text-orange-600 py-1 px-2 rounded-lg hover:bg-orange-50 transition-colors cursor-pointer"
        title="Chỉnh sửa lớp học"
      >
        <PencilSquareIcon class="w-3.5 h-3.5 text-orange-500" />
        <span>Sửa</span>
      </button>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import {
  UserGroupIcon, AcademicCapIcon, CalendarIcon, MapPinIcon, CalendarDaysIcon, PencilSquareIcon,
} from '@heroicons/vue/24/outline'
import StatusBadge from '@/components/course/StatusBadge.vue'

const props = defineProps({
  cls: { type: Object, required: true },
})
defineEmits(['click', 'edit'])

const currentStudentsCount = computed(() => props.cls.activeStudentsCount ?? props.cls.currentStudents ?? 0)
const maxCapacity = computed(() => props.cls.maxCapacity ?? props.cls.maxStudents)
const maxCapacityDisplay = computed(() => maxCapacity.value ?? '∞')
const hasCapacity = computed(() => Boolean(maxCapacity.value))

const fillPct = computed(() => {
  if (!maxCapacity.value) return 0
  return Math.min(100, Math.round(currentStudentsCount.value / maxCapacity.value * 100))
})

function formatDate(d) {
  return new Date(d).toLocaleDateString('vi-VN')
}
</script>
