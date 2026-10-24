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
        <span class="text-[11px] font-mono font-semibold text-orange-400 bg-orange-50 px-2 py-0.5 rounded-full border border-orange-200">
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
          <UserGroupIcon class="w-3.5 h-3.5 text-orange-300 flex-shrink-0" />
          <span>{{ cls.currentStudents ?? 0 }}/{{ cls.maxStudents ?? '∞' }} HV</span>
        </div>
        <div class="flex items-center gap-1.5">
          <AcademicCapIcon class="w-3.5 h-3.5 text-orange-300 flex-shrink-0" />
          <span class="truncate">{{ cls.teacherName ?? 'Chưa có GV' }}</span>
        </div>
        <div v-if="cls.startDate" class="flex items-center gap-1.5">
          <CalendarIcon class="w-3.5 h-3.5 text-orange-300 flex-shrink-0" />
          <span>{{ formatDate(cls.startDate) }}</span>
        </div>
        <div v-if="cls.room" class="flex items-center gap-1.5">
          <MapPinIcon class="w-3.5 h-3.5 text-orange-300 flex-shrink-0" />
          <span class="truncate">{{ cls.room }}</span>
        </div>
      </div>

      <!-- Capacity bar -->
      <div v-if="cls.maxStudents">
        <div class="flex justify-between text-[11px] text-gray-400 mb-1">
          <span>Sĩ số</span>
          <span>{{ Math.round(fillPct) }}%</span>
        </div>
        <div class="h-1.5 bg-orange-100 rounded-full overflow-hidden">
          <div
            :class="['h-full rounded-full transition-all duration-500', fillPct >= 90 ? 'bg-red-400' : 'bg-orange-400']"
            :style="{ width: fillPct + '%' }"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import {
  UserGroupIcon, AcademicCapIcon, CalendarIcon, MapPinIcon,
} from '@heroicons/vue/24/outline'
import StatusBadge from '@/components/course/StatusBadge.vue'

const props = defineProps({
  cls: { type: Object, required: true },
})
defineEmits(['click'])

const fillPct = computed(() => {
  if (!props.cls.maxStudents) return 0
  return Math.min(100, Math.round((props.cls.currentStudents ?? 0) / props.cls.maxStudents * 100))
})

function formatDate(d) {
  return new Date(d).toLocaleDateString('vi-VN')
}
</script>
