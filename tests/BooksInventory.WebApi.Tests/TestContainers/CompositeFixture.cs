
using BooksInventory.WebApi.Tests.TestContainers;

using Xunit;

public class CompositeFixture : IAsyncLifetime
{
    public PostgreSqlContainerFixture Postgres { get; private set; }
    public RedisContainerFixture Redis { get; private set; }

    public CompositeFixture()
    {
        Postgres = new PostgreSqlContainerFixture();
        Redis = new RedisContainerFixture();
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            Postgres.InitializeAsync(),
            Redis.InitializeAsync()
        );
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(
            Postgres.DisposeAsync(),
            Redis.DisposeAsync()
        );
    }
}