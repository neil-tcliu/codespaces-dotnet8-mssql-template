using Microsoft.Data.SqlClient;

namespace MssqlConsoleSample.Api;

internal static class DatabaseInitializer
{
    public static async Task EnsureDatabaseReadyAsync(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("Initial Catalog must be set in the connection string.");
        }

        var masterConnectionString = BuildMasterConnectionString(builder);

        await using (var masterConnection = new SqlConnection(masterConnectionString))
        {
            await masterConnection.OpenAsync();

            await using var createDatabaseCommand = masterConnection.CreateCommand();
            createDatabaseCommand.CommandText = $@"
IF DB_ID(N'{databaseName}') IS NULL
BEGIN
    CREATE DATABASE [{databaseName}];
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

    private static string BuildMasterConnectionString(SqlConnectionStringBuilder builder)
    {
        var masterBuilder = new SqlConnectionStringBuilder(builder.ConnectionString)
        {
            InitialCatalog = "master"
        };

        return masterBuilder.ConnectionString;
    }
}