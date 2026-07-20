using Centesimo.Domain;

namespace Centesimo.Application;

public interface ICategoryRepository
{
    Task Add(Category category, CancellationToken cancellationToken = default);
    Task<Category?> Get(Guid categoryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetAll(CancellationToken cancellationToken = default);
    Task Update(Category category, CancellationToken cancellationToken = default);
}
