using Centesimo.Application;
using Microsoft.EntityFrameworkCore;

namespace Centesimo.Infrastructure;

public abstract class RepositoryBase
{
    protected readonly CentesimoDbContext Context;

    protected RepositoryBase(CentesimoDbContext context) =>
        Context = context ?? throw new ArgumentNullException(nameof(context));

    protected async Task<Result<TValue>> UseContext<TValue>(
        Func<CentesimoDbContext, CancellationToken, Task<TValue>> query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Result<TValue>.Success(await query(Context, cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateException)
        {
            return Result<TValue>.Failure(InfrastructureErrors.PersistenceFailure);
        }
        catch (Exception)
        {
            return Result<TValue>.Failure(InfrastructureErrors.Unexpected);
        }
    }

    protected async Task<Result> SaveContext(
        Action<CentesimoDbContext> mutation,
        CancellationToken cancellationToken = default)
    {
        try
        {
            mutation(Context);
            await Context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateException)
        {
            return Result.Failure(InfrastructureErrors.PersistenceFailure);
        }
        catch (Exception)
        {
            return Result.Failure(InfrastructureErrors.Unexpected);
        }
    }

    protected async Task<Result> SaveContext(
        Func<CentesimoDbContext, CancellationToken, Task> mutation,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await mutation(Context, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateException)
        {
            return Result.Failure(InfrastructureErrors.PersistenceFailure);
        }
        catch (Exception)
        {
            return Result.Failure(InfrastructureErrors.Unexpected);
        }
    }

    protected async Task<Result<TValue>> SaveContextInTransaction<TValue>(
        Func<CentesimoDbContext, CancellationToken, Task<TValue>> mutation,
        CancellationToken cancellationToken = default,
        bool clearChangeTracker = false)
    {
        try
        {
            await using var transaction = await Context.Database
                .BeginTransactionAsync(cancellationToken);
            var value = await mutation(Context, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result<TValue>.Success(value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateException)
        {
            return Result<TValue>.Failure(InfrastructureErrors.PersistenceFailure);
        }
        catch (Exception)
        {
            return Result<TValue>.Failure(InfrastructureErrors.Unexpected);
        }
        finally
        {
            if (clearChangeTracker)
                Context.ChangeTracker.Clear();
        }
    }
}
