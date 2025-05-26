using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BooksInventory.WebApi.Tests.Factories;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly CompositeFixture fixture;

    public CustomWebApplicationFactory(CompositeFixture fixture)
    {
        this.fixture = fixture;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddDbContext<BooksInventoryDbContext>(options =>
            {
                options.UseNpgsql(this.fixture.Postgres.Container.GetConnectionString());
            });
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = this.fixture.Redis.Container.GetConnectionString();
                options.InstanceName = "BooksInventoryCache:";
            });
        });

        builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            });
    }
}
