using Xunit;

namespace BooksInventory.WebApi.Tests;

[CollectionDefinition(nameof(IntegrationTestsCollection))]
public class IntegrationTestsCollection : ICollectionFixture<CompositeFixture> { }
