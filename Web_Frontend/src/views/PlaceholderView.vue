<!-- Generic placeholder page used by all sub-routes -->
<template>
  <div class="flex flex-col items-center justify-center min-h-[60vh] text-center">
    <div class="w-20 h-20 rounded-full bg-orange-100 flex items-center justify-center mb-4">
      <component :is="icon" class="w-9 h-9 text-orange-400" />
    </div>
    <h2 class="text-xl font-bold text-orange-900 mb-2">{{ title }}</h2>
    <p class="text-sm text-gray-500 max-w-xs">
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
