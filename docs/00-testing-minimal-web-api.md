# Testing Minimal Web APIs with ASP.NET: A Developer's Guide 🚀

Writing tests for Web APIs isn’t always fun, but it doesn’t have to be hard. In this guide, I’ll show you how to write clean, effective integration tests for an ASP.NET minimal Web API, giving you a solid starting point to build on. 💡

---

## Setting Up the Stage 🛠️

Before we get to testing, we need an API to test. We’re keeping it simple - a book inventory with in-memory storage. No databases, no heavy frameworks, just a clean ASP.NET minimal API.

Run these commands to set up your solution:

```bash
dotnet new sln --name BooksInventory

mkdir src tests
dotnet new web -o src/BooksInventory.WebApi
dotnet new xunit -o tests/BooksInventory.WebApi.Tests

dotnet sln add src/BooksInventory.WebApi
dotnet sln add tests/BooksInventory.WebApi.Tests
dotnet add tests/BooksInventory.WebApi.Tests reference src/BooksInventory.WebApi

dotnet add tests/BooksInventory.WebApi.Tests package FluentAssertions
dotnet add tests/BooksInventory.WebApi.Tests package Microsoft.AspNetCore.Mvc.Testing
```

---

## Understanding the Book Inventory API 📖

The API provides two endpoints:

- **POST `/addBook`**: Accepts a JSON payload with `Title`, `Author`, and `ISBN`, stores it, and returns a unique `BookId`.
- **GET `/books/{id}`**: Fetches book details using `BookId`.

Here’s our API in `Program.cs`:

```csharp
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var books = new ConcurrentDictionary<string, Book>();

app.MapPost("/addBook", (AddBookRequest request) =>
{
    var bookId = Guid.NewGuid().ToString();
    var book = new Book(request.Title, request.Author, request.ISBN);

    if (!books.TryAdd(bookId, book))
    {
        return Results.Problem("Failed to add book due to a concurrency issue.");
    }

    return Results.Ok(new AddBookResponse(bookId));
});

app.MapGet("/books/{id}", (string id) =>
{
    if (books.TryGetValue(id, out var book))
    {
        return Results.Ok(book);
    }
    return Results.NotFound(new { Message = "Book not found", BookId = id });
});

app.Run();

public record AddBookRequest(string Title, string Author, string ISBN);
public record AddBookResponse(string BookId);
public record Book(string Title, string Author, string ISBN);

// Explicitly define Program as partial for integration tests
public partial class Program { }
```

---

## Writing Integration Tests 🧪

We’ll use **xUnit**, **WebApplicationFactory**, and **FluentAssertions**.

Here's our test file, `BookInventoryTests.cs`:

```csharp
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BooksInventory.WebApi.Tests;

public class BookInventoryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BookInventoryTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AddBook_ReturnsBookId()
    {
        var request = new AddBookRequest("AI Engineering", "Chip Huyen", "1098166302");
        var content = request.GetHttpContent();

        var response = await _client.PostAsync("/addBook", content);

        response.EnsureSuccessStatusCode();
        var result = await response.DeserializeAsync<AddBookResponse>();
        result?.Should().NotBeNull();
        result!.BookId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetBook_ReturnsBookDetails()
    {
        var addRequest = new AddBookRequest("AI Engineering", "Chip Huyen", "1234567890");
        var addResponse = await _client.PostAsync("/addBook", addRequest.GetHttpContent());
        var bookId = (await addResponse.DeserializeAsync<AddBookResponse>())?.BookId;

        var getResponse = await _client.GetAsync($"/books/{bookId}");

        getResponse.EnsureSuccessStatusCode();
        var book = await getResponse.DeserializeAsync<Book>();
        book.Should().BeEquivalentTo(
            new Book(
                addRequest.Title,
                addRequest.Author,
                addRequest.ISBN));
    }
}
```

---

## Keep It Clean: Reusable Extension Methods ✨

Testing should be easy, not filled with repeated code for serialization and deserialization. Let’s clean things up with some helper methods.

```csharp
using System.Text;
using System.Text.Json;

public static class HttpContentExtensions
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<T?> DeserializeAsync<T>(this HttpResponseMessage response)
    {
        return JsonSerializer.Deserialize<T>(
            await response.Content.ReadAsStringAsync(),
            SerializerOptions);
    }

    public static HttpContent GetHttpContent<T>(this T obj) where T : class
    {
        return new StringContent(
            JsonSerializer.Serialize(obj),
            Encoding.UTF8, "application/json");
    }
}
```

---

## Manual Testing (for When You Just Want to Click a Button) 🔘

Sometimes, you just want to test an API without writing a test case. That’s where REST Client in VS Code comes in. Create a `.http` file like this:

```http
POST {{baseUrl}}/addBook HTTP/1.1
Content-Type: application/json

{
    "Title": "The Pragmatic Programmer",
    "Author": "Andy Hunt and Dave Thomas",
    "ISBN": "9780135957059"
}

###

# Test GET /books/{id} (replace {id} with a valid BookId from the POST response)
GET {{baseUrl}}/books/{id} HTTP/1.1
Accept: application/json
```

Then run the requests right inside VS Code. No Postman required. 😎

---

## Wrapping Up: Integration Testing for Web APIs Can Be Fun 🎉

Testing ASP.NET minimal Web APIs doesn’t have to be a painful experience. We kept things lightweight:

- Used `WebApplicationFactory` for integration tests instead of mocking everything. 🔧
- Avoided test boilerplate with `FluentAssertions` and extension methods. 💅
- Leveraged **REST Client** for quAick manual testing. ⚡

Try this approach in your own projects and see how much smoother your testing workflow becomes. Got a cool testing trick? Share it in the comments or hit me up on GitHub [here](https://github.com/dorinandreidragan/books-inventory/tree/episode/00-testing-minimal-web-api). 📬
