
using Testcontainers.Redis;

using Xunit;

public class RedisContainerFixture : IAsyncLifetime
{
    public RedisContainer Container { get; private set; }

    public RedisContainerFixture()
    {
        Container = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
    }

    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}