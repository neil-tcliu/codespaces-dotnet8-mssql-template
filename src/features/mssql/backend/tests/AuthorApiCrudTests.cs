using System.Net;
using System.Net.Http.Json;
using MssqlCrudBackend.Features.Authors;
using Xunit;

namespace MssqlCrudBackend.Tests;

public sealed class AuthorApiCrudTests
{
    [Fact]
    public async Task PostAndGet_ShouldReturnCreatedAuthor()
    {
        await using var factory = new MssqlCrudBackendFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/authors/", new CreateAuthorRequest("Isaac", "Asimov"));
        var createdAuthor = await createResponse.Content.ReadFromJsonAsync<Author>();
        var getResponse = await client.GetAsync($"/api/authors/{createdAuthor!.Id}");
        var fetchedAuthor = await getResponse.Content.ReadFromJsonAsync<Author>();

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(fetchedAuthor);
        Assert.Equal("Isaac", fetchedAuthor.FirstName);
        Assert.Equal("Asimov", fetchedAuthor.LastName);
    }

    [Fact]
    public async Task GetAll_ShouldReturnInsertedAuthors()
    {
        await using var factory = new MssqlCrudBackendFactory();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/authors/", new CreateAuthorRequest("Octavia", "Butler"));
        await client.PostAsJsonAsync("/api/authors/", new CreateAuthorRequest("Ursula", "Le Guin"));

        var response = await client.GetAsync("/api/authors/");
        var authors = await response.Content.ReadFromJsonAsync<List<Author>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(authors);
        Assert.Equal(2, authors.Count);
    }

    [Fact]
    public async Task Put_ShouldUpdateExistingAuthor()
    {
        await using var factory = new MssqlCrudBackendFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/authors/", new CreateAuthorRequest("Arthur", "C Clarke"));
        var createdAuthor = await createResponse.Content.ReadFromJsonAsync<Author>();

        var updateResponse = await client.PutAsJsonAsync($"/api/authors/{createdAuthor!.Id}", new UpdateAuthorRequest("Arthur", "Clarke"));
        var updatedAuthor = await updateResponse.Content.ReadFromJsonAsync<Author>();

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updatedAuthor);
        Assert.Equal("Clarke", updatedAuthor.LastName);
    }

    [Fact]
    public async Task Delete_ShouldRemoveExistingAuthor()
    {
        await using var factory = new MssqlCrudBackendFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/authors/", new CreateAuthorRequest("Philip", "Dick"));
        var createdAuthor = await createResponse.Content.ReadFromJsonAsync<Author>();

        var deleteResponse = await client.DeleteAsync($"/api/authors/{createdAuthor!.Id}");
        var getResponse = await client.GetAsync($"/api/authors/{createdAuthor.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}