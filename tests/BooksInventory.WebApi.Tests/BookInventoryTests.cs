using Docker.DotNet.Models;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BooksInventory.WebApi.Tests;

[CollectionDefinition(nameof(IntegrationTestsCollection))]
public class IntegrationTestsCollection : ICollectionFixture<PostgreSqlContainerFixture> { }

[Collection(nameof(IntegrationTestsCollection))]
public class BooksInventoryTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory factory;
    private readonly HttpClient client;
    private readonly BooksInventoryDbContext dbContext;

    public BooksInventoryTests(PostgreSqlContainerFixture fixture)
    {
        this.factory = new CustomWebApplicationFactory(fixture.Postgres.GetConnectionString());
        this.client = this.factory.CreateClient();

        // Create a scope to retrieve a scoped instance of the DB context.
        // This allows direct interaction with the database for setup and teardown.
        var scope = this.factory.Services.CreateScope();
        dbContext = scope.ServiceProvider.GetRequiredService<BooksInventoryDbContext>();
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
        var bookId = (await addResponse.DeserializeAsync<AddBookResponse>())?.BookId;

        var getResponse = await this.client.GetAsync($"/books/{bookId}");

        getResponse.EnsureSuccessStatusCode();
        var book = await getResponse.DeserializeAsync<Book>();
        book.Should().BeEquivalentTo(
            new Book
            {
                Id = bookId!.Value,
                Title = addRequest.Title,
                Author = addRequest.Author,
                ISBN = addRequest.ISBN
            });
    }
}