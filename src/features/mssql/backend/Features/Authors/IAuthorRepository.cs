namespace MssqlCrudBackend.Features.Authors;

public interface IAuthorRepository
{
    Task<IReadOnlyList<Author>> GetAllAsync();

    Task<Author?> GetByIdAsync(int id);

    Task<Author> CreateAsync(CreateAuthorRequest request);

    Task<Author?> UpdateAsync(int id, UpdateAuthorRequest request);

    Task<bool> DeleteAsync(int id);
}