<template>
  <!-- Class row — List view -->
  <div
    @click="$emit('click', cls)"
    class="group flex items-center gap-4 bg-white rounded-lg border border-stone-300 px-5 py-3.5
           shadow-[0_1px_2px_rgba(0,0,0,0.04)] hover:border-orange-500 transition-colors duration-150 cursor-pointer"
  >
    <!-- Code -->
    <span class="hidden sm:block text-xs font-mono font-semibold text-orange-800 bg-orange-50 px-2 py-1 rounded w-20 text-center flex-shrink-0 border border-orange-200 tabular-nums">
      {{ cls.code ?? `LH-${cls.id}` }}
    </span>

    <!-- Name -->
    <div class="flex-1 min-w-0">
      <p class="font-bold text-stone-900 group-hover:text-orange-600 transition-colors truncate">
        {{ cls.name }}
      </p>
      <p class="text-xs text-stone-500 truncate mt-0.5 tabular-nums">
        GV: {{ cls.teacherName ?? 'Chưa phân công' }}
        <template v-if="cls.courseName"> · {{ cls.courseName }}</template>
        <template v-else-if="cls.courseLevelName"> · {{ cls.courseLevelName }}</template>
        <template v-if="cls.startDate"> · {{ formatDate(cls.startDate) }}</template>
      </p>
    </div>

    <!-- Capacity -->
    <div class="hidden md:flex items-center gap-1 text-xs font-medium text-stone-600 flex-shrink-0 tabular-nums">
      <UserGroupIcon class="w-3.5 h-3.5 text-orange-600" />
      {{ currentStudentsCount }}/{{ maxCapacityDisplay }} HV
    </div>

    <!-- Room -->
    <div v-if="cls.room" class="hidden lg:flex items-center gap-1 text-xs text-stone-500 flex-shrink-0 w-24">
      <MapPinIcon class="w-3.5 h-3.5 text-orange-600" />
      <span class="truncate">{{ cls.room }}</span>
    </div>

    <!-- Status -->
    <StatusBadge :status="cls.status" />

    <!-- Edit action button -->
    <button
      type="button"
      @click.stop="$emit('edit', cls)"
      class="inline-flex items-center justify-center text-stone-700 bg-white border border-stone-300 hover:bg-stone-100 hover:border-stone-400 p-1.5 rounded-md transition-colors flex-shrink-0 cursor-pointer"
      title="Chỉnh sửa lớp học"
    >
      <PencilSquareIcon class="w-4 h-4 text-orange-600" />
    </button>

    <!-- Arrow -->
    <ArrowRightIcon class="w-4 h-4 text-stone-400 group-hover:text-orange-600 group-hover:translate-x-1 transition-all flex-shrink-0" />
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { UserGroupIcon, MapPinIcon, ArrowRightIcon, PencilSquareIcon } from '@heroicons/vue/24/outline'
import StatusBadge from '@/components/course/StatusBadge.vue'

const props = defineProps({
  cls: { type: Object, required: true },
})
defineEmits(['click', 'edit'])

const currentStudentsCount = computed(() => props.cls.activeStudentsCount ?? props.cls.currentStudents ?? 0)
const maxCapacityDisplay = computed(() => props.cls.maxCapacity ?? props.cls.maxStudents ?? '∞')

function formatDate(d) {
  return new Date(d).toLocaleDateString('vi-VN')
}
</script>
