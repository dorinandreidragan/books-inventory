using System.Text.Json;

using Microsoft.Extensions.Caching.Distributed;

namespace BooksInventory.WebApi.Tests.Extensions;

public static class BooksInventoryRedisExtensions
{
    public static string GetCacheKey(this int bookId)
    {
        return $"book_{bookId}";
    }

    public static async Task UpdateTitleAsync(this IDistributedCache cache, int bookId, string title)
    {
        var existing = await cache.GetByIdAsync(bookId);
        var updated = existing with { Title = title };
        await cache.SetStringAsync(
            bookId.GetCacheKey(),
            JsonSerializer.Serialize(updated));
    }

    public static async Task<Book> GetByIdAsync(this IDistributedCache cache, int bookId)
    {
        var book = await cache.GetStringAsync(bookId.GetCacheKey());
        return JsonSerializer.Deserialize<Book>(book!)!;
    }
}