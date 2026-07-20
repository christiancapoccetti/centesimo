using System.IO.Compression;
using Centesimo.Infrastructure;
using Microsoft.Data.Sqlite;

namespace Centesimo.Infrastructure.Tests;

public sealed class MoneyManagerBackupReader_should_expected_behavior
{
    [Fact]
    public async Task Read_imports_only_supported_active_expenses()
    {
        await using var backup = await CreateBackup(SchemaAndData);

        var result = await new MoneyManagerBackupReader().Read(backup);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Categories);
        Assert.Single(result.Value.Tags);
        var expense = Assert.Single(result.Value.Expenses);
        Assert.Equal(1234, expense.AmountCents);
        Assert.Equal(new DateOnly(2026, 7, 20), expense.OccurredOn);
        Assert.Equal("Lunch", expense.Note);
        Assert.Equal(result.Value.Tags[0].SourceUid, expense.TagSourceUid);
        Assert.Equal(3, result.Value.IgnoredCount);
        Assert.Equal(1, result.Value.UncategorizedCount);
    }

    [Fact]
    public async Task Read_rejects_an_invalid_header()
    {
        await using var backup = new MemoryStream([1, 2, 3, 4]);

        var result = await new MoneyManagerBackupReader().Read(backup);

        Assert.True(result.IsFailure);
        Assert.Equal("Import.InvalidBackup", result.Error.Code);
    }

    [Fact]
    public async Task Read_rejects_an_unsupported_schema()
    {
        await using var backup = await CreateBackup("CREATE TABLE category (uid TEXT PRIMARY KEY);");

        var result = await new MoneyManagerBackupReader().Read(backup);

        Assert.True(result.IsFailure);
        Assert.Equal("Import.UnsupportedSchema", result.Error.Code);
    }

    [Fact]
    public async Task Read_rejects_too_many_archive_entries()
    {
        await using var backup = await CreateArchive(33);

        var result = await new MoneyManagerBackupReader().Read(backup);

        Assert.True(result.IsFailure);
        Assert.Equal("Import.InvalidArchive", result.Error.Code);
    }

    [Fact]
    public async Task Read_returns_a_failure_for_malformed_typed_values()
    {
        await using var backup = await CreateBackup(SchemaAndData.Replace("'Lunch'", "X'0102'"));

        var result = await new MoneyManagerBackupReader().Read(backup);

        Assert.True(result.IsFailure);
        Assert.Equal("Import.InvalidDatabase", result.Error.Code);
    }

    [Fact]
    public async Task Read_rejects_identifiers_over_the_supported_limit()
    {
        var longUid = new string('x', 201);
        await using var backup = await CreateBackup(SchemaAndData.Replace("expense-1", longUid));

        var result = await new MoneyManagerBackupReader().Read(backup);

        Assert.True(result.IsFailure);
        Assert.Equal("Import.InvalidDatabase", result.Error.Code);
    }
    private static async Task<MemoryStream> CreateArchive(int entryCount)
    {
        await using var zip = new MemoryStream();
        using (var archive = new ZipArchive(zip, ZipArchiveMode.Create, true))
            for (var index = 0; index < entryCount; index++)
            {
                var entry = archive.CreateEntry(index == 0 ? "MyFinance.db" : $"entry-{index}");
                await using var destination = entry.Open();
                await destination.WriteAsync(new byte[16]);
            }

        var result = new MemoryStream();
        await result.WriteAsync(new byte[] { 8, 8, 8, 8, 8, 8, 8, 8 });
        zip.Position = 0;
        await zip.CopyToAsync(result);
        result.Position = 0;
        return result;
    }
    private static async Task<MemoryStream> CreateBackup(string sql)
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"centesimo-test-{Guid.NewGuid():N}.db");
        try
        {
            await using (var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False"))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                await command.ExecuteNonQueryAsync();
            }

            await using var zip = new MemoryStream();
            using (var archive = new ZipArchive(zip, ZipArchiveMode.Create, true))
            {
                var entry = archive.CreateEntry("MyFinance.db");
                await using var destination = entry.Open();
                await using var source = File.OpenRead(databasePath);
                await source.CopyToAsync(destination);
            }

            var result = new MemoryStream();
            await result.WriteAsync(new byte[] { 8, 8, 8, 8, 8, 8, 8, 8 });
            zip.Position = 0;
            await zip.CopyToAsync(result);
            result.Position = 0;
            return result;
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    private const string SchemaAndData = """
        CREATE TABLE category (uid TEXT PRIMARY KEY, title TEXT, type TEXT, icon TEXT, color INTEGER, isRemoved INTEGER);
        CREATE TABLE tag (uid TEXT PRIMARY KEY, name TEXT, isRemoved INTEGER);
        CREATE TABLE [transaction] (uid TEXT PRIMARY KEY, type TEXT, amountInDefaultCurrency INTEGER, date TEXT, comment TEXT, isRemoved INTEGER);
        CREATE TABLE sync_link (entityType TEXT, entityUid TEXT, otherType TEXT, otherUid TEXT, isRemoved INTEGER);
        INSERT INTO category VALUES ('expense-category', 'Food', 'Expense', 'food', 1, 0);
        INSERT INTO category VALUES ('income-category', 'Salary', 'Income', 'money', 2, 0);
        INSERT INTO tag VALUES ('tag-1', 'Restaurant', 0);
        INSERT INTO [transaction] VALUES ('expense-1', 'Expense', 1234, '2026-07-20', 'Lunch', 0);
        INSERT INTO [transaction] VALUES ('income-1', 'Income', 5000, '2026-07-20', '', 0);
        INSERT INTO [transaction] VALUES ('removed-1', 'Expense', 200, '2026-07-20', '', 1);
        INSERT INTO [transaction] VALUES ('uncategorized-1', 'Expense', 300, '2026-07-20', '', 0);
        INSERT INTO [transaction] VALUES ('regular-1', 'Expense', 400, '2026-07-20', '', 0);
        INSERT INTO sync_link VALUES ('Transaction', 'expense-1', 'Category', 'expense-category', 0);
        INSERT INTO sync_link VALUES ('Transaction', 'expense-1', 'Tag', 'tag-1', 0);
        INSERT INTO sync_link VALUES ('Transaction', 'removed-1', 'Category', 'expense-category', 0);
        INSERT INTO sync_link VALUES ('Transaction', 'regular-1', 'Category', 'expense-category', 0);
        INSERT INTO sync_link VALUES ('RegularPaymentPeriod', 'period-1', 'Transaction', 'regular-1', 0);
        """;
}