<template>
  <!-- Class card — Grid view -->
  <div
    @click="$emit('click', cls)"
    class="group bg-white rounded-lg border border-stone-300 shadow-[0_1px_2px_rgba(0,0,0,0.04)] hover:border-orange-500
           transition-colors duration-150 cursor-pointer overflow-hidden flex flex-col"
  >
    <!-- Top accent -->
    <div class="h-1 w-full bg-orange-600" />

    <div class="p-5 flex-1">
      <!-- Code + Status -->
      <div class="flex items-center justify-between mb-2">
        <span class="text-[11px] font-mono font-semibold text-orange-800 bg-orange-50 px-2 py-0.5 rounded border border-orange-200 tabular-nums">
          {{ cls.code ?? `LH-${cls.id}` }}
        </span>
        <StatusBadge :status="cls.status" />
      </div>

      <!-- Name -->
      <h3 class="font-bold text-stone-900 group-hover:text-orange-600 transition-colors line-clamp-2 mb-1">
        {{ cls.name }}
      </h3>
      <p v-if="cls.courseName" class="text-[11px] text-orange-700 font-semibold truncate mb-2.5">
        {{ cls.courseName }}
      </p>

      <!-- Info grid -->
      <div class="grid grid-cols-2 gap-2 text-xs text-stone-600 mb-4 tabular-nums">
        <div class="flex items-center gap-1.5">
          <UserGroupIcon class="w-3.5 h-3.5 text-orange-600 flex-shrink-0" />
          <span>{{ currentStudentsCount }}/{{ maxCapacityDisplay }} HV</span>
        </div>
        <div class="flex items-center gap-1.5">
          <AcademicCapIcon class="w-3.5 h-3.5 text-orange-600 flex-shrink-0" />
          <span class="truncate">{{ cls.teacherName ?? 'Chưa có GV' }}</span>
        </div>
        <div v-if="cls.startDate" class="flex items-center gap-1.5">
          <CalendarIcon class="w-3.5 h-3.5 text-orange-600 flex-shrink-0" />
          <span>{{ formatDate(cls.startDate) }}</span>
        </div>
        <div v-if="cls.room || cls.courseLevelName" class="flex items-center gap-1.5">
          <MapPinIcon class="w-3.5 h-3.5 text-orange-600 flex-shrink-0" />
          <span class="truncate">{{ cls.room ?? cls.courseLevelName }}</span>
        </div>
      </div>

      <!-- Capacity bar -->
      <div v-if="hasCapacity">
        <div class="flex justify-between text-[11px] text-stone-500 font-medium mb-1 tabular-nums">
          <span>Sĩ số</span>
          <span>{{ Math.round(fillPct) }}%</span>
        </div>
        <div class="h-1.5 bg-stone-200 rounded overflow-hidden">
          <div
            :class="['h-full rounded transition-all duration-500', fillPct >= 90 ? 'bg-rose-600' : 'bg-orange-600']"
            :style="{ width: fillPct + '%' }"
          />
        </div>
      </div>
    </div>

    <!-- Actions row -->
    <div class="px-5 py-3 border-t border-stone-200 bg-stone-50/50 flex items-center justify-between text-xs mt-auto">
      <router-link
        :to="{ name: 'schedule', query: { classId: cls.id } }"
        @click.stop
        class="inline-flex items-center gap-1 text-[11px] font-semibold text-orange-600 hover:text-orange-700 hover:underline"
      >
        <CalendarDaysIcon class="w-3.5 h-3.5 text-orange-600" />
        <span>Thời khóa biểu</span>
      </router-link>

      <div class="flex items-center gap-2">
        <button
          type="button"
          @click.stop="$emit('edit', cls)"
          class="inline-flex items-center gap-1 text-[11px] font-semibold text-stone-800 bg-white border border-stone-300 hover:bg-stone-100 hover:border-stone-400 py-1 px-2.5 rounded-md transition-colors cursor-pointer"
          title="Chỉnh sửa lớp học"
        >
          <PencilSquareIcon class="w-3.5 h-3.5 text-orange-600" />
          <span>Sửa</span>
        </button>

        <span class="text-[11px] text-orange-600 group-hover:translate-x-0.5 transition-transform font-semibold">
          Chi tiết →
        </span>
      </div>
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
