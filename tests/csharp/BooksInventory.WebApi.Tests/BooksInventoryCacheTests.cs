using BooksInventory.WebApi;
using BooksInventory.WebApi.Tests;
using BooksInventory.WebApi.Tests.Extensions;
using BooksInventory.WebApi.Tests.Factories;

using FluentAssertions;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

[Collection(nameof(IntegrationTestsCollection))]
public class BooksInventoryCacheTests : IAsyncLifetime
{

    private readonly CustomWebApplicationFactory factory;
    private readonly HttpClient client;
    private readonly BooksInventoryDbContext db;
    private readonly IDistributedCache redis;
    private readonly CompositeFixture fixture;

    public BooksInventoryCacheTests(CompositeFixture fixture)
    {
        this.fixture = fixture;
        this.factory = new CustomWebApplicationFactory(fixture);
        this.client = this.factory.CreateClient();

        // Create a scope to retrieve scoped instances of DB context and Redis cache
        // for direct database and cache interactions during tests
        var scope = this.factory.Services.CreateScope();
        this.db = scope.ServiceProvider.GetRequiredService<BooksInventoryDbContext>();
        this.redis = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
    }

    public async Task InitializeAsync()
    {
        // Clean the database and remove all books to ensure test isolation.
        db.Books.RemoveRange(db.Books);
        await db.SaveChangesAsync();
    }

    // Dispose resources to maintain isolation after each test run.
    public Task DisposeAsync()
    {
        this.client.Dispose();
        this.factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task InMemoryCacheHit_AfterWarmup()
    {
        var book = new Book
        {
            Title = "t1",
            Author = "a1",
            ISBN = "isbn1"
        };
        db.Books.Add(book);
        await db.SaveChangesAsync();

        // Cache warm-up (should populate both in-memory and Redis cache)
        await this.client.GetAsync($"/books/{book.Id}");

        // Mutate db and Redis to differ from the in-memory cache.
        await db.UpdateBookAsync(book with { Title = "t1_db_updated" });
        await redis.UpdateBookAsync(book with { Title = "t1_redis_updated" });

        // Request the book again (should return stale value from in-memory cache).
        var response = await this.client.GetAsync($"/books/{book.Id}");

        // Assert
        var bookFromInMemory = await response.DeserializeAsync<Book>();
        bookFromInMemory!.Title.Should().Be("t1");

        Book bookFromDb = await db.GetByIdAsync(book.Id);
        bookFromDb!.Title.Should().Be("t1_db_updated");

        var json = await redis.GetStringAsync(book.Id.GetCacheKey());
        json.Should().Contain("t1_redis_updated");
    }

    [Fact]
    public async Task RedisCacheHit_AfterWarmUp()
    {
        var book = new Book
        {
            Title = "t1",
            Author = "a1",
            ISBN = "isbn1"
        };
        db.Books.Add(book);
        await db.SaveChangesAsync();

        // Cache warm-up (should populate both in-memory and Redis cache)
        await this.client.GetAsync($"/books/{book.Id}");

        // App restart (should clear the in-memory cache)
        this.factory.Dispose();
        using var newFactory = new CustomWebApplicationFactory(this.fixture);
        using var newClient = newFactory.CreateClient();

        // Mutate db to differ from redis cache.
        await db.UpdateBookAsync(book with { Title = "t1_db_updated" });

        // Request the book again (should return stale value from redis cache).
        var response = await newClient.GetAsync($"/books/{book.Id}");

        // Assert
        var bookFromRedis = await response.DeserializeAsync<Book>();
        bookFromRedis!.Title.Should().Be("t1");
        Book bookFromDb = await db.GetByIdAsync(book.Id);
        bookFromDb!.Title.Should().Be("t1_db_updated");
    }
}