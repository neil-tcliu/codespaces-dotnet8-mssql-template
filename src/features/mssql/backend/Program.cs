using MssqlCrudBackend.Features.Authors;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddProblemDetails();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var useInMemoryRepository = builder.Environment.IsEnvironment("Testing")
        || builder.Configuration.GetValue<bool>("Authors:UseInMemoryRepository");
    var allowFallbackOnBootstrapFailure = builder.Configuration.GetValue("Authors:AllowInMemoryFallbackOnBootstrapFailure", true);

    if (!useInMemoryRepository)
    {
        try
        {
            await AuthorDatabaseBootstrapper.EnsureReadyAsync(builder.Configuration);
        }
        catch (Exception exception) when (allowFallbackOnBootstrapFailure)
        {
            useInMemoryRepository = true;
            Log.Warning(exception,
            "SQL Server bootstrap failed. Falling back to the in-memory author repository.");
        }
    }

    if (useInMemoryRepository)
    {
        builder.Services.AddSingleton<IAuthorRepository, InMemoryAuthorRepository>();
    }
    else
    {
        builder.Services.AddScoped<IAuthorRepository, SqlAuthorRepository>();
    }

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.UseSwagger();
    app.UseSwaggerUI();

    Log.Information("Starting backend with {RepositoryMode} repository in {Environment} environment.",
        useInMemoryRepository ? "in-memory" : "sqlserver",
        app.Environment.EnvironmentName);

    app.MapGet("/api/health", (IHostEnvironment environment) => Results.Ok(new
    {
        status = "ok",
        environment = environment.EnvironmentName,
        repository = useInMemoryRepository ? "in-memory" : "sqlserver"
    }));
    app.MapAuthorEndpoints();

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "The backend terminated unexpectedly.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;