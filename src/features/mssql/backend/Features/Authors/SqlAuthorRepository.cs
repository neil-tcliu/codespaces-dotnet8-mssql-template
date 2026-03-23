using Dapper;
using Microsoft.Data.SqlClient;
using MssqlFeature.ConnectionString;

namespace MssqlCrudBackend.Features.Authors;

internal sealed class SqlAuthorRepository : IAuthorRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqlAuthorRepository> _logger;

    public SqlAuthorRepository(IConfiguration configuration, ILogger<SqlAuthorRepository> logger)
    {
        _connectionString = SqlServerConnectionStringResolver.Resolve(configuration);
        _logger = logger;
    }

    public async Task<IReadOnlyList<Author>> GetAllAsync()
    {
        const string sql = @"
SELECT
    Id,
    FirstName,
    LastName
FROM dbo.Authors
ORDER BY Id;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var authors = await connection.QueryAsync<Author>(sql);
            var authorList = authors.ToList();
            _logger.LogInformation("Loaded {AuthorCount} authors from SQL Server.", authorList.Count);
            return authorList;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to load authors from SQL Server.");
            throw;
        }
    }

    public async Task<Author?> GetByIdAsync(int id)
    {
        const string sql = @"
SELECT
    Id,
    FirstName,
    LastName
FROM dbo.Authors
WHERE Id = @Id;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var author = await connection.QuerySingleOrDefaultAsync<Author>(sql, new { Id = id });
            _logger.LogInformation(author is null
                ? "Author {AuthorId} was not found in SQL Server."
                : "Loaded author {AuthorId} from SQL Server.",
                id);
            return author;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to load author {AuthorId} from SQL Server.", id);
            throw;
        }
    }

    public async Task<Author> CreateAsync(CreateAuthorRequest request)
    {
        const string sql = @"
INSERT INTO dbo.Authors (FirstName, LastName)
OUTPUT INSERTED.Id, INSERTED.FirstName, INSERTED.LastName
VALUES (@FirstName, @LastName);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var author = await connection.QuerySingleAsync<Author>(sql, new
            {
                request.FirstName,
                request.LastName
            });

            _logger.LogInformation("Created author {AuthorId} in SQL Server.", author.Id);
            return author;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to create author in SQL Server.");
            throw;
        }
    }

    public async Task<Author?> UpdateAsync(int id, UpdateAuthorRequest request)
    {
        const string sql = @"
UPDATE dbo.Authors
SET FirstName = @FirstName,
    LastName = @LastName
OUTPUT INSERTED.Id, INSERTED.FirstName, INSERTED.LastName
WHERE Id = @Id;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);

            var author = await connection.QuerySingleOrDefaultAsync<Author>(sql, new
            {
                Id = id,
                request.FirstName,
                request.LastName
            });

            _logger.LogInformation(author is null
                ? "Author {AuthorId} was not found for update in SQL Server."
                : "Updated author {AuthorId} in SQL Server.",
                id);
            return author;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to update author {AuthorId} in SQL Server.", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = @"
DELETE FROM dbo.Authors
WHERE Id = @Id;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var affectedRows = await connection.ExecuteAsync(sql, new { Id = id });
            _logger.LogInformation(affectedRows == 1
                ? "Deleted author {AuthorId} from SQL Server."
                : "Author {AuthorId} was not found for deletion in SQL Server.",
                id);
            return affectedRows == 1;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to delete author {AuthorId} from SQL Server.", id);
            throw;
        }
    }
}