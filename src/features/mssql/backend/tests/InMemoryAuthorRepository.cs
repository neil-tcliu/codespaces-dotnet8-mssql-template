using MssqlCrudBackend.Features.Authors;

namespace MssqlCrudBackend.Tests;

internal sealed class InMemoryAuthorRepository : IAuthorRepository
{
    private readonly List<Author> _authors = [];
    private int _nextId = 1;

    public Task<IReadOnlyList<Author>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyList<Author>>(_authors.OrderBy(author => author.Id).ToList());
    }

    public Task<Author?> GetByIdAsync(int id)
    {
        return Task.FromResult(_authors.SingleOrDefault(author => author.Id == id));
    }

    public Task<Author> CreateAsync(CreateAuthorRequest request)
    {
        var author = new Author(_nextId++, request.FirstName, request.LastName);
        _authors.Add(author);
        return Task.FromResult(author);
    }

    public Task<Author?> UpdateAsync(int id, UpdateAuthorRequest request)
    {
        var index = _authors.FindIndex(author => author.Id == id);
        if (index < 0)
        {
            return Task.FromResult<Author?>(null);
        }

        var updated = new Author(id, request.FirstName, request.LastName);
        _authors[index] = updated;
        return Task.FromResult<Author?>(updated);
    }

    public Task<bool> DeleteAsync(int id)
    {
        var removed = _authors.RemoveAll(author => author.Id == id) == 1;
        return Task.FromResult(removed);
    }
}