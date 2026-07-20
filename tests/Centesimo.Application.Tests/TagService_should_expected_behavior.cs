using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class TagService_should_expected_behavior
{
    [Fact]
    public async Task Should_return_only_active_tags_ordered_by_name()
    {
        var categoryId = Guid.NewGuid();
        var categories = new FakeCategoryRepository();
        var tags = new FakeTagRepository();
        var archived = new Tag(Guid.NewGuid(), categoryId, "Archived");
        archived.Archive();
        tags.Items.AddRange([
            new Tag(Guid.NewGuid(), categoryId, "Work"),
            archived,
            new Tag(Guid.NewGuid(), categoryId, "Family")
        ]);
        var service = new TagService(categories, tags);

        var result = await service.GetActive(categoryId);

        Assert.True(result.IsSuccess);
        Assert.Collection(
            result.Value,
            tag => Assert.Equal("Family", tag.Name),
            tag => Assert.Equal("Work", tag.Name));
    }

    [Fact]
    public async Task Should_create_and_archive_a_tag()
    {
        var category = new Category(Guid.NewGuid(), "Food", "cart", "#176B5B");
        var categories = new FakeCategoryRepository();
        categories.Items.Add(category);
        var tags = new FakeTagRepository();
        var service = new TagService(categories, tags);

        var created = await service.Create(category.CategoryId, "Lunch");
        var archived = await service.Archive(created.Value.TagId);

        Assert.True(created.IsSuccess);
        Assert.True(archived.IsSuccess);
        Assert.True(created.Value.IsArchived);
        Assert.Empty((await service.GetActive(category.CategoryId)).Value);
    }
}
