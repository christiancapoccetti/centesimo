using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class ExpenseService_should_expected_behavior
{
    [Fact]
    public async Task Create_update_and_delete_with_optional_values()
    {
        var categories = new FakeCategoryRepository();
        var tags = new FakeTagRepository();
        var expenses = new FakeExpenseRepository();
        var category = new Category(Guid.NewGuid(), "Food", "cart", "#123456");
        var tag = new Tag(Guid.NewGuid(), category.CategoryId, "Market");
        categories.Items.Add(category);
        tags.Items.Add(tag);
        var service = new ExpenseService(categories, tags, expenses);
        var created = await service.Create(new(category.CategoryId, 100, new DateOnly(2026, 7, 1), tag.TagId, "Note", "photo.jpg"));

        var updated = await service.Update(created.Value.ExpenseId, new(category.CategoryId, 250, new DateOnly(2026, 7, 2)));
        var deleted = await service.Delete(created.Value.ExpenseId);

        Assert.True(updated.IsSuccess);
        Assert.True(deleted.IsSuccess);
        Assert.Empty(expenses.Items);
    }

    [Fact]
    public async Task Reject_archived_category_and_mismatched_or_archived_tag()
    {
        var categories = new FakeCategoryRepository();
        var tags = new FakeTagRepository();
        var expenses = new FakeExpenseRepository();
        var category = new Category(Guid.NewGuid(), "Food", "cart", "#123456");
        categories.Items.Add(category);
        var otherTag = new Tag(Guid.NewGuid(), Guid.NewGuid(), "Other");
        tags.Items.Add(otherTag);
        var service = new ExpenseService(categories, tags, expenses);

        var mismatch = await service.Create(new(category.CategoryId, 100, new DateOnly(2026, 7, 1), otherTag.TagId));
        otherTag.Archive();
        var archivedTag = await service.Create(new(category.CategoryId, 100, new DateOnly(2026, 7, 1), otherTag.TagId));
        category.Archive();
        var archivedCategory = await service.Create(new(category.CategoryId, 100, new DateOnly(2026, 7, 1)));

        Assert.Equal("Tag.CategoryMismatch", mismatch.Error.Code);
        Assert.Equal("Tag.Archived", archivedTag.Error.Code);
        Assert.Equal("Category.Archived", archivedCategory.Error.Code);
    }
}

