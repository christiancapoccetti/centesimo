namespace Centesimo.Domain;

public sealed class Tag
{
    public Guid TagId { get; }
    public Guid CategoryId { get; }
    public string Name { get; }
    public bool IsArchived { get; private set; }

    public Tag(Guid tagId, Guid categoryId, string name)
    {
        if (tagId == Guid.Empty || categoryId == Guid.Empty)
        {
            throw new ArgumentException("Tag and category IDs are required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tag name is required.", nameof(name));
        }

        TagId = tagId;
        CategoryId = categoryId;
        Name = name.Trim();
    }

    public void Archive() => IsArchived = true;
}
