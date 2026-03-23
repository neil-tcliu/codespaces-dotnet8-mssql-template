using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MssqlFeature.ConnectionString;

public static class SqlServerConnectionStringResolver
{
    public static string Resolve(IConfiguration configuration, string connectionStringName = "SqlServer")
    {
        var connectionString = configuration.GetConnectionString(connectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{connectionStringName}' is missing.");
        }

        var builder = new SqlConnectionStringBuilder(connectionString);
        var passwordOverride = configuration["MSSQL_SA_PASSWORD"];

        if (!string.IsNullOrWhiteSpace(passwordOverride))
        {
            builder.Password = passwordOverride;
        }

        return builder.ConnectionString;
    }
}