using System.Collections;

namespace Centesimo.App.Controls;
public partial class CategorySelector : ContentView
{
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IEnumerable), typeof(CategorySelector));
    public IEnumerable? Items { get => (IEnumerable?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public event EventHandler<object>? CategoryTapped;
    public CategorySelector() => InitializeComponent();
    private void OnTapped(object? sender, TappedEventArgs e) { if (sender is BindableObject { BindingContext: object item }) CategoryTapped?.Invoke(this, item); }
}
