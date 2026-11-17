<template>
  <div class="flex items-center justify-center py-16 flex-col gap-3">
    <div class="w-14 h-14 rounded-lg bg-stone-100 border border-stone-300 flex items-center justify-center">
      <component :is="icon" class="w-7 h-7 text-stone-400" />
    </div>
    <p class="text-sm font-semibold text-stone-700">{{ message }}</p>
    <p v-if="sub" class="text-xs text-stone-400">{{ sub }}</p>
  </div>
</template>

<script setup>
import { InboxIcon } from '@heroicons/vue/24/outline'

defineProps({
  icon: { default: InboxIcon },
  message: { type: String, default: 'Không có dữ liệu' },
  sub: { type: String, default: '' },
})
</script>
