using Centesimo.Domain;

namespace Centesimo.Application;

public interface ITagRepository
{
    Task Add(Tag tag, CancellationToken cancellationToken = default);
    Task<Tag?> Get(Guid tagId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tag>> GetByCategory(Guid categoryId, CancellationToken cancellationToken = default);
    Task Update(Tag tag, CancellationToken cancellationToken = default);
}
