using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Centesimo.Infrastructure.Tests;

public sealed class RepositoryBase_should_expected_behavior
{
    [Fact]
    public async Task Convert_query_and_save_success_to_results()
    {
        using var database = new TestDatabase();
        var probe = new RepositoryBaseProbe(database.Context);
        var category = CreateCategory("Food");

        var saved = await probe.Save(category);
        var count = await probe.CountCategories();

        Assert.True(saved.IsSuccess);
        Assert.Equal(1, count.Value);
    }

    [Fact]
    public async Task Convert_database_update_exception_without_exposing_details()
    {
        using var database = new TestDatabase();
        var probe = new RepositoryBaseProbe(database.Context);
        await probe.Save(CreateCategory("Food"));

        var failed = await probe.Save(CreateCategory("Food"));

        Assert.Equal("Infrastructure.PersistenceFailure", failed.Error.Code);
        Assert.Equal("Non \u00E8 stato possibile salvare i dati.", failed.Error.Message);
    }

    [Fact]
    public async Task Convert_unexpected_exception_without_exposing_details()
    {
        using var database = new TestDatabase();
        var probe = new RepositoryBaseProbe(database.Context);

        var failed = await probe.ThrowFromQuery();

        Assert.Equal("Infrastructure.Unexpected", failed.Error.Code);
        Assert.Equal("Si \u00E8 verificato un errore imprevisto.", failed.Error.Message);
    }

    [Fact]
    public async Task Roll_back_transaction_when_mutation_fails()
    {
        using var database = new TestDatabase();
        var probe = new RepositoryBaseProbe(database.Context);

        var failed = await probe.FailTransaction(CreateCategory("Food"));
        database.Context.ChangeTracker.Clear();

        Assert.True(failed.IsFailure);
        Assert.Empty(await database.Context.Categories.ToListAsync());
    }

    [Fact]
    public async Task Propagate_cooperative_cancellation()
    {
        using var database = new TestDatabase();
        var probe = new RepositoryBaseProbe(database.Context);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            probe.CancelQuery(cancellation.Token));
    }
    private static Category CreateCategory(string name) =>
        new(Guid.NewGuid(), name, "cart", "#123456");

    private sealed class RepositoryBaseProbe(CentesimoDbContext context) : RepositoryBase(context)
    {
        public Task<Result> Save(Category category) =>
            SaveContext(db => db.Categories.Add(category));

        public Task<Result<int>> CountCategories() =>
            UseContext((db, token) => db.Categories.CountAsync(token));

        public Task<Result<int>> CancelQuery(CancellationToken cancellationToken) =>
            UseContext<int>((_, token) => Task.FromCanceled<int>(token), cancellationToken);
        public Task<Result<int>> ThrowFromQuery() =>
            UseContext<int>((_, _) => throw new InvalidOperationException("Technical detail"));

        public Task<Result<bool>> FailTransaction(Category category) =>
            SaveContextInTransaction<bool>((db, _) =>
            {
                db.Categories.Add(category);
                throw new InvalidOperationException("Technical detail");
            });
    }
}


