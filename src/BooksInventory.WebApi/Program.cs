using BooksInventory.WebApi;

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<BooksInventoryDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});
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

app.MapGet("/books/{id}", async (int id, BooksInventoryDbContext db) =>
{
    var book = await db.Books.FindAsync(id);
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