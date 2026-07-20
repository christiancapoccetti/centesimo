using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class CategoryService_should_expected_behavior
{
    [Fact]
    public async Task Create_set_budget_and_archive()
    {
        var repository = new FakeCategoryRepository();
        var service = new CategoryService(repository);
        var created = await service.Create("Food", "cart", "#123456");

        await service.SetBudget(created.Value.CategoryId, new Money(20_000));
        await service.Archive(created.Value.CategoryId);

        Assert.Equal(20_000, created.Value.MonthlyBudget?.Cents);
        Assert.True(created.Value.IsArchived);
    }

    [Fact]
    public async Task Propagate_infrastructure_failure_instead_of_not_found()
    {
        var repository = new FakeCategoryRepository
        {
            Failure = InfrastructureErrors.Unexpected
        };
        var service = new CategoryService(repository);

        var result = await service.Archive(Guid.NewGuid());

        Assert.Equal("Infrastructure.Unexpected", result.Error.Code);
    }
    [Fact]
    public async Task Return_only_active_categories_and_update_details()
    {
        var repository = new FakeCategoryRepository();
        var active = new Category(Guid.NewGuid(), "Food", "cart", "#176B5B");
        var archived = new Category(Guid.NewGuid(), "Old", "more", "#6A4F88");
        archived.Archive();
        repository.Items.AddRange([active, archived]);
        var service = new CategoryService(repository);

        var updated = await service.Update(active.CategoryId, "Dining", "home", "#8B4A5D", new Money(5000));
        var categories = await service.GetActive();

        Assert.True(updated.IsSuccess);
        Assert.Single(categories.Value);
        Assert.Equal("Dining", categories.Value[0].Name);
    }}


