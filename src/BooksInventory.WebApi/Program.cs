using BooksInventory.WebApi;

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register the PostgreSQL service with EF Core.
// This replaces any default in-memory service configuration.
builder.Services.AddDbContext<BooksInventoryDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

var app = builder.Build();

// Inject the DB context to add a new book asynchronously.
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

// Inject the DB context to get a book asynchronously.
app.MapGet("/books/{id}", async (int id, BooksInventoryDbContext db) =>
{
    var book = await db.Books.FindAsync(id);
    return book is not null
        ? Results.Ok(book)
        : Results.NotFound(new { Message = "Book not found", BookId = id });
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
};

// Explicitly define Program as partial for integration tests.
public partial class Program { }