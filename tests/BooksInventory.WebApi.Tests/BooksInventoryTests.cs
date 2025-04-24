using BooksInventory.WebApi.Tests.Factories;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace BooksInventory.WebApi.Tests;

[Collection(nameof(IntegrationTestsCollection))]
public class BooksInventoryTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory factory;
    private readonly HttpClient client;
    private readonly BooksInventoryDbContext dbContext;

    public BooksInventoryTests(CompositeFixture fixture)
    {
        this.factory = new CustomWebApplicationFactory(fixture);
        this.client = this.factory.CreateClient();

        // Create a scope to retrieve a scoped instance of the DB context.
        // This allows direct interaction with the database for setup and teardown.
        var scope = this.factory.Services.CreateScope();
        this.dbContext = scope.ServiceProvider.GetRequiredService<BooksInventoryDbContext>();
    }

    public async Task InitializeAsync()
    {
        // Clean the database to ensure test isolation.
        dbContext.Books.RemoveRange(dbContext.Books);
        await dbContext.SaveChangesAsync();
    }

    // Dispose resources to maintain isolation after each test run.
    public Task DisposeAsync()
    {
        this.client.Dispose();
        this.factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AddBook_ReturnsBookId()
    {
        var request = new AddBookRequest("AI Engineering", "Chip Huyen", "1098166302");
        var content = request.GetHttpContent();

        var response = await this.client.PostAsync("/addBook", content);

        response.EnsureSuccessStatusCode();
        var result = await response.DeserializeAsync<AddBookResponse>();
        result?.Should().NotBeNull();
        result!.BookId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetBook_ReturnsBookDetails()
    {
        var addRequest = new AddBookRequest("AI Engineering", "Chip Huyen", "1234567890");
        var addResponse = await this.client.PostAsync("/addBook", addRequest.GetHttpContent());
        var bookId = (await addResponse.DeserializeAsync<AddBookResponse>())!.BookId;

        var getResponse = await this.client.GetAsync($"/books/{bookId}");

        getResponse.EnsureSuccessStatusCode();
        var book = await getResponse.DeserializeAsync<Book>();
        book.Should().BeEquivalentTo(
            new Book
            {
                Id = bookId,
                Title = addRequest.Title,
                Author = addRequest.Author,
                ISBN = addRequest.ISBN
            });
    }

    [Fact]
    public async Task RemoveBook_RemovesBookSuccessfully()
    {
        var addRequest = new AddBookRequest("AI Engineering", "Chip Huyen", "1234567890");
        var addResponse = await this.client.PostAsync("/addBook", addRequest.GetHttpContent());
        var bookId = (await addResponse.DeserializeAsync<AddBookResponse>())!.BookId;

        var deleteResponse = await this.client.DeleteAsync($"/books/{bookId}");

        deleteResponse.EnsureSuccessStatusCode();

        var getResponse = await this.client.GetAsync($"/books/{bookId}");
        getResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllBooks_ReturnsAllBooks()
    {
        var book1 = new AddBookRequest("AI Engineering", "Chip Huyen", "1234567890");
        var book2 = new AddBookRequest("Clean Code", "Robert C. Martin", "0987654321");

        await this.client.PostAsync("/addBook", book1.GetHttpContent());
        await this.client.PostAsync("/addBook", book2.GetHttpContent());

        var response = await this.client.GetAsync("/books");

        response.EnsureSuccessStatusCode();
        var books = await response.DeserializeAsync<List<Book>>();
        books.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchBooks_ByTitle_ReturnsMatchingBooks()
    {
        var book1 = new AddBookRequest("AI Engineering", "Chip Huyen", "1234567890");
        var book2 = new AddBookRequest("Clean Code", "Robert C. Martin", "0987654321");

        await this.client.PostAsync("/addBook", book1.GetHttpContent());
        await this.client.PostAsync("/addBook", book2.GetHttpContent());

        var response = await this.client.GetAsync("/books/search?title=AI Engineering");

        response.EnsureSuccessStatusCode();
        var books = await response.DeserializeAsync<List<Book>>();
        books.Should().ContainSingle(b => b.Title == "AI Engineering");
    }

    [Fact]
    public async Task SearchBooks_ByAuthor_ReturnsMatchingBooks()
    {
        var book1 = new AddBookRequest("AI Engineering", "Chip Huyen", "1234567890");
        var book2 = new AddBookRequest("Clean Code", "Robert C. Martin", "0987654321");

        await this.client.PostAsync("/addBook", book1.GetHttpContent());
        await this.client.PostAsync("/addBook", book2.GetHttpContent());

        var response = await this.client.GetAsync("/books/search?author=Chip Huyen");

        response.EnsureSuccessStatusCode();
        var books = await response.DeserializeAsync<List<Book>>();
        books.Should().ContainSingle(b => b.Author == "Chip Huyen");
    }

    [Fact]
    public async Task SearchBooks_ByISBN_ReturnsMatchingBooks()
    {
        var book1 = new AddBookRequest("AI Engineering", "Chip Huyen", "1234567890");
        var book2 = new AddBookRequest("Clean Code", "Robert C. Martin", "0987654321");

        await this.client.PostAsync("/addBook", book1.GetHttpContent());
        await this.client.PostAsync("/addBook", book2.GetHttpContent());

        var response = await this.client.GetAsync("/books/search?isbn=1234567890");

        response.EnsureSuccessStatusCode();
        var books = await response.DeserializeAsync<List<Book>>();
        books.Should().ContainSingle(b => b.ISBN == "1234567890");
    }
}