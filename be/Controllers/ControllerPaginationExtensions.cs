using be.Contracts;
using Microsoft.EntityFrameworkCore;

namespace be.Controllers;

internal static class ControllerPaginationExtensions
{
    public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var totalItems = await query.CountAsync();
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)normalizedPageSize);
        var items = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync();

        return new PagedResponse<T>(
            normalizedPage,
            normalizedPageSize,
            totalItems,
            totalPages,
            items);
    }
}
