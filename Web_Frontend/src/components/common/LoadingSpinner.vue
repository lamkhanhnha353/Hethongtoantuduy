<template>
  <div class="flex items-center justify-center py-16">
    <div class="flex flex-col items-center gap-3">
      <div class="w-10 h-10 border-4 border-stone-200 border-t-orange-600 rounded-full animate-spin" />
      <p class="text-sm font-medium text-stone-500">{{ label }}</p>
    </div>
  </div>
</template>

<script setup>
defineProps({
  label: { type: String, default: 'Đang tải...' },
})
</script>
