import { computed, ref } from 'vue';

/**
 * Composable dùng chung cho phân trang (hỗ trợ cả Server-side PagedResult<T> và Client-side list).
 */
export function usePagination(options = {}) {
  const {
    initialPage = 1,
    initialPageSize = 20,
    pageSizeOptions = [10, 20, 50, 100],
  } = options;

  const page = ref(initialPage);
  const pageSize = ref(initialPageSize);
  const totalCount = ref(0);
  const totalPages = ref(0);

  const effectiveTotalPages = computed(() => {
    if (totalPages.value > 0) return totalPages.value;
    if (totalCount.value > 0 && pageSize.value > 0) {
      return Math.ceil(totalCount.value / pageSize.value);
    }
    return 1;
  });

  const rangeStart = computed(() => {
    if (totalCount.value === 0) return 0;
    return (page.value - 1) * pageSize.value + 1;
  });

  const rangeEnd = computed(() => {
    if (totalCount.value === 0) return 0;
    return Math.min(page.value * pageSize.value, totalCount.value);
  });

  /**
   * Đồng bộ state từ phản hồi PagedResult<T> của backend.
   * Hỗ trợ cả camelCase lẫn PascalCase đề phòng serializer khác nhau.
   */
  const applyPagedResult = (paged) => {
    const items = paged?.items ?? paged?.Items ?? [];
    const serverTotal = paged?.totalCount ?? paged?.TotalCount ?? items.length;
    const serverPageSize = paged?.pageSize ?? paged?.PageSize ?? pageSize.value;
    const serverPage = paged?.page ?? paged?.Page ?? page.value;
    const serverTotalPages =
      paged?.totalPages ??
      paged?.TotalPages ??
      (serverTotal === 0 || serverPageSize <= 0
        ? 0
        : Math.ceil(serverTotal / serverPageSize));

    totalCount.value = Number(serverTotal) || 0;
    pageSize.value = Number(serverPageSize) || initialPageSize;
    totalPages.value = Number(serverTotalPages) || 0;
    page.value = Math.max(1, Number(serverPage) || 1);

    return items;
  };

  /**
   * Cắt trang ở client khi toàn bộ dữ liệu đã được xử lý sẵn trong bộ nhớ
   * (hoặc dùng trước khi thực hiện lazy-enrichment từng item trên trang hiện tại).
   */
  const applyClientList = (allItems = []) => {
    const source = Array.isArray(allItems) ? allItems : [];
    totalCount.value = source.length;
    totalPages.value =
      source.length === 0 || pageSize.value <= 0
        ? 0
        : Math.ceil(source.length / pageSize.value);

    const maxPage = Math.max(1, totalPages.value);
    if (page.value > maxPage) {
      page.value = maxPage;
    } else if (page.value < 1) {
      page.value = 1;
    }

    const startIndex = (page.value - 1) * pageSize.value;
    return source.slice(startIndex, startIndex + pageSize.value);
  };

  const resetPage = () => {
    page.value = 1;
  };

  const buildParams = (extra = {}) => ({
    page: page.value,
    pageSize: pageSize.value,
    ...extra,
  });

  return {
    page,
    pageSize,
    totalCount,
    totalPages,
    pageSizeOptions,
    effectiveTotalPages,
    rangeStart,
    rangeEnd,
    applyPagedResult,
    applyClientList,
    resetPage,
    buildParams,
  };
}
