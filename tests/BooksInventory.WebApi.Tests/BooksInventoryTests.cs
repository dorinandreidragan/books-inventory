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
        int bookId = await AddBookAsync("book1", "author1", "isbn1");

        var getResponse = await this.client.GetAsync($"/books/{bookId}");

        getResponse.EnsureSuccessStatusCode();
        var addedBook = await getResponse.DeserializeAsync<Book>();
        addedBook.Should().BeEquivalentTo(
            new Book
            {
                Id = bookId,
                Title = "book1",
                Author = "author1",
                ISBN = "isbn1"
            });
    }


    [Fact]
    public async Task RemoveBook_RemovesBookSuccessfully()
    {
        var bookId = await AddBookAsync("book1", "author1", "isbn1");

        var deleteResponse = await this.client.DeleteAsync($"/books/{bookId}");

        deleteResponse.EnsureSuccessStatusCode();
        var getResponse = await this.client.GetAsync($"/books/{bookId}");
        getResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllBooks_ReturnsAllBooks()
    {
        await AddBookAsync("book1", "author1", "isbn1");
        await AddBookAsync("book2", "author2", "isbn2");

        var response = await this.client.GetAsync("/books");

        response.EnsureSuccessStatusCode();
        var books = await response.DeserializeAsync<List<Book>>();
        books.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllBooks_WithPagination_ReturnsPaginatedResults()
    {
        await AddBookAsync("book1", "author1", "isbn1");
        await AddBookAsync("book2", "author2", "isbn2");
        await AddBookAsync("book3", "author3", "isbn3");
        await AddBookAsync("book4", "author4", "isbn4");
        await AddBookAsync("book5", "author5", "isbn5");

        var response = await this.client.GetAsync("/books?page=2&pageSize=2");

        response.EnsureSuccessStatusCode();
        var paginatedBooks = await response.DeserializeAsync<List<Book>>();
        paginatedBooks.Should().HaveCount(2);
        paginatedBooks[0].Title.Should().Be("book3");
        paginatedBooks[1].Title.Should().Be("book4");
    }

    [Fact]
    public async Task UpdateBook_UpdatesBookSuccessfully()
    {
        var bookId = await AddBookAsync("book1", "author1", "isbn1");
        var updateRequest = new AddBookRequest("book1 updated", "author1 updated", "isbn1 updated");
        var updateContent = updateRequest.GetHttpContent();

        var updateResponse = await this.client.PutAsync($"/books/{bookId}", updateContent);

        updateResponse.EnsureSuccessStatusCode();
        Book? updatedBook = await GetBookByIdAsync(bookId);
        updatedBook.Should().BeEquivalentTo(
            new Book
            {
                Id = bookId,
                Title = updateRequest.Title,
                Author = updateRequest.Author,
                ISBN = updateRequest.ISBN
            });
    }

    [Fact]
    public async Task SearchBooks_ByTitle_ReturnsMatchingBooks()
    {
        await AddBookAsync("book1", "author1", "isbn1");
        await AddBookAsync("book2", "author2", "isbn2");
        await AddBookAsync("book3", "author3", "isbn3");

        var response = await this.client.GetAsync("/books/search?title=book");

        response.EnsureSuccessStatusCode();
        var matchingBooks = await response.DeserializeAsync<List<Book>>();
        matchingBooks.Should().HaveCount(3);
        matchingBooks.Should().Contain(b => b.Title == "book1");
        matchingBooks.Should().Contain(b => b.Title == "book2");
        matchingBooks.Should().Contain(b => b.Title == "book3");
    }

    [Fact]
    public async Task SearchBooks_ByAuthor_ReturnsMatchingBooks()
    {
        await AddBookAsync("book1", "author1", "isbn1");
        await AddBookAsync("book2", "author2", "isbn2");
        await AddBookAsync("book3", "author1", "isbn3");

        var response = await this.client.GetAsync("/books/search?author=author1");

        response.EnsureSuccessStatusCode();
        var matchingBooks = await response.DeserializeAsync<List<Book>>();
        matchingBooks.Should().HaveCount(2);
        matchingBooks.Should().Contain(b => b.Title == "book1");
        matchingBooks.Should().Contain(b => b.Title == "book3");
    }

    [Fact]
    public async Task SearchBooks_ByISBN_ReturnsMatchingBook()
    {
        await AddBookAsync("book1", "author1", "isbn1");
        await AddBookAsync("book2", "author2", "isbn2");
        await AddBookAsync("book3", "author3", "isbn3");

        var response = await this.client.GetAsync("/books/search?isbn=isbn2");

        response.EnsureSuccessStatusCode();
        var matchingBooks = await response.DeserializeAsync<List<Book>>();
        matchingBooks.Should().HaveCount(1);
        matchingBooks[0].Title.Should().Be("book2");
    }

    [Fact]
    public async Task SearchBooks_WithPagination_ReturnsPaginatedResults()
    {
        await AddBookAsync("book1", "author1", "isbn1");
        await AddBookAsync("book2", "author2", "isbn2");
        await AddBookAsync("book3", "author3", "isbn3");
        await AddBookAsync("book4", "author4", "isbn4");
        await AddBookAsync("book5", "author5", "isbn5");

        var response = await this.client.GetAsync("/books/search?title=book&page=2&pageSize=2");

        response.EnsureSuccessStatusCode();
        var paginatedBooks = await response.DeserializeAsync<List<Book>>();
        paginatedBooks.Should().HaveCount(2);
        paginatedBooks[0].Title.Should().Be("book3");
        paginatedBooks[1].Title.Should().Be("book4");
    }

    private async Task<int> AddBookAsync(string title, string author, string isbn)
    {
        var addRequest = new AddBookRequest(title, author, isbn);
        var addResponse = await this.client.PostAsync("/addBook", addRequest.GetHttpContent());
        var bookId = (await addResponse.DeserializeAsync<AddBookResponse>())!.BookId;
        return bookId;
    }

    private async Task<Book?> GetBookByIdAsync(int bookId)
    {
        var getResponse = await this.client.GetAsync($"/books/{bookId}");
        getResponse.EnsureSuccessStatusCode();
        return await getResponse.DeserializeAsync<Book>();
    }

}
