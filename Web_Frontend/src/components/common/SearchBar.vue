<template>
  <!-- Search bar with icon -->
  <div class="relative flex-1 min-w-0">
    <MagnifyingGlassIcon
      class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-orange-400 pointer-events-none"
    />
    <input
      :value="modelValue"
      @input="$emit('update:modelValue', $event.target.value)"
      type="text"
      :placeholder="placeholder"
      class="w-full pl-9 pr-4 py-2.5 text-sm bg-white border border-orange-200 rounded-xl
             text-gray-700 placeholder-gray-400
             focus:outline-none focus:ring-2 focus:ring-orange-300 focus:border-orange-300
             transition-all duration-150"
    />
    <!-- Clear button -->
    <button
      v-if="modelValue"
      @click="$emit('update:modelValue', '')"
      class="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 transition-colors"
    >
      <XMarkIcon class="w-4 h-4" />
    </button>
  </div>
</template>

<script setup>
import { MagnifyingGlassIcon, XMarkIcon } from '@heroicons/vue/24/outline'

defineProps({
  modelValue: { type: String, default: '' },
  placeholder: { type: String, default: 'Tìm kiếm...' },
})
defineEmits(['update:modelValue'])
</script>
