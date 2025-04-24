namespace BooksInventory.WebApi.Tests.Extensions;

public static class BooksInventoryDbContextExtensions
{
    public static async Task UpdateTitleAsync(this BooksInventoryDbContext db, int bookId, string title)
    {
        var existing = await db.Books.FindAsync(bookId);
        var updated = existing! with { Title = title };
        db.Entry(existing).CurrentValues.SetValues(updated);
        await db.SaveChangesAsync();
    }

    public static async Task<Book> GetByIdAsync(this BooksInventoryDbContext db, int bookId)
    {
        return (await db.Books.FindAsync(bookId))!;
    }
}