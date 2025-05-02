using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BooksInventory.WebApi.Tests;

public class BookInventoryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BookInventoryTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AddBook_ReturnsBookId()
    {
        var request = new AddBookRequest("AI Engineering", "Chip Huyen", "1098166302");
        var content = request.GetHttpContent();

        var response = await _client.PostAsync("/addBook", content);

        response.EnsureSuccessStatusCode();
        var result = await response.DeserializeAsync<AddBookResponse>();
        result?.Should().NotBeNull();
        result!.BookId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetBook_ReturnsBookDetails()
    {
        var addRequest = new AddBookRequest("AI Engineering", "Chip Huyen", "1234567890");
        var addResponse = await _client.PostAsync("/addBook", addRequest.GetHttpContent());
        var bookId = (await addResponse.DeserializeAsync<AddBookResponse>())?.BookId;

        var getResponse = await _client.GetAsync($"/books/{bookId}");

        getResponse.EnsureSuccessStatusCode();
        var book = await getResponse.DeserializeAsync<Book>();
        book.Should().BeEquivalentTo(
            new Book(
                addRequest.Title,
                addRequest.Author,
                addRequest.ISBN));
    }
}