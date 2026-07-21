using System.Collections;

namespace Centesimo.App.Controls;

public partial class CategoryBreakdownList : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(CategoryBreakdownList));

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public event EventHandler<CategoryBreakdownItemClickedEventArgs>? CategoryClicked;

    public CategoryBreakdownList() => InitializeComponent();

    private void OnCategoryClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: CategoryBreakdownItemViewModel category })
            CategoryClicked?.Invoke(this, new(category));
    }
}

public sealed record CategoryBreakdownItemViewModel(
    Guid CategoryId,
    string Name,
    string Icon,
    string Color,
    string Amount,
    string SecondaryLabel,
    string StatusLabel,
    double Progress,
    bool IsActionable)
{
    public string Description => $"{Name}, {Amount}, {SecondaryLabel}, {StatusLabel}.";
}

public sealed class CategoryBreakdownItemClickedEventArgs(CategoryBreakdownItemViewModel category) : EventArgs
{
    public CategoryBreakdownItemViewModel Category { get; } = category;
}
