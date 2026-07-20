using Centesimo.Domain;

namespace Centesimo.Domain.Tests;

public sealed class Tag_should_expected_behavior
{
    [Fact]
    public void Belong_to_one_category_and_archive()
    {
        var categoryId = Guid.NewGuid();
        var tag = new Tag(Guid.NewGuid(), categoryId, " Supermarket ");

        tag.Archive();

        Assert.Equal(categoryId, tag.CategoryId);
        Assert.Equal("Supermarket", tag.Name);
        Assert.True(tag.IsArchived);
    }
}
