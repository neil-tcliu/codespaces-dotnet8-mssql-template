namespace MssqlCrudBackend.Features.Authors;

internal sealed class InMemoryAuthorRepository : IAuthorRepository
{
    private readonly List<Author> _authors = [];
    private readonly ILogger<InMemoryAuthorRepository> _logger;
    private int _nextId = 1;

    public InMemoryAuthorRepository(ILogger<InMemoryAuthorRepository> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<Author>> GetAllAsync()
    {
        var authors = _authors.OrderBy(author => author.Id).ToList();
        _logger.LogInformation("Loaded {AuthorCount} authors from the in-memory repository.", authors.Count);
        return Task.FromResult<IReadOnlyList<Author>>(authors);
    }

    public Task<Author?> GetByIdAsync(int id)
    {
        var author = _authors.SingleOrDefault(existingAuthor => existingAuthor.Id == id);
        _logger.LogInformation(author is null
            ? "Author {AuthorId} was not found in the in-memory repository."
            : "Loaded author {AuthorId} from the in-memory repository.",
            id);
        return Task.FromResult(author);
    }

    public Task<Author> CreateAsync(CreateAuthorRequest request)
    {
        var author = new Author(_nextId++, request.FirstName, request.LastName);
        _authors.Add(author);
        _logger.LogInformation("Created author {AuthorId} in the in-memory repository.", author.Id);
        return Task.FromResult(author);
    }

    public Task<Author?> UpdateAsync(int id, UpdateAuthorRequest request)
    {
        var index = _authors.FindIndex(author => author.Id == id);
        if (index < 0)
        {
            _logger.LogInformation("Author {AuthorId} was not found for update in the in-memory repository.", id);
            return Task.FromResult<Author?>(null);
        }

        var updated = new Author(id, request.FirstName, request.LastName);
        _authors[index] = updated;
        _logger.LogInformation("Updated author {AuthorId} in the in-memory repository.", id);
        return Task.FromResult<Author?>(updated);
    }

    public Task<bool> DeleteAsync(int id)
    {
        var removed = _authors.RemoveAll(author => author.Id == id) == 1;
        _logger.LogInformation(removed
            ? "Deleted author {AuthorId} from the in-memory repository."
            : "Author {AuthorId} was not found for deletion in the in-memory repository.",
            id);
        return Task.FromResult(removed);
    }
}