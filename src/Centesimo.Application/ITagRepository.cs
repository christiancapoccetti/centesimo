using Centesimo.Domain;

namespace Centesimo.Application;

public interface ITagRepository
{
    Task<Result> Add(Tag tag, CancellationToken cancellationToken = default);
    Task<Result<Tag?>> Get(Guid tagId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<Tag>>> GetByCategory(Guid categoryId, CancellationToken cancellationToken = default);
    Task<Result> Update(Tag tag, CancellationToken cancellationToken = default);
}
