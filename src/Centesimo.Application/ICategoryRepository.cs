using Centesimo.Domain;

namespace Centesimo.Application;

public interface ICategoryRepository
{
    Task<Result> Add(Category category, CancellationToken cancellationToken = default);
    Task<Result<Category?>> Get(Guid categoryId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<Category>>> GetAll(CancellationToken cancellationToken = default);
    Task<Result> Update(Category category, CancellationToken cancellationToken = default);
}
