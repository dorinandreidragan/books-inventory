using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BooksInventory.WebApi.Tests.Factories;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string postgreSqlConnectionString;

    public CustomWebApplicationFactory(string postgreSqlConnectionString)
    {
        this.postgreSqlConnectionString = postgreSqlConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Overwrite the existing DB context registration so that tests use the PostgreSQL container.
        builder.ConfigureServices(services =>
        {
            services.AddDbContext<BooksInventoryDbContext>(options =>
            {
                options.UseNpgsql(postgreSqlConnectionString);
            });
        });
    }
}
