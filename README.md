# 📚 Books Inventory: A Hands-On Guide to Modern Software Development

![Build Status](https://github.com/dorinandreidragan/books-inventory/actions/workflows/ci.yml/badge.svg)

## 🚀 Introduction

The **Books Inventory** repository is a hands-on demonstration of modern software development and
system design techniques using .NET and a variety of tools. This project serves as a learning
resource for developers who want to explore best practices in building, testing, and deploying
robust web applications.

The repository showcases:

- 🛠️ Building a minimal Web API with ASP.NET.
- ✅ Writing clean and effective integration tests.
- 🐳 Leveraging tools like Testcontainers for advanced testing scenarios.
- 🏗️ Applying modern system design principles.

Stay tuned for more episodes as we continue to expand this repository! 🚀

## 🏁 Getting Started

To set up your local environment and test the REST API, follow these steps:

1. Clone the repository:

   ```bash
   git clone https://github.com/dorinandreidragan/books-inventory.git
   ```

2. Navigate to the project directory:

   ```bash
   cd books-inventory
   ```

3. Start the required services using Docker Compose:

   ```bash
   docker-compose up -d
   ```

4. Initialize the database using the `dotnet ef` tool:

   ```bash
   dotnet ef database update --project src/BooksInventory.WebApi/BooksInventory.WebApi.csproj
   ```

5. Build and run the application:

   ```bash
   dotnet run --project src/BooksInventory.WebApi/BooksInventory.WebApi.csproj
   ```

6. Explore the API using the provided HTTP file:

   Open `BooksInventory.http` in your favorite HTTP client (e.g., VS Code REST Client) to test the API
   endpoints.

## 🏗️ Building

To build the project, ensure you have the .NET SDK installed. Then, run the following command:

```bash
 dotnet build
```

## 🧪 Testing

Run the tests using the following command:

```bash
 dotnet test
```

The tests include:

- 🔍 Integration tests for the Web API.
- 🐳 Tests leveraging Testcontainers for database interactions.

## 🔄 Continuous Integration

This repository uses GitHub Actions for Continuous Integration (CI). The CI pipeline is defined in
the `.github/workflows/ci.yml` file. Every pull request and commit to the main branch triggers the CI pipeline automatically, ensuring that the codebase remains stable and reliable.

## 📖 Documentation

The `docs` folder contains detailed guides and tutorials:

- **Episode 1**: [Testing Minimal Web APIs with ASP.NET](./docs/01-testing-minimal-web-api.md)
- **Episode 2**: [Get Ready for Testcontainers](./docs/02-testcontainers-postgresql.md)
- **Episode 3**: [HybridCache & Redis: Cache Smarter, Not Harder for ASP.NET APIs](./docs/03-testcontainers-redis.md)
- **Episode 4**: [If you can't observe it, you can't operate it](./docs/04-opentelemetry-tracing.md)
- **Episode 5**: [The hidden cost of caching: The hidden cost of caching: detecting stale reads with locust](./docs/05-hidden-cost-of-caching.md)

More episodes will be added to cover advanced topics in software development and system design.

## ✨ Features

- **ASP.NET Minimal Web API**: A lightweight and modern approach to building web APIs.
- **Integration Testing**: Comprehensive tests to ensure the reliability of the application.
- **Testcontainers**: Simplified testing with containerized dependencies like PostgreSQL.
- **System Design**: Practical examples of designing scalable and maintainable systems.

## 🤝 Contribute

Contributions are welcome! Whether it's fixing a bug, adding a new feature, or improving
documentation, feel free to open issues or submit pull requests. Let's make this project even better
together! 💡

## 📜 License

This project is licensed under the MIT License. See the [LICENSE](./LICENSE) file for details.

## 🙌 Acknowledgments

This repository is inspired by the need to provide practical, real-world examples of modern software
development practices. Special thanks to the contributors and the open-source community for their
support.
