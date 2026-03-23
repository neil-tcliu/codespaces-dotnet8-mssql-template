using Dapper;
using Microsoft.Data.SqlClient;
using MssqlConsoleSample.Types;

namespace MssqlConsoleSample.Api;

internal sealed class AuthorRepository
{
    private readonly string _connectionString;

    public AuthorRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task SeedAsync()
    {
        const string sql = @"
IF NOT EXISTS (SELECT 1 FROM dbo.Authors)
BEGIN
    INSERT INTO dbo.Authors (FirstName, LastName)
    VALUES
        (N'Isaac', N'Asimov'),
        (N'Octavia', N'Butler'),
        (N'Ursula', N'Le Guin');
END";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync(sql);
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

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var authors = await connection.QueryAsync<Author>(sql);
        return authors.ToList();
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

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        return await connection.QuerySingleOrDefaultAsync<Author>(sql, new { Id = id });
    }

    public async Task<int> InsertAsync(string firstName, string lastName)
    {
        const string sql = @"
INSERT INTO dbo.Authors (FirstName, LastName)
OUTPUT INSERTED.Id
VALUES (@FirstName, @LastName);";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        return await connection.QuerySingleAsync<int>(sql, new
        {
            FirstName = firstName,
            LastName = lastName
        });
    }

    public async Task<bool> UpdateAsync(int id, string firstName, string lastName)
    {
        const string sql = @"
UPDATE dbo.Authors
SET FirstName = @FirstName,
    LastName = @LastName
WHERE Id = @Id;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var affectedRows = await connection.ExecuteAsync(sql, new
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName
        });

        return affectedRows == 1;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = @"
DELETE FROM dbo.Authors
WHERE Id = @Id;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var affectedRows = await connection.ExecuteAsync(sql, new { Id = id });
        return affectedRows == 1;
    }
}