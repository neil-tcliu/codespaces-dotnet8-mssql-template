using Microsoft.Data.SqlClient;
using MssqlFeature.ConnectionString;

namespace MssqlCrudBackend.Features.Authors;

internal static class AuthorDatabaseBootstrapper
{
    public static async Task EnsureReadyAsync(IConfiguration configuration)
    {
        var connectionString = SqlServerConnectionStringResolver.Resolve(configuration);
        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;
        var dataFilePath = configuration["DatabaseFiles:DataFilePath"];
        var logFilePath = configuration["DatabaseFiles:LogFilePath"];

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("Initial Catalog must be set in the connection string.");
        }

        if (string.IsNullOrWhiteSpace(dataFilePath))
        {
            throw new InvalidOperationException("DatabaseFiles:DataFilePath must be set.");
        }

        if (string.IsNullOrWhiteSpace(logFilePath))
        {
            throw new InvalidOperationException("DatabaseFiles:LogFilePath must be set.");
        }

        var escapedDatabaseName = databaseName.Replace("]", "]]", StringComparison.Ordinal);
        var escapedLogicalDatabaseName = EscapeSqlLiteral(databaseName);
        var escapedLogicalLogName = EscapeSqlLiteral($"{databaseName}_log");
        var escapedDataFilePath = EscapeSqlLiteral(dataFilePath);
        var escapedLogFilePath = EscapeSqlLiteral(logFilePath);

        var masterConnectionString = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        }.ConnectionString;

        await using (var masterConnection = new SqlConnection(masterConnectionString))
        {
            await masterConnection.OpenAsync();

            await using var createDatabaseCommand = masterConnection.CreateCommand();
            createDatabaseCommand.CommandText = $@"
IF DB_ID(N'{databaseName}') IS NULL
BEGIN
    CREATE DATABASE [{escapedDatabaseName}]
    ON PRIMARY
    (
        NAME = N'{escapedLogicalDatabaseName}',
        FILENAME = N'{escapedDataFilePath}'
    )
    LOG ON
    (
        NAME = N'{escapedLogicalLogName}',
        FILENAME = N'{escapedLogFilePath}'
    );
END";

            await createDatabaseCommand.ExecuteNonQueryAsync();
        }

        await using var appConnection = new SqlConnection(connectionString);
        await appConnection.OpenAsync();

        await using var createTableCommand = appConnection.CreateCommand();
        createTableCommand.CommandText = @"
IF OBJECT_ID(N'dbo.Authors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Authors
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL
    );
END";

        await createTableCommand.ExecuteNonQueryAsync();
    }

    private static string EscapeSqlLiteral(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }
}