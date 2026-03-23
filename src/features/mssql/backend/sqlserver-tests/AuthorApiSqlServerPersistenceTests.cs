using System.Net;
using System.Net.Http.Json;
using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MssqlCrudBackend.Features.Authors;
using MssqlFeature.ConnectionString;
using Xunit;

namespace MssqlCrudBackend.SqlServerTests;

public sealed class AuthorApiSqlServerPersistenceTests
{
    [Fact]
    public async Task Health_ShouldReportSqlServerRepository()
    {
        await using var factory = new SqlServerMssqlCrudBackendFactory();
        using var client = factory.CreateClient();

        var health = await client.GetFromJsonAsync<HealthResponse>("/api/health");

        Assert.NotNull(health);
        Assert.Equal("ok", health.Status);
        Assert.Equal("sqlserver", health.Repository);
    }

    [Fact]
    public async Task Post_ShouldPersistAuthorToSqlServer_AndRemainReadableAfterRestart()
    {
        int authorId;

        await using (var firstFactory = new SqlServerMssqlCrudBackendFactory())
        {
            using var firstClient = firstFactory.CreateClient();
            await AssertSqlServerModeAsync(firstClient);

            var connectionString = ResolveConnectionString(firstFactory);
            await ResetAuthorsAsync(connectionString);

            var createResponse = await firstClient.PostAsJsonAsync("/api/authors/", new CreateAuthorRequest("Nnedi", "Okorafor"));
            var createdAuthor = await createResponse.Content.ReadFromJsonAsync<Author>();

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            Assert.NotNull(createdAuthor);

            var persistedAuthor = await LoadAuthorDirectlyAsync(connectionString, createdAuthor!.Id);

            Assert.NotNull(persistedAuthor);
            Assert.Equal("Nnedi", persistedAuthor!.FirstName);
            Assert.Equal("Okorafor", persistedAuthor.LastName);

            authorId = createdAuthor.Id;
        }

        await using var secondFactory = new SqlServerMssqlCrudBackendFactory();
        using var secondClient = secondFactory.CreateClient();
        await AssertSqlServerModeAsync(secondClient);

        var getResponse = await secondClient.GetAsync($"/api/authors/{authorId}");
        var fetchedAuthor = await getResponse.Content.ReadFromJsonAsync<Author>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(fetchedAuthor);
        Assert.Equal("Nnedi", fetchedAuthor!.FirstName);
        Assert.Equal("Okorafor", fetchedAuthor.LastName);
    }

    private static string ResolveConnectionString(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        return SqlServerConnectionStringResolver.Resolve(configuration);
    }

    private static async Task AssertSqlServerModeAsync(HttpClient client)
    {
        var health = await client.GetFromJsonAsync<HealthResponse>("/api/health");

        Assert.NotNull(health);
        Assert.Equal("sqlserver", health!.Repository);
    }

    private static async Task<Author?> LoadAuthorDirectlyAsync(string connectionString, int authorId)
    {
        const string sql = @"
SELECT
    Id,
    FirstName,
    LastName
FROM dbo.Authors
WHERE Id = @Id;";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        return await connection.QuerySingleOrDefaultAsync<Author>(sql, new { Id = authorId });
    }

    private static async Task ResetAuthorsAsync(string connectionString)
    {
        const string sql = @"
DELETE FROM dbo.Authors;
DBCC CHECKIDENT ('dbo.Authors', RESEED, 0);";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync(sql);
    }

    private sealed record HealthResponse(string Status, string Environment, string Repository);
}