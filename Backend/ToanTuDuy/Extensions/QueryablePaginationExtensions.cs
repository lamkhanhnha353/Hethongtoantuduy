using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ToanTuDuy.DTOs;

namespace ToanTuDuy.Extensions
{
    public static class QueryablePaginationExtensions
    {
        /// <summary>
        /// Thực hiện phân trang trực tiếp trên IQueryable tại tầng SQL Server (COUNT + OFFSET/FETCH).
        /// Luôn đảm bảo IQueryable đã được OrderBy deterministically trước khi gọi hàm này.
        /// </summary>
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> query,
            int page = 1,
            int pageSize = PaginationDefaults.DefaultPageSize,
            int defaultPageSize = PaginationDefaults.DefaultPageSize,
            int maxPageSize = PaginationDefaults.MaxPageSize,
            CancellationToken cancellationToken = default)
        {
            var (normalizedPage, normalizedSize) = PaginationDefaults.Normalize(
                page,
                pageSize,
                defaultPageSize,
                maxPageSize);

            var totalCount = await query.CountAsync(cancellationToken);
            if (totalCount == 0 || (normalizedPage - 1L) * normalizedSize >= totalCount)
            {
                return new PagedResult<T>
                {
                    Items = Array.Empty<T>(),
                    Page = normalizedPage,
                    PageSize = normalizedSize,
                    TotalCount = totalCount
                };
            }

            var items = await query
                .Skip((normalizedPage - 1) * normalizedSize)
                .Take(normalizedSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<T>
            {
                Items = items,
                Page = normalizedPage,
                PageSize = normalizedSize,
                TotalCount = totalCount
            };
        }

        /// <summary>
        /// Phân trang cho danh sách đã nạp sẵn trong bộ nhớ (ví dụ sau tính toán giao dịch).
        /// </summary>
        public static PagedResult<T> ToPagedResult<T>(
            this IEnumerable<T> source,
            int page = 1,
            int pageSize = PaginationDefaults.DefaultPageSize,
            int defaultPageSize = PaginationDefaults.DefaultPageSize,
            int maxPageSize = PaginationDefaults.MaxPageSize)
        {
            var (normalizedPage, normalizedSize) = PaginationDefaults.Normalize(
                page,
                pageSize,
                defaultPageSize,
                maxPageSize);

            var materialized = source as IReadOnlyCollection<T> ?? source.ToList();
            var totalCount = materialized.Count;
            if (totalCount == 0 || (normalizedPage - 1L) * normalizedSize >= totalCount)
            {
                return new PagedResult<T>
                {
                    Items = Array.Empty<T>(),
                    Page = normalizedPage,
                    PageSize = normalizedSize,
                    TotalCount = totalCount
                };
            }

            var items = materialized
                .Skip((normalizedPage - 1) * normalizedSize)
                .Take(normalizedSize)
                .ToList();

            return new PagedResult<T>
            {
                Items = items,
                Page = normalizedPage,
                PageSize = normalizedSize,
                TotalCount = totalCount
            };
        }
    }
}
