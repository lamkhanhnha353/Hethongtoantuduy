using System;
using System.Collections.Generic;

namespace ToanTuDuy.DTOs
{
    public static class PaginationDefaults
    {
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;

        public static (int Page, int PageSize) Normalize(
            int page,
            int pageSize,
            int defaultPageSize = DefaultPageSize,
            int maxPageSize = MaxPageSize)
        {
            var normalizedPage = Math.Max(1, page);
            var normalizedSize = pageSize <= 0
                ? defaultPageSize
                : Math.Min(pageSize, maxPageSize);

            return (normalizedPage, normalizedSize);
        }
    }

    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => TotalCount == 0 || PageSize <= 0
            ? 0
            : (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }

    public class AdminAccountPagedResponse : PagedResult<AdminAccountResponse>
    {
        public int ActiveCount { get; set; }
        public int BlockedCount { get; set; }
    }

    public class AdminInvoicePagedResponse : PagedResult<AdminInvoiceResponse>
    {
        public int TotalAllCount { get; set; }
        public int PaidCount { get; set; }
        public int PendingCount { get; set; }
        public long PaidTotalAmount { get; set; }
    }
}
