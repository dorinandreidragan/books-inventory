## Commands

```bash
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis --version 9.0.2

dotnet add package Testcontainers.Redis --version 4.4.0
```

## Flow

- Want to add caching because: // TODO: explain why caching is needed.
- Testing the cache is working as expected
  - Refactor to use composite fixture class
    - Postgres and Redis fixture
  - Refactor the CustomWebApplicationFixture class to allow service injection and decouple from connection strings
  - Requires isolation to be able to test the cache
    - Using repository pattern in this case

## Topics

- composite test fixture
- HybridCache
  - stampede protection
  - two cache stages: in-memory, out-of-process
  - vs IDistributedCache
  - how to test
- CustomWebApplicationFactory improved, the services are injected, no more coupling on the connection strings
- Logging

## References

- Distributed caching in ASP.NET Core: https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-9.0
- Response caching: https://learn.microsoft.com/en-us/aspnet/core/performance/caching/response?view=aspnetcore-9.0
- HybridCache in ASP.NET Core
- Testconainers Redis module: https://testcontainers.com/modules/redis/?language=dotnet
