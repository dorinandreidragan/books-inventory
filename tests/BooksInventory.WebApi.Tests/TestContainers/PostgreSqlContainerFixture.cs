using Microsoft.EntityFrameworkCore;

using Testcontainers.PostgreSql;

using Xunit;

namespace BooksInventory.WebApi.Tests.TestContainers;

public class PostgreSqlContainerFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; private set; }

    public PostgreSqlContainerFixture()
    {
        Container = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await Container.StartAsync();

        // Ensure that the database schema is created by applying migrations.
        var options = new DbContextOptionsBuilder<BooksInventoryDbContext>()
            .UseNpgsql(Container.GetConnectionString())
            .Options;

        using var context = new BooksInventoryDbContext(options);
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}
