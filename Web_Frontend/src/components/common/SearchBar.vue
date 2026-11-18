<template>
  <!-- Search bar with icon -->
  <div class="relative flex-1 min-w-0">
    <MagnifyingGlassIcon
      class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-stone-400 pointer-events-none"
    />
    <input
      :value="modelValue"
      @input="$emit('update:modelValue', $event.target.value)"
      type="text"
      maxlength="100"
      :placeholder="placeholder"
      class="w-full h-9 pl-9 pr-8 text-sm bg-stone-50/70 border border-stone-300 rounded-md
             text-stone-900 placeholder-stone-400
             focus:bg-white focus:outline-none focus:ring-2 focus:ring-orange-500/20 focus:border-orange-600
             transition-colors duration-150"
    />
    <!-- Clear button -->
    <button
      v-if="modelValue"
      @click="$emit('update:modelValue', '')"
      class="absolute right-2.5 top-1/2 -translate-y-1/2 text-stone-400 hover:text-stone-700 transition-colors"
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
