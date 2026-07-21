using System.IO.Compression;
using Centesimo.Infrastructure;
using Centesimo.Domain;
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
        Assert.Equal(25000, result.Value.Categories[0].MonthlyBudgetCents);
        Assert.Single(result.Value.Tags);
        Assert.Equal(2, result.Value.Expenses.Count);
        var expense = Assert.Single(result.Value.Expenses, value => value.SourceUid == "expense-1");
        Assert.Equal(1234, expense.AmountCents);
        Assert.Equal(new DateOnly(2026, 7, 20), expense.OccurredOn);
        Assert.Equal("Lunch", expense.Note);
        Assert.Equal(result.Value.Tags[0].SourceUid, expense.TagSourceUid);
        var regularExpense = Assert.Single(result.Value.Expenses, value => value.SourceUid == "regular-1");
        Assert.Equal(400, regularExpense.AmountCents);
        Assert.Equal(2, result.Value.IgnoredCount);
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
    public async Task Read_maps_active_monthly_regular_payment_and_keeps_its_historical_occurrence()
    {
        await using var backup = await CreateBackup(SchemaAndData + """
            CREATE TABLE reminding (uid TEXT PRIMARY KEY, period TEXT, transactionType TEXT, startDate TEXT, endDate TEXT, lastTransactionDate TEXT, enabled INTEGER, isRemoved INTEGER, remindingType TEXT);
            CREATE TABLE regular_payment_period (uid TEXT PRIMARY KEY, startDate TEXT, endDate TEXT, lastTransactionDate TEXT, isRemoved INTEGER);
            INSERT INTO reminding VALUES ('reminder-1','Month','Expense','2026-07-20','','2026-07-20',1,0,'regularPayments');
            INSERT INTO regular_payment_period VALUES ('period-1','2026-07-20','','2026-07-20',0);
            INSERT INTO sync_link VALUES ('Reminding','reminder-1','RegularPaymentPeriod','period-1',0);
            """);

        var result = await new MoneyManagerBackupReader().Read(backup);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.Expenses, value => value.SourceUid == "regular-1");
        var payment = Assert.Single(result.Value.RecurringPaymentsOrEmpty);
        Assert.Equal("reminder-1", payment.SourceUid);
        Assert.Equal(RecurrenceFrequency.Monthly, payment.Frequency);
        Assert.Equal(new DateOnly(2026, 8, 20), payment.NextDueOn);
    }

    [Fact]
    public async Task Read_advances_past_a_regular_payment_occurrence_recorded_today()
    {
        var schema = SchemaAndData.Replace("2026-07-20", "2026-07-21") + """
            CREATE TABLE reminding (uid TEXT PRIMARY KEY, period TEXT, transactionType TEXT, startDate TEXT, endDate TEXT, lastTransactionDate TEXT, enabled INTEGER, isRemoved INTEGER, remindingType TEXT);
            CREATE TABLE regular_payment_period (uid TEXT PRIMARY KEY, startDate TEXT, endDate TEXT, lastTransactionDate TEXT, isRemoved INTEGER);
            INSERT INTO reminding VALUES ('reminder-today','Month','Expense','2026-07-21','','2026-07-21',1,0,'regularPayments');
            INSERT INTO regular_payment_period VALUES ('period-1','2026-07-21','','2026-07-21',0);
            INSERT INTO sync_link VALUES ('Reminding','reminder-today','RegularPaymentPeriod','period-1',0);
            """;
        await using var backup = await CreateBackup(schema);

        var result = await new MoneyManagerBackupReader().Read(backup);

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateOnly(2026, 8, 21), Assert.Single(result.Value.RecurringPaymentsOrEmpty).NextDueOn);
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
        CREATE TABLE category (uid TEXT PRIMARY KEY, title TEXT, type TEXT, icon TEXT, color INTEGER, limitAmount INTEGER, isRemoved INTEGER);
        CREATE TABLE tag (uid TEXT PRIMARY KEY, name TEXT, isRemoved INTEGER);
        CREATE TABLE [transaction] (uid TEXT PRIMARY KEY, type TEXT, amountInDefaultCurrency INTEGER, date TEXT, comment TEXT, isRemoved INTEGER);
        CREATE TABLE sync_link (entityType TEXT, entityUid TEXT, otherType TEXT, otherUid TEXT, isRemoved INTEGER);
        INSERT INTO category VALUES ('expense-category', 'Food', 'Expense', 'food', 1, 25000, 0);
        INSERT INTO category VALUES ('income-category', 'Salary', 'Income', 'money', 2, 50000, 0);
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
