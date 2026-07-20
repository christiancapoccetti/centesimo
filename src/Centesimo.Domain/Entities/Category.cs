namespace Centesimo.Domain;

public sealed class Category
{
    public Guid CategoryId { get; }
    public string Name { get; private set; }
    public string Icon { get; private set; }
    public string Color { get; private set; }
    public Money? MonthlyBudget { get; private set; }
    public bool IsArchived { get; private set; }

    public Category(Guid categoryId, string name, string icon, string color, Money? monthlyBudget = null)
    {
        if (categoryId == Guid.Empty)
            throw new ArgumentException("Category ID is required.", nameof(categoryId));

        CategoryId = categoryId;
        Name = RequireText(name, nameof(name));
        Icon = RequireText(icon, nameof(icon));
        Color = RequireText(color, nameof(color));
        MonthlyBudget = monthlyBudget;
    }

    public void UpdateDetails(string name, string icon, string color, Money? monthlyBudget)
    {
        Name = RequireText(name, nameof(name));
        Icon = RequireText(icon, nameof(icon));
        Color = RequireText(color, nameof(color));
        MonthlyBudget = monthlyBudget;
    }
    public void SetBudget(Money? monthlyBudget) => MonthlyBudget = monthlyBudget;
    public void Archive() => IsArchived = true;
    public void Restore() => IsArchived = false;

    private static string RequireText(string value, string parameterName)
    {
        if (value.IsEmpty())
            throw new ArgumentException("A value is required.", parameterName);

        return value.Trim();
    }
}
