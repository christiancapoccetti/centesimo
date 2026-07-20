using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class CategoryNameValidation_should_expected_behavior
{
    [Fact]
    public async Task Should_reject_duplicate_name_ignoring_case_and_whitespace()
    {
        var repository = new FakeCategoryRepository();
        repository.Items.Add(new Category(Guid.NewGuid(), "Food", "cart", "#176B5B"));
        var service = new CategoryService(repository);

        var result = await service.Create(" food ", "home", "#8B4A5D");

        Assert.True(result.IsFailure);
        Assert.Equal("Category.NameAlreadyExists", result.Error.Code);
        Assert.Single(repository.Items);
    }

    [Fact]
    public async Task Should_allow_unchanged_name_and_reject_another_category_name_on_update()
    {
        var repository = new FakeCategoryRepository();
        var food = new Category(Guid.NewGuid(), "Food", "cart", "#176B5B");
        var travel = new Category(Guid.NewGuid(), "Travel", "car", "#4F5F7A");
        repository.Items.AddRange([food, travel]);
        var service = new CategoryService(repository);

        var unchanged = await service.Update(food.CategoryId, "food", "home", "#8B4A5D", null);
        var duplicate = await service.Update(food.CategoryId, "Travel", "home", "#8B4A5D", null);

        Assert.True(unchanged.IsSuccess);
        Assert.True(duplicate.IsFailure);
        Assert.Equal("Category.NameAlreadyExists", duplicate.Error.Code);
    }

    [Fact]
    public async Task Should_propagate_repository_failure_during_duplicate_check()
    {
        var repository = new FakeCategoryRepository { Failure = InfrastructureErrors.Unexpected };
        var service = new CategoryService(repository);

        var result = await service.Create("Food", "cart", "#176B5B");

        Assert.True(result.IsFailure);
        Assert.Equal("Infrastructure.Unexpected", result.Error.Code);
    }
}
