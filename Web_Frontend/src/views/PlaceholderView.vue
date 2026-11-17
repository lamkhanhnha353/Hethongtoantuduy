<!-- Generic placeholder page used by all sub-routes -->
<template>
  <div class="flex flex-col items-center justify-center min-h-[60vh] text-center">
    <div class="w-16 h-16 rounded-lg bg-stone-100 border border-stone-300 flex items-center justify-center mb-4">
      <component :is="icon" class="w-8 h-8 text-orange-600" />
    </div>
    <h2 class="text-xl font-bold text-stone-900 mb-2">{{ title }}</h2>
    <p class="text-sm text-stone-500 max-w-xs">
      Trang này đang được phát triển. Vui lòng quay lại sau!
    </p>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { WrenchScrewdriverIcon } from '@heroicons/vue/24/outline'

const route = useRoute()
const title = computed(() => route.meta?.title ?? 'Đang phát triển')
const icon = WrenchScrewdriverIcon
</script>
