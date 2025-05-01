namespace BooksInventory.WebApi.Tests.Extensions;

public static class BooksInventoryDbContextExtensions
{
    public static async Task UpdateBookAsync(this BooksInventoryDbContext db, Book updated)
    {
        var existing = await db.Books.FindAsync(updated.Id);
        db.Entry(existing!).CurrentValues.SetValues(updated);
        await db.SaveChangesAsync();
    }

    public static async Task<Book> GetByIdAsync(this BooksInventoryDbContext db, int bookId)
    {
        return (await db.Books.FindAsync(bookId))!;
    }
}