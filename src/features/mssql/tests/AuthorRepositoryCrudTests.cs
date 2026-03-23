using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MssqlFeature.ConnectionString;
using MssqlConsoleSample.Api;
using Xunit;

namespace MssqlConsoleSample.Tests;

public sealed class AuthorRepositoryCrudTests : IAsyncLifetime
{
    private readonly string _connectionString;
    private readonly AuthorRepository _repository;

    public AuthorRepositoryCrudTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        _connectionString = SqlServerConnectionStringResolver.Resolve(configuration);

        _repository = new AuthorRepository(_connectionString);
    }

    public async Task InitializeAsync()
    {
        await DatabaseInitializer.EnsureDatabaseReadyAsync(_connectionString);
        await ResetAuthorsAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task InsertAsync_ShouldPersistAuthor()
    {
        var authorId = await _repository.InsertAsync("Frank", "Herbert");

        var author = await _repository.GetByIdAsync(authorId);

        Assert.NotNull(author);
        Assert.Equal("Frank", author.FirstName);
        Assert.Equal("Herbert", author.LastName);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnInsertedAuthors()
    {
        await _repository.InsertAsync("Mary", "Shelley");
        await _repository.InsertAsync("Ray", "Bradbury");

        var authors = await _repository.GetAllAsync();

        Assert.Equal(2, authors.Count);
        Assert.Contains(authors, author => author.FirstName == "Mary" && author.LastName == "Shelley");
        Assert.Contains(authors, author => author.FirstName == "Ray" && author.LastName == "Bradbury");
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyPersistedAuthor()
    {
        var authorId = await _repository.InsertAsync("Arthur", "C Clarke");

        var updated = await _repository.UpdateAsync(authorId, "Arthur", "Clarke");
        var author = await _repository.GetByIdAsync(authorId);

        Assert.True(updated);
        Assert.NotNull(author);
        Assert.Equal("Arthur", author.FirstName);
        Assert.Equal("Clarke", author.LastName);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemovePersistedAuthor()
    {
        var authorId = await _repository.InsertAsync("Philip", "Dick");

        var deleted = await _repository.DeleteAsync(authorId);
        var author = await _repository.GetByIdAsync(authorId);

        Assert.True(deleted);
        Assert.Null(author);
    }

    private async Task ResetAuthorsAsync()
    {
        const string sql = @"
DELETE FROM dbo.Authors;
DBCC CHECKIDENT ('dbo.Authors', RESEED, 0);";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync(sql);
    }
}