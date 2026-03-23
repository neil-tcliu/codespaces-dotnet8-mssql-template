namespace MssqlCrudBackend.Features.Authors;

internal sealed class AuthorEndpointsLog;

public static class AuthorEndpoints
{
    public static IEndpointRouteBuilder MapAuthorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/authors").WithTags("Authors");

        group.MapGet("/", async (IAuthorRepository repository, ILogger<AuthorEndpointsLog> logger) =>
        {
            logger.LogInformation("Handling request to list authors.");
            var authors = await repository.GetAllAsync();
            return Results.Ok(authors);
        });

        group.MapGet("/{id:int}", async (int id, IAuthorRepository repository, ILogger<AuthorEndpointsLog> logger) =>
        {
            logger.LogInformation("Handling request to get author {AuthorId}.", id);
            var author = await repository.GetByIdAsync(id);
            return author is null ? Results.NotFound() : Results.Ok(author);
        });

        group.MapPost("/", async (CreateAuthorRequest request, IAuthorRepository repository, ILogger<AuthorEndpointsLog> logger) =>
        {
            var validationError = Validate(request.FirstName, request.LastName);
            if (validationError is not null)
            {
                logger.LogWarning("Create author validation failed.");
                return validationError;
            }

            logger.LogInformation("Creating author {FirstName} {LastName}.", request.FirstName, request.LastName);
            var author = await repository.CreateAsync(request);
            return Results.Created($"/api/authors/{author.Id}", author);
        });

        group.MapPut("/{id:int}", async (int id, UpdateAuthorRequest request, IAuthorRepository repository, ILogger<AuthorEndpointsLog> logger) =>
        {
            var validationError = Validate(request.FirstName, request.LastName);
            if (validationError is not null)
            {
                logger.LogWarning("Update author validation failed for author {AuthorId}.", id);
                return validationError;
            }

            logger.LogInformation("Updating author {AuthorId}.", id);
            var author = await repository.UpdateAsync(id, request);
            return author is null ? Results.NotFound() : Results.Ok(author);
        });

        group.MapDelete("/{id:int}", async (int id, IAuthorRepository repository, ILogger<AuthorEndpointsLog> logger) =>
        {
            logger.LogInformation("Deleting author {AuthorId}.", id);
            var deleted = await repository.DeleteAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return app;
    }

    private static IResult? Validate(string firstName, string lastName)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(firstName))
        {
            errors["firstName"] = ["FirstName is required."];
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            errors["lastName"] = ["LastName is required."];
        }

        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }
}