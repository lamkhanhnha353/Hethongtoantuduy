<template>
  <!-- List / Grid toggle buttons -->
  <div class="flex items-center gap-1 bg-stone-100 border border-stone-300 rounded-md p-0.5">
    <button
      v-for="mode in modes"
      :key="mode.value"
      @click="$emit('update:modelValue', mode.value)"
      :class="[
        'flex items-center justify-center w-8 h-8 rounded transition-colors duration-150',
        modelValue === mode.value
          ? 'bg-orange-600 text-white shadow-xs'
          : 'text-stone-600 hover:bg-stone-200/70 hover:text-stone-900',
      ]"
      :title="mode.label"
    >
      <component :is="mode.icon" class="w-4 h-4" />
    </button>
  </div>
</template>

<script setup>
import { Bars3Icon, Squares2X2Icon } from '@heroicons/vue/24/outline'

defineProps({
  modelValue: { type: String, default: 'grid' },
})
defineEmits(['update:modelValue'])

const modes = [
  { value: 'list', label: 'Dạng danh sách', icon: Bars3Icon },
  { value: 'grid', label: 'Dạng lưới', icon: Squares2X2Icon },
]
</script>
