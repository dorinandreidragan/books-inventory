using Microsoft.EntityFrameworkCore;

using Testcontainers.PostgreSql;

using Xunit;

namespace BooksInventory.WebApi.Tests.TestContainers;

public class PostgreSqlContainerFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; private set; }

    public PostgreSqlContainerFixture()
    {
        Postgres = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await Postgres.StartAsync();

        // Ensure that the database schema is created by applying migrations.
        var options = new DbContextOptionsBuilder<BooksInventoryDbContext>()
            .UseNpgsql(Postgres.GetConnectionString())
            .Options;

        using var context = new BooksInventoryDbContext(options);
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Postgres.DisposeAsync().AsTask();
}
