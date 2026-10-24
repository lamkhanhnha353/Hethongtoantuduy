<template>
  <div class="flex items-center justify-center py-16 flex-col gap-3">
    <div class="w-16 h-16 rounded-full bg-orange-100 flex items-center justify-center">
      <component :is="icon" class="w-8 h-8 text-orange-300" />
    </div>
    <p class="text-sm font-medium text-gray-500">{{ message }}</p>
    <p v-if="sub" class="text-xs text-gray-400">{{ sub }}</p>
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
