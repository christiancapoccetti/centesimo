using System.Security.Cryptography;
using System.Text;

namespace Centesimo.Application;

public sealed record MoneyManagerCategory(string SourceUid, string Name, string Icon, string Color);
public sealed record MoneyManagerTag(string SourceUid, string CategorySourceUid, string Name);
public sealed record MoneyManagerExpense(string SourceUid, string CategorySourceUid, string? TagSourceUid,
    long AmountCents, DateOnly OccurredOn, string Note);
public sealed record MoneyManagerImportData(IReadOnlyList<MoneyManagerCategory> Categories,
    IReadOnlyList<MoneyManagerTag> Tags, IReadOnlyList<MoneyManagerExpense> Expenses,
    int IgnoredCount, int UncategorizedCount);
public sealed record MoneyManagerPersisted(int CategoriesAdded, int TagsAdded, int ExpensesAdded);
public sealed record MoneyManagerImportReport(int CategoriesAdded, int TagsAdded, int ExpensesAdded,
    int IgnoredCount, int UncategorizedCount);

public interface IMoneyManagerBackupReader
{
    Task<Result<MoneyManagerImportData>> Read(Stream backup, CancellationToken cancellationToken = default);
}

public interface IMoneyManagerImportRepository
{
    Task<Result<MoneyManagerPersisted>> Import(MoneyManagerImportData data,
        CancellationToken cancellationToken = default);
}

public sealed class MoneyManagerImportService(IMoneyManagerBackupReader reader, IMoneyManagerImportRepository repository)
{
    public async Task<Result<MoneyManagerImportReport>> Import(Stream backup,
        CancellationToken cancellationToken = default)
    {
        if (backup is null || !backup.CanRead)
            return Result<MoneyManagerImportReport>.Failure(MoneyManagerImportErrors.InvalidBackup);

        var read = await reader.Read(backup, cancellationToken);
        if (read.IsFailure)
            return Result<MoneyManagerImportReport>.Failure(read.Error);

        var persisted = await repository.Import(read.Value, cancellationToken);
        if (persisted.IsFailure)
            return Result<MoneyManagerImportReport>.Failure(persisted.Error);

        return Result<MoneyManagerImportReport>.Success(new MoneyManagerImportReport(
            persisted.Value.CategoriesAdded, persisted.Value.TagsAdded, persisted.Value.ExpensesAdded,
            read.Value.IgnoredCount, read.Value.UncategorizedCount));
    }
}

public static class MoneyManagerImportIds
{
    public static Guid Create(string entityType, string sourceUid)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"MoneyManager:{entityType}:{sourceUid}"));
        return new Guid(bytes[..16]);
    }
}

public static class MoneyManagerImportErrors
{
    public static readonly Error InvalidBackup = new("Import.InvalidBackup", "Il file di backup non è valido.");
    public static readonly Error InvalidArchive = new("Import.InvalidArchive", "Il backup non contiene un archivio valido.");
    public static readonly Error BackupTooLarge = new("Import.BackupTooLarge", "Il backup supera il limite di 256 MB.");
    public static readonly Error DatabaseMissing = new("Import.DatabaseMissing", "Il backup non contiene MyFinance.db.");
    public static readonly Error InvalidDatabase = new("Import.InvalidDatabase", "Il database del backup non è valido.");
    public static readonly Error UnsupportedSchema = new("Import.UnsupportedSchema", "La versione del backup non è supportata.");
}
