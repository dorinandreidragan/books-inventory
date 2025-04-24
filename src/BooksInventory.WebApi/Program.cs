using BooksInventory.WebApi;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<BooksInventoryDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
    options.InstanceName = "BooksInventoryCache:";
});
builder.Services.AddHybridCache();

var app = builder.Build();

app.MapPost("/addBook", async (AddBookRequest request, BooksInventoryDbContext db) =>
{
    var book = new Book
    {
        Title = request.Title,
        Author = request.Author,
        ISBN = request.ISBN
    };
    db.Books.Add(book);
    await db.SaveChangesAsync();
    return Results.Ok(new AddBookResponse(book.Id));
});

app.MapGet("/books/{id}", async (int id, BooksInventoryDbContext db, HybridCache cache) =>
{
    var book = await cache.GetOrCreateAsync($"book_{id}", async (cancellationToken) =>
    {
        return await db.Books.FindAsync([id], cancellationToken);
    });

    return book is not null
        ? Results.Ok(book)
        : Results.NotFound(new { Message = "Book not found", BookId = id });
});

app.MapGet("/books", async (BooksInventoryDbContext db) =>
{
    var books = await db.Books.ToListAsync();
    return Results.Ok(books);
});

app.MapGet("/books/search", async (string? title, string? author, string? isbn, BooksInventoryDbContext db) =>
{
    var query = db.Books.AsQueryable();

    if (!string.IsNullOrEmpty(title))
    {
        query = query.Where(b => b.Title.Contains(title));
    }

    if (!string.IsNullOrEmpty(author))
    {
        query = query.Where(b => b.Author.Contains(author));
    }

    if (!string.IsNullOrEmpty(isbn))
    {
        query = query.Where(b => b.ISBN == isbn);
    }

    var books = await query.ToListAsync();
    return Results.Ok(books);
});

app.MapDelete("/books/{id}", async (int id, BooksInventoryDbContext db) =>
{
    var book = await db.Books.FindAsync(id);
    if (book is null)
    {
        return Results.NotFound(new { Message = "Book not found", BookId = id });
    }

    db.Books.Remove(book);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapPut("/books/{id}", async (int id, AddBookRequest request, BooksInventoryDbContext db, HybridCache cache) =>
{
    var book = await db.Books.FindAsync(id);
    if (book is null)
    {
        return Results.NotFound(new { Message = "Book not found", BookId = id });
    }

    book = book with { Title = request.Title, Author = request.Author, ISBN = request.ISBN };
    db.Books.Update(book);
    await db.SaveChangesAsync();

    // Update the cache
    await cache.SetAsync($"book_{id}", book);

    return Results.Ok(book);
});

app.Run();

public record AddBookRequest(string Title, string Author, string ISBN);
public record AddBookResponse(int BookId);
public record Book
{
    public int Id { get; init; }
    public required string Title { get; init; }
    public required string Author { get; init; }
    public required string ISBN { get; init; }
}

// Explicitly define Program as partial for integration tests
public partial class Program { }