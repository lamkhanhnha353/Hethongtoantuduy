<template>
  <!-- Status badge chip -->
  <span
    :class="[
      'inline-flex items-center gap-1 text-[11px] font-semibold px-2 py-0.5 rounded-full flex-shrink-0',
      cfg.class,
    ]"
  >
    <span class="w-1.5 h-1.5 rounded-full" :class="cfg.dot" />
    {{ cfg.label }}
  </span>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  status: { type: [String, Number], default: null },
})

const MAP = {
  active:   { label: 'Đang mở',    class: 'bg-green-100 text-green-700',   dot: 'bg-green-500' },
  inactive: { label: 'Tạm dừng',   class: 'bg-gray-100 text-gray-500',     dot: 'bg-gray-400' },
  draft:    { label: 'Nháp',        class: 'bg-yellow-100 text-yellow-700', dot: 'bg-yellow-500' },
  full:     { label: 'Đã đầy',      class: 'bg-red-100 text-red-600',       dot: 'bg-red-500' },
  upcoming: { label: 'Sắp mở',     class: 'bg-blue-100 text-blue-600',     dot: 'bg-blue-500' },
}
const FALLBACK = { label: 'Không rõ', class: 'bg-orange-100 text-orange-600', dot: 'bg-orange-400' }

const cfg = computed(() => {
  const key = String(props.status ?? '').toLowerCase()
  return MAP[key] ?? FALLBACK
})
</script>
