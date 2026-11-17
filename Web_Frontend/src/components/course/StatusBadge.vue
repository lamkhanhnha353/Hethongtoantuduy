<template>
  <!-- Status badge chip -->
  <span
    :class="[
      'inline-flex items-center gap-1.5 text-[11px] font-semibold px-2 py-0.5 rounded border flex-shrink-0',
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
  active:   { label: 'Đang mở',    class: 'bg-emerald-50 text-emerald-800 border-emerald-300', dot: 'bg-emerald-600' },
  inactive: { label: 'Tạm dừng',   class: 'bg-stone-100 text-stone-700 border-stone-300',     dot: 'bg-stone-400' },
  draft:    { label: 'Nháp',        class: 'bg-amber-50 text-amber-800 border-amber-300',       dot: 'bg-amber-500' },
  full:     { label: 'Đã đầy',      class: 'bg-rose-50 text-rose-800 border-rose-300',         dot: 'bg-rose-600' },
  upcoming: { label: 'Sắp mở',     class: 'bg-orange-50 text-orange-800 border-orange-300',    dot: 'bg-orange-600' },
}
const FALLBACK = { label: 'Không rõ', class: 'bg-stone-100 text-stone-700 border-stone-300', dot: 'bg-stone-400' }

const cfg = computed(() => {
  const key = String(props.status ?? '').toLowerCase()
  return MAP[key] ?? FALLBACK
})
</script>
