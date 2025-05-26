using System.Text.Json;

using Microsoft.Extensions.Caching.Distributed;

namespace BooksInventory.WebApi.Tests.Extensions;

public static class BooksInventoryRedisExtensions
{
    public static string GetCacheKey(this int bookId)
    {
        return $"book_{bookId}";
    }

    public static async Task UpdateBookAsync(this IDistributedCache cache, Book book)
    {
        await cache.RemoveAsync(book.Id.GetCacheKey());
        await cache.SetStringAsync(
            book.Id.GetCacheKey(),
            JsonSerializer.Serialize(book));
    }

    public static async Task<Book> GetByIdAsync(this IDistributedCache cache, int bookId)
    {
        var rawData = (await cache.GetStringAsync(bookId.GetCacheKey()))!;
        string json = ExtractJsonFromHybridCacheValue(rawData);
        return JsonSerializer.Deserialize<Book>(json)!;
    }

    /// <summary>
    /// Extracts the JSON portion from the raw data stored in the hybrid cache.
    /// This operation is necessary because the hybrid cache adds metadata to the stored values,
    /// and this method ensures that only the valid JSON content is returned for deserialization.
    /// </summary>
    /// <param name="rawData">The raw data retrieved from the hybrid cache.</param>
    /// <returns>The JSON portion of the raw data.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the raw data does not contain valid JSON.</exception>
    private static string ExtractJsonFromHybridCacheValue(string rawData)
    {
        // Locate JSON start position
        int jsonStartIndex = rawData.IndexOf('{');
        int jsonEndIndex = rawData.LastIndexOf('}');

        if (jsonStartIndex < 0 || jsonEndIndex <= jsonStartIndex)
        {
            throw new InvalidOperationException("Invalid hybrid cache value format.");
        }

        return rawData.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1);
    }
}