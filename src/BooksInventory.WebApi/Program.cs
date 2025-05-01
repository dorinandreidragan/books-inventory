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

app.MapGet("/books", async (int? page, int? pageSize, BooksInventoryDbContext db) =>
{
    try
    {
        var (currentPage, currentPageSize) = GetPaginationValues(page, pageSize);
        ValidatePaginationValues(currentPage, currentPageSize);

        var books = await db.Books
            .OrderBy(b => b.Id)
            .Skip((currentPage - 1) * currentPageSize)
            .Take(currentPageSize)
            .ToListAsync();

        return Results.Ok(books);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { ex.Message });
    }
});

app.MapGet("/books/search", async (string? title, string? author, string? isbn, int? page, int? pageSize, BooksInventoryDbContext db) =>
{
    try
    {
        var (currentPage, currentPageSize) = GetPaginationValues(page, pageSize);
        ValidatePaginationValues(currentPage, currentPageSize);

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

        var books = await query
            .OrderBy(b => b.Id)
            .Skip((currentPage - 1) * currentPageSize)
            .Take(currentPageSize)
            .ToListAsync();

        return Results.Ok(books);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { ex.Message });
    }
});

app.MapDelete("/books/{id}", async (int id, BooksInventoryDbContext db, HybridCache cache) =>
{
    var book = await db.Books.FindAsync(id);
    if (book is null)
    {
        return Results.NotFound(new { Message = "Book not found", BookId = id });
    }

    db.Books.Remove(book);
    await db.SaveChangesAsync();

    // Remove the entry from the cache
    await cache.RemoveAsync($"book_{id}");

    return Results.NoContent();
});

app.MapPut("/books/{id}", async (int id, AddBookRequest request, BooksInventoryDbContext db, HybridCache cache) =>
{
    // AsNoTracking ensures that EF Core doesn't track the original entity,
    // avoiding the multiple-instance conflict when calling Update(),
    // and improves performance since we'll update the entity right away.
    var book = await db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
    if (book is null)
    {
        return Results.NotFound(new { Message = "Book not found", BookId = id });
    }

    book = book with
    {
        Title = request.Title,
        Author = request.Author,
        ISBN = request.ISBN
    };

    db.Books.Update(book);
    await db.SaveChangesAsync();

    // Update the cache
    await cache.SetAsync($"book_{id}", book);

    return Results.Ok(book);
});

app.Run();

static (int CurrentPage, int CurrentPageSize) GetPaginationValues(int? page, int? pageSize)
{
    const int defaultPage = 1;
    const int defaultPageSize = 10;

    int currentPage = page ?? defaultPage;
    int currentPageSize = pageSize ?? defaultPageSize;

    return (currentPage, currentPageSize);
}

static void ValidatePaginationValues(int currentPage, int currentPageSize)
{
    const int maxPageSize = 100;

    if (currentPage <= 0 || currentPageSize <= 0)
    {
        throw new ArgumentException("Page and pageSize must be greater than 0.");
    }

    if (currentPageSize > maxPageSize)
    {
        throw new ArgumentException($"Page size cannot be greater than {maxPageSize}.");
    }
}

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