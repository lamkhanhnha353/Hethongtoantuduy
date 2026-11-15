<template>
  <div
    v-if="showWhenEmpty || totalCount > 0"
    :class="[
      'flex flex-wrap items-center justify-between gap-3 select-none',
      compact ? 'px-3 py-2.5 text-xs' : 'px-4 py-3 text-sm',
      bordered ? 'border-t border-slate-100 bg-slate-50/60' : '',
    ]"
  >
    <!-- Range Summary -->
    <div class="text-slate-500">
      <template v-if="totalCount > 0">
        <template v-if="compact">
          <span class="font-semibold text-slate-700">{{ rangeStart }}–{{ rangeEnd }}</span>
          <span> / {{ totalCount }} {{ itemLabel }}</span>
        </template>
        <template v-else>
          Hiển thị
          <span class="font-semibold text-slate-700">{{ rangeStart }}</span>–<span class="font-semibold text-slate-700">{{ rangeEnd }}</span>
          trong tổng
          <span class="font-semibold text-slate-800">{{ totalCount }}</span>
          {{ itemLabel }}
        </template>
      </template>
      <template v-else>
        Không có {{ itemLabel }} nào
      </template>
    </div>

    <!-- Navigation Controls -->
    <div class="flex flex-wrap items-center gap-2">
      <!-- Page Size Selector -->
      <div v-if="showPageSize && !compact" class="flex items-center gap-1.5 mr-1">
        <select
          :value="pageSize"
          :disabled="disabled"
          aria-label="Số dòng mỗi trang"
          class="rounded-lg border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-medium text-slate-700 shadow-2xs transition focus:border-orange-400 focus:outline-none focus:ring-2 focus:ring-orange-100 disabled:cursor-not-allowed disabled:opacity-50"
          @change="handlePageSizeChange"
        >
          <option v-for="size in normalizedPageSizeOptions" :key="size" :value="size">
            {{ size }} / trang
          </option>
        </select>
      </div>

      <!-- Prev Button -->
      <button
        type="button"
        :disabled="disabled || page <= 1"
        class="inline-flex items-center gap-1 rounded-lg border border-slate-200 bg-white font-medium text-slate-600 transition hover:border-orange-300 hover:bg-orange-50 hover:text-orange-600 disabled:cursor-not-allowed disabled:opacity-40 disabled:hover:border-slate-200 disabled:hover:bg-white disabled:hover:text-slate-600"
        :class="compact ? 'px-2 py-1 text-xs' : 'px-2.5 py-1.5 text-xs'"
        @click="goToPage(page - 1)"
      >
        <ChevronLeftIcon class="h-3.5 w-3.5" />
        <span v-if="!compact">Trước</span>
      </button>

      <!-- Page Numbers -->
      <template v-for="(token, idx) in pageTokens" :key="`${token}-${idx}`">
        <span
          v-if="token === '...'"
          class="px-1.5 text-xs font-medium text-slate-400"
        >
          ...
        </span>
        <button
          v-else
          type="button"
          :disabled="disabled"
          class="min-w-7 rounded-lg border text-center text-xs font-semibold transition disabled:cursor-not-allowed disabled:opacity-50"
          :class="[
            compact ? 'px-2 py-1' : 'px-2.5 py-1.5',
            token === page
              ? 'border-orange-500 bg-orange-500 text-white shadow-xs'
              : 'border-slate-200 bg-white text-slate-600 hover:border-orange-300 hover:bg-orange-50 hover:text-orange-600',
          ]"
          @click="goToPage(token)"
        >
          {{ token }}
        </button>
      </template>

      <!-- Next Button -->
      <button
        type="button"
        :disabled="disabled || page >= effectiveTotalPages"
        class="inline-flex items-center gap-1 rounded-lg border border-slate-200 bg-white font-medium text-slate-600 transition hover:border-orange-300 hover:bg-orange-50 hover:text-orange-600 disabled:cursor-not-allowed disabled:opacity-40 disabled:hover:border-slate-200 disabled:hover:bg-white disabled:hover:text-slate-600"
        :class="compact ? 'px-2 py-1 text-xs' : 'px-2.5 py-1.5 text-xs'"
        @click="goToPage(page + 1)"
      >
        <span v-if="!compact">Sau</span>
        <ChevronRightIcon class="h-3.5 w-3.5" />
      </button>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import { ChevronLeftIcon, ChevronRightIcon } from '@heroicons/vue/24/outline';

const props = defineProps({
  page: {
    type: Number,
    required: true,
  },
  pageSize: {
    type: Number,
    required: true,
  },
  totalCount: {
    type: Number,
    default: 0,
  },
  totalPages: {
    type: Number,
    default: 0,
  },
  pageSizeOptions: {
    type: Array,
    default: () => [10, 20, 50, 100],
  },
  itemLabel: {
    type: String,
    default: 'mục',
  },
  compact: {
    type: Boolean,
    default: false,
  },
  bordered: {
    type: Boolean,
    default: true,
  },
  showPageSize: {
    type: Boolean,
    default: true,
  },
  showWhenEmpty: {
    type: Boolean,
    default: false,
  },
  disabled: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(['update:page', 'update:pageSize', 'change']);

const effectiveTotalPages = computed(() => {
  if (props.totalPages > 0) return props.totalPages;
  if (props.totalCount > 0 && props.pageSize > 0) {
    return Math.ceil(props.totalCount / props.pageSize);
  }
  return 1;
});

const rangeStart = computed(() => {
  if (props.totalCount <= 0) return 0;
  return (props.page - 1) * props.pageSize + 1;
});

const rangeEnd = computed(() => {
  if (props.totalCount <= 0) return 0;
  return Math.min(props.page * props.pageSize, props.totalCount);
});

const normalizedPageSizeOptions = computed(() => {
  const set = new Set([...(props.pageSizeOptions || [10, 20, 50, 100]), props.pageSize]);
  return Array.from(set)
    .map((n) => Number(n))
    .filter((n) => Number.isFinite(n) && n > 0)
    .sort((a, b) => a - b);
});

const pageTokens = computed(() => {
  const total = effectiveTotalPages.value;
  const current = Math.min(Math.max(1, props.page), total);
  const maxSimple = props.compact ? 5 : 7;

  if (total <= maxSimple) {
    return Array.from({ length: total }, (_, idx) => idx + 1);
  }

  const delta = props.compact ? 0 : 1;
  const tokens = [1];
  const left = Math.max(2, current - delta);
  const right = Math.min(total - 1, current + delta);

  if (left > 2) tokens.push('...');
  for (let p = left; p <= right; p += 1) {
    tokens.push(p);
  }
  if (right < total - 1) tokens.push('...');
  tokens.push(total);

  return tokens;
});

const goToPage = (target) => {
  if (props.disabled) return;
  const clamped = Math.min(Math.max(1, target), effectiveTotalPages.value);
  if (clamped === props.page) return;
  emit('update:page', clamped);
  emit('change', { page: clamped, pageSize: props.pageSize });
};

const handlePageSizeChange = (event) => {
  if (props.disabled) return;
  const nextSize = Number(event.target.value);
  if (!Number.isFinite(nextSize) || nextSize <= 0 || nextSize === props.pageSize) return;
  emit('update:pageSize', nextSize);
  emit('update:page', 1);
  emit('change', { page: 1, pageSize: nextSize });
};
</script>
