using Centesimo.Application;

namespace Centesimo.Application.Tests;

public sealed class MoneyManagerImportService_should_expected_behavior
{
    [Fact]
    public async Task Preview_reports_the_parsed_records_and_import_reuses_the_same_data()
    {
        var data = new MoneyManagerImportData(
            [new MoneyManagerCategory("category-1", "Food", "cart", "#176B5B")],
            [new MoneyManagerTag("tag-1", "category-1", "Restaurant")],
            [
                new MoneyManagerExpense("expense-1", "category-1", "tag-1", 1234,
                    new DateOnly(2026, 7, 20), "Lunch"),
                new MoneyManagerExpense("expense-2", "category-1", null, 500,
                    new DateOnly(2026, 7, 21), "Coffee")
            ],
            3,
            1);
        var reader = new StubReader(data);
        var repository = new CapturingRepository();
        var service = new MoneyManagerImportService(reader, repository);

        var preview = await service.Preview(new MemoryStream([1]));
        Assert.Null(repository.ImportedData);
        var imported = await service.Import(preview.Value);

        Assert.True(preview.IsSuccess);
        Assert.Equal(1, preview.Value.CategoriesCount);
        Assert.Equal(1, preview.Value.TagsCount);
        Assert.Equal(2, preview.Value.ExpensesCount);
        Assert.Same(data, repository.ImportedData);
        Assert.True(imported.IsSuccess);
    }

    private sealed class StubReader(MoneyManagerImportData data) : IMoneyManagerBackupReader
    {
        public Task<Result<MoneyManagerImportData>> Read(Stream backup,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<MoneyManagerImportData>.Success(data));
    }

    private sealed class CapturingRepository : IMoneyManagerImportRepository
    {
        public MoneyManagerImportData? ImportedData { get; private set; }

        public Task<Result<MoneyManagerPersisted>> Preview(MoneyManagerImportData data,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<MoneyManagerPersisted>.Success(new MoneyManagerPersisted(1, 1, 2)));

        public Task<Result<MoneyManagerPersisted>> Import(MoneyManagerImportData data,
            CancellationToken cancellationToken = default)
        {
            ImportedData = data;
            return Task.FromResult(Result<MoneyManagerPersisted>.Success(new MoneyManagerPersisted(1, 1, 2)));
        }
    }
}
