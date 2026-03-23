using Microsoft.Extensions.Configuration;
using MssqlConsoleSample.Api;
using MssqlFeature.ConnectionString;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var connectionString = SqlServerConnectionStringResolver.Resolve(configuration);

Console.WriteLine("Preparing database...");

await DatabaseInitializer.EnsureDatabaseReadyAsync(connectionString);

var repository = new AuthorRepository(connectionString);

await repository.SeedAsync();
var authors = await repository.GetAllAsync();

Console.WriteLine();
Console.WriteLine("Authors in DapperSampleDb:");

foreach (var author in authors)
{
    Console.WriteLine($"- {author.Id}: {author.FirstName} {author.LastName}");
}

Console.WriteLine();
Console.WriteLine("Done.");